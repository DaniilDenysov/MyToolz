using MyToolz.InputManagement.Commands;
using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MyToolz.FreeCamera
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public class FreeCameraController : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private FreeCameraSO settings;

        [Header("Input Actions")]
        [SerializeField] private InputCommandSO toggleAction;
        [SerializeField] private InputCommandSO moveAction;
        [SerializeField] private InputCommandSO lookAction;
        [SerializeField] private InputCommandSO verticalAction;
        [SerializeField] private InputCommandSO scrollAction;
        [SerializeField] private InputCommandSO boostAction;

        private float scrollSpeedMultiplier = 1f;
        private float yaw;
        private float pitch;
        private float targetYaw;
        private float targetPitch;
        private float yawVelocity;
        private float pitchVelocity;
        private Vector3 currentVelocity;
        private Vector3 targetPosition;
        private bool isActive;
        private Camera controlledCamera;
        private AudioListener audioListener;
        private CursorLockMode previousCursorLockMode;
        private bool previousCursorVisible;

        public bool IsActive => isActive;

        public event Action<bool> OnToggled;

        private void Awake()
        {
            controlledCamera = GetComponent<Camera>();
            audioListener = GetComponent<AudioListener>();
        }

        private void OnEnable()
        {
            SubscribeInputActions();
        }

        private void OnDisable()
        {
            UnsubscribeInputActions();

            if (isActive)
                Deactivate();
        }

        private void Start()
        {
            InitializeRotation();
        }

        private void Update()
        {
            if (!isActive)
                return;

            UpdateLook();
            UpdateMovement();
        }

        private void InitializeRotation()
        {
            Vector3 euler = transform.eulerAngles;
            yaw = euler.y;
            pitch = euler.x;

            if (pitch > 180f)
                pitch -= 360f;

            targetYaw = yaw;
            targetPitch = pitch;
            targetPosition = transform.position;
        }

        private void Activate()
        {
            isActive = true;
            controlledCamera.enabled = true;

            if (audioListener != null)
                audioListener.enabled = true;

            previousCursorLockMode = Cursor.lockState;
            previousCursorVisible = Cursor.visible;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            InitializeRotation();
            OnToggled?.Invoke(true);
        }

        private void Deactivate()
        {
            isActive = false;
            controlledCamera.enabled = false;

            if (audioListener != null)
                audioListener.enabled = false;

            Cursor.lockState = previousCursorLockMode;
            Cursor.visible = previousCursorVisible;

            OnToggled?.Invoke(false);
        }

        private void UpdateLook()
        {
            Vector2 lookInput = lookAction != null ? lookAction.ReadValue<Vector2>() : Vector2.zero;

            targetYaw += lookInput.x * settings.LookSensitivity * Time.unscaledDeltaTime;
            targetPitch -= lookInput.y * settings.LookSensitivity * Time.unscaledDeltaTime;
            targetPitch = Mathf.Clamp(targetPitch, -89f, 89f);

            yaw = Mathf.SmoothDampAngle(yaw, targetYaw, ref yawVelocity, settings.LookSmoothTime);
            pitch = Mathf.SmoothDamp(pitch, targetPitch, ref pitchVelocity, settings.LookSmoothTime);

            transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        }

        private void UpdateMovement()
        {
            Vector2 moveInput = moveAction != null ? moveAction.ReadValue<Vector2>() : Vector2.zero;
            float verticalInput = verticalAction != null ? verticalAction.ReadValue<float>() : 0f;
            bool isBoosting = boostAction != null && boostAction.IsPressed();

            float currentSpeed = settings.MoveSpeed * scrollSpeedMultiplier;

            if (isBoosting)
                currentSpeed *= settings.BoostMultiplier;

            Vector3 direction = new Vector3(moveInput.x, verticalInput, moveInput.y);
            Vector3 worldMove = transform.TransformDirection(direction) * currentSpeed * Time.unscaledDeltaTime;

            targetPosition += worldMove;
            transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref currentVelocity, settings.MoveSmoothTime);
        }

        private void OnTogglePerformed()
        {
            if (isActive)
                Deactivate();
            else
                Activate();
        }

        private void OnScrollPerformed(InputCommandSO inputCommandSO)
        {
            if (!isActive)
                return;

            Vector2 scrollDelta = inputCommandSO.ReadValue<Vector2>();

            if (Mathf.Abs(scrollDelta.y) < 0.01f)
                return;

            scrollSpeedMultiplier += scrollDelta.y * settings.ScrollSensitivity * Time.unscaledDeltaTime;
            scrollSpeedMultiplier = Mathf.Clamp(
                scrollSpeedMultiplier,
                settings.MinSpeed / settings.MoveSpeed,
                settings.MaxSpeed / settings.MoveSpeed
            );
        }

        private void SubscribeInputActions()
        {
            if (toggleAction != null && toggleAction != null)
            {
                toggleAction.OnPerformed += OnTogglePerformed;
            }

            if (scrollAction != null && scrollAction != null)
            {
                scrollAction.OnInputPerformed += OnScrollPerformed;
            }
        }

        private void UnsubscribeInputActions()
        {
            if (toggleAction != null && toggleAction != null)
                toggleAction.OnPerformed -= OnTogglePerformed;

            if (scrollAction != null && scrollAction != null)
                scrollAction.OnInputPerformed -= OnScrollPerformed;
        }

        public void SetSettings(FreeCameraSO newSettings)
        {
            settings = newSettings;
        }

        public void ResetSpeed()
        {
            scrollSpeedMultiplier = 1f;
        }

        public void TeleportTo(Vector3 position, Quaternion rotation)
        {
            transform.SetPositionAndRotation(position, rotation);
            targetPosition = position;
            currentVelocity = Vector3.zero;
            InitializeRotation();
        }
    }
}
