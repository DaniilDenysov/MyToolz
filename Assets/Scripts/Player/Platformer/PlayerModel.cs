using MyToolz.DesignPatterns.EventBus;
using MyToolz.Player.Platformer.Events;
using MyToolz.Player.Platformer.Interfaces;
using MyToolz.ScriptableObjects.Inventory;
using MyToolz.ScriptableObjects.Player.Platformer.Combat;
using MyToolz.ScriptableObjects.Player.Platformer.Movement;
using Sirenix.OdinInspector;
using System;
using UnityEngine;
using Zenject;


namespace MyToolz.Player.Platformer.Interfaces
{
    public interface IReadOnlyPlayerCombatModel
    {
        public bool Locked { get; }
        public Transform Context { get; }
        public PlayerAttackSO PlayerAttackSO { get; }
        public WeaponSO WeaponSO { get; }
    }

    public interface IPlayerCombatModel : IReadOnlyPlayerCombatModel
    {
        /// <summary>
        /// 1st argument is an old weapon, 2nd argument is a new weapon
        /// </summary>
        public event Action<WeaponSO, WeaponSO> WeaponModelUpdated;
        public event Action<PlayerAttackSO, PlayerAttackSO> AttackModelUpdated;
        public void SetWeaponSO(WeaponSO weaponSO);
        public void SetLocked(bool state);
        public void SetPlayerAttackSO(PlayerAttackSO attackSO);
        public void Initialize();
        public bool IsAttackValid();
        public bool IsAttacking();
    }
    public interface IReadOnlyPlayerModel
    {
        public Transform Context { get; }
        public StanceItemSO StanceItemSO { get; }
    }
    public interface IPlayerModel : IReadOnlyPlayerModel
    {
        /// <summary>
        /// 1st argument - old stance, 2nd argument - new stance
        /// </summary>
        public event Action<StanceItemSO, StanceItemSO> ModelUpdate;
        public void Initialize();
        public void SetStanceItemSO(StanceItemSO stanceItemSO);
    }

    public interface IReadOnlyPlayerMovementModel
    {
        public bool Locked { get; }
        public PlayerMovementStats Stats { get; }
        public bool IsGrounded { get; }
        public Vector2 ExternalVelocity { get; }

        public Vector2 Velocity { get; }
        public Rigidbody2D Rigidbody { get; }
    }

    public interface IPlayerMovementModel : IReadOnlyPlayerMovementModel
    {
        public void SetLocked(bool locked);
        public void SetExternalVelocity(Vector2 velocity);
        public void SetVelocity(Vector2 velocity);
        public void SetIsGrounded(bool isGrounded);
        public void SetRigidbody(Rigidbody2D rigidbody);
    }
}

namespace MyToolz.Player.Platformer.Combat
{
    [System.Serializable]
    public class PlayerCombatModel : IPlayerCombatModel
    {
        [SerializeField, Required] protected Transform context;
        [SerializeField] protected WeaponSO weaponSO;
        [SerializeField] protected PlayerAttackSO playerAttackSO;
        protected DiContainer container;
        public WeaponSO WeaponSO => weaponSO;

        public PlayerAttackSO PlayerAttackSO => playerAttackSO;
        public bool Locked => locked;
        public Transform Context => context;

        public event Action<PlayerAttackSO, PlayerAttackSO> AttackModelUpdated;
        public event Action<WeaponSO, WeaponSO> WeaponModelUpdated;
        protected bool locked;
        public void SetWeaponSO(WeaponSO weaponSO)
        {
            if (weaponSO == null) return;
            if (weaponSO == WeaponSO) return;
            container.Inject(weaponSO);
            WeaponModelUpdated?.Invoke(WeaponSO, weaponSO);
            this.weaponSO = weaponSO;
        }

        public void SetPlayerAttackSO(PlayerAttackSO attackSO)
        {
            if (attackSO == null) return;
            if (attackSO == playerAttackSO) return;
            container.Inject(attackSO);
            AttackModelUpdated?.Invoke(PlayerAttackSO, attackSO);
            this.playerAttackSO = attackSO;
        }

        [Inject]
        private void Construct(DiContainer container)
        {
            this.container = container;
        }

        public void Initialize()
        {
            container.Inject(playerAttackSO);
            container.Inject(weaponSO);
        }

        public bool IsAttackValid()
        {
            return playerAttackSO?.IsAttackValid() ?? false;
        }

        public bool IsAttacking()
        {
            return playerAttackSO?.IsAttacking() ?? false;
        }

        public void SetLocked(bool state)
        {
           locked = state;
        }
    }
}

namespace MyToolz.Player.Platformer.Model
{
    [System.Serializable]
    public class PlayerModel : IPlayerModel
    {
        protected StanceItemSO stanceItemSO;
        
        [SerializeField, Required] protected Transform context;

        public StanceItemSO StanceItemSO => stanceItemSO;

        public Transform Context => context;

        public event Action<StanceItemSO, StanceItemSO> ModelUpdate;
        public void Initialize()
        {

        }
        public void SetStanceItemSO(StanceItemSO stanceItemSO)
        {
            if (stanceItemSO == null) return;
            if (StanceItemSO == stanceItemSO) return;
            ModelUpdate?.Invoke(StanceItemSO, stanceItemSO);
            this.stanceItemSO = stanceItemSO;
        }
    }
}

namespace MyToolz.Player.Platformer.Movement.Model
{
    [System.Serializable]
    public class PlayerMovementModel : IReadOnlyPlayerMovementModel, IPlayerMovementModel
    {
        [SerializeField, Required] private PlayerMovementStats stats;
        [SerializeField] private bool locked;
        private Vector2 externalVelocity;
        private Vector2 velocity;
        private bool isGrounded;
        private Rigidbody2D rigidbody;
        public Vector2 ExternalVelocity => externalVelocity;
        public Vector2 Velocity => velocity;

        public bool IsGrounded => isGrounded;

        public Rigidbody2D Rigidbody => rigidbody;

        public PlayerMovementStats Stats => stats;

        public bool Locked => locked;

        public void SetIsGrounded(bool isGrounded)
        {
            this.isGrounded = isGrounded;
            if (isGrounded) EventBus<PlayerGrounded>.Raise(new PlayerGrounded());
        }

        public void SetRigidbody(Rigidbody2D rigidbody)
        {
            this.rigidbody = rigidbody;
        }

        public void SetExternalVelocity(Vector2 velocity)
        {
            externalVelocity = velocity;
        }

        public void SetVelocity(Vector2 velocity)
        {
            if (locked)
            {
                this.velocity = Vector2.zero;
                return;
            }
            this.velocity = velocity;
        }

        public void SetLocked(bool locked)
        {         
            this.locked = locked;
        }
    }
}
