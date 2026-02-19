using DG.Tweening;
using MyToolz.EditorToolz;
using MyToolz.Utilities.Debug;
using UnityEngine;

namespace MyToolz.Tweener.UI.Tweens
{
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
                DebugUtility.LogError(this, "MoveTweenStrategy requires RectTransform.");
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
                    DebugUtility.LogError(this, "MoveTweenStrategy Transform references are null.");
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
                    DebugUtility.LogError(this, "MoveTweenStrategy expects RectTransforms for UI tweening.");
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
}
