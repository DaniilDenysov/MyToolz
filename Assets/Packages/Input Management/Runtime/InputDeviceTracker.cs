using MyToolz.Utilities.Debug;
using System;
using System.Collections.Generic;
using UnityEngine.InputSystem;

namespace MyToolz.InputManagement
{
    public class InputDeviceTracker : IDisposable
    {
        private readonly List<InputAction> subscribedActions = new();
        private InputDevice lastDevice;
        private bool disposed;

        public InputDevice LastDevice => lastDevice;

        public event Action<InputDevice> OnInputDeviceChanged;

        public void SubscribeToActionMap(InputActionAsset asset)
        {
            if (asset == null)
            {
                DebugUtility.LogError(this, $"{nameof(asset)} is null!");
                return;
            }

            foreach (var map in asset.actionMaps)
            {
                foreach (var action in map.actions)
                {
                    action.performed += OnActionPerformed;
                    subscribedActions.Add(action);
                }
            }
        }

        public void UnsubscribeAll()
        {
            foreach (var action in subscribedActions)
            {
                action.performed -= OnActionPerformed;
            }
            subscribedActions.Clear();
        }

        private void OnActionPerformed(InputAction.CallbackContext ctx)
        {
            var device = ctx.control?.device;
            if (device == null || device == lastDevice) return;
            lastDevice = device;
            OnInputDeviceChanged?.Invoke(device);
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            UnsubscribeAll();
        }
    }
}
