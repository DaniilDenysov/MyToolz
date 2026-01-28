using UnityEngine;

namespace MyToolz.ScriptableObjects.GameSettings
{
    [CreateAssetMenu(fileName = "BoolSettingSO", menuName = "MyToolz/GameSettings/BoolSettingSO")]
    public class BoolSettingSO : SettingSOGeneric<bool>
    {
        protected override bool IsValueValid(bool value)
        {
            return true;
        }

        protected override bool IsCurrentValueValid()
        {
            return IsValueValid(currentValue);
        }
    }
}
