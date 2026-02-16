using MyToolz.Core;
using MyToolz.EditorToolz;
using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Zenject;

namespace MyToolz.Player.Input
{
    //We need separate enum for handling things like Pressed and Released which are not present in InputActionPhase
    public enum InputPhase
    {
        Pressed,
        Released,
        Canceled,
        Performed,
        Started
    }

    [CreateAssetMenu(fileName = "InputCommandSO", menuName = "MyToolz/Input/InputCommandSO")]
    public class InputCommandSO : ScriptableObjectPlus
    {
        private DefaultInputActions inputActions;

        [SerializeField, Required] protected string inputName = "Input Name";
        [SerializeField] protected InputPhase inputActionPhase = InputPhase.Performed;
        [SerializeField, Required] protected InputActionReference inputActionReference;
        public InputPhase InputActionPhase => inputActionPhase;
        public string InputName => inputName;
        public InputActionReference InputActionReference => inputActionReference;
        public event Action<InputCommandSO> Pressed;
        public event Action<InputCommandSO> Released;
        public event Action<InputCommandSO> Canceled;
        public event Action<InputCommandSO> Performed;
        public event Action<InputCommandSO> Started;


        [Inject]
        private void Construct(DefaultInputActions inputActions)
        {
            this.inputActions = inputActions;
        }

        public void Register()
        {
            var action = ResolveAction();
            if (action == null) return;

            switch (inputActionPhase)
            {
                case InputPhase.Started:
                    action.started += OnCallback;
                    break;
                case InputPhase.Performed:
                    action.performed += OnCallback;
                    break;
                case InputPhase.Canceled:
                    action.canceled += OnCallback;
                    break;
                default:
                    break;
            }
        }

        public void Unregister()
        {
            var action = ResolveAction();
            if (action == null) return;

            action.started -= OnCallback;
            action.performed -= OnCallback;
            action.canceled -= OnCallback;
        }

        private InputAction ResolveAction()
        {
            if (InputActionReference == null) return null;
            var refAction = InputActionReference.action;
            if (refAction == null) return null;

            if (inputActions != null && inputActions.asset != null)
            {
                var a = inputActions.asset.FindAction(refAction.name, true);
                if (a != null) return a;
            }

            return refAction;
        }

        private void OnCallback(UnityEngine.InputSystem.InputAction.CallbackContext context)
        {
            switch (inputActionPhase)
            {
                case InputPhase.Started:
                    if (context.phase != UnityEngine.InputSystem.InputActionPhase.Started) return;
                    Log($"{name} started!");
                    Performed?.Invoke(this);
                    break;

                case InputPhase.Performed:
                    if (context.phase != UnityEngine.InputSystem.InputActionPhase.Performed) return;
                    Log($"{name} performed!");
                    Performed?.Invoke(this);
                    break;

                case InputPhase.Canceled:
                    if (context.phase != UnityEngine.InputSystem.InputActionPhase.Canceled) return;
                    Log($"{name} canceled!");
                    Canceled?.Invoke(this);
                    break;

                case InputPhase.Pressed:
                    if (context.phase != UnityEngine.InputSystem.InputActionPhase.Started) return;
                    if (!IsPressed()) return;
                    Log($"{name} pressed!");
                    Pressed?.Invoke(this);
                    break;

                case InputPhase.Released:
                    if (context.phase != UnityEngine.InputSystem.InputActionPhase.Canceled) return;
                    if (!WasPressedThisFrame()) return;
                    Log($"{name} released!");
                    Released?.Invoke(this);
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

        //private bool IsPressed(UnityEngine.InputSystem.InputAction.CallbackContext context)
        //{
        //    var t = context.action != null ? context.action.activeControl.valueType : null;

        //    if (t == typeof(float)) return context.ReadValue<float>() > 0.5f;
        //    if (t == typeof(Vector2)) return context.ReadValue<Vector2>().sqrMagnitude > 0.0001f;
        //    if (t == typeof(Vector3)) return context.ReadValue<Vector3>().sqrMagnitude > 0.0001f;
        //    if (t == typeof(int)) return context.ReadValue<int>() != 0;

        //    return context.ReadValueAsButton();
        //}

        //private bool WasPressed(UnityEngine.InputSystem.InputAction.CallbackContext context)
        //{
        //    var t = context.action != null ? context.action.activeControl.valueType : null;

        //    if (t == typeof(float)) return context.ReadValue<float>() > 0.0001f;
        //    if (t == typeof(Vector2)) return context.ReadValue<Vector2>().sqrMagnitude > 0.0001f;
        //    if (t == typeof(Vector3)) return context.ReadValue<Vector3>().sqrMagnitude > 0.0001f;
        //    if (t == typeof(int)) return context.ReadValue<int>() != 0;

        //    return context.ReadValueAsButton();
        //}
    }
}
