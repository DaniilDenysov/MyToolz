using System;
using System.Reflection;
using MyToolz.UI.Management;
using MyToolz.Utilities.Debug;
using NUnit.Framework;
using UnityEngine;

namespace MyToolz.Tests.EditMode
{
    /// <summary>
    /// Base class for EditMode tests that exercise MyToolz systems which log through
    /// <see cref="DebugUtility"/>. It swaps <see cref="LogGate.Settings"/> for a throwaway
    /// settings object with logging disabled, so the tested code's diagnostic logs (including
    /// error logs on invalid input) neither spam the console nor fail the test run, and restores
    /// the original gate afterwards. Tests that assert on logging behaviour must not use this base.
    /// </summary>
    public abstract class SilentLogTest
    {
        private LogGateSettingsSO _previous;
        private LogGateSettingsSO _silent;

        [SetUp]
        public void SilenceLogs()
        {
            _previous = LogGate.Settings;
            _silent = ScriptableObject.CreateInstance<LogGateSettingsSO>();
            _silent.SetDefaultEnabled(false);
            LogGate.Settings = _silent; // setter clears the type cache
        }

        [TearDown]
        public void RestoreLogs()
        {
            LogGate.Settings = _previous;
            if (_silent != null)
            {
                UnityEngine.Object.DestroyImmediate(_silent);
                _silent = null;
            }
        }
    }

    /// <summary>Hand-built <see cref="IUILayer"/> that records enter/exit calls for assertions.</summary>
    internal sealed class FakeUILayer : IUILayer
    {
        public FakeUILayer(UILayerSO layer) => Layer = layer;

        public UILayerSO Layer { get; }
        public bool IsActive { get; private set; }
        public int EnterCount { get; private set; }
        public int ExitCount { get; private set; }

        public void OnEnter()
        {
            IsActive = true;
            EnterCount++;
        }

        public void OnExit()
        {
            IsActive = false;
            ExitCount++;
        }
    }

    /// <summary>Hand-built <see cref="IUIState"/> that records enter/exit calls for assertions.</summary>
    internal sealed class FakeUIState : IUIState
    {
        public bool IsActive { get; private set; }
        public int EnterCount { get; private set; }
        public int ExitCount { get; private set; }

        public void OnEnter()
        {
            IsActive = true;
            EnterCount++;
        }

        public void OnExit()
        {
            IsActive = false;
            ExitCount++;
        }
    }

    internal static class UILayerFactory
    {
        private static readonly FieldInfo ActivationModeField =
            typeof(UILayerSO).GetField("activationMode", BindingFlags.Instance | BindingFlags.NonPublic);

        /// <summary>
        /// Creates an in-memory <see cref="UILayerSO"/> with the given activation mode.
        /// <see cref="UILayerSO.ActivationMode"/> is read-only, so the backing serialized field is
        /// set through reflection — the one indirection the test cannot avoid for an SO-only property.
        /// </summary>
        public static UILayerSO Create(ActivationMode mode, string name)
        {
            var so = ScriptableObject.CreateInstance<UILayerSO>();
            so.name = name;
            Assert.NotNull(ActivationModeField, "UILayerSO.activationMode field not found — did the field get renamed?");
            ActivationModeField.SetValue(so, mode);
            return so;
        }
    }
}
