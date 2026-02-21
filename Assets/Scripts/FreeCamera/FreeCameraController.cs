using MyToolz.EditorToolz;
using MyToolz.Events;
using MyToolz.Player.Input;
using MyToolz.UI.Management;
using MyToolz.Utilities.Debug;
using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MyToolz.FreeCamera
{
    [RequireComponent(typeof(Camera))]
    public class FreeCameraController : MonoBehaviour, IEventListener
    {
        [Header("Settings")]
        [SerializeField, Range(0.1f, 10f)] private float scrollSensitivity = 0.5f;
        [SerializeField, Range(0.1f, 100f)] private float minSpeed = 0.5f;
        [SerializeField, Range(0.1f, 100f)] private float maxSpeed = 50f;
        [SerializeField, Range(0, 100f)] private float moveSpeed = 10f;
        [SerializeField, Range(0, 1000f)] private float lookSensitivity = 200f;
        [SerializeField, Range(0, 100f)] private float boostMultiplier = 1f;
        [SerializeField, Range(0, 1f)] private float moveSmoothTime = 0.15f;
        [SerializeField, Range(0, 1f)] private float lookSmoothTime = 0.05f;
        [Header("Input")]
        [SerializeField, Required] private InputCommandSO toggleInputCommandSO;
        [SerializeField, Required] private InputCommandSO moveActionInputCommandSO;
        [SerializeField, Required] private InputCommandSO verticalInputCommandSO;
        [SerializeField, Required] private InputCommandSO scrollInputCommandSO;
        [SerializeField, Required] private InputCommandSO lookInputCommandSO;
        //TODO: add subclass selector
        [SerializeReference, Required] private InputMode inputMode;

        private float scrollSpeedMultiplier = 1f;
        private Vector2 moveInput;
        private Vector2 lookInput;
        private float yaw;
        private float pitch;
        private bool cameraToggled;
        private Camera freeCamera;
        private AudioListener audioListener;
        private Vector3 currentVelocity;
        private Vector3 targetPosition;
        private float targetYaw;
        private float targetPitch;
        private float yawVelocity;
        private float pitchVelocity;
        private float verticalInput;

        private void Awake()
        {
            audioListener = GetComponent<AudioListener>();
            freeCamera = GetComponent<Camera>();
        }
        private void OnEnable()
        {
            RegisterEvents();
        }
        private void OnDisable()
        {
            UnregisterEvents();
        }

        private void OnDestroy()
        {
            UnregisterEvents();
        }

        private void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
            yaw = transform.eulerAngles.y;
            pitch = transform.eulerAngles.x;
            targetYaw = yaw;
            targetPitch = pitch;
            targetPosition = transform.position;
        }

        private void OnScroll(InputCommandSO inputCommandSO)
        {
            Vector2 scrollDelta = inputCommandSO.ReadValue<Vector2>();
            if (Mathf.Abs(scrollDelta.y) > 0.01f)
            {
                scrollSpeedMultiplier += scrollDelta.y * scrollSensitivity * Time.unscaledDeltaTime;
                scrollSpeedMultiplier = Mathf.Clamp(scrollSpeedMultiplier, minSpeed / moveSpeed, maxSpeed / moveSpeed);
            }
        }

        private void OnCameraTggled(InputCommandSO inputCommandSO)
        {
            DebugUtility.Log(this, "Free camera toggled!");
            cameraToggled = !cameraToggled;
            audioListener.enabled = cameraToggled;
            if (cameraToggled) freeCamera.enabled = true;
            else freeCamera.enabled = false;
        }

        private void Update()
        {
            if (!cameraToggled) return;

            moveInput = moveActionInputCommandSO.ReadValue<Vector2>();
            lookInput = lookInputCommandSO.ReadValue<Vector2>();
            verticalInput = verticalInputCommandSO.ReadValue<float>();

            targetYaw += lookInput.x * lookSensitivity;
            targetPitch -= lookInput.y * lookSensitivity;
            targetPitch = Mathf.Clamp(targetPitch, -89f, 89f);

            yaw = Mathf.SmoothDampAngle(yaw, targetYaw, ref yawVelocity, lookSmoothTime);
            pitch = Mathf.SmoothDamp(pitch, targetPitch, ref pitchVelocity, lookSmoothTime);

            transform.eulerAngles = new Vector3(pitch, yaw, 0f);

            float currentSpeed = moveSpeed * scrollSpeedMultiplier;
            if (Keyboard.current.leftShiftKey.isPressed) currentSpeed *= boostMultiplier;

            Vector3 inputMove = new Vector3(moveInput.x, verticalInput, moveInput.y);
            Vector3 desiredMove = transform.TransformDirection(inputMove) * currentSpeed * Time.deltaTime;
            targetPosition += desiredMove;
            transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref currentVelocity, moveSmoothTime);
        }

        public void RegisterEvents()
        {
            if (toggleInputCommandSO)
            {
                toggleInputCommandSO.Performed += OnCameraTggled;
            }
            else
            {
                DebugUtility.LogError(this, "ToggleAction is missing, it is not allowed!");
            }

            if (scrollInputCommandSO)
            {
                scrollInputCommandSO.Performed += OnScroll;
            }
            else
            {
                DebugUtility.LogError(this, "ScrollAction is missing, it is not allowed!");
            }
        }

        public void UnregisterEvents()
        {
            if (toggleInputCommandSO)
            {
                toggleInputCommandSO.Performed -= OnCameraTggled;
            }
            else
            {
                DebugUtility.LogError(this, "ToggleAction is missing, it is not allowed!");
            }

            if (scrollInputCommandSO)
            {
                scrollInputCommandSO.Performed -= OnScroll;
            }
            else
            {
                DebugUtility.LogError(this, "ScrollAction is missing, it is not allowed!");
            }
        }
    }
}
