using MyToolz.DesignPatterns.StateMachine;
using MyToolz.DesignPatterns.StateMachine.SimplePriorityBased;
using MyToolz.InputManagement.Commands;
using MyToolz.Utilities.Debug;
using MyToolz.EditorToolz;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using Zenject;

namespace MyToolz.InputManagement
{
    [Serializable]
    public class InputRegister
    {
    }

    public class InputStateManagementInstaller : MonoInstaller
    {
        [SerializeField] private List<InputCommandSO> inputCommands;
        [SerializeField] private List<InputModeSO> inputModeSOs;
        [SerializeField] private InputStateManager inputStateManager = new();
        [SerializeField, Required] private InputModeSO defaultInputModeSO;
        [SerializeField, Required] private InputActionAsset inputActions;
        [SerializeField, Tooltip("Keep every action of the asset enabled regardless of the active input mode (1.x behaviour). " +
            "Off: only the actions of the active InputModeSO are enabled.")]
        private bool keepAllActionsEnabled;

        private InputDeviceTracker deviceTracker;
        private bool initialized;

        public override void InstallBindings()
        {
            Initialize();
            Container.Bind<InputActionAsset>().FromInstance(inputActions).AsSingle();
            Container.Bind<IInputStateManager>().FromInstance(inputStateManager).AsSingle();
            Container.Bind<InputStateManager>().FromInstance(inputStateManager).AsSingle();

            if (defaultInputModeSO != null)
            {
                inputStateManager.ChangeState(defaultInputModeSO);
            }

            deviceTracker?.Dispose();
            deviceTracker = new InputDeviceTracker();
            deviceTracker.SubscribeToActionMap(inputActions);
            Container.Bind<InputDeviceTracker>().FromInstance(deviceTracker).AsSingle();
        }

        private void OnDestroy()
        {
            Teardown();
        }

        /// <summary>Exits the active mode, releases command subscriptions and the device tracker.</summary>
        public void Teardown()
        {
            if (!initialized) return;
            initialized = false;

            inputStateManager.Clear();
            UnregisterBindings();
            deviceTracker?.Dispose();
            deviceTracker = null;
        }

#if UNITY_EDITOR
        [ContextMenu("Rebuild")]
        public void Rebuild()
        {
            inputCommands = FindAllAssets<InputCommandSO>();
            inputModeSOs = FindAllAssets<InputModeSO>();
            UnityEditor.EditorUtility.SetDirty(this);
        }

        private static List<T> FindAllAssets<T>() where T : ScriptableObject
        {
            return UnityEditor.AssetDatabase.FindAssets($"t:{typeof(T).Name}")
                .Select(UnityEditor.AssetDatabase.GUIDToAssetPath)
                .Select(UnityEditor.AssetDatabase.LoadAssetAtPath<T>)
                .Where(a => a != null)
                .ToList();
        }
#endif

        public void Initialize()
        {
            if (inputActions == null)
            {
                DebugUtility.LogError(this, $"{nameof(inputActions)} is null!");
                return;
            }

            if (initialized)
            {
                return;
            }

            // Start from a known state: nothing enabled until the default mode enables its own actions.
            if (keepAllActionsEnabled)
                inputActions.Enable();
            else
                inputActions.Disable();

            RegisterBindings();
            initialized = true;
        }

        public void RegisterBindings()
        {
            foreach (var cmd in inputCommands)
            {
                if (cmd == null) continue;
                cmd.Initialize(inputActions, this);
                cmd.Register();
            }

            foreach (var mode in inputModeSOs)
            {
                if (mode == null) continue;
                mode.Initialize(inputActions);
            }
        }

        public void UnregisterBindings()
        {
            foreach (var cmd in inputCommands)
            {
                if (cmd == null) continue;
                cmd.Release(this);
            }
        }
    }
}
