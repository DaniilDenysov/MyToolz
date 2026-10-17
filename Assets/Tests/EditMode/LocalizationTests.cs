using System.Collections.Generic;
using System.Reflection;
using MyToolz.Localization;
using NUnit.Framework;
using UnityEngine;

namespace MyToolz.Tests.EditMode
{
    public class LocalizationDatabaseTests : SilentLogTest
    {
        private readonly List<Object> created = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (Object o in created) Object.DestroyImmediate(o);
            created.Clear();
        }

        [Test]
        public void Columns_AreMatchedByCode_NotByListOrder()
        {
            LocalizationLanguageSO english = Language("en");
            LocalizationLanguageSO french = Language("fr");
            // List order is the reverse of the CSV column order.
            LocalizationDatabaseSO database = Database("key,en,fr\ngreeting,Hello,Bonjour\n", french, english);

            Assert.That(database.TryTranslate("greeting", english, out string en), Is.True);
            Assert.That(en, Is.EqualTo("Hello"));
            Assert.That(database.TryTranslate("greeting", french, out string fr), Is.True);
            Assert.That(fr, Is.EqualTo("Bonjour"));
        }

        [Test]
        public void HeaderWithoutLanguageNames_FallsBackToListOrder()
        {
            LocalizationLanguageSO english = Language("en");
            LocalizationLanguageSO french = Language("fr");
            LocalizationDatabaseSO database = Database("key,A,B\ngreeting,Hello,Bonjour\n", english, french);

            database.TryTranslate("greeting", french, out string fr);

            Assert.That(fr, Is.EqualTo("Bonjour"));
        }

        [Test]
        public void LanguageMissingFromNamedHeader_DoesNotTakeAnotherColumn()
        {
            LocalizationLanguageSO english = Language("en");
            LocalizationLanguageSO german = Language("de");
            LocalizationDatabaseSO database = Database("key,en,fr\ngreeting,Hello,Bonjour\n", english, german);

            Assert.That(database.TryTranslate("greeting", german, out _), Is.False);
        }

        [Test]
        public void DuplicateKey_KeepsTheFirstDefinition()
        {
            LocalizationLanguageSO english = Language("en");
            LocalizationDatabaseSO database = Database("key,en\ngreeting,Hello\ngreeting,Howdy\n", english);

            database.TryTranslate("greeting", english, out string value);

            Assert.That(value, Is.EqualTo("Hello"));
            Assert.That(database.Keys.Count, Is.EqualTo(1));
        }

        [Test]
        public void Contains_MatchesAnotherInstanceWithTheSameCode()
        {
            LocalizationLanguageSO english = Language("en");
            LocalizationLanguageSO englishCopy = Language("en");
            LocalizationDatabaseSO database = Database("key,en\ngreeting,Hello\n", english);

            Assert.That(database.Contains(englishCopy), Is.True);
        }

        [Test]
        public void DeviceLanguage_MatchesByConfiguredListOrCode()
        {
            LocalizationLanguageSO english = Language("English");
            LocalizationLanguageSO polish = Language("pl");
            SetField(polish, "deviceLanguages", new[] { SystemLanguage.Polish });

            Assert.That(LocalizationManager.FindForDevice(new[] { english, polish }, SystemLanguage.Polish), Is.SameAs(polish));
            Assert.That(LocalizationManager.FindForDevice(new[] { english, polish }, SystemLanguage.English), Is.SameAs(english));
            Assert.That(LocalizationManager.FindForDevice(new[] { english, polish }, SystemLanguage.Japanese), Is.Null);
        }

        private LocalizationLanguageSO Language(string code)
        {
            var language = ScriptableObject.CreateInstance<LocalizationLanguageSO>();
            language.name = code;
            SetField(language, "code", code);
            created.Add(language);
            return language;
        }

        private LocalizationDatabaseSO Database(string csv, params LocalizationLanguageSO[] languages)
        {
            var asset = new TextAsset(csv);
            var database = ScriptableObject.CreateInstance<LocalizationDatabaseSO>();
            SetField(database, "csv", asset);
            SetField(database, "languages", new List<LocalizationLanguageSO>(languages));
            database.Reload();
            created.Add(asset);
            created.Add(database);
            return database;
        }

        private static void SetField(object target, string field, object value) =>
            target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    }
}
