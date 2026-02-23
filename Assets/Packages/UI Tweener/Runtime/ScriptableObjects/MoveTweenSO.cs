using DG.Tweening;
using MyToolz.EditorToolz;
using UnityEngine;

namespace MyToolz.Tweener.UI
{
    [CreateAssetMenu(fileName = "MoveTweenSO", menuName = "MyToolz/UITweener/MoveTweenSO")]
    public class MoveTweenSO : ScriptableObject
    {
        [SerializeField] 
        private bool useConcrete;

        [SerializeField, HideIf("@!useConcrete")]
        private Vector2 vecFrom;
        [SerializeField, HideIf("@!useConcrete")]
        private Vector2 vecTo;

        [SerializeField]
        private float duration = 1f;

        [SerializeField]
        private Ease ease = Ease.Linear;

        public bool UseConcrete => useConcrete;
        public Vector2 VecFrom => vecFrom;
        public Vector2 VecTo => vecTo;
        public float Duration => duration;
        public Ease Ease => ease;
    }
}