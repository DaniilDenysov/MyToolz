using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;

namespace MyToolz.Tweener.UI.Tweens
{
    [System.Serializable]
    public class OnCompleteCallbackTweenStrategy : TweenStrategy
    {
        [SerializeField] private UnityEvent onCompleteCallback;

        public override Tween GetTween()
        {
            var dummyTween = DOVirtual.DelayedCall(0f, () =>
            {
                onCompleteCallback?.Invoke();
            });

            return dummyTween;
        }
    }
}
