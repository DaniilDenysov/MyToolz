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
        private object owner;

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

        public void Initialize(InputActionAsset sharedAsset) => Initialize(sharedAsset, null);

        /// <summary>
        /// Binds this command to <paramref name="sharedAsset"/> on behalf of <paramref name="context"/>.
        /// Subscriptions on a previously resolved action are released first, so re-initializing (a new
        /// scene, a second context, domain-reload-free play mode) never leaves stale callbacks behind.
        /// </summary>
        public void Initialize(InputActionAsset sharedAsset, object context)
        {
            if (registered)
            {
                Unregister();
            }

            if (IsAlive(owner) && context != null && !ReferenceEquals(owner, context))
            {
                DebugUtility.LogWarning(this, $"{inputName} is being re-bound by another input context; the previous context no longer receives it.");
            }

            owner = context;
            runtimeAsset = sharedAsset;
            resolvedAction = null;
            registered = false;
        }

        // A destroyed Unity owner (e.g. the installer of an unloaded scene) no longer holds the command.
        private static bool IsAlive(object candidate) =>
            candidate is UnityEngine.Object unityObject ? unityObject != null : candidate != null;

        /// <summary>Unregisters and forgets the asset, if <paramref name="context"/> still owns this command.</summary>
        public void Release(object context)
        {
            if (IsAlive(owner) && context != null && !ReferenceEquals(owner, context))
            {
                return;
            }

            Unregister();
            owner = null;
            runtimeAsset = null;
            resolvedAction = null;
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
