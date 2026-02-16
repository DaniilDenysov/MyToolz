using MyToolz.DesignPatterns.StateMachine;
using MyToolz.Player.Platformer.Interfaces;
using MyToolz.Player.Platformer.Movement.Interfaces;
using MyToolz.Player.Platformer.Movement.Presenter;
using ProjectClyde.Player;
using UnityEngine;
using Zenject;

namespace MyToolz.ScriptableObjects.Player.Platformer.Movement
{
    public abstract class HorizontalMovementSO : MovementStateSO
    {
        protected SpriteRenderer spriteRenderer => context.SpriteRenderer;

        public override void OnFixedUpdate()
        {
            float acceleration = isGrounded ? playerMovementStats.GroundAcceleration : playerMovementStats.AirAcceleration;
            float deceleration = isGrounded ? playerMovementStats.GroundDeceleration : playerMovementStats.AirDeceleration;
            CalculateMovement(acceleration, deceleration, movementInput);
            Flip();
        }

        protected void Flip()
        {
            if (movementInput.x > 0.01f) spriteRenderer.flipX = false;
            else if (movementInput.x < -0.01f) spriteRenderer.flipX = true;
        }

        public abstract void CalculateMovement(float acceleration, float decelleration, Vector2 moveInput);
    }

    public abstract class MovementStateSO : ScriptableObject, IState, IFixedUpdatable, IUpdatable
    {
        protected float jumpInput  => context.JumpInput;
        protected bool jumpWasPressed => context.JumpWasPressed;
        protected bool jumpIsHeld => context.JumpIsHeld;
        protected bool jumpWasReleased => context.JumpWasReleased;
        protected bool runIsHeld => context.RunIsHeld;
        protected Vector2 movementInput => context.MovementInput;
        protected bool dashWasPressed => context.DashWasPressed;
        protected bool isGrounded => context.IsGrounded;
        protected PlayerMovementPresenter context;
        protected PlayerMovementStats playerMovementStats => playerModel?.StanceItemSO?.Stats;
        protected IReadOnlyPlayerMovementModel playerMovementModel;
        protected IReadOnlyPlayerModel playerModel;

        public Vector2 Velocity { get; protected set; }


        [Inject]
        public virtual void Construct(IReadOnlyPlayerModel playerModel,IReadOnlyPlayerMovementModel playerMovementModel ,PlayerMovementPresenter context)
        {
            this.playerModel = playerModel;
            this.playerMovementModel = playerMovementModel;
            this.context = context;
        }

        public virtual void OnFixedUpdate()
        {

        }


        public virtual void OnEnter()
        {

        }

        public virtual void OnExit()
        {

        }

        public virtual void OnUpdate()
        {
           
        }

        public virtual void ResetState()
        {

        }
    }
}
