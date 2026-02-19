using MyToolz.EditorToolz;
using MyToolz.HealthSystem.Interfaces;
using MyToolz.ScriptableObjects.AI.Platformer;
using MyToolz.Utilities.Debug;
using UnityEngine;
using Zenject;

namespace MyToolz.AI.Platformer.Presenters
{
    public interface IReadOnlyEnemyMovementModel
    {
        float CurrentFriction { get; }
        bool IsGrounded { get; }
        Vector2 Velocity { get; }
        Vector2 Direction { get; }
        Vector2 ExternalVelocity { get; }
        Rigidbody2D Rigidbody2D { get; }
    }

    //TODO: refactor interface to a smaller once
    public interface IEnemyMovementPresenter
    {
        public bool IsActive { get; set; }
        public void ApplyFlip(Vector2 direction);
        public void Jump();
        public void Do(EnemyMovementState enemyMovementState);
        public bool CanMoveDirection(Vector2 direction);
        public bool ShouldStopAtEdge(float desiredX);
    }

    [System.Serializable]
    public abstract class EnemyMovementState
    {
        public Vector2 ExternalVelocity => externalVelocity;
        protected Vector2 externalVelocity;

        public Vector2 Velocity => velocity;
        protected Vector2 velocity;
        protected LayerMask wallLayerMask => enemyMovementSO.WallLayerMask;
        protected Transform context => model.Context;
        protected Transform unitContext => model.UnitContext;
        protected Vector2 unitContextVec => unitContext?.position ?? Vector2.zero;
        protected bool isGrounded => movementModel.IsGrounded;
        protected EnemyMovementSO enemyMovementSO => model.EnemyMovementSO;
        protected IReadOnlyEnemyMovementModel movementModel;
        protected IReadOnlyEnemyModel model;
        protected IHealthModel healthModel;
        protected IEnemyMovementPresenter enemyMovementPresenter;
        protected float fixedDeltaTime => Time.fixedDeltaTime;

        [Inject]
        private void Construct(IEnemyMovementPresenter enemyMovementPresenter, IReadOnlyEnemyMovementModel movementModel, IReadOnlyEnemyModel model, IHealthModel healthModel)
        {
            this.enemyMovementPresenter = enemyMovementPresenter;
            this.movementModel = movementModel;
            this.model = model;
            this.healthModel = healthModel;
        }

        public virtual bool IsFinsihed()
        {
            return enemyMovementSO == null;
        }

        public virtual void OnEnter()
        {

        }

        public virtual void OnExit()
        {

        }

        protected bool ShouldStopAtEdge(float desiredX) => enemyMovementPresenter.ShouldStopAtEdge(desiredX);
        protected bool CanMoveDirection(Vector2 direction) => enemyMovementPresenter.CanMoveDirection(direction);

        public virtual void Update()
        {
            velocity = Vector2.Lerp(velocity, Vector2.zero, movementModel.CurrentFriction * fixedDeltaTime);
        }
    }

    [System.Serializable]
    public class MoveToEnemyMovementState : GroundMovementState
    {
        private Vector2 contextVec => unitContextVec;
        private Vector2 destination;
        private Vector2 targetVelocity;
        private bool reached;

        public MoveToEnemyMovementState(Vector2 destination)
        {
            Initialize(destination);
        }

        public MoveToEnemyMovementState()
        {

        }

        public void Initialize(Vector2 destination)
        {
            this.destination = destination;
        }

        public override void OnEnter()
        {
            base.OnEnter();
            reached = false;
            targetVelocity = Vector2.zero;
            velocity = Vector2.zero;
        }

        public override bool IsFinsihed()
        {
            var toTarget = destination == Vector2.zero ? Vector2.zero : (destination - contextVec);
            var dist = toTarget.magnitude;

            var desiredDir = reached || destination == Vector2.zero ? Vector2.zero : (dist <= 0.0001f ? Vector2.zero : toTarget / dist);

            var doEdgeCheck = useGravity && enemyMovementSO.StopAtEdges;
            var atEdge = doEdgeCheck ? ShouldStopAtEdge(desiredDir.x) : false;

            return (enemyMovementSO.StopAtEdges && atEdge) || !CanMoveDirection(desiredDir) || reached;
        }

        public override void Update()
        {
            var current = velocity;

            var toTarget = destination == Vector2.zero ? Vector2.zero : (destination - contextVec);
            var dist = toTarget.magnitude;

            if (dist <= arriveThreshold || toTarget == Vector2.zero)
            {
                reached = true;
                targetVelocity = Vector2.zero;
                velocity = Vector2.zero;
                return;
            }

            var desiredDir = reached || destination == Vector2.zero ? Vector2.zero : (dist <= 0.0001f ? Vector2.zero : toTarget / dist);

            var doEdgeCheck = useGravity && enemyMovementSO.StopAtEdges;
            var atEdge = doEdgeCheck ? ShouldStopAtEdge(desiredDir.x) : false;

            if (atEdge) desiredDir = -desiredDir;

            targetVelocity = desiredDir * enemyMovementSO.MaxSpeed;

            current.x = targetVelocity.x;

            if (reached && Mathf.Abs(current.x) <= 0.01f)
            {
                current.x = 0f;
            }


            velocity = current;
            if (current.x != 0f) enemyMovementPresenter.ApplyFlip(current);
        }
    }

    [System.Serializable]
    public abstract class GroundMovementState : EnemyMovementState
    {
        protected float arriveThreshold => enemyMovementSO.ArriveThreshold;
        protected bool useGravity => enemyMovementSO.UseGravity;
    }

    [System.Serializable]
    public class AlwaysFacePLayerEnemyMovementState : GroundMovementState
    {
        private Transform target;
        private bool reached;
        private Vector2 targetVelocity;

        public AlwaysFacePLayerEnemyMovementState(Transform target)
        {
            Initialize(target);
        }

        public AlwaysFacePLayerEnemyMovementState()
        {

        }

        public void Initialize(Transform target)
        {
            if (target == null) return;
            this.target = target;
        }

        public override bool IsFinsihed()
        {
            return false;
        }

        public override void Update()
        {
            var destination = target == null ? Vector2.zero : new Vector2(target.transform.position.x, target.transform.position.y);
            var toTarget = destination == Vector2.zero ? Vector2.zero : (destination - unitContextVec);
            enemyMovementPresenter.ApplyFlip(toTarget.normalized);
        }
    }

    [System.Serializable]
    public class FollowEnemyMovementState : GroundMovementState
    {
        private Transform target;
        private bool reached;
        private Vector2 targetVelocity;

        public FollowEnemyMovementState(Transform target)
        {
            Initialize(target);
        }

        public FollowEnemyMovementState()
        {

        }

        public void Initialize(Transform target)
        {
            if (target == null) return;
            this.target = target;
        }

        public override void OnEnter()
        {
            base.OnEnter();
            reached = false;
        }

        public override void OnExit()
        {
            base.OnExit();
            reached = false;
            velocity = Vector2.zero;
        }

        public override bool IsFinsihed()
        {
            return base.IsFinsihed() || target == null || reached;
        }

        public override void Update()
        {
            var current = velocity;
            var destination = target == null ? Vector2.zero :  new Vector2(target.transform.position.x, target.transform.position.y);
            var toTarget = destination == Vector2.zero ? Vector2.zero : (destination - unitContextVec);
            var dist = toTarget.magnitude;

            if (dist <= arriveThreshold || toTarget == Vector2.zero)
            {
                reached = true;
                targetVelocity = Vector2.zero;
                velocity = Vector2.zero;
                return;
            }

            var desiredDir = reached || destination == Vector2.zero ? Vector2.zero : (dist <= 0.0001f ? Vector2.zero : toTarget / dist);

            var doEdgeCheck = useGravity && enemyMovementSO.StopAtEdges;
            var atEdge = doEdgeCheck ? ShouldStopAtEdge(desiredDir.x) : false;

            if (atEdge) desiredDir = -desiredDir;

            targetVelocity = desiredDir * enemyMovementSO.MaxSpeed;

            current.x = targetVelocity.x;

            if (reached && Mathf.Abs(current.x) <= 0.01f)
            {
                current.x = 0f;
            }


            velocity = current;
            if (current.x != 0f) enemyMovementPresenter.ApplyFlip(current);
        }
    }

    [System.Serializable]
    public class RearBackEnemyMovementState : GroundMovementState
    {
        private Vector2 destination;
        private Vector2 targetVelocity;
        private bool reached;
        private float duration;
        private float speed;
        private Vector2 direction;
        public bool IsRearing => elapsed < duration && !reached;
        private float elapsed;

        public RearBackEnemyMovementState()
        {
        }

        public void Initialize(Vector2 direction, float duration, float speed)
        {
            this.direction = direction;
            this.duration = Mathf.Max(0f, duration);
            this.speed = Mathf.Max(0f, speed);
        }

        public override void OnEnter()
        {
            base.OnEnter();

            elapsed = 0f;
            reached = false;
            targetVelocity = Vector2.zero;

            var dir = direction;
            dir.y = 0f;

            if (dir.sqrMagnitude > 0.0001f) dir.Normalize();
            else dir = Vector2.zero;

            enemyMovementPresenter.ApplyFlip(-dir);

            var distance = speed * duration;
            destination = unitContextVec + dir * distance;
        }

        public override void OnExit()
        {
            base.OnExit();
            reached = false;
            targetVelocity = Vector2.zero;
        }

        public override bool IsFinsihed()
        {
            var toTarget = destination - unitContextVec;
            var dist = toTarget.magnitude;

            var desiredDir = reached ? Vector2.zero : (dist <= 0.0001f ? Vector2.zero : toTarget / dist);

            var doEdgeCheck = useGravity && enemyMovementSO.StopAtEdges;
            var atEdge = doEdgeCheck ? ShouldStopAtEdge(desiredDir.x) : false;

            return base.IsFinsihed() || atEdge || !CanMoveDirection(desiredDir) || reached;
        }

        public override void Update()
        {
            elapsed += Time.fixedDeltaTime;
            var current = velocity;

            var toTarget = destination - unitContextVec;
            var dist = toTarget.magnitude;

            if (dist <= arriveThreshold || elapsed >= duration)
            {
                reached = true;
                targetVelocity = Vector2.zero;
            }

            var desiredDir = reached ? Vector2.zero : (dist <= 0.0001f ? Vector2.zero : toTarget.normalized);

            var maxSpeed = speed > 0f ? speed : enemyMovementSO.MaxSpeed;
            targetVelocity = desiredDir * maxSpeed;
            current.x = targetVelocity.x;

            if (reached && Mathf.Abs(current.x) <= 0.01f)
            {
                current.x = 0f;
            }

            velocity = current;
        }
    }


    [System.Serializable]
    public class ChargeEnemyMovementState : EnemyMovementState
    {
        public Vector2 Direction => direction;
        public bool IsCharging => chargeTimer >= 0f && chargeTimer < duration;
        private bool flipDuringCharge => (enemyMovementSO?.EnableCharge ?? false) && (enemyMovementSO?.FlipDuringCharge ?? false);
        private float chargeTimer = float.MinValue;
        private float speed;
        private float duration;
        private Vector2 direction;

        public ChargeEnemyMovementState()
        {
        }

        public void Initialize(Vector2 direction, float duration, float speed)
        {
            direction.y = 0f;

            if (direction.sqrMagnitude > 0.0001f) direction.Normalize();
            else direction = Vector2.zero;

            this.direction = direction;
            this.duration = Mathf.Max(0f, duration);
            this.speed = Mathf.Max(0f, speed);
            chargeTimer = 0f;
        }

        public ChargeEnemyMovementState(Vector2 direction, float duration, float speed)
        {
            Initialize(direction, duration, speed);
        }

        public override void OnEnter()
        {
            base.OnEnter();
            chargeTimer = 0f;
        }

        public override void OnExit()
        {
            base.OnExit();
            chargeTimer = duration;
            velocity = Vector2.zero;
        }

        public override void Update()
        {
            var res = 1f;
            velocity = direction * (speed * res);
            chargeTimer += Time.deltaTime;
            if (flipDuringCharge) enemyMovementPresenter.ApplyFlip(direction);
        }

        public void Stop()
        {
            chargeTimer = duration;
            velocity = Vector2.zero;
        }

        public override bool IsFinsihed()
        {
            return (chargeTimer >= duration || !CanMoveDirection(direction));
        }
    }


    [RequireComponent(typeof(Rigidbody2D))]
    public class EnemyMovementPresenter : MonoBehaviour, IReadOnlyEnemyMovementModel, IEnemyMovementPresenter
    {
        [FoldoutGroup("References"), Required, SerializeField] private Transform groundCheck;
        [FoldoutGroup("References"), Required, SerializeField] private Transform leftCheck;
        [FoldoutGroup("References"), Required, SerializeField] private Transform rightCheck;

        [FoldoutGroup("References"), ShowInInspector, ReadOnly] private Rigidbody2D rb;
        [FoldoutGroup("Runtime"), ShowInInspector, ReadOnly] private bool locked;
        [FoldoutGroup("Runtime"), ShowInInspector, ReadOnly] private Vector2 targetVelocity;
        [FoldoutGroup("Runtime"), ShowInInspector, ReadOnly] private Vector2 externalVelocity;
        [FoldoutGroup("Runtime"), ShowInInspector, ReadOnly] private bool isGrounded;

        [FoldoutGroup("Runtime"), ShowInInspector, ReadOnly] private bool isActive;

        public float CurrentFriction => isGrounded ? enemyMovementSO.GroundFriction : enemyMovementSO.AirFriction;
        private EnemyMovementSO enemyMovementSO => enemyModel?.EnemyMovementSO;
        private bool enableGroundCheck => enemyModel?.EnemyMovementSO.UseGravity ?? true;
        public bool IsGrounded => isGrounded;
        public Vector2 Velocity => rb != null ? rb.velocity : targetVelocity;
        public Vector2 ExternalVelocity => externalVelocity;
        public Vector2 Direction => isFlipped ? Vector2.left : Vector2.right;
        private bool isFlipped => transform.rotation.y != 0;
      
        public bool IsActive { get => isActive; set => isActive = value; }

        protected IHealthModel healthModel;
        protected IEnemyModel enemyModel;

        public Rigidbody2D Rigidbody2D => rb;

        [FoldoutGroup("Runtime"), SerializeReference, ReadOnly] protected EnemyMovementState enemyMovementState;

        protected DiContainer container;

        [Inject]
        private void Construct(DiContainer container, IHealthModel model, IEnemyModel enemyModel)
        {
            this.container = container;
            healthModel = model;
            this.enemyModel = enemyModel;
        }

        private void Awake()
        {
            IsActive = true;
            rb = GetComponent<Rigidbody2D>();
            if (enemyMovementSO == null) DebugUtility.LogError(this, name + ": EnemyMovementSO is missing.");
        }

        private void FixedUpdate()
        {
            if (enemyMovementSO == null) return;

            rb.gravityScale = 0f;

            var useGravity = enemyMovementSO.UseGravity;
            var dt = Time.fixedDeltaTime;
            var current = rb.velocity;
            var isDead = false; // healthModel.IsDead;
            isGrounded = Physics2D.OverlapCircle(groundCheck.position, enemyMovementSO.GroundProbeRadius, enemyMovementSO.GroundLayer);

            if (!locked)
            {
                var canMove = (useGravity ? isGrounded : true) && isActive && !isDead;
                if (canMove)
                {
                    if (enemyMovementState?.IsFinsihed() ?? false) ChangeState(null);
                    enemyMovementState?.Update();
                    current = enemyMovementState?.Velocity ?? Vector2.zero;
                }
                else
                {
                    current.x = 0f;
                }
            }

            if (isDead) externalVelocity = Vector2.zero;

            if (useGravity || isDead)
            {
                const float gravityAccel = -50f;
                current.y += gravityAccel * dt;
                var maxFall = Mathf.Abs(enemyMovementSO.MaxFallSpeed);
                if (current.y < -maxFall) current.y = -maxFall;
                if (isGrounded && current.y < 0f) current.y = 0f;
            }

            externalVelocity = Vector2.Lerp(externalVelocity, Vector2.zero, CurrentFriction * dt);
            current += externalVelocity;

            rb.velocity = current;
        }

        public void ApplyFlip(Vector2 direction)
        {
            var x = direction != Vector2.zero ? direction.x : rb.velocity.x;
            if (Mathf.Abs(x) < 0.0001f) return;

            var faceLeft = x < 0f;
            if (enemyMovementSO.InvertFlip) faceLeft = !faceLeft;
            transform.rotation = Quaternion.Euler(transform.rotation.x, faceLeft ? 180 : 0, transform.rotation.z);
        }


        public bool ShouldStopAtEdge(float desiredX)
        {
            if (!enemyMovementSO.StopAtEdges || Mathf.Approximately(desiredX, 0f)) return false;
            var probe = desiredX < 0f ? leftCheck : rightCheck;
            var hit = Physics2D.Raycast(probe.position, Vector2.down, enemyMovementSO.EdgeProbeDepth, enemyMovementSO.GroundLayer);
            if (hit.collider != null) return false;
            var overlap = Physics2D.OverlapCircle(probe.position, enemyMovementSO.GroundProbeRadius * 0.5f, enemyMovementSO.GroundLayer);
            return overlap == null;
        }

        private void ChangeState(EnemyMovementState enemyMovementState)
        {
            DebugUtility.Log(this, $"[EnemyMovementPresenter] changed state from {this.enemyMovementState?.GetType().Name} to {enemyMovementState?.GetType().Name}");
            this.enemyMovementState?.OnExit();
            this.enemyMovementState = enemyMovementState;
            if (enemyMovementState != null) container.Inject(enemyMovementState);
            enemyMovementState?.OnEnter();
        }


        [Button]
        public void Jump()
        {
            if (!enemyMovementSO.EnableJump) return;
            externalVelocity = Vector2.up * enemyMovementSO.JumpForce;
        }

        public bool CanMoveDirection(Vector2 direction)
        {
            if (direction == Vector2.zero) return true;
            if (enemyMovementSO == null) return true;

            var dir = direction.normalized;

            if (enemyMovementSO.StopAtEdges && ShouldStopAtEdge(dir.x)) return false;

            var distance = enemyMovementSO.CanMoveDirCheckDistance;
            if (distance <= 0f) distance = 0.5f;

            var origin = (Vector2)transform.position;
            var end = origin + dir * distance;

            Debug.DrawLine(origin, end, Color.red, 5f);

            var hit = Physics2D.Raycast(origin, dir, distance, enemyMovementSO.WallLayerMask);
            if (hit.collider != null) return false;

            return true;
        }

        public void Do(EnemyMovementState enemyMovementState)
        {
            if (enemyMovementState == null) return;
            ChangeState(enemyMovementState);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (groundCheck != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(groundCheck.position, enemyMovementSO != null ? enemyMovementSO.GroundProbeRadius : 0.1f);
            }
            if (leftCheck != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(leftCheck.position, leftCheck.position + Vector3.down * (enemyMovementSO != null ? enemyMovementSO.EdgeProbeDepth : 0.3f));
                Gizmos.DrawWireSphere(leftCheck.position, (enemyMovementSO != null ? enemyMovementSO.GroundProbeRadius : 0.1f) * 0.5f);
            }
            if (rightCheck != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(rightCheck.position, rightCheck.position + Vector3.down * (enemyMovementSO != null ? enemyMovementSO.EdgeProbeDepth : 0.3f));
                Gizmos.DrawWireSphere(rightCheck.position, (enemyMovementSO != null ? enemyMovementSO.GroundProbeRadius : 0.1f) * 0.5f);
            }
        }
#endif
    }
}
