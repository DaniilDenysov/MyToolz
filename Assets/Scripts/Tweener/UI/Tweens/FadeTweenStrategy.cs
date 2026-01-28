using DG.Tweening;
using MyToolz.ScriptableObjects.UI.Tweens;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MyToolz.Tweener.UI.Tweens
{
    [System.Serializable]
    public class FadeTweenStrategy : TweenStrategy
    {
        [SerializeField, Required] protected CanvasGroup canvasGroup;

        [SerializeField, Required] protected FadeTweenData data;

        public override Tween GetTween()
        {
            if (canvasGroup == null)
            {
                LogError("FadeTweenStrategy requires a CanvasGroup.");
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
}
