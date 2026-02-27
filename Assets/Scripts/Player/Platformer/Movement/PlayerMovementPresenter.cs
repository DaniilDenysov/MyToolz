using MyToolz.DesignPatterns.EventBus;
using MyToolz.DesignPatterns.StateMachine.SimplePriorityBased;
using MyToolz.EditorToolz;
using MyToolz.HealthSystem.Interfaces;
using MyToolz.InputManagement.Commands;
using MyToolz.Player.Platformer.Interfaces;
using MyToolz.Player.Platformer.Movement.Interfaces;
using MyToolz.ScriptableObjects.Inventory;
using MyToolz.ScriptableObjects.Player.Platformer.Movement;
using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Zenject;

namespace MyToolz.Player.Platformer.Interfaces
{
    public interface IKnockOffable { void KnockOff(Vector2 forceDirection); }
}

namespace MyToolz.Player.Platformer.Movement.Interfaces
{
    public interface IPlayerMovementPresenter
    {
        public Vector2 CharacterDirection { get; }
        public void ResetMovement();
    }
    public interface IFixedUpdatable { void OnFixedUpdate(); }
    public interface IUpdatable { void OnUpdate(); }

}

namespace MyToolz.Player.Platformer.Events
{
    public struct PlayerGrounded : IEvent
    {

    }
}

namespace MyToolz.Player.Platformer.Movement.Presenter
{

    [RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D))]
    public class PlayerMovementPresenter : MonoBehaviour, IKnockOffable, IPriorityState, IPlayerMovementPresenter
    {
        [SerializeField, Range(0, 100)] protected uint priority = 100;
        [FoldoutGroup("References"), SerializeField, Required] private SpriteRenderer spriteRenderer;
        [SerializeField] private InputCommandSO jump;
        [SerializeField] private InputCommandSO run;
        [SerializeField] private InputCommandSO walk;
        [SerializeField] private InputCommandSO dash;
        private PlayerMovementStats stats => movementModel.Stats;

        private new Rigidbody2D rigidbody2D;
        public SpriteRenderer SpriteRenderer => spriteRenderer;
        protected StanceItemSO stance;
        private CapsuleCollider2D bodyCollider;
        private BoxCollider2D boxCollider;

        //TODO: find generic solution, for example create middle man which will handle input
        private DefaultInputActions inputActions;
        public float JumpInput { get; private set; }
        public bool JumpWasPressed { get; private set; }
        public bool JumpIsHeld { get; private set; }
        public bool JumpWasReleased { get; private set; }
        public bool RunIsHeld { get; private set; }
        public Vector2 MovementInput { get; private set; }
        public bool DashWasPressed { get; private set; }
        public Vector2 CharacterDirection
        {
            get
            {
                float x = MovementInput.x;
                if (x != 0f)
                {
                    spriteRenderer.flipX = x < 0f;
                }
                return spriteRenderer.flipX ? Vector2.left : Vector2.right;
            }
        }

        protected Vector2 moveVelocity { set => movementModel.SetVelocity(value); get => movementModel.Velocity; }
        protected Vector2 externalVelocity { set => movementModel.SetExternalVelocity(value); get => movementModel.ExternalVelocity; }

        public bool IsGrounded
        {
            get { return movementModel.IsGrounded; }
            private set 
            { 
                movementModel.SetIsGrounded(value);
            }
        }

        private RaycastHit2D groundHit;
        private RaycastHit2D headHit;

        public float VerticalVelocity { get; private set; }
        public bool BumpedHead { get; private set; }

        public uint Priority => priority;

        private float gravityScale = 1f;

        private IPlayerMovementModel movementModel;
        private IPlayerModel model;
        private DiContainer container;

        [Inject]
        private void Construct(IPlayerModel model,IPlayerMovementModel movementModel, DiContainer container, DefaultInputActions inputActions)
        {
            this.model = model;
            this.movementModel = movementModel;
            this.container = container;
            this.inputActions = inputActions;
        }

        private void Awake()
        {
            rigidbody2D = GetComponent<Rigidbody2D>();
            bodyCollider = GetComponent<CapsuleCollider2D>();
            boxCollider = GetComponent<BoxCollider2D>();
        }

        private void Start()
        {
            movementModel.SetRigidbody(rigidbody2D);
            InitializeStances();
            model.ModelUpdate += OnStanceSwithced;
        }

        private void OnStanceSwithced(StanceItemSO sO1, StanceItemSO sO2)
        {
            InitializeStances();
        }

        private void OnDestroy()
        {
            model.ModelUpdate -= OnStanceSwithced;
        }

        private void InitializeStances()
        {
            if (stance == model.StanceItemSO) return;
            stance = model.StanceItemSO;
            var s = stance;
            foreach (var state in s.MovementStates)
            {
                container.Inject(state);
                state.OnEnter();
            }
        }

        private void UpdateInput()
        {
            JumpWasPressed = jump.WasPerformedThisFrame();
            JumpInput = jump.ReadValue<float>();
            JumpIsHeld = jump.IsPressed();
            JumpWasReleased = jump.WasReleasedThisFrame();
            RunIsHeld = run.IsPressed();
            var raw = walk.ReadValue<Vector2>();
            float x = raw.x;
            MovementInput = new Vector2(x == 0f ? 0f : Mathf.Sign(x), 0f);
            DashWasPressed = dash.IsPressed();
            //if (JumpWasPressed || JumpIsHeld || JumpWasReleased || RunIsHeld || MovementInput.magnitude > 0 || DashWasPressed)
            //{
            //    playerStateMachine.ChangeState(this);
            //}
        }

        protected IPlayerHorizontalMovementView horizontalView;
        protected IPlayerVerticalMovementView verticalView;
        protected IHealthModel healthModel;

        [Inject]
        private void Construct(IHealthModel healthModel, IPlayerHorizontalMovementView view, IPlayerVerticalMovementView verticalView)
        {
            this.healthModel = healthModel;
            this.horizontalView = view;
            this.verticalView = verticalView;
        }

        private void Update()
        {
            if (stance == null) return;
            var s = stance;
            UpdateInput();
            foreach (var state in s.MovementStates) state.OnUpdate();
        }

        private void FixedUpdate()
        {
            InitializeStances();
            if (stance == null) return;
            CheckCollisions();

            float lerp = (IsGrounded ? stats.GroundDeceleration : stats.AirDeceleration) * Time.fixedDeltaTime;

            //if (!healthModel.IsDead)
            //{
                gravityScale = 1f;

                var s = stance;
                float x = 0f;
                float max = 0f;
                foreach (var state in s.MovementStates)
                {
                    state.OnFixedUpdate();
                    if (Mathf.Abs(state.Velocity.x) > Mathf.Abs(max))
                    {
                        max = state.Velocity.x;
                    }
                }
                x = max;
                if (BumpedHead && VerticalVelocity > 0f) VerticalVelocity = 0f;

                if (IsGrounded)
                {
                    if (VerticalVelocity <= 0f) VerticalVelocity = 0f;
                }
                else
                {
                    VerticalVelocity += stats.Gravity * gravityScale * Time.fixedDeltaTime;
                    if (VerticalVelocity < -stats.MaxFallSpeed) VerticalVelocity = -stats.MaxFallSpeed;
                }

                moveVelocity = new Vector2(x, VerticalVelocity);
            //}
            //else
            //{
            //    moveVelocity = Vector2.Lerp(moveVelocity, Vector2.zero, lerp);
            //}

            externalVelocity = Vector2.Lerp(externalVelocity, Vector2.zero, lerp);
            rigidbody2D.velocity = moveVelocity + externalVelocity;

        }

        private void CheckCollisions()
        {
            bool wasGrounded = IsGrounded;
            IsGrounded = CheckGround();
            if (!wasGrounded && IsGrounded)
            {
                ResetMovement();
            }
            BumpedHead = CheckBumpedHead();
        }

        public void ResetMovement()
        {
            int len = model.StanceItemSO.MovementStates.Length;
            for (int i = 0; i < len; i++)
            {
                model.StanceItemSO.MovementStates[i].ResetState();
            }
        }

        private bool CheckGround()
        {
            var origin = new Vector2(boxCollider.bounds.center.x, boxCollider.bounds.min.y);
            var boxCastSize = new Vector2(boxCollider.bounds.size.x, stats.GroundDetectionRayLength);
            groundHit = Physics2D.BoxCast(origin, boxCastSize, 0f, Vector2.down, stats.GroundDetectionRayLength, stats.GroundLayer);
            return groundHit.collider != null;
        }

        private bool CheckBumpedHead()
        {
            var origin = new Vector2(boxCollider.bounds.center.x, bodyCollider.bounds.max.y);
            var boxCastSize = new Vector2(boxCollider.bounds.size.x, stats.HeadDetectionRayLength);
            headHit = Physics2D.BoxCast(origin, boxCastSize, 0f, Vector2.up, stats.HeadDetectionRayLength, stats.GroundLayer);
            return headHit.collider != null;
        }


        public void KnockOff(Vector2 forceDirection) 
        {
            externalVelocity += forceDirection; 
        }

        public void SetVerticalVelocity(float value) { VerticalVelocity = value; }
        public void AddVerticalVelocity(float delta) { VerticalVelocity += delta; }
        public void SetGravityScale(float scale) { gravityScale = scale; }

        public void Initialize()
        {
         
        }

        public void OnEnter()
        {
            
        }

        public void OnExit()
        {
            moveVelocity = Vector2.zero;
        }
    }
}
