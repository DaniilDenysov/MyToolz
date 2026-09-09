using MyToolz.Utilities.Debug;
using NUnit.Framework;
using UnityEngine;

namespace MyToolz.Tests.EditMode
{
    public class LogGateSettingsSOTests
    {
        private LogGateSettingsSO _settings;

        [SetUp]
        public void SetUp() => _settings = ScriptableObject.CreateInstance<LogGateSettingsSO>();

        [TearDown]
        public void TearDown()
        {
            if (_settings != null)
            {
                Object.DestroyImmediate(_settings);
                _settings = null;
            }
        }

        [Test]
        public void DefaultEnabled_IsTrueByDefault()
        {
            Assert.IsTrue(_settings.DefaultEnabled);
        }

        [Test]
        public void SetDefaultEnabled_UpdatesValue()
        {
            _settings.SetDefaultEnabled(false);
            Assert.IsFalse(_settings.DefaultEnabled);
        }

        [Test]
        public void Set_ThenTryGet_ReturnsStoredValue()
        {
            _settings.Set("MyToolz.Foo", false);
            Assert.IsTrue(_settings.TryGet("MyToolz.Foo", out bool enabled));
            Assert.IsFalse(enabled);
        }

        [Test]
        public void TryGet_MissingPath_ReturnsFalse()
        {
            Assert.IsFalse(_settings.TryGet("Does.Not.Exist", out _));
        }

        [Test]
        public void Set_ExistingPath_UpdatesInPlace_DoesNotDuplicate()
        {
            _settings.Set("MyToolz.Foo", true);
            _settings.Set("MyToolz.Foo", false);

            Assert.AreEqual(1, _settings.Entries.Count, "re-setting the same path must overwrite, not append");
            Assert.IsTrue(_settings.TryGet("MyToolz.Foo", out bool enabled));
            Assert.IsFalse(enabled);
        }

        [Test]
        public void Remove_DeletesEntry()
        {
            _settings.Set("MyToolz.Foo", true);
            _settings.Remove("MyToolz.Foo");

            Assert.IsFalse(_settings.TryGet("MyToolz.Foo", out _));
            Assert.AreEqual(0, _settings.Entries.Count);
        }

        [Test]
        public void EmptyPath_IsNormalizedToWildcard()
        {
            _settings.Set("", true);
            Assert.IsTrue(_settings.TryGet("*", out bool viaWildcard));
            Assert.IsTrue(viaWildcard);
            Assert.IsTrue(_settings.TryGet("", out bool viaEmpty), "empty lookups normalize to '*' too");
            Assert.IsTrue(viaEmpty);
        }

        [Test]
        public void WhitespaceInPath_IsTrimmed()
        {
            _settings.Set("   MyToolz.Foo   ", true);
            Assert.IsTrue(_settings.TryGet("MyToolz.Foo", out bool enabled));
            Assert.IsTrue(enabled);
        }
    }
}
