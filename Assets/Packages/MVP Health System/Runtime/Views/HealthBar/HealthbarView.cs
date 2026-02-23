using DG.Tweening;
using MyToolz.Utilities.Debug;
using UnityEngine;
using UnityEngine.UI;

namespace MyToolz.UI.Healthbar
{
    public class HealthbarBarsView : MonoBehaviour
    {
        [SerializeField] private Image foreground;
        [SerializeField] private Image delayedBarDecreasing;
        [SerializeField] private Image delayedBarIncreasing;
        [SerializeField] private HealthbarSO healthbarSO;

        private bool ignoreTimeScale => healthbarSO.IgnoreTimeScale;
        private float foregroundDuration => healthbarSO.ForegroundDuration;
        private Ease foregroundEase => healthbarSO.ForegroundEase;
        private float decDelay => healthbarSO.DecDelay;
        private float decDuration => healthbarSO.DecDuration;
        private Ease decEase => healthbarSO.DecEase;
        private float incDelay => healthbarSO.IncDelay;
        private float incDuration => healthbarSO.IncDuration;
        private Ease incEase => healthbarSO.IncEase;

        private Tween fgTween;
        private Tween decSeq;
        private Tween incSeq;

        private void Awake()
        {
            if (foreground == null)
                DebugUtility.LogError(this, $"{nameof(HealthbarBarsView)}: Foreground Image is not assigned.");

#if UNITY_EDITOR
            Test();
#endif
        }

        private void OnDestroy()
        {
            KillTween(ref fgTween);
            KillTween(ref decSeq);
            KillTween(ref incSeq);
        }

#if UNITY_EDITOR
        [ContextMenu("Test Random")]
        private void Test()
        {
            UpdateView(Random.Range(0f, 100f), 0f, 100f);
        }
#endif

        public void UpdateView01(float normalized)
        {
            normalized = Mathf.Clamp01(SafeFloat(normalized, 0f));

            if (foreground == null)
                return;

            float fgNow = foreground.fillAmount;
            bool isHealing = normalized > fgNow;
            bool isDamaging = normalized < fgNow;

            KillTween(ref fgTween);

            if (delayedBarDecreasing != null)
            {
                KillTween(ref decSeq);

                if (isDamaging)
                {
                    AnimateFill(foreground, normalized, foregroundDuration, foregroundEase, ref fgTween);
                    AnimateFill(delayedBarDecreasing, normalized, decDuration, decEase, ref decSeq);
                    DOTween.Sequence().AppendInterval(Mathf.Max(0f, decDelay)).Append(decSeq);
                }
                else
                {
                    SetFillSafe(delayedBarDecreasing, normalized);
                }
            }

            if (delayedBarIncreasing != null)
            {
                KillTween(ref incSeq);

                if (isHealing)
                {
                    SetFillSafe(delayedBarIncreasing, normalized);
                    //AnimateFill(delayedBarIncreasing, normalized, incDuration, incEase, ref incSeq);
                    AnimateFill(foreground, normalized, incDuration, incEase, ref fgTween);
                    DOTween.Sequence().AppendInterval(Mathf.Max(0f, incDelay)).Append(fgTween);
                }
                else
                {
                    SetFillSafe(delayedBarIncreasing, normalized);
                }
            }
        }

        public void UpdateView(float currentHealth, float minHealth, float maxHealth)
        {
            currentHealth = SafeFloat(currentHealth, 0f);
            minHealth = SafeFloat(minHealth, 0f);
            maxHealth = SafeFloat(maxHealth, 1f);

            if (maxHealth < minHealth)
                (minHealth, maxHealth) = (maxHealth, minHealth);

            float range = maxHealth - minHealth;

            float normalized;
            if (range <= 0.000001f)
            {
                normalized = currentHealth >= maxHealth ? 1f : 0f;
            }
            else
            {
                float clamped = Mathf.Clamp(currentHealth, minHealth, maxHealth);
                normalized = (clamped - minHealth) / range;
            }

            UpdateView01(normalized);
        }

        private void AnimateFill(Image img, float target, float duration, Ease ease, ref Tween tweenField)
        {
            if (img == null) return;

            if (duration <= 0f)
            {
                SetFillSafe(img, target);
                return;
            }

            tweenField = img
                .DOFillAmount(target, duration)
                .SetEase(ease)
                .SetUpdate(ignoreTimeScale)
                .SetTarget(img)
                .OnKill(() => img.fillAmount = target)
                .OnComplete(() => img.fillAmount = target);
        }


        private void SetFillSafe(Image img, float normalized01)
        {
            if (img == null) return;
            normalized01 = Mathf.Clamp01(SafeFloat(normalized01, 0f));
            img.fillAmount = normalized01;
        }

        private float SafeFloat(float value, float fallback)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
                return fallback;
            return value;
        }

        private void KillTween(ref Tween t)
        {
            if (t == null) return;
            if (t.IsActive()) t.Kill();
            t = null;
        }
    }
}
