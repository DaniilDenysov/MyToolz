using System;
using System.Collections.Generic;
using MyToolz.DesignPatterns.Command;
using MyToolz.Utilities.Debug;
using UnityEngine;
using UnityEngine.InputSystem;
using Zenject;

namespace MyToolz.InputManagement.Commands.Pipeline
{
    [Serializable]
    public class InputCommandPipeline : CommandPipeline<IInputCommand>
    {
        [SerializeField] private List<InputCommandSO> register = new();

        private InputDeviceTracker deviceTracker;
        private bool initialized;

        public InputDeviceTracker DeviceTracker => deviceTracker;
        public IReadOnlyList<InputCommandSO> RegisteredCommands => register;

        public void Initialize(InputActionAsset inputActions, DiContainer container)
        {
            if (initialized) return;

            if (inputActions == null)
            {
                DebugUtility.LogError(this, $"{nameof(inputActions)} is null!");
                return;
            }
            if (container == null)
            {
                DebugUtility.LogError(this, $"{nameof(container)} is null!");
                return;
            }
            deviceTracker = new InputDeviceTracker();
            deviceTracker.SubscribeToActionMap(inputActions);

            foreach (var cmd in register)
            {
                if (cmd == null)
                {
                    DebugUtility.LogWarning(this, "Null entry found in register list. Skipping.");
                    continue;
                }
                container.Inject(cmd);
            }

            initialized = true;
        }

        public void RegisterBindings()
        {
            foreach (var cmd in register)
            {
                if (cmd == null) continue;
                cmd.Register();
            }
        }

        public void UnregisterBindings()
        {
            foreach (var cmd in register)
            {
                if (cmd == null) continue;
                cmd.Unregister();
            }
            deviceTracker?.UnsubscribeAll();
        }

        public override void Update()
        {
            var executing = GetExecutingCommandsInternal();
            for (int i = executing.Count - 1; i >= 0; i--)
            {
                var cmd = executing[i];
                cmd.Update();
                if (cmd.IsFinished)
                {
                    RemoveFinishedCommand(cmd);
                }
            }
            base.Update();
        }

        public void Dispose()
        {
            UnregisterBindings();
            Clear();
            deviceTracker?.Dispose();
            initialized = false;
        }
    }
}
