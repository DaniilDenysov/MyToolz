using System.Text.RegularExpressions;
using MyToolz.Utilities.Debug;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MyToolz.Tests.EditMode
{
    /// <summary>Context type used to drive DebugUtility's per-type log gating.</summary>
    internal class GatedContext { }

    public class LogExtensionsTests
    {
        [Test]
        public void Bold_WrapsInBoldTag()
        {
            Assert.AreEqual("<b>hello</b>", "hello".Bold());
        }

        [Test]
        public void Italic_WrapsInItalicTag()
        {
            Assert.AreEqual("<i>hello</i>", "hello".Italic());
        }

        [Test]
        public void Size_WrapsInSizeTag()
        {
            Assert.AreEqual("<size=20>hello</size>", "hello".Size(20f));
        }

        [Test]
        public void Color_WrapsInColorTag_WithRgbaHex()
        {
            string expected = $"<color=#{ColorUtility.ToHtmlStringRGBA(Color.red)}>hello</color>";
            Assert.AreEqual(expected, "hello".Color(Color.red));
        }

        [Test]
        public void Extensions_Compose()
        {
            string result = "hello".Bold().Size(14f);
            Assert.AreEqual("<size=14><b>hello</b></size>", result);
        }
    }

    public class DebugUtilityGatingTests
    {
        private LogGateSettingsSO _previous;
        private LogGateSettingsSO _settings;

        [SetUp]
        public void SetUp()
        {
            _previous = LogGate.Settings;
            _settings = ScriptableObject.CreateInstance<LogGateSettingsSO>();
            LogGate.Settings = _settings;
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
        public void LogError_IsEmitted_WhenGateEnablesType()
        {
            _settings.SetDefaultEnabled(true);
            _settings.Set(typeof(GatedContext).FullName, true);
            LogGate.ClearCache();

            LogAssert.Expect(LogType.Error, new Regex("GATE_ENABLED_MARKER"));
            DebugUtility.LogError(new GatedContext(), "GATE_ENABLED_MARKER");
        }

        [Test]
        public void LogError_IsSuppressed_WhenGateDisablesType()
        {
            _settings.SetDefaultEnabled(true);
            _settings.Set(typeof(GatedContext).FullName, false);
            LogGate.ClearCache();

            // If suppression is broken this error would surface and fail the test.
            DebugUtility.LogError(new GatedContext(), "GATE_DISABLED_MARKER");
            LogAssert.NoUnexpectedReceived();
        }
    }
}
