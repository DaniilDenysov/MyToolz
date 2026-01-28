using UnityEngine;

namespace MyToolz.ScriptableObjects.GameSettings
{
    [CreateAssetMenu(fileName = "StringSettingSO", menuName = "MyToolz/GameSettings/StringSettingSO")]
    public class StringSettingSO : SettingSOGeneric<string>
    {
        protected override bool IsValueValid(string value)
        {
            return !(string.IsNullOrEmpty(value) || string.IsNullOrWhiteSpace(value));
        }

        protected override bool IsCurrentValueValid()
        {
            return IsValueValid(currentValue);
        }
    }
}
