using MyToolz.Core;
using MyToolz.Player.Platformer.Interfaces;
using MyToolz.ScriptableObjects.Inventory;
using MyToolz.ScriptableObjects.Player.Platformer.Combat;
using Sirenix.OdinInspector;
using System;
using UnityEngine;
using Zenject;

namespace MyToolz.Player.Platformer.Interfaces
{
    public interface IPlayerCombatView
    {
        public event Action PlayerAttack;
        public event Action<WeaponSO> OnWeaponChanged;
        public event Action<PlayerAttackSO> OnPlayerAttackChanged;
        public void ShowAttack(bool result);
        public void InvokeAttack();
        public void InvokeCombo();
        public void SelectWeapon(WeaponSO weaponSO);
        public void SelectAttack(PlayerAttackSO playerAttackSO);
    }
}

namespace MyToolz.Player.Platformer.Combat.View
{
    public class PlayerCombatView : MonoBehaviourPlus, IPlayerCombatView
    {
        [SerializeField, Required] private Transform player;
        [SerializeField, Required] private Transform crosshair;
        [SerializeField, Range(1, 10)] private float maxRadius = 3f;

        private Camera mainCam;
        private IPlayerCombatModel model;
        private PlayerAttackSO playerAttack => model.PlayerAttackSO;

        public event Action PlayerAttack;
        public event Action<PlayerAttackSO> OnPlayerAttackChanged;
        public event Action<WeaponSO> OnWeaponChanged;

        [Inject]
        private void Construct(IPlayerCombatModel model)
        {
            this.model = model;
        }

        public void SelectWeapon(WeaponSO weaponSO)
        {
            OnWeaponChanged?.Invoke(weaponSO);
        }

        public void SelectAttack(PlayerAttackSO playerAttackSO)
        {
            OnPlayerAttackChanged?.Invoke(playerAttackSO);
        }

        private void Awake()
        {
            mainCam = Camera.main;
        }

        void LateUpdate()
        {
            Vector2 toTarget = playerAttack.RawDirection;
            Vector2 clamped = Vector2.ClampMagnitude(toTarget, maxRadius);
            crosshair.localPosition = clamped;
        }

        //TODO: refactor
        public void ShowAttack(bool result)
        {

        }

        public void InvokeAttack()
        {
            PlayerAttack?.Invoke();
        }

        public void InvokeCombo()
        {
            PlayerAttack?.Invoke();
        }
    }
}
