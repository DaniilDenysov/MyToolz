using System;

namespace MyToolz.Player.FPS.LoadoutSystem.View
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class DisplayAsAttribute : Attribute
    {
        public string DisplayName { get; }
        public float Min;
        public float Max;

        public DisplayAsAttribute(string displayName, float min, float max)
        {
            DisplayName = displayName;
            Min = min;
            Max = max;
        }
    }
}

