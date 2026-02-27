using MyToolz.EditorToolz;
using MyToolz.Utilities.Debug;
using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MyToolz.InputManagement.Commands
{
    [CreateAssetMenu(fileName = "InputCommandSO", menuName = "MyToolz/InputManagement/InputCommandSO")]
    public class InputCommandSO : ScriptableObject
    {
        [SerializeField] private string inputName = "InputManagement Name";
        [SerializeField] private InputPhase inputActionPhase = InputPhase.Performed;
        [SerializeField, Required] private InputActionReference inputActionReference;

        private InputActionAsset runtimeAsset;
        private InputAction resolvedAction;
        private bool registered;

        public InputPhase InputActionPhase => inputActionPhase;
        public string InputName => inputName;
        public InputActionReference InputActionReference => inputActionReference;

        public event Action<InputCommandSO> OnInputPressed;
        public event Action OnPressed;
        public event Action<InputCommandSO> OnInputReleased;
        public event Action OnReleased;
        public event Action<InputCommandSO> OnInputCanceled;
        public event Action OnCanceled;
        public event Action<InputCommandSO> OnInputPerformed;
        public event Action OnPerformed;
        public event Action<InputCommandSO> OnInputStarted;
        public event Action OnStarted;

        public void Initialize(InputActionAsset sharedAsset)
        {
            runtimeAsset = sharedAsset;
            resolvedAction = null;
            registered = false;
        }

        public bool IsActionEnabled()
        {
            var action = ResolveAction();
            return action != null && action.enabled;
        }

        public InputActionMap GetActionMap()
        {
            var action = ResolveAction();
            return action?.actionMap;
        }

        public void Register()
        {
            if (registered) return;

            var action = ResolveAction();
            if (action == null)
            {
                DebugUtility.LogError(this, $"Cannot register {inputName}: action could not be resolved.");
                return;
            }

            action.started += HandleCallback;
            action.performed += HandleCallback;
            action.canceled += HandleCallback;
            registered = true;
        }

        public void Unregister()
        {
            if (!registered) return;

            var action = ResolveAction();
            if (action == null) return;

            action.started -= HandleCallback;
            action.performed -= HandleCallback;
            action.canceled -= HandleCallback;
            registered = false;
        }

        private InputAction ResolveAction()
        {
            if (resolvedAction != null) return resolvedAction;
            if (inputActionReference == null) return null;

            var refAction = inputActionReference.action;
            if (refAction == null) return null;

            if (runtimeAsset != null)
            {
                resolvedAction = runtimeAsset.FindAction(refAction.id);
                if (resolvedAction != null) return resolvedAction;
            }

            resolvedAction = refAction;
            return resolvedAction;
        }

        private void HandleCallback(InputAction.CallbackContext context)
        {
            switch (inputActionPhase)
            {
                case InputPhase.Started:
                    if (context.phase != UnityEngine.InputSystem.InputActionPhase.Started) return;
                    OnStarted?.Invoke();
                    OnInputStarted?.Invoke(this);
                    break;

                case InputPhase.Performed:
                    if (context.phase != UnityEngine.InputSystem.InputActionPhase.Performed) return;
                    OnPerformed?.Invoke();
                    OnInputPerformed?.Invoke(this);
                    break;

                case InputPhase.Canceled:
                    if (context.phase != UnityEngine.InputSystem.InputActionPhase.Canceled) return;
                    OnCanceled?.Invoke();
                    OnInputCanceled?.Invoke(this);
                    break;

                case InputPhase.Pressed:
                    if (context.phase != UnityEngine.InputSystem.InputActionPhase.Started) return;
                    if (!IsPressed()) return;
                    OnPressed?.Invoke();
                    OnInputPressed?.Invoke(this);
                    break;

                case InputPhase.Released:
                    if (context.phase != UnityEngine.InputSystem.InputActionPhase.Canceled) return;
                    OnReleased?.Invoke();
                    OnInputReleased?.Invoke(this);
                    break;
            }
        }

        public bool WasPressedThisFrame() => ResolveAction()?.WasPressedThisFrame() ?? false;
        public bool IsPressed() => ResolveAction()?.IsPressed() ?? false;
        public bool WasPerformedThisFrame() => ResolveAction()?.WasPerformedThisFrame() ?? false;
        public bool WasReleasedThisFrame() => ResolveAction()?.WasReleasedThisFrame() ?? false;

        public T ReadValue<T>() where T : struct
        {
            return ResolveAction()?.ReadValue<T>() ?? default;
        }

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(inputName))
                inputName = "InputManagement Name";
        }
    }
}
