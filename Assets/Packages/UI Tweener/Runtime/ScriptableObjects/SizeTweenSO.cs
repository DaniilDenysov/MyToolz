using DG.Tweening;
using UnityEngine;

namespace MyToolz.Tweener.UI
{
    [CreateAssetMenu(fileName = "SizeTweenSO", menuName = "MyToolz/UITweener/SizeTweenSO")]
    public class SizeTweenSO : ScriptableObject
    {
        [SerializeField] private Vector2 fromSize = Vector2.one * 100;
        [SerializeField] private Vector2 toSize = Vector2.one * 200;
        [SerializeField] private float duration = 0.5f;
        [SerializeField] private Ease ease = Ease.Linear;

        public Vector2 FromSize => fromSize;
        public Vector2 ToSize => toSize;
        public float Duration => duration;
        public Ease Ease => ease;
    }
}