using MyToolz.Clock.Interfaces;
using MyToolz.UI.Management;
using System;
using TMPro;
using UnityEngine;

namespace MyToolz.Clock.View
{
    [Serializable]
    public abstract class TimeFormatStrategy
    {
        public abstract string Format(float seconds);

        protected static float ClampNonNegative(float v) => v < 0f ? 0f : v;

        protected static int FloorToInt(float v) => Mathf.FloorToInt(v);
        protected static int CeilToInt(float v) => Mathf.CeilToInt(v);

        protected static void SplitToHMS(int totalSeconds, out int hours, out int minutes, out int secs)
        {
            hours = totalSeconds / 3600;
            minutes = (totalSeconds % 3600) / 60;
            secs = totalSeconds % 60;
        }
    }

    [Serializable]
    public class MinutesSecondsFormat : TimeFormatStrategy
    {
        public override string Format(float seconds)
        {
            seconds = ClampNonNegative(seconds);

            int m = FloorToInt(seconds / 60f);
            int s = FloorToInt(seconds % 60f);

            return $"{m}:{s:00}";
        }
    }

    [Serializable]
    public class MinutesSecondsMillisecondsFormat : TimeFormatStrategy
    {
        public override string Format(float seconds)
        {
            seconds = ClampNonNegative(seconds);

            int m = FloorToInt(seconds / 60f);
            int s = FloorToInt(seconds % 60f);
            int ms = FloorToInt((seconds * 1000f) % 1000f);

            return $"{m}:{s:00}.{ms:000}";
        }
    }

    [Serializable]
    public class HoursMinutesSecondsFormat : TimeFormatStrategy
    {
        public override string Format(float seconds)
        {
            seconds = ClampNonNegative(seconds);

            int total = FloorToInt(seconds);
            SplitToHMS(total, out int h, out int m, out int s);

            return h > 0 ? $"{h}:{m:00}:{s:00}" : $"{m}:{s:00}";
        }
    }

    [Serializable]
    public class AdaptiveTimeFormat : TimeFormatStrategy
    {
        public override string Format(float seconds)
        {
            seconds = ClampNonNegative(seconds);

            if (seconds < 60f)
            {
                return $"{CeilToInt(seconds)}s";
            }

            int total = FloorToInt(seconds);
            SplitToHMS(total, out int h, out int m, out int s);

            if (h > 0) return $"{h}:{m:00}:{s:00}";
            return $"{m}:{s:00}";
        }
    }

    public class ClockView : IClockView
    {
        [SerializeReference, SubclassSelector] private TimeFormatStrategy timeFormatStrategy = new HoursMinutesSecondsFormat();
        [SerializeField] private UIScreenBase screen;
        [SerializeField] private TMP_Text display;

        public void Destroy(float model)
        {

        }

        public void Hide()
        {
            screen?.Close();
        }

        public void Initialize(float model)
        {
            
        }

        public void Show()
        {
            screen?.Open();
        }

        public void UpdateView(float model)
        {
            display?.SetText($"{timeFormatStrategy?.Format(model) ?? "Error"}");
        }
    }
}
