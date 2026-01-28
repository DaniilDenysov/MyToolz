using DG.Tweening;
using MyToolz.Utilities.Debug;
using UnityEngine;

namespace MyToolz.Tweener.UI.Tweens
{
    [System.Serializable]
    public class PulsateFadeTweenStrategy : FadeTweenStrategy
    {
        [SerializeField]
        private int loops = -1;

        [SerializeField]
        private LoopType loopType = LoopType.Yoyo;

        public override Tween GetTween()
        {
            var baseTween = base.GetTween();

            if (baseTween == null)
            {
                LogWarning("PulsateFadeTweenStrategy: base fade tween returned null.");
                return null;
            }

            baseTween.SetLoops(loops, loopType);

            return baseTween;
        }
    }
}