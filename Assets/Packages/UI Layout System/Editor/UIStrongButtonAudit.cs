#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace MyToolz.UI.Layout
{
    /// <summary>
    /// Editor-side enforcement for <see cref="UIStrongButton"/>. Three surfaces:
    /// 1. Automatic audit of the open scene(s) every time play mode is entered - broken bindings are
    ///    reported before the first click can silently do nothing.
    /// 2. Tools > MyToolz > UI Layout > Audit Buttons - the full report, including editor-only checks
    ///    the runtime cannot do: listeners switched to Off (disabled in the inspector, another silent
    ///    killer) and plain Buttons that lack the strong wrapper entirely.
    /// 3. Reused by UILayoutValidation.ValidateSceneUI so the Studio's "Validate Scene" covers buttons.
    /// </summary>
    [InitializeOnLoad]
    public static class UIStrongButtonAudit
    {
        static UIStrongButtonAudit()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.ExitingEditMode) return;
            var errors = new List<string>();
            var warnings = new List<string>();
            AuditStrongButtons(errors, warnings);
            foreach (var e in errors) Debug.LogError($"[UIStrongButton] {e}");
            // Warnings are skipped on play-enter: empty events may legitimately be bound by presenters.
        }

        [MenuItem("Tools/MyToolz/UI Layout/Audit Buttons")]
        public static void AuditButtonsMenu()
        {
            var errors = new List<string>();
            var warnings = new List<string>();
            int strongCount = AuditStrongButtons(errors, warnings);

            // Buttons without the wrapper get none of the protection - list them.
            int unwrapped = 0;
            foreach (var button in Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (button.GetComponent<UIStrongButton>() != null) continue;
                unwrapped++;
                warnings.Add($"'{HierarchyPath(button.transform)}' is a plain Button without UIStrongButton - its binding can break silently.");
            }

            foreach (var w in warnings) Debug.LogWarning($"[UIStrongButton] {w}");
            foreach (var e in errors) Debug.LogError($"[UIStrongButton] {e}");
            Debug.Log($"[UIStrongButton] Audit finished: {strongCount} strong button(s), {unwrapped} unwrapped, " +
                      $"{errors.Count} error(s), {warnings.Count} warning(s).");
        }

        /// <summary>Audits every UIStrongButton in the open scene(s). Returns the number audited.</summary>
        public static int AuditStrongButtons(List<string> errors, List<string> warnings)
        {
            var strongButtons = Object.FindObjectsByType<UIStrongButton>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var strong in strongButtons)
            {
                var localErrors = new List<string>();
                var localWarnings = new List<string>();
                strong.Audit(localErrors, localWarnings, editTime: true);
                AuditListenerStates(strong, localWarnings);

                string path = HierarchyPath(strong.transform);
                foreach (var e in localErrors) errors.Add($"'{path}': {e}");
                foreach (var w in localWarnings) warnings.Add($"'{path}': {w}");
            }
            return strongButtons.Length;
        }

        /// <summary>
        /// Editor-only check the runtime API cannot express: persistent listeners switched to Off in
        /// the inspector. Read via SerializedObject - no package/UnityEngine internals modified.
        /// </summary>
        private static void AuditListenerStates(UIStrongButton strong, List<string> warnings)
        {
            var button = strong.Button;
            if (button == null) return;

            var so = new SerializedObject(button);
            var calls = so.FindProperty("m_OnClick.m_PersistentCalls.m_Calls");
            if (calls == null || !calls.isArray) return;

            for (int i = 0; i < calls.arraySize; i++)
            {
                var state = calls.GetArrayElementAtIndex(i).FindPropertyRelative("m_CallState");
                if (state != null && state.enumValueIndex == 0) // UnityEventCallState.Off
                    warnings.Add($"onClick listener #{i} is switched Off in the inspector - it will never fire.");
            }
        }

        private static string HierarchyPath(Transform transform)
        {
            var path = transform.name;
            for (var t = transform.parent; t != null; t = t.parent)
                path = $"{t.name}/{path}";
            return path;
        }
    }
}
#endif
