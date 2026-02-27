using System;
using System.Collections.Generic;
using MyToolz.DesignPatterns.Command;
using MyToolz.Utilities.Debug;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MyToolz.InputManagement.Commands.Pipeline
{
    [Serializable]
    public class InputCommandPipeline : CommandPipeline<IInputCommand>
    {
        [SerializeField] private List<InputCommandSO> register = new();

        private InputDeviceTracker deviceTracker;
        private bool initialized;
        private InputActionAsset inputActions;

        public InputDeviceTracker DeviceTracker => deviceTracker;
        public IReadOnlyList<InputCommandSO> RegisteredCommands => register;

        public void Initialize(InputActionAsset inputActions)
        {
            if (initialized) return;

            if (inputActions == null)
            {
                DebugUtility.LogError(this, $"{nameof(inputActions)} is null!");
                return;
            }
            inputActions.Enable();
            this.inputActions = inputActions;
            deviceTracker = new InputDeviceTracker();
            deviceTracker.SubscribeToActionMap(inputActions);

            RegisterBindings();

            initialized = true;
        }

        public void RegisterBindings()
        {
            foreach (var cmd in register)
            {
                if (cmd == null) continue;
                cmd.Initialize(inputActions);
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
