using DG.Tweening;
using MyToolz.ScriptableObjects.UI.Tweens;
using MyToolz.Utilities.Debug;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MyToolz.Tweener.UI.Tweens
{
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
                LogError("ScaleTweenStrategy requires a Transform.");
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
}
