using MyToolz.Player.FPS.CombatSystem.Model;
using MyToolz.Player.FPS.CombatSystem.View;
using MyToolz.ScriptableObjects.Player.Platformer.Movement;
using MyToolz.Utilities.Debug;
using UnityEngine;
using Zenject;

namespace MyToolz.Player.FPS.CombatSystem.Presenter
{
    [System.Serializable]
    public abstract class CombatSystemStateHandler
    {
        #region Private fields

        [SerializeField] protected Camera mainCamera;

        protected ShootingMode shootingMode
        {
            get => weaponSO.Mode;
        }

        protected WeaponSO weaponSO
        {
            get => weaponModel.GetItemSO();
        }

        protected WeaponModel weaponModel
        {
            get => combatSystemController.WeaponModel;
        }

        protected WeaponAnimationsHandler weaponAnimationsHandler;

        protected bool canShoot
        {
            get => (lastTimeFired + weaponSO.FireRate <= Time.time);
        }

        protected bool needReload
        {
            get => weaponModel.CurrentBullets <= 0;
        }

        protected bool isAiming
        {
            get => weaponModel.IsAiming();
        }

        protected SprayPatternSO sprayPattern
        {
            get
            {
                if (isAiming)
                {
                    return weaponSO.AimingSprayPattern;
                }
                else
                {
                    return weaponSO.RunningSprayPattern;
                    //if (playerMovement == null) return weaponSO.DefaultSprayPattern;
                    //MovementStateSO movementState = playerMovement.CurrentMovementState;
                    //switch (movementState)
                    //{
                    //    case MovementState.Moving :
                    //        return weaponSO.RunningSprayPattern;
                    //    case MovementState.Sprinting :
                    //        return weaponSO.RunningSprayPattern;
                    //    case MovementState.TacticalSprinting :
                    //        return weaponSO.RunningSprayPattern;
                    //    case MovementState.Crouching:
                    //        return weaponSO.CrouchingSprayPattern;
                    //    case MovementState.Jumping:
                    //        return weaponSO.RunningSprayPattern;
                    //    case MovementState.JumpingOver:
                    //        return weaponSO.RunningSprayPattern;
                    //    case MovementState.Falling:
                    //        return weaponSO.RunningSprayPattern;
                    //    case MovementState.Sliding:
                    //        return weaponSO.RunningSprayPattern;
                    //    default:
                    //        return weaponSO.DefaultSprayPattern;
                    //}
                }
            }
        }

        protected Vector3 baseDirection
        {
            get
            {
                    return weaponModel.GetShootingPoint().forward;
            }
        }

        protected Vector3 spreadDirection
        {
            get
            {
                var pattern = sprayPattern;

                if (pattern == null)
                {
                    DebugUtility.Log(this, "Pattern is null!");
                    return baseDirection;
                }
                else
                {
                    Vector3 spread = pattern.GetPattern(weaponModel.RemainingMagPercentage, baseDirection);
                    return spread.normalized;
                }
            }
        }

        protected Vector2 spreadSize
        {
            get
            {
                return Vector2.one * (sprayPattern.Amount + additiveSpread.magnitude);
            }
        }

        protected Vector2 additiveSpread;

        protected WeaponVFXHandler weaponVFXHandler;

        protected WeaponSFXHandler weaponSFXHandler;

        protected CombatSystemController combatSystemController;

        protected FeedbackHandler weaponFeedbackHandler;

        protected float lastTimeFired;

        #endregion

        [Inject]
        public void Construct(
              WeaponAnimationsHandler weaponAnimationsHandler,
              WeaponVFXHandler weaponVFXHandler,
              WeaponSFXHandler weaponSFXHandler,
              CombatSystemController weaponController,
              FeedbackHandler weaponFeedbackHandler
          )
        {
            if (weaponAnimationsHandler == null) DebugUtility.LogError(this, "WeaponAnimationsHandler is null!");
            if (weaponVFXHandler == null) DebugUtility.LogError(this, "WeaponVFXHandler is null!");
            if (weaponSFXHandler == null) DebugUtility.LogError(this, "WeaponSFXHandler is null!");
            if (weaponController == null) DebugUtility.LogError(this, "CombatSystemController is null!");
            if (weaponFeedbackHandler == null) DebugUtility.LogError(this, "FeedbackHandler is null!");

            this.weaponFeedbackHandler = weaponFeedbackHandler;
            this.weaponAnimationsHandler = weaponAnimationsHandler;
            this.weaponVFXHandler = weaponVFXHandler;
            this.weaponSFXHandler = weaponSFXHandler;
            this.combatSystemController = weaponController;
        }


        public abstract void Enter();
        public abstract void Update();
        public abstract void Exit();
    }
}
