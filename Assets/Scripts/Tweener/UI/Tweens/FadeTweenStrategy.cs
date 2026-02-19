using DG.Tweening;
using MyToolz.EditorToolz;
using MyToolz.ScriptableObjects.UI.Tweens;
using MyToolz.Utilities.Debug;
using UnityEngine;

namespace MyToolz.Tweener.UI.Tweens
{
    [System.Serializable]
    public class FadeTweenStrategy : TweenStrategy
    {
        [SerializeField, Required] protected CanvasGroup canvasGroup;

        [SerializeField, Required] protected FadeTweenSO data;

        public override Tween GetTween()
        {
            if (canvasGroup == null)
            {
                DebugUtility.LogError(this, "FadeTweenStrategy requires a CanvasGroup.");
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
