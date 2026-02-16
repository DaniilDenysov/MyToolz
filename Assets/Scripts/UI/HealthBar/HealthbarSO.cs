using DG.Tweening;
using UnityEngine;

namespace MyToolz.UI.Healthbar
{
    [CreateAssetMenu(fileName = "HealthbarSO", menuName = "MyToolz/UI/HealthbarSO")]
    public class HealthbarSO : ScriptableObject
    {
        [Tooltip("If true, uses unscaled time (UI typically should ignore timeScale).")]
        [SerializeField] private bool ignoreTimeScale = true;

        [Header("Foreground")]
        [SerializeField, Min(0f)] private float foregroundDuration = 0.15f;
        [SerializeField] private Ease foregroundEase = Ease.OutCubic;

        [Header("Delayed Decreasing (damage chip)")]
        [SerializeField, Min(0f)] private float decDelay = 0.10f;
        [SerializeField, Min(0f)] private float decDuration = 0.25f;
        [SerializeField] private Ease decEase = Ease.OutCubic;

        [Header("Delayed Increasing (heal chip)")]
        [SerializeField, Min(0f)] private float incDelay = 0.05f;
        [SerializeField, Min(0f)] private float incDuration = 0.20f;
        [SerializeField] private Ease incEase = Ease.OutCubic;

        public bool IgnoreTimeScale => ignoreTimeScale;
        public float ForegroundDuration => foregroundDuration;
        public Ease ForegroundEase => foregroundEase;
        public float DecDelay => decDelay;
        public float DecDuration => decDuration;
        public Ease DecEase => decEase;
        public float IncDelay => incDelay;
        public float IncDuration => incDuration;
        public Ease IncEase => incEase;
    }
}
