using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace MyToolz.GameSettings.Data
{
    [Serializable]
    public class SettingEntry
    {
        [JsonProperty("id")]
        public string Id;

        [JsonProperty("value")]
        public JToken Value;

        public SettingEntry() { }

        public SettingEntry(string id, object value)
        {
            Id = id;
            Value = value != null ? JToken.FromObject(value) : JValue.CreateNull();
        }

        public T GetValue<T>()
        {
            if (Value == null || Value.Type == JTokenType.Null)
            {
                return default;
            }
            return Value.ToObject<T>();
        }
    }

    [Serializable]
    public class SavableData
    {
        [JsonProperty("data")]
        public List<SettingEntry> Data = new();
    }
}
