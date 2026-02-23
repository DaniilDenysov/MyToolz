using DG.Tweening;
using MyToolz.Utilities.Debug;
using UnityEngine;

namespace MyToolz.Tweener.UI.Tweens
{
    [System.Serializable]
    public class OffsetTweenStrategy : TweenStrategy
    {
        [SerializeField] private RectTransform rectTransform;
        [SerializeField] private OffsetTweenSO data;

        public override Tween GetTween()
        {
            if (rectTransform == null)
            {
                DebugUtility.LogError(this, "OffsetTweenStrategy requires RectTransform.");
                return null;
            }

            Vector2 resFromOffsetMin;
            Vector2 resFromOffsetMax;
            Vector2 resToOffsetMin;
            Vector2 resToOffsetMax;

            if (inverse)
            {
                resFromOffsetMin = data.ToOffsetMin;
                resToOffsetMin = data.FromOffsetMin;
                resFromOffsetMax = data.ToOffsetMax;
                resToOffsetMax = data.FromOffsetMax;
            }
            else
            {
                resFromOffsetMin = data.FromOffsetMin;
                resToOffsetMin = data.ToOffsetMin;
                resFromOffsetMax = data.FromOffsetMax;
                resToOffsetMax = data.ToOffsetMax;
            }

            rectTransform.offsetMin = resFromOffsetMin;
            rectTransform.offsetMax = resFromOffsetMax;

            var tweenMin = DOTween.To(
                () => rectTransform.offsetMin,
                v => rectTransform.offsetMin = v,
                resToOffsetMin,
                data.Duration
            ).SetEase(data.Ease);

            var tweenMax = DOTween.To(
                () => rectTransform.offsetMax,
                v => rectTransform.offsetMax = v,
                resToOffsetMax,
                data.Duration
            ).SetEase(data.Ease);

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