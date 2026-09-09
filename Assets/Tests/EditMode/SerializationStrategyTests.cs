using System.Collections.Generic;
using MyToolz.IO;
using NUnit.Framework;

namespace MyToolz.Tests.EditMode
{
    public class SerializationStrategyTests
    {
        [System.Serializable]
        public class SaveData
        {
            public int Level;
            public string Name;
            public List<string> Items = new();
            public Dictionary<string, int> Scores = new();
        }

        private static SaveData Sample() => new SaveData
        {
            Level = 7,
            Name = "Daniil",
            Items = new List<string> { "sword", "shield" },
            Scores = new Dictionary<string, int> { { "kills", 12 }, { "deaths", 3 } }
        };

        [Test]
        public void Newtonsoft_FileExtension_IsJson()
        {
            Assert.AreEqual(".json", new NewtonsoftJsonStrategy<SaveData>().FileExtension);
        }

        [Test]
        public void Newtonsoft_RoundTrips_ScalarsCollectionsAndDictionaries()
        {
            var strategy = new NewtonsoftJsonStrategy<SaveData>();
            SaveData original = Sample();

            string json = strategy.Serialize(original);
            SaveData restored = strategy.Deserialize(json);

            Assert.AreEqual(original.Level, restored.Level);
            Assert.AreEqual(original.Name, restored.Name);
            CollectionAssert.AreEqual(original.Items, restored.Items);
            CollectionAssert.AreEquivalent(original.Scores, restored.Scores);
        }

        [Test]
        public void Newtonsoft_Serialize_ProducesNonEmptyJson()
        {
            string json = new NewtonsoftJsonStrategy<SaveData>().Serialize(Sample());
            Assert.IsNotEmpty(json);
            StringAssert.Contains("\"Level\"", json);
            StringAssert.Contains("sword", json);
        }

        [Test]
        public void Newtonsoft_RoundTrips_EmptyObject()
        {
            var strategy = new NewtonsoftJsonStrategy<SaveData>();
            SaveData restored = strategy.Deserialize(strategy.Serialize(new SaveData()));

            Assert.AreEqual(0, restored.Level);
            Assert.IsNotNull(restored.Items);
            Assert.IsEmpty(restored.Items);
        }
    }
}
