using MyToolz.AI.Platformer.Interfaces;
using MyToolz.AI.Platformer.Presenters;
using MyToolz.DesignPatterns.StateMachine.PriorityBased;
using MyToolz.HealthSystem.Interfaces;
using MyToolz.ScriptableObjects.AI.Platformer;
using Sirenix.OdinInspector;
using System;
using UnityEngine;
using Zenject;

namespace MyToolz.AI.Core
{
    [Serializable]
    public abstract class MovementEnemyBehaviourState : EnemyBehaviourState
    {
        protected IEnemyMovementPresenter enemyMovementPresenter;

        [Inject]
        private void Construct(IEnemyMovementPresenter enemyMovementPresenter)
        {
            this.enemyMovementPresenter = enemyMovementPresenter;
        }
    }

    [Serializable]
    public class Idle : EnemyBehaviourState
    {

    }

    [Serializable]
    public class AttackPlayer : EnemyBehaviourState
    {
        [SerializeField, Required] protected Transform context;
        protected IEnemyCombatPresenter combatPresenter;
        protected IEnemyModel enemyModel;
        protected IEnemyMovementPresenter enemyMovementPresenter;
        protected EnemyCombatSO enemyCombatSO => enemyModel.EnemyCombatSO;
        protected Vector2 attackPosition;
        protected Vector2 targetDirection => ((enemyModel?.Player?.position ?? Vector2.zero) - context.position).normalized;
        protected Transform player => enemyModel?.Player ?? null;
        protected Vector2 playerPosition => enemyModel?.Player?.transform.position ?? enemyModel?.LastKnownPlayerPosition ?? Vector2.zero;
        protected Vector2 enemyPosition => context?.transform.position ?? Vector2.zero;
        protected float lastAttacked
        {
            get => enemyModel.LastAttacked;
            set { enemyModel.SetLastAttack(value); }
        }

        [Inject]
        private void Construct(IEnemyModel enemyModel, IEnemyCombatPresenter combatPresenter, IEnemyMovementPresenter enemyMovementPresenter)
        {
            this.enemyModel = enemyModel;
            this.combatPresenter = combatPresenter;
            this.enemyMovementPresenter = enemyMovementPresenter;
        }

        public override void Initialize()
        {
            base.Initialize();
            lastAttacked = float.MinValue;
        }

        public override void OnEnter()
        {
            base.OnEnter();
            lastAttacked = Time.time;
        }

        public override void OnUpdate()
        {
            enemyMovementPresenter.IsActive = !IsAttacking();
            if (IsAttacking() && enemyCombatSO.ShouldFaceTarget) enemyMovementPresenter.ApplyFlip(targetDirection);
            if (CanAttack() && IsAttacking()) lastAttacked = Time.time;
        }
        protected virtual bool IsWithinAttackRange() => Vector2.Distance(playerPosition, context.position) <= enemyCombatSO.AttackRange;
        protected virtual bool CanAttack() => (lastAttacked + enemyCombatSO.AttackDuration + enemyCombatSO.CoolDown) < Time.time;
        protected virtual bool IsAttacking() => (lastAttacked + enemyCombatSO.AttackDuration) > Time.time;

        //TODO: IMPLEMENT BETTER VALIDATION WHICH WILL ENSURE PLAYER IS ON THE SAME LEVEL AS ENEMY
        protected virtual bool CanBeReached() => true;

        public override bool IsConditionFullfilled()
        {
            return base.IsConditionFullfilled() && ((player != null && IsWithinAttackRange() && CanAttack() && CanBeReached()) || IsAttacking());
        }
    }

    public enum QueueMode
    {
        Exclude,
        Include
    }

    public enum Equality
    {
        MoreThanEqual,
        Equal,
        LessThanEqual
    }

    public enum ThresholdOptions
    {
        Percentage,
        Concrete
    }

    public enum SpawningOptions
    {
        FullPool,
        RandomFromPool
    }

    public enum PositionOptions
    {
        RandomPoint,
        RandomArea
    }

    public enum StatePhase
    {
        None,
        Anticipation,
        Action,
        PostAction
    }

    [Serializable]
    public abstract class BossLinearStateMachine : EnemyBehaviourState
    {
        public StatePhase CurrentPhase => currentPhase;
        [FoldoutGroup("Runtime"), ReadOnly, SerializeField] protected StatePhase currentPhase = StatePhase.None;
        protected bool isExecuting => currentPhase != StatePhase.None;

        public override void OnEnter()
        {
            base.OnEnter();
            currentPhase = StatePhase.Anticipation;
        }

        public override void OnUpdate()
        {
            base.OnUpdate();
            switch (currentPhase)
            {
                case StatePhase.Anticipation:
                    OnAnticipation();
                    break;

                case StatePhase.Action:
                    OnAction();
                    break;

                case StatePhase.PostAction:
                    OnPostAction();
                    break;
            }
        }

        /// <summary>
        /// Called every frame, until state is changed manually, it is important to change state to Action in this state, otherwise it will stuck in this state
        /// </summary>
        public abstract void OnAnticipation();

        /// <summary>
        /// Called every frame, until state is changed manually, it is important to change state to PostAnticipation in this state, otherwise it will stuck in this state
        /// </summary>
        public abstract void OnAction();

        /// <summary>
        /// Called every frame, until state is changed manually, it is important to change state to none in this state, otherwise it will stuck in this state
        /// </summary>
        public abstract void OnPostAction();

        public override void OnExit()
        {
            base.OnExit();
            currentPhase = StatePhase.None;
        }

        public override bool IsCurrentFinished()
        {
            return !isExecuting;
        }

        public override bool IsConditionFullfilled()
        {
            if (isExecuting) return true;
            return base.IsConditionFullfilled();
        }
    }


    [Serializable]
    public class FollowPlayerEnemyBehaviourState : MovementEnemyBehaviourState
    {
        [FoldoutGroup("Runtime"), ReadOnly, SerializeField] private float stopDistance => enemyModel?.EnemyMovementSO?.StopDistance ?? 1.25f;
        [FoldoutGroup("Runtime"), ReadOnly, SerializeField] private float loseSightDistance => enemyModel?.EnemyMovementSO?.LoseSightDistance ?? 12f;
        [FoldoutGroup("Runtime"), ReadOnly, SerializeField] private float currentDistance;

        private Transform player => enemyModel.Player;
        private IReadOnlyEnemyModel enemyModel;
        private FollowEnemyMovementState followEnemyMovementState = new();

        [Inject]
        private void Construct(IReadOnlyEnemyModel enemyModel)
        {
            this.enemyModel = enemyModel;
        }

        public override void OnEnter()
        {
            if (player == null) return;
            followEnemyMovementState.Initialize(player);
            enemyMovementPresenter.Do(followEnemyMovementState);
            Log("Follow state entered");
        }

        public override void OnExit()
        {
            Log("Follow state exited");
        }

        public override void OnUpdate()
        {
            if (player == null) return;
            currentDistance = Vector2.Distance(player.position, enemyMovementPresenterPosition2D());
            followEnemyMovementState.Initialize(player);
            enemyMovementPresenter.Do(followEnemyMovementState);
        }

        public override bool IsConditionFullfilled()
        {
            if (player == null) return false;
            var d = Vector2.Distance(player.position, enemyMovementPresenterPosition2D());
            if (d <= stopDistance) return false;
            return base.IsConditionFullfilled() && d <= loseSightDistance;
        }

        private Vector2 enemyMovementPresenterPosition2D()
        {
            var p = enemyModel?.UnitContext?.transform.position ?? Vector3.zero;
            return new Vector2(p.x, p.y);
        }
    }

    [Serializable]
    public class PatrolEnemyBehaviourState : MovementEnemyBehaviourState
    {
        public enum AxisMode { Both, XOnly, YOnly }

        [FoldoutGroup("Path"), SerializeField] private Transform[] points;
        [FoldoutGroup("Path"), SerializeField] private AxisMode axis = AxisMode.Both;

        [FoldoutGroup("Runtime"), ReadOnly, SerializeField] private float arriveDistance => enemyModel?.EnemyMovementSO?.ArriveDistance ?? 0.2f;
        [FoldoutGroup("Runtime"), ReadOnly, SerializeField] private float waitAtPoint => enemyModel?.EnemyMovementSO?.WaitAtPoint ?? 0.25f;
        [FoldoutGroup("Runtime"), ReadOnly, SerializeField] private int index;
        [FoldoutGroup("Runtime"), ReadOnly, SerializeField] private float waitTimer;
        [FoldoutGroup("Runtime"), ReadOnly, SerializeField] private bool waiting;
        private MoveToEnemyMovementState moveToEnemyMovementState = new();
        private IReadOnlyEnemyModel enemyModel;

        [Inject]
        private void Construct(IReadOnlyEnemyModel enemyModel)
        {
            this.enemyModel = enemyModel;
        }

        public override void Initialize()
        {
            base.Initialize();
            index = 0;
            waitTimer = 0f;
            waiting = false;
        }

        public override void OnEnter()
        {
            if (points == null || points.Length == 0) return;
            MoveToCurrent();
            Log("Patrol state entered");
        }

        public override void OnExit()
        {
            Log("Patrol state exited");
        }

        public override void OnUpdate()
        {
            if (points == null || points.Length == 0) return;

            var pos = enemyMovementPresenterPosition2D();
            var rawTarget = (Vector2)points[index].position;
            var target = ProjectTargetToAxis(rawTarget, pos);

            if (waiting)
            {
                waitTimer -= Time.deltaTime;
                if (waitTimer <= 0f)
                {
                    waiting = false;
                    Advance();
                    MoveToCurrent();
                }
                return;
            }

            if (HasArrived(pos, rawTarget))
            {
                waiting = true;
                waitTimer = waitAtPoint;
            }
        }

        public override bool IsConditionFullfilled()
        {
            return base.IsConditionFullfilled() && points != null && points.Length > 0 && enemyModel.Player == null;
        }

        private void Advance()
        {
            if (points == null || points.Length == 0) return;
            index = (index + 1) % points.Length;
        }

        private void MoveToCurrent()
        {
            if (points == null || points.Length == 0) return;

            var pos = enemyMovementPresenterPosition2D();
            var rawTarget = (Vector2)points[index].position;
            var target = ProjectTargetToAxis(rawTarget, pos);

            moveToEnemyMovementState.Initialize(target);
            enemyMovementPresenter.Do(moveToEnemyMovementState);
        }

        private Vector2 ProjectTargetToAxis(Vector2 rawTarget, Vector2 currentPos)
        {
            switch (axis)
            {
                case AxisMode.XOnly:
                    return new Vector2(rawTarget.x, currentPos.y);
                case AxisMode.YOnly:
                    return new Vector2(currentPos.x, rawTarget.y);
                default:
                    return rawTarget;
            }
        }

        private bool HasArrived(Vector2 pos, Vector2 rawTarget)
        {
            switch (axis)
            {
                case AxisMode.XOnly:
                    return Mathf.Abs(pos.x - rawTarget.x) <= arriveDistance;
                case AxisMode.YOnly:
                    return Mathf.Abs(pos.y - rawTarget.y) <= arriveDistance;
                default:
                    return Vector2.Distance(pos, rawTarget) <= arriveDistance;
            }
        }

        private Vector2 enemyMovementPresenterPosition2D()
        {
            var p = enemyModel.UnitContext?.position ?? Vector3.zero;
            return new Vector2(p.x, p.y);
        }
    }



    [Serializable]
    public class SearchPlayerEnemyBehaviourState : MovementEnemyBehaviourState
    {
        [FoldoutGroup("Runtime"), ReadOnly, SerializeField] private float duration => enemyModel?.EnemyMovementSO?.SearchDuration ?? 3f;
        [FoldoutGroup("Runtime"), ReadOnly, SerializeField] private float completeDistance => enemyModel?.EnemyMovementSO?.CompleteDistance ?? 0.4f;
        [FoldoutGroup("Runtime"), ReadOnly, SerializeField] private Vector3 lastKnownPlayerPosition => enemyModel?.LastKnownPlayerPosition ?? Vector3.zero;
        [FoldoutGroup("Runtime"), ReadOnly, SerializeField] private float timer;
        [FoldoutGroup("Runtime"), ReadOnly, SerializeField] private bool reached;

        private IEnemyModel enemyModel;
        private MoveToEnemyMovementState moveToEnemyMovementState = new();
        private Vector2 enemyMovementPresenterPosition2D
        {
            get
            {
                var p = (enemyMovementPresenter as Component)?.transform.position ?? Vector3.zero;
                return new Vector2(p.x, p.y);
            }
        }

        [Inject]
        private void Construct(IEnemyModel enemyModel)
        {
            this.enemyModel = enemyModel;
        }

        public override void OnEnter()
        {
            timer = duration;
            reached = false;
            moveToEnemyMovementState.Initialize(lastKnownPlayerPosition);
            enemyMovementPresenter.Do(moveToEnemyMovementState);
            Log("Search state entered");
        }

        public override void OnExit()
        {
            timer = 0f;
            reached = false;
            enemyModel.SetLastKnownPlayerPosition(Vector3.zero);
            Log("Search state exited");
        }

        public override void OnUpdate()
        {
            timer -= Time.deltaTime;
            var pos2d = enemyMovementPresenterPosition2D;
            var target2d = new Vector2(lastKnownPlayerPosition.x, lastKnownPlayerPosition.y);
            if (!reached && Vector2.Distance(pos2d, target2d) <= completeDistance)
            {
                reached = true;
            }
        }

        public override bool IsConditionFullfilled()
        {
            if (lastKnownPlayerPosition == Vector3.zero) return false;
            if (timer < 0f) return false;
            if (reached && timer < 0.01f) return false;
            return enemyModel.Player == null && base.IsConditionFullfilled();
        }
    }

    [Serializable]
    public abstract class EnemyBehaviourState : PriorityState
    {
        [SerializeField] protected bool enabled = true;

        public override bool IsCurrentFinished()
        {
            return true;
        }

        public override bool IsConditionFullfilled()
        {
            return enabled;
        }

        public virtual void DrawGizmos()
        {

        }
    }

    public interface IStunnable
    {
        public void Stun(float duration);
    }

    public class EnemyBehaviourManager : PriorityStateMachine<EnemyBehaviourState>
    {
#if UNITY_EDITOR
        public void OnDrawGizmos()
        {
            for (int i = 0;i<behaviourStates.Length;i++)
            {
                behaviourStates[i]?.DrawGizmos();
            }
        }
#endif
    }
}
