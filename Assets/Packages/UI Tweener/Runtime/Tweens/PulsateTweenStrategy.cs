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

        [SerializeField, ShowIf("@mode==PulsateMode.Scale")] private RectTransform targetTransform;
        [SerializeField, ShowIf("@mode==PulsateMode.SizeDelta")] private RectTransform targetRect;
        [SerializeField, ShowIf("@mode==PulsateMode.Fade")] private CanvasGroup canvasGroup;

        [SerializeField] private PulsateTweenSO data;

        public override Tween GetTween()
        {
            Tween tween = null;

            switch (data.Mode)
            {
                case PulsateMode.Scale:
                    if (targetTransform == null)
                    {
                        DebugUtility.LogError(this, "PulsateTweenStrategy: RectTransform is null.");
                        return null;
                    }

                    targetTransform.localScale = data.FromScale;

                    tween = targetTransform
                        .DOScale(data.ToScale, data.Duration)
                        .SetEase(data.Ease)
                        .SetLoops(data.Loops, LoopType.Yoyo);
                    break;

                case PulsateMode.SizeDelta:
                    if (targetRect == null)
                    {
                        DebugUtility.LogError(this, "PulsateTweenStrategy: RectTransform is null.");
                        return null;
                    }

                    var baseSize = targetRect.sizeDelta;

                    var fromSize = new Vector2(
                        baseSize.x * data.FromPercentage.x,
                        baseSize.y * data.FromPercentage.y
                    );

                    var toSize = new Vector2(
                        baseSize.x * data.ToPercentage.x,
                        baseSize.y * data.ToPercentage.y
                    );

                    targetRect.sizeDelta = fromSize;

                    tween = targetRect
                        .DOSizeDelta(toSize, data.Duration)
                        .SetEase(data.Ease)
                        .SetLoops(data.Loops, LoopType.Yoyo);
                    break;

                case PulsateMode.Fade:
                    if (canvasGroup == null)
                    {
                        DebugUtility.LogError(this, "PulsateTweenStrategy: CanvasGroup is null.");
                        return null;
                    }

                    canvasGroup.alpha = data.FromAlpha;

                    tween = canvasGroup
                        .DOFade(data.ToAlpha, data.Duration)
                        .SetEase(data.Ease)
                        .SetLoops(data.Loops, LoopType.Yoyo);
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