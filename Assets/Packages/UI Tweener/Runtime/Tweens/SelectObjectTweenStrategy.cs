using DG.Tweening;
using MyToolz.Tweener.UI;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MyToolz.Tweener.UI.Tweens
{
    [System.Serializable]
    public class SelectObjectTweenStrategy : TweenStrategy
    {
        [SerializeField] private GameObject selected;

        public override Tween GetTween()
        {
            return DOTween.Sequence().OnComplete(() =>
            {
                var eventSystem = EventSystem.current;
                eventSystem.SetSelectedGameObject(selected, new BaseEventData(eventSystem));
            });
        }
    }
}
