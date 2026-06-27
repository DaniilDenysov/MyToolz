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

        public event Action OnInput;
        public event Action<InputAction.CallbackContext, InputCommandSO> OnInputAction;
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
            InputAction action = ResolveAction();
            return action != null && action.enabled;
        }

        public InputActionMap GetActionMap()
        {
            InputAction action = ResolveAction();
            return action?.actionMap;
        }

        public void Register()
        {
            if (registered)
            {
                return;
            }

            InputAction action = ResolveAction();
            if (action == null)
            {
                DebugUtility.LogError(this, $"Cannot register {inputName}: action could not be resolved.");
                return;
            }

            action.started += HandleCallback;
            action.performed += HandleCallback;
            action.canceled += HandleCallback;
            registered = true;
            DebugUtility.Log(this, $"Registered {inputName}");
        }

        public void Unregister()
        {
            if (!registered)
            {
                return;
            }

            InputAction action = ResolveAction();
            if (action == null)
            {
                return;
            }

            action.started -= HandleCallback;
            action.performed -= HandleCallback;
            action.canceled -= HandleCallback;
            registered = false;
        }

        private InputAction ResolveAction()
        {
            if (resolvedAction != null)
            {
                return resolvedAction;
            }

            if (inputActionReference == null)
            {
                return null;
            }

            InputAction refAction = inputActionReference.action;
            if (refAction == null)
            {
                return null;
            }

            if (runtimeAsset != null)
            {
                resolvedAction = runtimeAsset.FindAction(refAction.id);
                if (resolvedAction != null)
                {
                    return resolvedAction;
                }
            }

            resolvedAction = refAction;
            return resolvedAction;
        }

        private void HandleCallback(InputAction.CallbackContext context)
        {
            OnInput?.Invoke();
            OnInputAction?.Invoke(context, this);

            switch (context.phase)
            {
                case UnityEngine.InputSystem.InputActionPhase.Started:
                    OnStarted?.Invoke();
                    OnInputStarted?.Invoke(this);
                    if (IsPressed())
                    {
                        OnPressed?.Invoke();
                        OnInputPressed?.Invoke(this);
                    }
                    break;
                case UnityEngine.InputSystem.InputActionPhase.Performed:
                    OnPerformed?.Invoke();
                    OnInputPerformed?.Invoke(this);
                    break;
                case UnityEngine.InputSystem.InputActionPhase.Canceled:
                    OnCanceled?.Invoke();
                    OnInputCanceled?.Invoke(this);
                    OnReleased?.Invoke();
                    OnInputReleased?.Invoke(this);
                    break;
            }
        }

        public bool WasPressedThisFrame()
        {
            InputAction action = ResolveAction();
            if (action == null)
            {
                return false;
            }

            if (!action.enabled)
            {
                return false;
            }

            return action.WasPressedThisFrame();
        }

        public bool IsPressed()
        {
            InputAction action = ResolveAction();
            if (action == null)
            {
                return false;
            }

            if (!action.enabled)
            {
                return false;
            }

            return action.IsPressed();
        }
        public bool WasPerformedThisFrame()
        {
            InputAction action = ResolveAction();
            if (action == null)
            {
                return false;
            }

            if (!action.enabled)
            {
                return false;
            }

            return action.WasPerformedThisFrame();
        }
        public bool WasReleasedThisFrame()
        {
            InputAction action = ResolveAction();
            if (action == null)
            {
                return false;
            }

            if (!action.enabled)
            {
                return false;
            }

            return action.WasReleasedThisFrame();
        }

        public T ReadValue<T>() where T : struct
        {
            InputAction action = ResolveAction();
            if (action == null)
            {
                return default(T);
            }

            //if (!action.enabled)
            //{
            //    return default(T);
            //}

            return action.ReadValue<T>();
        }

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(inputName))
            {
                inputName = "InputManagement Name";
            }
        }
    }
}
