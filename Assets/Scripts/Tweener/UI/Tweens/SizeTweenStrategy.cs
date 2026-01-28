using DG.Tweening;
using MyToolz.Utilities.Debug;
using UnityEngine;

namespace MyToolz.Tweener.UI.Tweens
{
    [System.Serializable]
    public class SizeTweenStrategy : TweenStrategy
    {
        [SerializeField] private RectTransform rectTransform;
        [SerializeField] private Vector2 fromSize = Vector2.one * 100;
        [SerializeField] private Vector2 toSize = Vector2.one * 200;
        [SerializeField] private float duration = 0.5f;
        [SerializeField] private Ease ease = Ease.Linear;

        public override Tween GetTween()
        {
            if (rectTransform == null)
            {
                LogError("SizeTweenStrategy requires a RectTransform.");
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
}