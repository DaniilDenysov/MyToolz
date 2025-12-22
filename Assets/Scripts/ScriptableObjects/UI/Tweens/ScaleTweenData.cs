using DG.Tweening;
using UnityEngine;

namespace MyToolz.ScriptableObjects.UI.Tweens
{
    [CreateAssetMenu(fileName = "ScaleTweenData", menuName = "Data/UITweener/ScaleTweenData")]
    public class ScaleTweenData : ScriptableObject
    {
        [SerializeField]
        private Vector3 fromScale = Vector3.one;

        public Vector3 FromScale
        {
            get => fromScale;
        }

        [SerializeField]
        private Vector3 toScale = Vector3.one;

        public Vector3 ToScale
        {
            get => toScale;
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