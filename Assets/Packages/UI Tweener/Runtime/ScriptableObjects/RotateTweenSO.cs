using DG.Tweening;
using UnityEngine;

namespace MyToolz.Tweener.UI
{
    [CreateAssetMenu(fileName = "RotateTweenSO", menuName = "MyToolz/UITweener/RotateTweenSO")]
    public class RotateTweenSO : ScriptableObject
    {
        [SerializeField]
        private Vector3 fromAngle = Vector3.zero;

        public Vector3 FromAngle
        {
            get => fromAngle;
        }

        [SerializeField]
        private Vector3 toAngle = Vector3.zero;

        public Vector3 ToAngle
        {
            get => toAngle;
        }

        [SerializeField]
        private float duration = 0.5f;

        public float Duration
        {
            get => duration;
        }

        [SerializeField]
        private Ease ease = Ease.Linear;

        public Ease Ease
        {
            get => ease;
        }
    }
}
