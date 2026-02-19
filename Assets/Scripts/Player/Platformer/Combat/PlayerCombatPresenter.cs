using MyToolz.DesignPatterns.StateMachine;
using MyToolz.EditorToolz;
using MyToolz.Player.Platformer.Combat.Interfaces;
using MyToolz.Player.Platformer.Interfaces;
using MyToolz.ScriptableObjects.Inventory;
using MyToolz.ScriptableObjects.Player.Platformer.Combat;
using MyToolz.Utilities.Debug;
using System;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace MyToolz.Player.Platformer.Combat.Interfaces
{
    public interface IUnitStateMachine<T> : IStateMachine<T> where T : IState
    {
        public bool IsExecuting(T state);
    }

    public interface IPlayerCombatPresenter
    {
        public event Action<RaycastHit2D> AttackSuccessful;
        public Transform ShootPoint { get; }
        public void Attack();
    }
}

namespace MyToolz.Player.Platformer.Combat.Presenter
{
    public class PlayerCombatPresenter : MonoBehaviour, IPlayerCombatPresenter
    {
        [SerializeField, Required] protected Transform shootPoint;

        public event Action<RaycastHit2D> AttackSuccessful;

        public Transform ShootPoint => shootPoint;

        protected PlayerAttackSO attack => model.PlayerAttackSO;
        protected DiContainer container;
        protected DefaultInputActions inputActions;
        protected IPlayerModel playerModel;
        protected IPlayerCombatModel model;
        protected IPlayerCombatView view;

        private readonly List<PlayerAttackSO> _currentlyBoundAttacks = new();

        [Inject]
        private void Construct(IPlayerCombatView view, IPlayerCombatModel model, IPlayerModel playerModel, DiContainer container, DefaultInputActions inputActions)
        {
            this.model = model;
            this.playerModel = playerModel;
            this.container = container;
            this.inputActions = inputActions;
            this.view = view;

            model.WeaponModelUpdated += OnWeaponModelUpdated;
            view.OnWeaponChanged += OnWeaponChanged;
            view.OnPlayerAttackChanged += OnAttackChanged;
            playerModel.ModelUpdate += OnPlayerModelUpdated;

            container.Inject(model);
            model.Initialize();
        }

        protected void Start()
        {
            if (playerModel?.StanceItemSO != null) RebindStance(playerModel.StanceItemSO);
        }

        protected void OnDisable()
        {
            UnbindAllAttacks();
        }

        public void ResetAttack()
        {

        }

        private void OnPlayerModelUpdated(StanceItemSO oldStance, StanceItemSO newStance)
        {
            RebindStance(newStance);
        }

        private void RebindStance(StanceItemSO stance)
        {
            UnbindAllAttacks();
            if (stance == null || stance.AattackStates == null) return;

            for (int i = 0; i < stance.AattackStates.Length; i++)
            {
                var atk = stance.AattackStates[i];
                if (atk == null) continue;
                container.Inject(atk);
                atk.RegisterBinding();
                _currentlyBoundAttacks.Add(atk);
            }

            DebugUtility.Log(this, "Rebound stance attacks: " + _currentlyBoundAttacks.Count);
        }

        private void UnbindAllAttacks()
        {
            if (_currentlyBoundAttacks.Count == 0) return;
            for (int i = 0; i < _currentlyBoundAttacks.Count; i++)
            {
                var atk = _currentlyBoundAttacks[i];
                if (atk == null) continue;
                atk.UnregisterBinding();
            }
            _currentlyBoundAttacks.Clear();
            DebugUtility.Log(this, "Unbound all stance attacks");
        }

        private void OnWeaponChanged(WeaponSO sO)
        {
            model.SetWeaponSO(sO);
        }

        private void OnAttackChanged(PlayerAttackSO playerAttackSO)
        {
            model.SetPlayerAttackSO(playerAttackSO);
        }

        private void OnWeaponModelUpdated(WeaponSO oldSO, WeaponSO newSO) { }

        public void Attack()
        {
            if (attack.Attack(out var hit))
            {
                AttackSuccessful?.Invoke(hit);
            }
        }
    }

}
