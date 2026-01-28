using DG.Tweening;
using UnityEngine;

namespace MyToolz.Tweener.UI.Tweens
{
    [System.Serializable]
    public class JoinUITweenerStrategy : TweenStrategy
    {
        [SerializeField] protected UITweener tweener;

        public override Tween GetTween()
        {
            return tweener.CreateSequence(trigger);
        }
    }
}
