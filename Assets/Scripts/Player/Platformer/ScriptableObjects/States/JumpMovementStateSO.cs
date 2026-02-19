using MyToolz.EditorToolz;
using MyToolz.Player.Platformer.Interfaces;
using MyToolz.Player.Platformer.Movement.Interfaces;
using UnityEngine;
using Zenject;

namespace MyToolz.ScriptableObjects.Player.Platformer.Movement
{
    [CreateAssetMenu(fileName = "JumpMovementStateSO", menuName = "ProjectClyde/Player/MovementStates/JumpMovementStateSO")]
    public class JumpMovementStateSO : MovementStateSO
    {
        public float JumpHeight => jumpHeight;
        public float JumpHeightCompensationFactor => jumpHeightCompensationFactor;
        public float TimeTillJumpApex => timeTillJumpApex;
        public float GravityOnReleaseMultiplier => gravityOnReleaseMultiplier;

        [FoldoutGroup("Jump")]
        [SerializeField, Tooltip("Target jump height")] protected bool dynamicJumpHeight = false;
        [SerializeField, Range(1f, 15f), Tooltip("Target jump height")] protected float jumpHeight = 6.5f;
        [FoldoutGroup("Jump")]
        [SerializeField, Range(1f, 2f), Tooltip("Multiplier ensuring consistent jump scaling")] protected float jumpHeightCompensationFactor = 1.054f;
        [FoldoutGroup("Jump")]
        [SerializeField, Range(1f, 5f), Tooltip("Extra gravity applied when jump is released early")] protected float gravityOnReleaseMultiplier = 2f;
        [FoldoutGroup("Jump")]
        [SerializeField, Range(1, 5), Tooltip("Total jumps available before landing")] protected int numberOfJumpsAllowed = 2;

        [FoldoutGroup("Jump/Apex")]
        [SerializeField, Range(0.1f, 1f), Tooltip("Time to reach jump peak")] protected float timeTillJumpApex = 0.35f;

        [FoldoutGroup("Jump/Apex")]
        //Upward Cancel Time"
        [SerializeField, Range(0.01f, 0.2f), Tooltip("Delay before upward velocity is canceled on early release")] protected float timeForUpwardCancel = 0.027f;

        [FoldoutGroup("Jump/Apex")]
        //Apex Threshold
        [SerializeField, Range(0.5f, 1f), Tooltip("Portion of ascent where apex behavior begins")] protected float apexThreshold = 0.97f;

        [FoldoutGroup("Jump/Apex")]
        //Apex Hang Time
        [SerializeField, Range(0.01f, 0.2f), Tooltip("Duration player can “hover” at apex")] protected float apexHangTime = 0.075f;

        [FoldoutGroup("Jump/Buffer")]
        //Buffer Time
        [SerializeField, Range(0.01f, 0.3f), Tooltip("Grace period after pressing jump for input to still register")] protected float jumpBufferTime = 0.125f;

        [FoldoutGroup("Jump/Buffer")]
        //Coyote Time
        [SerializeField, Range(0.01f, 0.3f), Tooltip("Grace period after leaving ground where jump is still valid")] protected float jumpCayoteTime = 0.1f;

        public int NumberOfJumpsAllowed => numberOfJumpsAllowed;
        protected bool isJumping;
        protected bool isFastFalling;
        protected bool isFalling;
        protected float fastFallTime;
        protected float fastFallReleaseSpeed;
        protected int numberOfJumpsUsed;

        protected float apexPoint;
        protected float timePastApexThreshold;
        protected bool isPastApexThreshold;

        protected float jumpBufferTimer;
        protected bool jumpReleasedDuringBuffer;
        protected float coyoteTimer;
        protected float initialJumpVelocity => playerMovementStats.InitialJumpVelocity;

        protected bool bumpedHead => context.BumpedHead;
        protected float jumpPressedTime;
        protected IPlayerVerticalMovementView verticalView;

        protected IPlayerModel model;
        public float Gravity { get; private set; }
        public float InitialJumpVelocity { get; private set; }
        public float AdjustmentFactorHeight { get; private set; }

        [Inject]
        private void Construct(IPlayerModel model, IPlayerVerticalMovementView verticalView)
        {
            this.verticalView = verticalView;
            this.model = model;
            model.StanceItemSO.Stats.Calculate(jumpHeight, jumpHeightCompensationFactor, timeTillJumpApex);
        }

        public override void OnUpdate()
        {
            UpdateTimers();
            CheckJump();
        }

        public override void OnFixedUpdate()
        {
            base.OnFixedUpdate();
            Jump();
        }

        protected virtual void CheckJump()
        {
            if (jumpWasPressed)
            {
                jumpBufferTimer = jumpBufferTime;
                jumpReleasedDuringBuffer = false;
            }
            if (jumpWasReleased && !dynamicJumpHeight) 
            { 
                if (jumpBufferTimer > 0f)  jumpReleasedDuringBuffer = true; 

                if (isJumping && context.VerticalVelocity > 0f) 
                { 
                    if (isPastApexThreshold) 
                    { 
                        isPastApexThreshold = false;
                        isFastFalling = true; 
                        fastFallTime = timeForUpwardCancel; 
                        context.SetVerticalVelocity(0f); 
                    } 
                } 
                else 
                { 
                    isFastFalling = true; 
                    fastFallReleaseSpeed = context.VerticalVelocity; 
                } 
            }
            if (jumpWasReleased && dynamicJumpHeight)
            {
                if (jumpBufferTimer > 0f) jumpReleasedDuringBuffer = true;

                if (isJumping && context.VerticalVelocity > 0f)
                {
                    isFastFalling = true;
                    fastFallTime = 0f;
                    fastFallReleaseSpeed = context.VerticalVelocity;
                }
                else
                {
                    isFastFalling = true;
                    fastFallTime = 0f;
                    fastFallReleaseSpeed = context.VerticalVelocity;
                }
            }


            if (jumpBufferTimer > 0f && !isJumping && (isGrounded || coyoteTimer > 0f))
            {
                InitiateJump(1);
                if (jumpReleasedDuringBuffer)
                {
                    isFastFalling = true;
                    fastFallReleaseSpeed = context.VerticalVelocity;
                }
            }
            else if (jumpBufferTimer > 0f && isJumping && numberOfJumpsUsed < NumberOfJumpsAllowed)
            {
                isFastFalling = false;
                InitiateJump(1);
            }
            else if (jumpBufferTimer > 0f && isFalling && numberOfJumpsUsed < NumberOfJumpsAllowed - 1)
            {
                InitiateJump(2);
                isFastFalling = false;
            }

            if (isGrounded && context.VerticalVelocity <= 0f)
            {
                if (isJumping || isFastFalling || isFalling) verticalView.OnLanding(context.VerticalVelocity);
                isJumping = false;
                isFastFalling = false;
                isFalling = false;
                fastFallTime = 0f;
                isPastApexThreshold = false;
                numberOfJumpsUsed = 0;
                context.SetVerticalVelocity(0f);
            }
        }

        protected virtual void InitiateJump(int numberOfJumpsUsed)
        {
            if (!isJumping) isJumping = true;
            verticalView.OnJump();
            jumpBufferTimer = 0f;
            this.numberOfJumpsUsed += numberOfJumpsUsed;
            context.SetVerticalVelocity(initialJumpVelocity);
        }

        protected virtual void Jump()
        {
            if (isJumping)
            {
                if (bumpedHead) isFastFalling = true;

                if (context.VerticalVelocity >= 0f && !dynamicJumpHeight)
                {
                    apexPoint = Mathf.InverseLerp(initialJumpVelocity, 0f, context.VerticalVelocity);

                    if (apexPoint > apexThreshold)
                    {
                        if (!isPastApexThreshold)
                        {
                            isPastApexThreshold = true;
                            timePastApexThreshold = 0f;
                        }

                        timePastApexThreshold += Time.fixedDeltaTime;
                        if (timePastApexThreshold < apexHangTime)
                        {
                            context.SetGravityScale(0f);
                            context.SetVerticalVelocity(0f);
                        }
                        else
                        {
                            context.SetGravityScale(1f);
                        }
                    }
                    else
                    {
                        isPastApexThreshold = false;
                        context.SetGravityScale(1f);
                    }
                }
                else if (context.VerticalVelocity >= 0f && dynamicJumpHeight) 
                { 
                    apexPoint = Mathf.InverseLerp(initialJumpVelocity, 0f, context.VerticalVelocity); 
                    if (apexPoint > apexThreshold) 
                    { 
                        if (!isPastApexThreshold) 
                        { 
                            isPastApexThreshold = true; 
                            timePastApexThreshold = 0f; 
                        } 
                        if (isPastApexThreshold) 
                        { 
                            timePastApexThreshold += Time.fixedDeltaTime; 
                            if (timePastApexThreshold < apexHangTime) 
                            { 
                                context.SetGravityScale(0f); 
                                context.SetVerticalVelocity(0f); 
                            } 
                        } 
                    } 
                    else 
                    { 
                        isPastApexThreshold = false; 
                        context.SetGravityScale(1f); 
                    } 
                }
                else if (!isFastFalling && !dynamicJumpHeight)
                {
                    context.SetGravityScale(gravityOnReleaseMultiplier);
                }
                else if (context.VerticalVelocity < 0f)
                {
                    isFastFalling = true;
                }
            }

            if (isFastFalling)
            {
                if (fastFallTime >= timeForUpwardCancel)
                {
                    context.SetGravityScale(gravityOnReleaseMultiplier);
                }
                else
                {
                    context.SetGravityScale(0f);
                    context.SetVerticalVelocity(Mathf.Lerp(fastFallReleaseSpeed, 0f, fastFallTime / timeForUpwardCancel));
                }
                fastFallTime += Time.fixedDeltaTime;
            }

            if (!isGrounded && !isJumping)
            {
                isFalling = true;
            }

            Velocity = Vector2.zero;
        }

        protected virtual void UpdateTimers()
        {
            jumpBufferTimer -= Time.deltaTime;
            if (isGrounded) coyoteTimer = jumpCayoteTime;
            else coyoteTimer -= Time.deltaTime;
        }

        public override void ResetState()
        {
            base.ResetState();
            numberOfJumpsUsed = 0;
        }
    }
}
