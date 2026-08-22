using DG.Tweening;
using MyToolz.EditorToolz;
using MyToolz.Utilities.Debug;
using UnityEngine;

namespace MyToolz.Tweener.UI.Tweens
{
    [System.Serializable]
    public class RotateTweenStrategy : TweenStrategy
    {
        [SerializeField, Required] private RotateTweenSO data;

        [SerializeField, Required]
        private RectTransform targetTransform;

        public override Tween GetTween()
        {
            if (targetTransform == null)
            {
                DebugUtility.LogError(this, "RotateTweenStrategy requires a RectTransform.");
                return null;
            }

            Vector3 resFrom;
            Vector3 resTo;

            if (inverse)
            {
                resFrom = data.ToAngle;
                resTo = data.FromAngle;
            }
            else
            {
                resFrom = data.FromAngle;
                resTo = data.ToAngle;
            }

            targetTransform.localEulerAngles = resFrom;

            var tween = targetTransform
                .DOLocalRotate(resTo, data.Duration)
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
}
