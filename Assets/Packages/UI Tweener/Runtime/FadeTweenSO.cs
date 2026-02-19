using DG.Tweening;
using UnityEngine;

namespace MyToolz.ScriptableObjects.UI.Tweens
{
    [CreateAssetMenu(fileName = "FadeTweenSO", menuName = "Data/UITweener/FadeTweenSO")]
    public class FadeTweenSO : ScriptableObject
    {
        [SerializeField,Range(0f,1f)] protected float fromAlpha = 1f;
        public float FromAlpha
        {
            get => fromAlpha;
        }
        [SerializeField, Range(0f, 1f)] protected float toAlpha = 0f;
        public float ToAlpha
        {
            get => toAlpha;
        }
        [SerializeField, Range(0.0001f, 1000f)] protected float duration = 1f;
        public float Duration
        {
            get => duration;
        }
        [SerializeField] protected Ease ease = Ease.Linear;
        public Ease Ease
        {
            get => ease;
        }
    }
}