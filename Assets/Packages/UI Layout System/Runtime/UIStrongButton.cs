using DG.Tweening;
using MyToolz.Tweener.UI;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MyToolz.UI.Layout
{
    /// <summary>
    /// Drop-in <see cref="Button"/> subclass with a strong onClick reference. Unity's onClick fails
    /// SILENTLY when the bound script is deleted (missing target), the method is renamed/removed
    /// (dangling name), or nothing was ever bound - the click just does nothing. This button makes
    /// every one of those a loud, clickable error until resolved:
    ///
    /// - Edit time: the inspector (UIStrongButtonEditor) shows the broken listeners in a red box;
    ///   scene validation and the play-mode-enter audit report them (see UIStrongButtonAudit).
    /// - Runtime: one frame after Start (so presenters get a chance to bind in code) the button is
    ///   audited; failures log errors every time the scene runs and, optionally, force the button
    ///   non-interactable so breakage is impossible to miss.
    ///
    /// Being a Button subclass, it works everywhere a Button does (presenter fields typed Button,
    /// Selectable navigation, UI raycasts). Code bindings: the audit cannot see plain
    /// <c>onClick.AddListener</c> calls, so presenters should bind through <see cref="Bind"/> (or
    /// turn off Require Binding for intentionally event-less buttons). An empty event at edit time
    /// is only a warning - it becomes an error at runtime when nothing has arrived.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("MyToolz/UI Layout/Strong Button")]
    public class UIStrongButton : Button
    {
        [Tooltip("Treat a button with no working binding as broken. Disable only for buttons that are intentionally event-less.")]
        [SerializeField] private bool requireBinding = true;

        [Tooltip("While broken, force the button non-interactable so the breakage is visible in play, not just in the console.")]
        [SerializeField] private bool disableWhenBroken = true;

        private int runtimeBindings;
        private bool disabledByAudit;

        private UITweener tweener;
        private bool tweenerResolved;

        /// <summary>Kept for callers written against the wrapper API; the strong button IS the button.</summary>
        public Button Button => this;

        private UITweener Tweener
        {
            get
            {
                if (!tweenerResolved)
                {
                    tweener = GetComponent<UITweener>();
                    tweenerResolved = true;
                }

                return tweener;
            }
        }

        // ---------------------------------------------------------------------- code binding API ----

        /// <summary>
        /// Presenter-friendly binding: adds the listener AND registers it with the strong reference,
        /// so the runtime audit knows a code binding exists. Re-audits immediately (restores
        /// interactability if the button was disabled as broken).
        /// </summary>
        public void Bind(UnityAction action, string description = null)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            onClick.AddListener(action);
            runtimeBindings++;
            if (Application.isPlaying) ReportRuntime();
        }

        /// <summary>Removes a listener previously added with <see cref="Bind"/>.</summary>
        public void Unbind(UnityAction action)
        {
            if (action == null) return;
            onClick.RemoveListener(action);
            runtimeBindings = Mathf.Max(0, runtimeBindings - 1);
        }

        // --------------------------------------------------------------------------- lifecycle ----

        protected override void Start()
        {
            base.Start();
            if (Application.isPlaying) AuditAfterBindWindow().Forget();
        }

        private async UniTaskVoid AuditAfterBindWindow()
        {
            // One frame of grace: presenters typically bind in Awake/OnEnable/Start.
            await UniTask.Yield(PlayerLoopTiming.Update);
            if (this != null) ReportRuntime();
        }

        // --------------------------------------------------------------------- click queueing ----

        /// <summary>
        /// When a <see cref="UITweener"/> with an OnClick animation is present the click event is
        /// queued on top of that animation - it fires only once the animation has finished, instead
        /// of instantly. Buttons with no click animation fire immediately, as usual.
        /// </summary>
        public override void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            Click();
        }

        public override void OnSubmit(BaseEventData eventData)
        {
            Click();
        }

        private void Click()
        {
            if (!IsActive() || !IsInteractable())
                return;

            Tween clickTween = Tweener != null ? Tweener.CreateSequence(ActivationTrigger.OnClick) : null;
            if (clickTween == null)
            {
                onClick.Invoke();
                return;
            }

            InvokeAfter(clickTween.Duration(), this.GetCancellationTokenOnDestroy()).Forget();
        }

        private async UniTaskVoid InvokeAfter(float seconds, CancellationToken token)
        {
            if (seconds > 0f)
                await UniTask.Delay(TimeSpan.FromSeconds(seconds), DelayType.UnscaledDeltaTime, PlayerLoopTiming.Update, token);

            if (this != null)
                onClick.Invoke();
        }

        /// <summary>Runs the audit, logs errors (clickable, with hierarchy path) and applies disableWhenBroken.</summary>
        public void ReportRuntime()
        {
            var errors = new List<string>();
            var warnings = new List<string>();
            Audit(errors, warnings, editTime: false);

            if (errors.Count > 0)
            {
                foreach (var error in errors)
                    Debug.LogError($"[UIStrongButton] {HierarchyPath()}: {error}", this);
                if (disableWhenBroken && interactable)
                {
                    interactable = false;
                    disabledByAudit = true;
                }
            }
            else if (disabledByAudit)
            {
                interactable = true;
                disabledByAudit = false;
            }
        }

        // ------------------------------------------------------------------------------- audit ----

        /// <summary>
        /// Inspects onClick and reports broken listeners. At edit time an empty event is a warning
        /// (it may be bound from code at runtime); at runtime it is an error.
        /// </summary>
        public void Audit(List<string> errors, List<string> warnings, bool editTime)
        {
            int persistentCount = onClick.GetPersistentEventCount();
            int workingListeners = 0;

            for (int i = 0; i < persistentCount; i++)
            {
                var target = onClick.GetPersistentTarget(i);
                string method = onClick.GetPersistentMethodName(i);

                if (target == null)
                {
                    errors.Add($"onClick listener #{i}: target is missing - the script or object it was bound to was deleted.");
                    continue;
                }
                if (string.IsNullOrEmpty(method))
                {
                    errors.Add($"onClick listener #{i} on '{target.name}': no method selected.");
                    continue;
                }
                if (!HasPublicMethod(target, method))
                {
                    errors.Add($"onClick listener #{i}: {target.GetType().Name}.{method}() no longer exists - " +
                                "the method was renamed or removed, so the event is silently empty.");
                    continue;
                }
                workingListeners++;
            }

            if (requireBinding && workingListeners == 0 && runtimeBindings == 0)
            {
                if (persistentCount > 0)
                {
                    // Broken listeners already reported above; nothing extra to add.
                }
                else if (editTime)
                {
                    warnings.Add("onClick has no binding. If a presenter binds it from code, use " +
                                 "UIStrongButton.Bind() - otherwise this becomes a runtime error.");
                }
                else
                {
                    errors.Add("onClick is empty - nothing bound in the inspector and nothing arrived via " +
                               "UIStrongButton.Bind(). Bind it, or disable Require Binding if this is intentional.");
                }
            }
        }

        /// <summary>
        /// UnityEvents can only bind public methods (and property setters, which are public methods
        /// named set_X), so a public-method name check exactly detects rename/removal without
        /// false-positives on signatures.
        /// </summary>
        private static bool HasPublicMethod(UnityEngine.Object target, string methodName)
        {
            var methods = target.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
            foreach (var method in methods)
                if (method.Name == methodName) return true;
            return false;
        }

        private string HierarchyPath()
        {
            var path = name;
            for (var t = transform.parent; t != null; t = t.parent)
                path = $"{t.name}/{path}";
            return path;
        }
    }
}
