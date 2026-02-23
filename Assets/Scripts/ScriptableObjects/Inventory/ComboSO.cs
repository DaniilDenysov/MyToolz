using MyToolz.DesignPatterns.EventBus;
using MyToolz.EditorToolz;
using MyToolz.HealthSystem;
using MyToolz.HealthSystem.Interfaces;
using MyToolz.InputManagement.Commands;
using MyToolz.Player.Platformer.Events;
using MyToolz.Player.Platformer.Interfaces;
using MyToolz.Player.Platformer.Movement.Interfaces;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace MyToolz.Player.Platformer.Combo
{
    [System.Serializable]
    public class FlowComboStrategy : ComboStrategy
    {
        //private IPlayerHealthPresenter presenter;

        //[Inject]
        //private void Construct(IPlayerHealthPresenter presenter)
        //{
        //    this.presenter = presenter;
        //}

        public override void Rise()
        {
            //presenter.ActivateShield();
        }
    }

    [System.Serializable]
    public class FuryComboStrategy : ComboStrategy
    {
        [SerializeField] private LayerMask layerMask;
        [SerializeReference] private DamageType damageType;
        [SerializeField] private float range = 5f;
        [SerializeField] private Vector2 direction = Vector2.down;
        [SerializeField] private float force = 100f;
        [SerializeField] private float shakeForce = 25f;
        private IPlayerMovementModel playerMovementModel;
        private IPlayerVerticalMovementView verticalMovement;
        private IPlayerModel playerModel;
        private EventBinding<PlayerGrounded> playerGroundedBinding;

        [Inject]
        private void Construct(IPlayerModel playerModel, IPlayerVerticalMovementView verticalMovement, IPlayerMovementModel playerMovementModel)
        {
            this.playerModel = playerModel;
            this.verticalMovement = verticalMovement;
            this.playerMovementModel = playerMovementModel;
        }

        public override void Rise()
        {
            playerGroundedBinding = new EventBinding<PlayerGrounded>(OnGrounded);
            EventBus<PlayerGrounded>.Register(playerGroundedBinding);
            playerMovementModel?.SetExternalVelocity(playerMovementModel.ExternalVelocity + (direction * force));
        }

        public bool ProcessAttack()
        {
            var result = Physics2D.OverlapCircleAll(playerModel.Context.position, range, layerMask);
            bool hitSomething = false;
            foreach (var hit in result)
            {
                if (ProcessHit(hit))
                {
                    hitSomething = true;
                }
            }

            return hitSomething;
        }

        protected bool ProcessHit(Collider2D collider)
        {
            if (collider != null && collider.TryGetComponent(out IDamagable damagable))
            {
                damagable.DoDamage(damageType);
                return true;
            }
            return false;
        }

        private void OnGrounded()
        {
            verticalMovement?.OnLanding(shakeForce);
            ProcessAttack();
            EventBus<PlayerGrounded>.Deregister(playerGroundedBinding);
        }
    }

    [System.Serializable]
    public abstract class ComboStrategy
    {
        public abstract void Rise();
    }

    [CreateAssetMenu(fileName = "ComboSO", menuName = "ProjectClyde/Player/Combos/ComboSO")]
    public class ComboSO : ScriptableObject
    {
        [SerializeReference] private ComboStrategy comboStrategy;
        [SerializeField] private bool preserveOrder;
        public bool PreserveOrder => preserveOrder;
        [SerializeField, Min(0)] private float timing;
        public float Timing => timing;
        [SerializeField] private List<InputCommandSO> commands;
        public IReadOnlyList<InputCommandSO> Commands => commands;

        [Inject]
        private void Construct(DiContainer container)
        {
            container.Inject(comboStrategy);
        }

        [Button]
        public void Activate()
        {
            comboStrategy?.Rise();
        }
    }
}
