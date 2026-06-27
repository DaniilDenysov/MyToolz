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

        public override void InstallBindings()
        {
            Initialize();
            Container.Bind<InputActionAsset>().FromInstance(inputActions).AsSingle();
            Container.Bind<IStateMachine<IPriorityState>>().FromInstance(inputStateManager as IStateMachine<IPriorityState>).AsSingle();
            Container.Bind<InputStateManager>().FromInstance(inputStateManager).AsSingle();
            inputStateManager.ChangeState(defaultInputModeSO);

            var deviceTracker = new InputDeviceTracker();
            deviceTracker.SubscribeToActionMap(inputActions);
            Container.Bind<InputDeviceTracker>().FromInstance(deviceTracker).AsSingle();
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
            inputActions.Enable();
            RegisterBindings();
        }

        public void RegisterBindings()
        {
            foreach (var cmd in inputCommands)
            {
                if (cmd == null) continue;
                cmd.Initialize(inputActions);
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
                cmd.Unregister();
            }
        }
    }
}
