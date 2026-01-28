using System.Collections.Generic;
using UnityEngine;

namespace MyToolz.ScriptableObjects.GameSettings
{
    public abstract class MultipleOptionSettingSO<Value, Options> : SettingSOGeneric<Value> where Options : List<Value>
    {
        [SerializeField] protected Options options;

        public int CurrentValueIndex => options.IndexOf(currentValue);
        public int DefaultValueIndex => options.IndexOf(defaultValue);

        public IReadOnlyList<Value> AvailableOptions => options.AsReadOnly();

        public virtual string ValueToString(Value value) => value.ToString();

        protected override bool IsCurrentValueValid()
        {
            return base.IsCurrentValueValid() && options.Contains(currentValue);
        }
    }
}