using DG.Tweening;
using MyToolz.Utilities.Debug;
using UnityEngine;

namespace MyToolz.Tweener.UI.Tweens
{
    [System.Serializable]
    public class MoveTweenStrategy : TweenStrategy
    {
        [SerializeField] private RectTransform rectTransform;
        [SerializeField] private RectTransform from;
        [SerializeField] private RectTransform to;
        [SerializeField] private MoveTweenSO data;

        public override Tween GetTween()
        {
            if (rectTransform == null)
            {
                DebugUtility.LogError(this, "MoveTweenStrategy requires RectTransform.");
                return null;
            }

            Vector2 resFrom;
            Vector2 resTo;

            if (data.UseConcrete)
            {
                resFrom = inverse ? data.VecTo : data.VecFrom;
                resTo = inverse ? data.VecFrom : data.VecTo;
            }
            else
            {
                if (from == null || to == null)
                {
                    DebugUtility.LogError(this, "MoveTweenStrategy Transform references are null.");
                    return null;
                }

                var fromRect = from;
                var toRect = to;

                if (fromRect != null && toRect != null)
                {
                    resFrom = inverse ? toRect.anchoredPosition : fromRect.anchoredPosition;
                    resTo = inverse ? fromRect.anchoredPosition : toRect.anchoredPosition;
                }
                else
                {
                    DebugUtility.LogError(this, "MoveTweenStrategy expects RectTransforms for UI tweening.");
                    return null;
                }
            }


            rectTransform.anchoredPosition = resFrom;
            var tween = rectTransform.DOAnchorPos(resTo, data.Duration).SetEase(data.Ease);

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
