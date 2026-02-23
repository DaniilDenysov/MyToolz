using DG.Tweening;
using MyToolz.EditorToolz;
using UnityEngine;
using static MyToolz.Tweener.UI.Tweens.PulsateTweenStrategy;

namespace MyToolz.Tweener.UI
{
    [CreateAssetMenu(fileName = "PulsateTweenSO", menuName = "MyToolz/UITweener/PulsateTweenSO")]
    public class PulsateTweenSO : ScriptableObject
    {
        [SerializeField] private PulsateMode mode = PulsateMode.Scale;
        [SerializeField] private float duration = 0.5f;
        [SerializeField] private int loops = -1; // -1 = infinite
        [SerializeField] private Ease ease = Ease.InOutSine;
        [SerializeField, ShowIf("@mode==PulsateMode.Scale")] private Vector3 fromScale = Vector3.one;
        [SerializeField, ShowIf("@mode==PulsateMode.Scale")] private Vector3 toScale = Vector3.one * 1.2f;
        [SerializeField, ShowIf("@mode==PulsateMode.SizeDelta")] private Vector2 fromPercentage = Vector2.one;
        [SerializeField, ShowIf("@mode==PulsateMode.SizeDelta")] private Vector2 toPercentage = new Vector2(1.2f, 1.2f);
        [SerializeField, ShowIf("@mode==PulsateMode.Fade")] private float fromAlpha = 1f;
        [SerializeField, ShowIf("@mode==PulsateMode.Fade")] private float toAlpha = 0.5f;

        public PulsateMode Mode => mode;
        public float Duration => duration;
        public int Loops => loops;
        public Ease Ease => ease;
        public Vector3 FromScale => fromScale;
        public Vector3 ToScale => toScale;
        public Vector2 FromPercentage => fromPercentage;
        public Vector2 ToPercentage => toPercentage;
        public float FromAlpha => fromAlpha;
        public float ToAlpha => toAlpha;
    }
}