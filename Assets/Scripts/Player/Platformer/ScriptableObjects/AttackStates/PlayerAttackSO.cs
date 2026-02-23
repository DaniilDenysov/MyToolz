using MyToolz.Animations;
using MyToolz.Audio.Events;
using MyToolz.DesignPatterns.EventBus;
using MyToolz.DesignPatterns.StateMachine;
using MyToolz.EditorToolz;
using MyToolz.HealthSystem;
using MyToolz.HealthSystem.Interfaces;
using MyToolz.InputManagement.Commands;
using MyToolz.Player.Platformer.Combat;
using MyToolz.Player.Platformer.Combat.Interfaces;
using MyToolz.Player.Platformer.Interfaces;
using MyToolz.Player.Platformer.Movement.Interfaces;
using MyToolz.ScriptableObjects.Audio;
using MyToolz.ScriptableObjects.Inventory;
using UnityEngine;
using Zenject;

namespace MyToolz.Player.Platformer.Combat
{
    [System.Serializable]
    public abstract class DirectionSelectionStrategy
    {
        protected DefaultInputActions inputActions;
        protected IPlayerCombatModel combatModel;
        protected Transform shootPoint => combatModel.Context;
        protected Vector2 shootPointVec => shootPoint.position;
        protected Camera Cam => Camera.main;

        [Inject]
        private void Construct(DefaultInputActions inputActions, IPlayerCombatModel combatModel)
        {
            this.inputActions = inputActions;
            this.combatModel = combatModel;
        }

        public abstract Vector2 GetRawDirection();
        public virtual Vector2 GetDirection() => GetRawDirection().sqrMagnitude > 0f ? GetRawDirection().normalized : Vector2.zero;
    }

    [System.Serializable]
    public class CircleDirectionSelectionStrategy : DirectionSelectionStrategy
    {
        protected IPlayerMovementPresenter playerMovementPresenter;

        [Inject]
        private void Construct(IPlayerMovementPresenter playerMovementPresenter)
        {
            this.playerMovementPresenter = playerMovementPresenter;
        }

        public override Vector2 GetRawDirection()
        {
            Vector2 look = inputActions?.Player.Move.ReadValue<Vector2>() ?? Vector2.zero;
            if (look == Vector2.zero) return playerMovementPresenter.CharacterDirection;

            Vector3 center = Cam.WorldToScreenPoint(shootPoint.position);
            float depth = center.z;
            look = look.normalized;

            float radius = Mathf.Max(Screen.width, Screen.height);
            Vector3 targetScreen = center + new Vector3(look.x, look.y, 0f) * radius;

            Vector3 world = Cam.ScreenToWorldPoint(new Vector3(targetScreen.x, targetScreen.y, depth));
            return ((Vector2)(world - shootPoint.position)).normalized;
        }
    }

    [System.Serializable]
    public class LeftRightDirecctionSelectionStrategy : DirectionSelectionStrategy
    {
        protected IPlayerModel playerModel;
        protected IPlayerMovementPresenter playerMovementPresenter;
        protected Transform player => playerModel?.Context;

        [Inject]
        private void Construct(IPlayerMovementPresenter playerMovementPresenter, IPlayerModel playerModel)
        {
            this.playerMovementPresenter = playerMovementPresenter;
            this.playerModel = playerModel;
        }

        public override Vector2 GetRawDirection()
        {
            Vector2 look = inputActions?.Player.Move.ReadValue<Vector2>() ?? Vector2.zero;
            if (look == Vector2.zero) return playerMovementPresenter.CharacterDirection;

            Vector3 center = Cam.WorldToScreenPoint(shootPoint.position);
            float depth = center.z;

            look = look.normalized;

            float radius = Mathf.Max(Screen.width, Screen.height);
            Vector3 targetScreen = center + new Vector3(look.x, look.y, 0f) * radius;

            Vector3 world = Cam.ScreenToWorldPoint(new Vector3(targetScreen.x, targetScreen.y, depth));
            return world.x > player.position.x ? Vector2.right : Vector2.left;
        }
    }

    [System.Serializable]
    public abstract class AttackValidationStrategy
    {
        public abstract bool IsAttackValid();
    }

    [System.Serializable]
    public class FuryAttackValidationStrategy : AttackValidationStrategy
    {
        private IPlayerMovementModel playerMovementModel;

        [Inject]
        private void Construct(IPlayerMovementModel playerMovementModel)
        {
            this.playerMovementModel = playerMovementModel;
        }

        public override bool IsAttackValid()
        {
            return playerMovementModel?.IsGrounded ?? false;
        }
    }

    [System.Serializable]
    public class FlowAttackValidationStrategy : AttackValidationStrategy
    {
        public override bool IsAttackValid()
        {
            return true;
        }
    }

    public struct HitData
    {
        public Vector3 point;
        public GameObject hitObject;
    }
}

namespace MyToolz.ScriptableObjects.Player.Platformer.Combat
{
    public abstract class PlayerAttackSO : ScriptableObject
    {
        [SerializeField, FoldoutGroup("Config")] protected bool enablePullforce;
        [SerializeField, FoldoutGroup("Config")] protected bool interruptedByMovement = true;
        public bool InterruptedByMovement => interruptedByMovement;
        [SerializeField, FoldoutGroup("Binding"), Required] protected InputCommandSO inputBinding;
        protected WeaponSO weaponSO => combatModel.WeaponSO;
        protected bool enableStun => combatModel.WeaponSO.EnableStun;
        protected float stunDuration => combatModel.WeaponSO.StunDuration;
        protected float knockBackForce => weaponSO.KnockBackForce;
        protected float attackRange => weaponSO.AttackRange;
        protected float pullForce => weaponSO.PullForce;
        protected float distance => weaponSO.Distance;
        protected DamageType damageType => weaponSO.DamageType;
        protected float coolDown => GetAttackCoolDown();
        protected LayerMask layerMask => weaponSO.LayerMask;
        protected float attackDuration => 0.1f;
        [SerializeReference, Required, FoldoutGroup("Config")] protected DirectionSelectionStrategy directionSelectionStrategy = new CircleDirectionSelectionStrategy();
        [SerializeReference, Required, FoldoutGroup("Config")] protected AttackValidationStrategy attackValidationStrategy = new FlowAttackValidationStrategy();
        [SerializeField, Required, FoldoutGroup("Config")] protected bool enableKnockBack = true;
        [SerializeField, Required, FoldoutGroup("Config"), ShowIf("@enableKnockBack")] protected float playerKnockBackForce;
        public Vector2 Direction => directionSelectionStrategy?.GetDirection() ?? Vector2.zero;
        public Vector2 RawDirection => directionSelectionStrategy?.GetRawDirection() ?? Vector2.zero;
        protected IPlayerMovementModel playerMovementModel;
        protected IPlayerCombatView view;
        protected IPlayerCombatPresenter presenter;
        protected IPlayerMovementPresenter playerMovementPresenter;
   
        protected IPlayerCombatModel combatModel;
        protected DefaultInputActions inputActions;
        protected Transform shootPoint => presenter.ShootPoint;
        protected DiContainer container;
        protected float lastAttacked = float.MinValue;
        protected IAnimatorStateMachine<IState> animator;

        [Inject]
        private void Construct(IPlayerMovementPresenter playerMovementPresenter, IAnimatorStateMachine<IState> animator, DiContainer container,IPlayerCombatModel combatModel, IPlayerCombatPresenter presenter, DefaultInputActions inputActions, IPlayerCombatView view, IPlayerMovementModel playerMovementModel)
        {
            this.playerMovementPresenter = playerMovementPresenter;
            this.animator = animator;
            this.combatModel = combatModel;
            this.presenter = presenter;
            this.view = view;
            this.inputActions = inputActions;
            this.playerMovementModel = playerMovementModel;
            this.container = container;
            container.Inject(directionSelectionStrategy);
        }

        protected virtual float GetAttackCoolDown()
        {
            return weaponSO.UsageCoolDown;
        }

        public void RegisterBinding()
        {
            inputBinding.OnPerformed += Attack;
        }

        public void UnregisterBinding()
        {
            inputBinding.OnPerformed -= Attack;
        }

        public virtual bool IsAttacking()
        {
            return lastAttacked > 0 && lastAttacked + attackDuration > Time.time;
        }

        protected virtual void Attack(InputCommandSO inputBinding)
        {
            if (!IsAttackValid()) return;
            if (!CanAttack() && !IsAttacking()) return;
            combatModel.SetPlayerAttackSO(this);
            lastAttacked = Time.time;
        }

        public virtual bool CanAttack()
        {
            return lastAttacked + attackDuration + coolDown <= Time.time; // && (animator.TryGetCurrentState(out PlayerAnimationState state) && state is not AttackAnimationState);
        }

        public bool IsAttackValid()
        {
            container.Inject(attackValidationStrategy);
            return (attackValidationStrategy?.IsAttackValid() ?? false) && !combatModel.Locked;
        }

        public virtual bool Attack(out RaycastHit2D hit)
        {
            bool res = ProcessAttack(out hit);
            if (res)
            {
                playerMovementPresenter.ResetMovement();
                if (enableKnockBack)
                {
                    Vector2 attackDirection = (combatModel.Context.position - hit.collider.transform.position).normalized;
                    playerMovementModel.SetExternalVelocity(playerMovementModel.ExternalVelocity + (attackDirection * playerKnockBackForce));
                }
            }
            if (enablePullforce)
            {
                playerMovementModel.SetExternalVelocity(playerMovementModel.ExternalVelocity + (Direction * pullForce));
            }
            view.ShowAttack(res);
            if (res) PlayClip(combatModel.WeaponSO.HitClip);
            else PlayClip(combatModel.WeaponSO.MissClip);
            return res;
        }

        public virtual void ResetAttack()
        {

        }

        protected void PlayClip(AudioClipSO audioClipSO)
        {
            EventBus<PlayAudioClipSO>.Raise(new PlayAudioClipSO()
            {
                AudioClipSO = audioClipSO
            });
        }


        public virtual bool ProcessAttack(out RaycastHit2D hit)
        {
            var result = Physics2D.CircleCastAll(shootPoint.position, attackRange, Direction, distance, layerMask);
            hit = default;
            bool landedHit = false;
            for (int i = 0; i<result.Length;i++)
            {
                hit = result[i];
                if (ProcessHit(hit))
                {
                    landedHit = true;
                    Vector2 pos = shootPoint.position;
                    //EventBus<PoolRequest<WorldVFX>>.Raise(new PoolRequest<WorldVFX>()
                    //{
                    //    Prefab = weaponSO.SliceVFX,
                    //    Position = hit.point,
                    //    Rotation = Quaternion.Euler(0, 0, Mathf.Atan2((hit.point - pos).y, (hit.point - pos).x) * Mathf.Rad2Deg)
                    //});
                }
            }
            return landedHit;
        }

        public virtual bool ProcessHit(RaycastHit2D hit)
        {
            if (!hit.collider) return false;
            if (hit.collider.gameObject.TryGetComponent(out IDamagable damagable))
            {
                damagable.DoDamage(damageType);
            }
            if (hit.collider.gameObject.TryGetComponent(out IKnockOffable knockOffable))
            {
                knockOffable.KnockOff(playerMovementPresenter.CharacterDirection * knockBackForce);
            }
            return true;
        }

        protected virtual void OnEnable()
        {
            lastAttacked = float.MinValue;
        }

        protected virtual void OnDisable()
        {
            lastAttacked = float.MinValue;
        }
    }
}
