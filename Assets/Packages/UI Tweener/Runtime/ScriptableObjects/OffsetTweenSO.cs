using DG.Tweening;
using UnityEngine;

namespace MyToolz.Tweener.UI
{
    [CreateAssetMenu(fileName = "OffsetTweenSO", menuName = "MyToolz/UITweener/OffsetTweenSO")]
    public class OffsetTweenSO : ScriptableObject
    {
        [SerializeField] private Vector2 fromOffsetMin = Vector2.zero;
        [SerializeField] private Vector2 fromOffsetMax = Vector2.zero;
        [SerializeField] private Vector2 toOffsetMin = Vector2.zero;
        [SerializeField] private Vector2 toOffsetMax = Vector2.zero;
        [SerializeField] private float duration = 0.5f;
        [SerializeField] private Ease ease = Ease.OutCubic;

        public Vector2 FromOffsetMin => fromOffsetMin;
        public Vector2 FromOffsetMax => fromOffsetMax;
        public Vector2 ToOffsetMin => toOffsetMin;
        public Vector2 ToOffsetMax => toOffsetMax;
        public float Duration => duration;
        public Ease Ease => ease;
    }
}
