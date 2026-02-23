using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Zenject;

namespace MyToolz.InputManagement.Commands
{
    [CreateAssetMenu(fileName = "InputCommandSO", menuName = "MyToolz/InputManagement/InputCommandSO")]
    public class InputCommandSO : ScriptableObject
    {
        [SerializeField] private string inputName = "InputManagement Name";
        [SerializeField] private InputPhase inputActionPhase = InputPhase.Performed;
        [SerializeField] private InputActionReference inputActionReference;

        private DefaultInputActions inputActions;
        private InputAction cachedAction;
        private bool isCacheDirty = true;

        public InputPhase InputActionPhase => inputActionPhase;
        public string InputName => inputName;
        public InputActionReference InputActionReference => inputActionReference;

        public event Action<InputCommandSO> OnPressed;
        public event Action<InputCommandSO> OnReleased;
        public event Action<InputCommandSO> OnCanceled;
        public event Action<InputCommandSO> OnPerformed;
        public event Action<InputCommandSO> OnStarted;

        [Inject]
        private void Construct(DefaultInputActions inputActions)
        {
            this.inputActions = inputActions;
            isCacheDirty = true;
        }

        public void Register()
        {
            var action = ResolveAction();
            if (action == null) return;

            switch (inputActionPhase)
            {
                case InputPhase.Started:
                    action.started += HandleCallback;
                    break;
                case InputPhase.Performed:
                    action.performed += HandleCallback;
                    break;
                case InputPhase.Canceled:
                    action.canceled += HandleCallback;
                    break;
                case InputPhase.Pressed:
                    action.started += HandleCallback;
                    break;
                case InputPhase.Released:
                    action.canceled += HandleCallback;
                    break;
            }
        }

        public void Unregister()
        {
            var action = ResolveAction();
            if (action == null) return;

            action.started -= HandleCallback;
            action.performed -= HandleCallback;
            action.canceled -= HandleCallback;
        }

        private InputAction ResolveAction()
        {
            if (!isCacheDirty && cachedAction != null) return cachedAction;

            if (inputActionReference == null) return null;
            var refAction = inputActionReference.action;
            if (refAction == null) return null;

            if (inputActions?.asset != null)
            {
                var found = inputActions.asset.FindAction(refAction.name, true);
                if (found != null)
                {
                    cachedAction = found;
                    isCacheDirty = false;
                    return cachedAction;
                }
            }

            cachedAction = refAction;
            isCacheDirty = false;
            return cachedAction;
        }

        private void HandleCallback(InputAction.CallbackContext context)
        {
            switch (inputActionPhase)
            {
                case InputPhase.Started:
                    if (context.phase != UnityEngine.InputSystem.InputActionPhase.Started) return;
                    OnStarted?.Invoke(this);
                    break;

                case InputPhase.Performed:
                    if (context.phase != UnityEngine.InputSystem.InputActionPhase.Performed) return;
                    OnPerformed?.Invoke(this);
                    break;

                case InputPhase.Canceled:
                    if (context.phase != UnityEngine.InputSystem.InputActionPhase.Canceled) return;
                    OnCanceled?.Invoke(this);
                    break;

                case InputPhase.Pressed:
                    if (context.phase != UnityEngine.InputSystem.InputActionPhase.Started) return;
                    if (!IsPressed()) return;
                    OnPressed?.Invoke(this);
                    break;

                case InputPhase.Released:
                    if (context.phase != UnityEngine.InputSystem.InputActionPhase.Canceled) return;
                    OnReleased?.Invoke(this);
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
