using DG.Tweening;
using MyToolz.EditorToolz;
using MyToolz.Utilities.Debug;
using UnityEngine;

namespace MyToolz.Tweener.UI.Tweens
{
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

        [SerializeField, ShowIf("@mode==PulsateMode.Scale")] private RectTransform targetTransform;
        [SerializeField, ShowIf("@mode==PulsateMode.SizeDelta")] private RectTransform targetRect;
        [SerializeField, ShowIf("@mode==PulsateMode.Fade")] private CanvasGroup canvasGroup;
        [SerializeField] private float duration = 0.5f;
        [SerializeField] private int loops = -1; // -1 = infinite
        [SerializeField] private Ease ease = Ease.InOutSine;
        [SerializeField, ShowIf("@mode==PulsateMode.Scale")] private Vector3 fromScale = Vector3.one;
        [SerializeField, ShowIf("@mode==PulsateMode.Scale")] private Vector3 toScale = Vector3.one * 1.2f;
        [SerializeField, ShowIf("@mode==PulsateMode.SizeDelta")] private Vector2 fromPercentage = Vector2.one;
        [SerializeField, ShowIf("@mode==PulsateMode.SizeDelta")] private Vector2 toPercentage = new Vector2(1.2f, 1.2f);
        [SerializeField, ShowIf("@mode==PulsateMode.Fade")] private float fromAlpha = 1f;
        [SerializeField, ShowIf("@mode==PulsateMode.Fade")] private float toAlpha = 0.5f;

        public override Tween GetTween()
        {
            Tween tween = null;

            switch (mode)
            {
                case PulsateMode.Scale:
                    if (targetTransform == null)
                    {
                        DebugUtility.LogError(this, "PulsateTweenStrategy: RectTransform is null.");
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
                        DebugUtility.LogError(this, "PulsateTweenStrategy: RectTransform is null.");
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
                        DebugUtility.LogError(this, "PulsateTweenStrategy: CanvasGroup is null.");
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
}