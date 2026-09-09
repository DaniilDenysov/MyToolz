using MyToolz.Utilities.Debug;
using NUnit.Framework;
using UnityEngine;

// Fixture types placed in a controlled namespace hierarchy so the gate's exact / namespace /
// parent-namespace resolution can be asserted against real Type.FullName / Type.Namespace values.
namespace MyToolz.Tests.GateFixtures
{
    internal class RootType { }
}

namespace MyToolz.Tests.GateFixtures.Sub
{
    internal class SubType { }
}

namespace MyToolz.Tests.EditMode
{
    using GateFixtures;
    using GateFixtures.Sub;

    public class LogGateTests
    {
        private LogGateSettingsSO _previous;
        private LogGateSettingsSO _settings;

        [SetUp]
        public void SetUp()
        {
            _previous = LogGate.Settings;
            _settings = ScriptableObject.CreateInstance<LogGateSettingsSO>();
            LogGate.Settings = _settings; // installs the SO and clears the type cache
        }

        [TearDown]
        public void TearDown()
        {
            LogGate.Settings = _previous;
            if (_settings != null)
            {
                Object.DestroyImmediate(_settings);
                _settings = null;
            }
        }

        [Test]
        public void UnknownType_UsesDefaultEnabled_True()
        {
            _settings.SetDefaultEnabled(true);
            LogGate.ClearCache();
            Assert.IsTrue(LogGate.ShouldLog(typeof(RootType)));
        }

        [Test]
        public void UnknownType_UsesDefaultEnabled_False()
        {
            _settings.SetDefaultEnabled(false);
            LogGate.ClearCache();
            Assert.IsFalse(LogGate.ShouldLog(typeof(RootType)));
        }

        [Test]
        public void ExactTypeEntry_OverridesDefault()
        {
            _settings.SetDefaultEnabled(true);
            _settings.Set(typeof(RootType).FullName, false);
            LogGate.ClearCache();
            Assert.IsFalse(LogGate.ShouldLog(typeof(RootType)));
        }

        [Test]
        public void NamespaceEntry_AppliesToTypesInThatNamespace()
        {
            _settings.SetDefaultEnabled(true);
            _settings.Set(typeof(RootType).Namespace, false); // "MyToolz.Tests.GateFixtures"
            LogGate.ClearCache();
            Assert.IsFalse(LogGate.ShouldLog(typeof(RootType)));
        }

        [Test]
        public void ParentNamespaceEntry_AppliesToNestedNamespaces()
        {
            _settings.SetDefaultEnabled(true);
            _settings.Set("MyToolz.Tests", false); // grandparent of SubType's namespace
            LogGate.ClearCache();
            Assert.IsFalse(LogGate.ShouldLog(typeof(SubType)));
        }

        [Test]
        public void NearestNamespaceEntry_WinsOverAncestor()
        {
            _settings.SetDefaultEnabled(true);
            _settings.Set("MyToolz.Tests", false);
            _settings.Set(typeof(SubType).Namespace, true); // "MyToolz.Tests.GateFixtures.Sub"
            LogGate.ClearCache();
            Assert.IsTrue(LogGate.ShouldLog(typeof(SubType)), "the closest matching namespace must win");
        }

        [Test]
        public void ExactTypeEntry_WinsOverNamespaceEntry()
        {
            _settings.Set(typeof(RootType).Namespace, false);
            _settings.Set(typeof(RootType).FullName, true);
            LogGate.ClearCache();
            Assert.IsTrue(LogGate.ShouldLog(typeof(RootType)));
        }

        [Test]
        public void GlobalWildcard_UsedWhenNoOtherMatch()
        {
            _settings.SetDefaultEnabled(true);
            _settings.Set("*", false);
            LogGate.ClearCache();
            Assert.IsFalse(LogGate.ShouldLog(typeof(RootType)), "the '*' entry applies when no exact/namespace entry matches");
        }

        [Test]
        public void NullContext_AlwaysLogs()
        {
            _settings.SetDefaultEnabled(false);
            LogGate.ClearCache();
            Assert.IsTrue(LogGate.ShouldLog((object)null), "a null context has no type to gate, so it logs");
        }

        [Test]
        public void ObjectContext_GatesByRuntimeType()
        {
            _settings.SetDefaultEnabled(true);
            _settings.Set(typeof(RootType).FullName, false);
            LogGate.ClearCache();
            Assert.IsFalse(LogGate.ShouldLog(new RootType()));
        }

        [Test]
        public void Result_IsCached_UntilCacheCleared()
        {
            _settings.SetDefaultEnabled(true);
            Assert.IsTrue(LogGate.ShouldLog(typeof(RootType)));

            // Mutating the SO directly does not invalidate LogGate's per-type cache.
            _settings.Set(typeof(RootType).FullName, false);
            Assert.IsTrue(LogGate.ShouldLog(typeof(RootType)), "cached result should still be returned");

            LogGate.ClearCache();
            Assert.IsFalse(LogGate.ShouldLog(typeof(RootType)), "after clearing the cache the new entry takes effect");
        }

        [Test]
        public void SettingsSetter_ClearsCache()
        {
            _settings.SetDefaultEnabled(true);
            Assert.IsTrue(LogGate.ShouldLog(typeof(RootType)));

            var replacement = ScriptableObject.CreateInstance<LogGateSettingsSO>();
            replacement.SetDefaultEnabled(false);
            LogGate.Settings = replacement; // must clear the cache so the new default applies

            Assert.IsFalse(LogGate.ShouldLog(typeof(RootType)));
            Object.DestroyImmediate(replacement);
        }
    }
}
