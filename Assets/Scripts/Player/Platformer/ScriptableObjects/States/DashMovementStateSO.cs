using DG.Tweening;
using DG.Tweening.Core.Easing;
using MyToolz.EditorToolz;
using MyToolz.HealthSystem.Interfaces;
using MyToolz.Player.Platformer.Interfaces;
using MyToolz.Player.Platformer.Movement.Interfaces;
using UnityEngine;
using Zenject;

namespace MyToolz.Player.Platformer.Interfaces
{
    public interface IPlayerDashView
    {
        public void DashCoolDown(float dashed, float duration);
        public void DashStarted(Vector2 dir);
        public void Dashing(Vector2 dir);
        public void EndDashing(Vector2 dir);
    }
}

namespace MyToolz.ScriptableObjects.Player.Platformer.Movement
{

    [CreateAssetMenu(fileName = "DashMovementStateSO", menuName = "ProjectClyde/Player/MovementStates/DashMovementStateSO")]
    public class DashMovementStateSO : HorizontalMovementSO
    {
        [FoldoutGroup("Dash"), SerializeField, Tooltip("Initial horizontal dash force")] private float dashForce = 300f;
        [FoldoutGroup("Dash"), SerializeField, Tooltip("Curve controlling dash force falloff over time")] private Ease forceMultiplierCurve = Ease.InOutSine;
        [FoldoutGroup("Dash"), SerializeField, Range(0.1f, 10f), Tooltip("Minimum time before next dash")] private float dashTimeCoolDown = 5f;
        [FoldoutGroup("Dash"), SerializeField, Range(0.1f, 5f), Tooltip("Duration dash force applies")] private float dashDuration = 2f;
        [FoldoutGroup("Dash"), SerializeField] private bool dashGrounded = false;
        [FoldoutGroup("Dash"), SerializeField] private bool dashUseGravity = true;
        [FoldoutGroup("Dash"), SerializeField, HideIf("@dashUseGravity"), Range(0f,1f)] private float gravityEnablingCompletionPercentage = 0.9f;
        [FoldoutGroup("Dash"), SerializeField] private bool invincibility = false;

        private float currentDashAcceleration = 0f;
        private float lastTimeDashPressed;
        private float dashDirection = 0f;
        private IPlayerDashView dashView;
        private IReadOnlyPlayerMovementModel model;
        private IHealthModel healthModel;
        private IPlayerMovementPresenter playerMovementPresenter;
        private bool dashing;
        private float elapsedTime;

        [Inject]
        private void Construct(IPlayerMovementPresenter playerMovementPresenter,IHealthModel healthModel, IReadOnlyPlayerMovementModel model, IPlayerDashView dashView)
        {
            this.playerMovementPresenter = playerMovementPresenter;
            this.healthModel = healthModel;
            this.dashView = dashView;
            lastTimeDashPressed = float.MinValue;
            this.model = model;
        }

        public override void OnEnter()
        {
            base.OnEnter();
            model.Rigidbody.constraints = RigidbodyConstraints2D.None;
            model.Rigidbody.constraints = RigidbodyConstraints2D.FreezeRotation;
        }

        public override void OnExit()
        {
            base.OnExit();
            model.Rigidbody.constraints = RigidbodyConstraints2D.None;
            model.Rigidbody.constraints = RigidbodyConstraints2D.FreezeRotation;
        }

        public override void CalculateMovement(float acceleration, float decelleration, Vector2 moveInput)
        {
            var now = Time.fixedTime;
            decelleration = playerModel?.StanceItemSO?.Stats?.AirDeceleration ?? decelleration;
            if (dashGrounded && !isGrounded) return;

            if (dashWasPressed && now >= lastTimeDashPressed + dashTimeCoolDown && !dashing)
            {
                dashDirection = playerMovementPresenter.CharacterDirection.x > 0 ? 1 : -1f;
                dashView.DashStarted(Vector2.right * dashDirection);
                //EventBus<PoolRequest<DynamicFeedback>>.Raise(new PoolRequest<DynamicFeedback>()
                //{
                //    Prefab = dashFeedback,
                //    Callback = (d) => { d.PlayFeedbacks(); }
                //});
                lastTimeDashPressed = now;
                dashing = true;
                //if (invincibility && isGrounded) healthModel.IsInvincible = true;
                if (!dashUseGravity) model.Rigidbody.constraints = RigidbodyConstraints2D.FreezePositionY | RigidbodyConstraints2D.FreezeRotation;
            }
            else
            {
                dashView.DashCoolDown(lastTimeDashPressed, dashTimeCoolDown);
            }

            var isDashWindow = dashing && now < lastTimeDashPressed + dashDuration; //&& !attackWasPressed;

            float t = Mathf.Clamp01((now - lastTimeDashPressed) / dashDuration);
            float mult = isDashWindow ? 1 - EvaluateEase(forceMultiplierCurve, now - lastTimeDashPressed, dashDuration) : 0f;
            currentDashAcceleration = dashDirection * (dashForce * mult);

            if (t >= gravityEnablingCompletionPercentage)
            {
                model.Rigidbody.constraints = RigidbodyConstraints2D.None;
                model.Rigidbody.constraints = RigidbodyConstraints2D.FreezeRotation;
            }
            if (!isDashWindow && dashing)
            {
                dashing = false;
                //if (invincibility) healthModel.IsInvincible = false;
                model.Rigidbody.constraints = RigidbodyConstraints2D.None;
                model.Rigidbody.constraints = RigidbodyConstraints2D.FreezeRotation;
                currentDashAcceleration = 0f;
            }   

            float targetVelocityX = currentDashAcceleration;
            float lerpTime = decelleration * Time.fixedDeltaTime;
            Velocity = dashing ? new Vector2(Mathf.Lerp(Velocity.x, targetVelocityX, lerpTime), Velocity.y) : Vector2.zero;
        }

        public float EvaluateEase(Ease ease, float time, float duration)
        {
            return EaseManager.Evaluate(ease, null, time, duration, 0f, 0f);
        }


        public override void ResetState()
        {
            base.ResetState();
            lastTimeDashPressed = float.MinValue;
        }

    }
}