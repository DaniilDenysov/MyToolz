using UnityEngine;

namespace MyToolz.ScriptableObjects.GameSettings
{
    [CreateAssetMenu(fileName = "FloatSettingSO", menuName = "MyToolz/GameSettings/FloatSettingSO")]
    public class FloatSettingSO : SettingSOGeneric<double>
    {
        [SerializeField] protected double minValue;
        [SerializeField] protected double maxValue;

        public double MinValue => minValue;
        public double MaxValue => maxValue;

        protected override bool IsValueValid(double value)
        {
            return value >= minValue && value <= maxValue;
        }

        protected override bool IsCurrentValueValid()
        {
            return IsValueValid(currentValue);
        }
    }
}
