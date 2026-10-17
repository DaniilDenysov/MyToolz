using DG.Tweening;
using MyToolz.Extensions;
using MyToolz.Utilities.Debug;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MyToolz.Tweener.UI
{
    /// <summary>
    /// Marks a component on the same GameObject that plays the <see cref="ActivationTrigger.OnClick"/>
    /// sequence itself (e.g. UIStrongButton, which also defers its onClick until the animation ends).
    /// UITweener then ignores the pointer click, so the click animation is not started twice.
    /// </summary>
    public interface IUITweenClickOwner { }

    [System.Serializable]
    public abstract class TweenStrategy : AbstractTweenStrategy
    {
        public ActivationTrigger Trigger
        {
            get => trigger;
        }
        [SerializeField] protected ActivationTrigger trigger = ActivationTrigger.Enable;
        [SerializeField] protected bool inverseIfReached = false;
        protected bool inverse = false;
    }

    public enum ActivationTrigger
    {
        Manual,
        Awake,
        Start,
        Enable,
        Disable,
        OnClick,
        OnEnter,
        OnExit
    }

    public class UITweener : Tweener<TweenStrategy>, IPointerClickHandler, IPointerExitHandler, IPointerEnterHandler
    {
        [SerializeField] private CanvasGroup canvasGroup;

        [SerializeField]
        //If enabled, the UI will block interactions during transitions
        private bool blockInteractionDuringTween = true;

        private Dictionary<ActivationTrigger, List<TweenStrategy>> strategies = new Dictionary<ActivationTrigger, List<TweenStrategy>>();

        private ActivationTrigger[] activationTriggers
        {
            get => strategies.Keys.ToArray();
        }

        private bool HasTrigger(ActivationTrigger activationTrigger) =>
            activationTriggers.Contains(activationTrigger);

        public static Dictionary<ActivationTrigger, List<TweenStrategy>> CreateStrategyMapping(TweenStrategy[] tweenStrategies)
        {
            Dictionary<ActivationTrigger, List<TweenStrategy>> strategies = new Dictionary<ActivationTrigger, List<TweenStrategy>>();
            foreach (var strategy in tweenStrategies)
            {
                if (strategies.TryGetValue(strategy.Trigger, out var list))
                {
                    list.Add(strategy);
                }
                else
                {
                    list = new List<TweenStrategy>();
                    list.Add(strategy);
                    strategies.TryAdd(strategy.Trigger, list);
                }
            }
            return strategies;
        }

        private void Awake()
        {
            strategies = CreateStrategyMapping(tweenStrategies);
            CreateSequence(ActivationTrigger.Awake).Play();
        }

        private void Start()
        {
            CreateSequence(ActivationTrigger.Start).Play();
        }

        private void OnEnable()
        {
            CreateSequence(ActivationTrigger.Enable).Play();
        }

        // A disabled UI element must not keep animating (and blocking its canvas group) in the background.
        private void OnDisable()
        {
            CancelSequence();
        }

        private bool clickOwnerResolved;
        private bool hasClickOwner;

        private bool HasClickOwner
        {
            get
            {
                if (!clickOwnerResolved)
                {
                    hasClickOwner = GetComponent<IUITweenClickOwner>() != null;
                    clickOwnerResolved = true;
                }

                return hasClickOwner;
            }
        }

        private void SetInteractable(bool state)
        {
            if (!blockInteractionDuringTween) return;
            if (canvasGroup == null) return;
            canvasGroup.interactable = state;
            canvasGroup.blocksRaycasts = state;
        }


        public void SetActive(bool state)
        {
            if (state)
            {
                bool alreadyActive = gameObject.activeInHierarchy;
                gameObject.SetActive(true);

                // Activating an inactive object invokes OnEnable, which already starts the
                // enable sequence. Only start it here when the object was already active.
                if (alreadyActive && HasTrigger(ActivationTrigger.Enable))
                    CreateSequence(ActivationTrigger.Enable).Play();
            }
            else
            {
                if (HasTrigger(ActivationTrigger.Disable))
                {
                    var seq = CreateSequence(ActivationTrigger.Disable);
                    if (seq != null)
                    {
                        seq.OnComplete(() => gameObject.SetActive(false)).Play();
                    }
                    else
                    {
                        gameObject.SetActive(false);
                    }
                }
                else
                {
                    DebugUtility.Log(this, "No Disable tween");
                    gameObject.SetActive(false);
                }
            }
        }

        public override Tween CreateSequence(List<TweenStrategy> strategiesList)
        {
            if (strategiesList == null || strategiesList.Count == 0)
                return null;

            if (paralelExecution)
            {
                Sequence sequence = DOTween.Sequence();
                foreach (var strategy in strategiesList)
                {
                    var tween = strategy?.GetTween();
                    if (tween != null)
                    {
                        sequence.Join(tween);
                    }
                }
                sequence.OnComplete(() =>
                {
                    SetInteractable(true);
                })
                .OnKill(() =>
                {
                    SetInteractable(true);
                });
                ApplyTimeMode(sequence);
                Track(sequence);
                return sequence;
            }
            else
            {
                Sequence sequence = DOTween.Sequence();

                foreach (var strategy in strategiesList)
                {
                    var tween = strategy.GetTween();
                    if (tween != null)
                    {
                        sequence.Append(tween);
                    }
                }
                sequence.OnComplete(() =>
                {
                    SetInteractable(true);
                })
                .OnKill(() =>
                {
                    SetInteractable(true);
                });
                ApplyTimeMode(sequence);
                Track(sequence);
                return sequence;
            }
        }

        public Tween CreateSequence(ActivationTrigger trigger)
        {
            if (strategies.TryGetValue(trigger, out var strategiesList))
            {
                CancelSequence();
                SetInteractable(false);
                return CreateSequence(strategiesList);
            }
            return null;
        }


        public void OnPointerEnter(PointerEventData eventData)
        {
            CreateSequence(ActivationTrigger.OnEnter).Play();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            CreateSequence(ActivationTrigger.OnExit).Play();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (HasClickOwner)
                return;

            CreateSequence(ActivationTrigger.OnClick).Play();
        }
    }
}
