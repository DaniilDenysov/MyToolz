using DG.Tweening;
using MyToolz.Utilities.Debug;
using UnityEngine;

namespace MyToolz.Tweener.UI.Tweens
{
    [System.Serializable]
    public class OffsetTweenStrategy : TweenStrategy
    {
        [SerializeField] private RectTransform rectTransform;
        [SerializeField] private Vector2 fromOffsetMin = Vector2.zero;
        [SerializeField] private Vector2 fromOffsetMax = Vector2.zero;
        [SerializeField] private Vector2 toOffsetMin = Vector2.zero;
        [SerializeField] private Vector2 toOffsetMax = Vector2.zero;
        [SerializeField] private float duration = 0.5f;
        [SerializeField] private Ease ease = Ease.OutCubic;

        public override Tween GetTween()
        {
            if (rectTransform == null)
            {
                LogError("OffsetTweenStrategy requires RectTransform.");
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
}