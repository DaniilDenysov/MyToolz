using UnityEngine;

namespace MyToolz.ScriptableObjects.GameSettings
{
    [CreateAssetMenu(fileName = "IntSettingSO", menuName = "MyToolz/GameSettings/IntSettingSO")]
    public class IntSettingSO : SettingSOGeneric<int>
    {
        [SerializeField] protected int minValue;
        [SerializeField] protected int maxValue;

        public int MinValue => minValue;
        public int MaxValue => maxValue;

        protected override bool IsValueValid(int value)
        {
            return value >= minValue && value <= maxValue;
        }

        protected override bool IsCurrentValueValid()
        {
            return IsValueValid(currentValue);
        }
    }
}
