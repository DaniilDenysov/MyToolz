using UnityEngine;

namespace MyToolz.ScriptableObjects.GameSettings
{
    [CreateAssetMenu(fileName = "UintSettingSO", menuName = "MyToolz/GameSettings/UintSettingSO")]
    public class UintSettingSO : SettingSOGeneric<uint>
    {
        [SerializeField] protected uint minValue = uint.MinValue;
        [SerializeField] protected uint maxValue = uint.MaxValue;

        public uint MinValue => minValue;
        public uint MaxValue => maxValue;

        protected override bool IsValueValid(uint value)
        {
            return value >= minValue && value <= maxValue;
        }

        protected override bool IsCurrentValueValid()
        {
            return IsValueValid(currentValue);
        }
    }
}
