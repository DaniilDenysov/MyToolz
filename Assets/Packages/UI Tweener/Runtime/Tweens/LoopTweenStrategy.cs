using DG.Tweening;
using MyToolz.Utilities.Debug;
using UnityEngine;

namespace MyToolz.Tweener.UI.Tweens
{
    [System.Serializable]
    public class LoopTweenStrategy : MergeTweenStrategy
    {
        [SerializeField] private int loops = -1;
        [SerializeField] private LoopType loopType = LoopType.Restart;

        public override Tween GetTween()
        {
            var baseTween = base.GetTween();

            if (baseTween == null)
            {
                DebugUtility.LogWarning(this, "LoopTweenStrategy: Underlying MergeTweenStrategy returned null.");
                return null;
            }

            baseTween.SetLoops(loops, loopType);

            return baseTween;
        }
    }
}
