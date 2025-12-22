using DG.Tweening;
using MyToolz.ScriptableObjects.UI.Tweens;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using MyToolz.Utilities.Debug;

namespace MyToolz.Tweener.UI
{
    [System.Serializable]
    public class LoopTweenStrategy : MergeTweenStrategy
    {
        [SerializeField]
        private int loops = -1;

        [SerializeField]
        private LoopType loopType = LoopType.Restart;

        public override Tween GetTween()
        {
            var baseTween = base.GetTween();

            if (baseTween == null)
            {
                DebugUtility.Log("LoopTweenStrategy: Underlying MergeTweenStrategy returned null.");
                return null;
            }

            baseTween.SetLoops(loops, loopType);

            return baseTween;
        }
    }

    [System.Serializable]
    public class PulsateTweenStrategy : TweenStrategy
    {
        public enum PulsateMode
        {
            Scale,
            SizeDelta,
            Fade
        }

        [SerializeField]
        private PulsateMode mode = PulsateMode.Scale;

        [SerializeField, ShowIf("mode", PulsateMode.Scale)]
        private RectTransform targetTransform;

        [SerializeField, ShowIf("mode", PulsateMode.SizeDelta)]
        private RectTransform targetRect;

        [SerializeField, ShowIf("mode", PulsateMode.Fade)]
        private CanvasGroup canvasGroup;

        [SerializeField]
        private float duration = 0.5f;

        [SerializeField]
        private int loops = -1; // -1 = infinite

        [SerializeField]
        private Ease ease = Ease.InOutSine;

        [SerializeField, ShowIf("mode", PulsateMode.Scale)]
        private Vector3 fromScale = Vector3.one;

        [SerializeField, ShowIf("mode", PulsateMode.Scale)]
        private Vector3 toScale = Vector3.one * 1.2f;

        [SerializeField, ShowIf("mode", PulsateMode.SizeDelta)]
        private Vector2 fromPercentage = Vector2.one;

        [SerializeField, ShowIf("mode", PulsateMode.SizeDelta)]
        private Vector2 toPercentage = new Vector2(1.2f, 1.2f);

        [SerializeField, ShowIf("mode", PulsateMode.Fade)]
        private float fromAlpha = 1f;

        [SerializeField, ShowIf("mode", PulsateMode.Fade)]
        private float toAlpha = 0.5f;

        public override Tween GetTween()
        {
            Tween tween = null;

            switch (mode)
            {
                case PulsateMode.Scale:
                    if (targetTransform == null)
                    {
                        DebugUtility.LogError("PulsateTweenStrategy: RectTransform is null.");
                        return null;
                    }

                    targetTransform.localScale = fromScale;

                    tween = targetTransform
                        .DOScale(toScale, duration)
                        .SetEase(ease)
                        .SetLoops(loops, LoopType.Yoyo);
                    break;

                case PulsateMode.SizeDelta:
                    if (targetRect == null)
                    {
                        DebugUtility.LogError("PulsateTweenStrategy: RectTransform is null.");
                        return null;
                    }

                    var baseSize = targetRect.sizeDelta;

                    var fromSize = new Vector2(
                        baseSize.x * fromPercentage.x,
                        baseSize.y * fromPercentage.y
                    );

                    var toSize = new Vector2(
                        baseSize.x * toPercentage.x,
                        baseSize.y * toPercentage.y
                    );

                    targetRect.sizeDelta = fromSize;

                    tween = targetRect
                        .DOSizeDelta(toSize, duration)
                        .SetEase(ease)
                        .SetLoops(loops, LoopType.Yoyo);
                    break;

                case PulsateMode.Fade:
                    if (canvasGroup == null)
                    {
                        DebugUtility.LogError("PulsateTweenStrategy: CanvasGroup is null.");
                        return null;
                    }

                    canvasGroup.alpha = fromAlpha;

                    tween = canvasGroup
                        .DOFade(toAlpha, duration)
                        .SetEase(ease)
                        .SetLoops(loops, LoopType.Yoyo);
                    break;
            }

            if (inverseIfReached && tween != null)
            {
                tween.OnComplete(() =>
                {
                    inverse = !inverse;
                });
            }

            return tween;
        }
    }

    [System.Serializable]
    public class MergeTweenStrategy : TweenStrategy
    {
        [InfoBox("Only parent trigger is taken into account!")]
        [SerializeField]
        private bool parallel = true;

        [SerializeReference]
        private TweenStrategy [] innerStrategies;

        public override Tween GetTween()
        {
            if (innerStrategies == null || innerStrategies.Length == 0)
            {
                DebugUtility.LogWarning("MergeTweenStrategy: No inner strategies assigned.");
                return null;
            }

            if (parallel)
            {
                var sequence = DOTween.Sequence();

                foreach (var strategy in innerStrategies)
                {
                    var tween = strategy.GetTween();
                    if (tween != null)
                    {
                        sequence.Join(tween);
                    }
                }

                return sequence;
            }
            else
            {
                var sequence = DOTween.Sequence();

                foreach (var strategy in innerStrategies)
                {
                    var tween = strategy.GetTween();
                    if (tween != null)
                    {
                        sequence.Append(tween);
                    }
                }

                return sequence;
            }
        }
    }


    [System.Serializable]
    public class SizeTweenStrategy : TweenStrategy
    {
        [SerializeField]
        private RectTransform rectTransform;

        [SerializeField]
        private Vector2 fromSize = Vector2.one * 100;

        [SerializeField]
        private Vector2 toSize = Vector2.one * 200;

        [SerializeField]
        private float duration = 0.5f;

        [SerializeField]
        private Ease ease = Ease.Linear;

        public override Tween GetTween()
        {
            if (rectTransform == null)
            {
                DebugUtility.LogError("SizeTweenStrategy requires a RectTransform.");
                return null;
            }

            Vector2 resFrom;
            Vector2 resTo;

            if (inverse)
            {
                resFrom = toSize;
                resTo = fromSize;
            }
            else
            {
                resFrom = fromSize;
                resTo = toSize;
            }

            rectTransform.sizeDelta = resFrom;

            var tween = rectTransform
                .DOSizeDelta(resTo, duration)
                .SetEase(ease);

            if (inverseIfReached)
            {
                tween.OnComplete(() =>
                {
                    inverse = !inverse;
                });
            }

            return tween;
        }
    }

    [System.Serializable]
    public class ScaleTweenStrategy : TweenStrategy
    {
        [SerializeField, Required] private ScaleTweenData data;

        [SerializeField, Required]
        private RectTransform targetTransform;

        public override Tween GetTween()
        {
            if (targetTransform == null)
            {
                DebugUtility.LogError("ScaleTweenStrategy requires a Transform.");
                return null;
            }

            Vector3 resFrom;
            Vector3 resTo;

            if (inverse)
            {
                resFrom = data.ToScale;
                resTo = data.FromScale;
            }
            else
            {
                resFrom = data.FromScale;
                resTo = data.ToScale;
            }

            targetTransform.localScale = resFrom;

            var tween = targetTransform
                .DOScale(resTo, data.Duration)
                .SetEase(data.Ease);

            if (inverseIfReached)
            {
                tween.OnComplete(() =>
                {
                    inverse = !inverse;
                });
            }

            return tween;
        }
    }

    [System.Serializable]
    public class OffsetTweenStrategy : TweenStrategy
    {
        [SerializeField] private RectTransform rectTransform;

        [SerializeField]
        private Vector2 fromOffsetMin = Vector2.zero;

        [SerializeField]
        private Vector2 fromOffsetMax = Vector2.zero;

        [SerializeField]
        private Vector2 toOffsetMin = Vector2.zero;

        [SerializeField]
        private Vector2 toOffsetMax = Vector2.zero;

        [SerializeField]
        private float duration = 0.5f;

        [SerializeField]
        private Ease ease = Ease.OutCubic;

        public override Tween GetTween()
        {
            if (rectTransform == null)
            {
                DebugUtility.LogError("OffsetTweenStrategy requires RectTransform.");
                return null;
            }

            Vector2 resFromOffsetMin;
            Vector2 resFromOffsetMax;
            Vector2 resToOffsetMin;
            Vector2 resToOffsetMax;

            if (inverse)
            {
                resFromOffsetMin = toOffsetMin;
                resToOffsetMin = fromOffsetMin;
                resFromOffsetMax = toOffsetMax;
                resToOffsetMax = fromOffsetMax;
            }
            else
            {
                resFromOffsetMin = fromOffsetMin;
                resToOffsetMin = toOffsetMin;
                resFromOffsetMax = fromOffsetMax;
                resToOffsetMax = toOffsetMax;
            }

            rectTransform.offsetMin = resFromOffsetMin;
            rectTransform.offsetMax = resFromOffsetMax;

            var tweenMin = DOTween.To(
                () => rectTransform.offsetMin,
                v => rectTransform.offsetMin = v,
                resToOffsetMin,
                duration
            ).SetEase(ease);

            var tweenMax = DOTween.To(
                () => rectTransform.offsetMax,
                v => rectTransform.offsetMax = v,
                resToOffsetMax,
                duration
            ).SetEase(ease);

            var sequence = DOTween.Sequence();
            sequence.Join(tweenMin);
            sequence.Join(tweenMax);

            if (inverseIfReached)
            {
                sequence.OnComplete(() => inverse = !inverse);
            }

            return sequence;
        }
    }

    [System.Serializable]
    public class DelayTweenStrategy : TweenStrategy
    {
        [SerializeField, Range(0f, 1000f)] private float delay = 1f;

        public override Tween GetTween()
        {
            return DOTween.Sequence().SetDelay(delay);
        }
    }

    [System.Serializable]
    public class OnCompleteCallbackTweenStrategy : TweenStrategy
    {
        [SerializeField] private UnityEvent onCompleteCallback;

        public override Tween GetTween()
        {
            var dummyTween = DOVirtual.DelayedCall(0f, () =>
            {
                onCompleteCallback?.Invoke();
            });

            return dummyTween;
        }
    }
    [System.Serializable]
    public class MoveTweenStrategy : TweenStrategy
    {
        [SerializeField] private RectTransform rectTransform;

        [SerializeField] private bool useConcrete;

        [SerializeField, HideIf("useConcrete")]
        private RectTransform from;
        [SerializeField, HideIf("useConcrete")]
        private RectTransform to;

        [SerializeField, HideIf("@!useConcrete")]
        private Vector2 vecFrom;
        [SerializeField, HideIf("@!useConcrete")]
        private Vector2 vecTo;

        [SerializeField]
        private float duration = 1f;

        [SerializeField]
        private Ease ease = Ease.Linear;

        public override Tween GetTween()
        {
            if (rectTransform == null)
            {
                DebugUtility.LogError("MoveTweenStrategy requires RectTransform.");
                return null;
            }

            Vector2 resFrom;
            Vector2 resTo;

            if (useConcrete)
            {
                resFrom = inverse ? vecTo : vecFrom;
                resTo = inverse ? vecFrom : vecTo;
            }
            else
            {
                if (from == null || to == null)
                {
                    DebugUtility.LogError("MoveTweenStrategy Transform references are null.");
                    return null;
                }

                var fromRect = from as RectTransform;
                var toRect = to as RectTransform;

                if (fromRect != null && toRect != null)
                {
                    resFrom = inverse ? toRect.anchoredPosition : fromRect.anchoredPosition;
                    resTo = inverse ? fromRect.anchoredPosition : toRect.anchoredPosition;
                }
                else
                {
                    DebugUtility.LogError("MoveTweenStrategy expects RectTransforms for UI tweening.");
                    return null;
                }
            }


            rectTransform.anchoredPosition = resFrom;
            var tween = rectTransform.DOAnchorPos(resTo, duration).SetEase(ease);

            if (inverseIfReached)
            {
                tween.OnComplete(() =>
                {
                    inverse = !inverse;
                });
            }

            return tween;
        }
    }

    [System.Serializable]
    public class SpriteSwapTweenStrategy : TweenStrategy
    {
        [SerializeField, Required] private Sprite newSprite;
        [SerializeField, Required] private Image spriteRenderer;
        [SerializeField] private bool fade;
        [SerializeField, ShowIf("@fade")] private float fadeDuration = 0.2f;

        public override Tween GetTween()
        {
            if (spriteRenderer == null || newSprite == null)
            {
                DebugUtility.LogError("SpriteSwapTweenStrategy: SpriteRenderer or NewSprite is null.");
                return null;
            }

            Sequence sequence = DOTween.Sequence();
            if (fade)
            {
                sequence.Append(spriteRenderer.DOFade(0f, fadeDuration))
                        .AppendCallback(() =>
                        {
                            spriteRenderer.sprite = newSprite;
                        })
                        .Append(spriteRenderer.DOFade(1f, fadeDuration));
            }
            else
            {
                sequence.AppendCallback(() => {
                  spriteRenderer.sprite = newSprite;
                });
            }
            return sequence;
        }
    }

    [System.Serializable]
    public class PulsateFadeTweenStrategy : FadeTweenStrategy
    {
        [SerializeField]
        private int loops = -1;

        [SerializeField]
        private LoopType loopType = LoopType.Yoyo;

        public override Tween GetTween()
        {
            var baseTween = base.GetTween();

            if (baseTween == null)
            {
                DebugUtility.LogWarning("PulsateFadeTweenStrategy: base fade tween returned null.");
                return null;
            }

            baseTween.SetLoops(loops, loopType);

            return baseTween;
        }
    }

    [System.Serializable]
    public class FadeTweenStrategy : TweenStrategy
    {
        [SerializeField, Required] protected CanvasGroup canvasGroup;

        [SerializeField, Required] protected FadeTweenData data;

        public override Tween GetTween()
        {
            if (canvasGroup == null)
            {
                DebugUtility.LogError("FadeTweenStrategy requires a CanvasGroup.");
                return null;
            }

            float resFrom;
            float resTo;

            if (inverse)
            {
                resFrom = data.ToAlpha;
                resTo = data.FromAlpha;
            }
            else
            {
                resFrom = data.FromAlpha;
                resTo = data.ToAlpha;
            }

            canvasGroup.alpha = resFrom;

            var tween = canvasGroup
                .DOFade(resTo, data.Duration)
                .From(resFrom)
                .SetEase(data.Ease);

            if (inverseIfReached)
            {
                tween.OnComplete(() =>
                {
                    inverse = !inverse;
                });
            }

            return tween;
        }

    }


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
        [InfoBox("If enabled, the UI will block interactions during transitions.")]
        private bool blockInteractionDuringTween = true;

        private Dictionary<ActivationTrigger, List<TweenStrategy>> strategies = new Dictionary<ActivationTrigger, List<TweenStrategy>>();

    
        private bool disabling = false;

        private bool originalInteractable;
        private bool originalBlocksRaycasts;

        private ActivationTrigger[] activationTriggers
        {
            get => strategies.Keys.ToArray();
        }

        private bool interactable
        {
            get
            {
                if (canvasGroup == null) return true;
                else return canvasGroup.interactable;
            }
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
            ExecuteSequence(ActivationTrigger.Awake);
        }

        private void Start()
        {
            ExecuteSequence(ActivationTrigger.Start);
        }

        private void OnEnable()
        {
            ExecuteSequence(ActivationTrigger.Enable);
        }

        private void CacheOriginalInteractable()
        {
            if (canvasGroup == null) return;

            originalInteractable = canvasGroup.interactable;
            originalBlocksRaycasts = canvasGroup.blocksRaycasts;
        }

        private void SetInteractable(bool state)
        {
            if (canvasGroup == null) return;

            if (state)
            {
                canvasGroup.interactable = originalInteractable;
                canvasGroup.blocksRaycasts = originalBlocksRaycasts;
            }
            else
            {
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }
        }

        public void SetActive(bool state)
        {
            if (gameObject.activeInHierarchy == state) return;
            
            if (state)
            {
                gameObject.SetActive(true);
                //if (HasTrigger(ActivationTrigger.Enable))
                //{
                //    ExecuteSequence(ActivationTrigger.Enable);
                //}
            }
            else
            {
                if (HasTrigger(ActivationTrigger.Disable))
                {
                    if (disabling) return;
                    disabling = true;
                    ExecuteSequence(ActivationTrigger.Disable);
                }
                else
                {
                    gameObject.SetActive(false);
                }
            }
        }

        private void CheckDisableAfterTween(int total, int completed)
        {
            if (disabling && completed >= total)
            {
                gameObject.SetActive(false);
                disabling = false;
            }
        }

        public override void ExecuteSequence(List<TweenStrategy> strategiesList)
        {
            if (paralelExecution)
            {
                int tweensToComplete = 0;
                int tweensCompleted = 0;

                foreach (var strategy in strategiesList)
                {
                    var tween = strategy.GetTween();
                    if (tween != null)
                    {
                        tweensToComplete++;
                        runningTweens.Add(tween);

                        tween.OnComplete(() =>
                        {
                            tweensCompleted++;
                            if (tweensCompleted >= tweensToComplete)
                            {
                                CheckDisableAfterTween(tweensToComplete, tweensCompleted);
                            }
                        })
                        .OnKill(() =>
                        {
                            SetInteractable(true);
                        });

                        tween.Play();
                    }
                }

                if (tweensToComplete == 0)
                {
                    CheckDisableAfterTween(0, 0);
                    SetInteractable(true);
                }
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
                        runningTweens.Add(tween);
                    }
                }
                sequence.OnComplete(() =>
                {
                    SetInteractable(true);
                    CheckDisableAfterTween(1, 1);
                })
                .OnKill(() =>
                {
                    SetInteractable(true);
                });

                sequence.Play();
            }
        }

        public void ExecuteSequence(ActivationTrigger trigger)
        {
            if (!interactable) return;

            if (strategies.TryGetValue(trigger, out var strategiesList))
            {
                CancelSequence();

                if (blockInteractionDuringTween && strategiesList.Count > 0)
                {
                    CacheOriginalInteractable();
                    SetInteractable(false);
                }

                ExecuteSequence(strategiesList);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            ExecuteSequence(ActivationTrigger.OnEnter);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            ExecuteSequence(ActivationTrigger.OnExit);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            ExecuteSequence(ActivationTrigger.OnClick);
        }
    }
}