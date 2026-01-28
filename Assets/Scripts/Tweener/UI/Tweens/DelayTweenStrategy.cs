using DG.Tweening;
using UnityEngine;

namespace MyToolz.Tweener.UI.Tweens
{
    [System.Serializable]
    public class DelayTweenStrategy : TweenStrategy
    {
        [SerializeField, Range(0f, 1000f)] private float delay = 1f;

        public override Tween GetTween()
        {
            return DOTween.Sequence().SetDelay(delay);
        }
    }
}
