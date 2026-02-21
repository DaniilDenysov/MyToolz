using System;
using MyToolz.Utilities.Debug;
using Newtonsoft.Json;
using UnityEngine;

namespace MyToolz.IO
{
    [Serializable]
    public abstract class SerializationStrategy<T> where T : class
    {
        public abstract string FileExtension { get; }
        public abstract string Serialize(T data);
        public abstract T Deserialize(string raw);
    }

    [Serializable]
    public sealed class NewtonsoftJsonStrategy<T> : SerializationStrategy<T> where T : class
    {
        [SerializeField] private bool _prettyPrint = true;

        public override string FileExtension => ".json";

        public override string Serialize(T data) =>
            JsonConvert.SerializeObject(data, _prettyPrint ? Formatting.Indented : Formatting.None);

        public override T Deserialize(string raw) =>
            JsonConvert.DeserializeObject<T>(raw);
    }

    [Serializable]
    public sealed class UnityJsonStrategy<T> : SerializationStrategy<T> where T : class
    {
        [SerializeField] private bool _prettyPrint = true;

        public override string FileExtension => ".json";

        public override string Serialize(T data)
        {
            DebugUtility.LogWarning(this, 
                "[SaveLoadBase] UnityJsonStrategy does not support collections, dictionaries, or non-public fields. " +
                "Use NewtonsoftJsonStrategy unless you have a specific reason not to.");
            return JsonUtility.ToJson(data, _prettyPrint);
        }

        public override T Deserialize(string raw)
        {
            DebugUtility.LogWarning(this,
                "[SaveLoadBase] UnityJsonStrategy does not support collections, dictionaries, or non-public fields. " +
                "Use NewtonsoftJsonStrategy unless you have a specific reason not to.");
            return JsonUtility.FromJson<T>(raw);
        }
    }
}