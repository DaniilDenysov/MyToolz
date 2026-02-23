using UnityEngine;
using MyToolz.DesignPatterns.EventBus;
using MyToolz.Player.FPS.CombatSystem.Model;
using MyToolz.Networking.Events;
using MyToolz.UI.Events;
using MyToolz.HealthSystem.Interfaces;
using MyToolz.HealthSystem;
using MyToolz.Player.FPS.CombatSystem.View;
using MyToolz.Player.FPS.CombatSystem.Presenter;
using MyToolz.UI.Notifications.Model;
using MyToolz.UI.Notifications.View;
using MyToolz.InputManagement.Commands;

namespace MyToolz.Player.FPS.CombatSystem
{
    public interface IHitbox
    {
        public HitStatus GetHitStatus();
    }
}
namespace MyToolz.Player.FPS.CombatSystem.Presenter
{
    public abstract class PlayerAttackHandler : CombatSystemStateHandler
    {
        [SerializeField] protected InputCommandSO shootInputCommandSO;
        [SerializeField] protected InputCommandSO aimInputCommandSO;

        public override void Enter()
        {
            lastTimeFired = -weaponSO.FireRate;
        }

        public override void Exit()
        {
            weaponModel.StopFire();
        }

        public override void Update()
        {
            UpdateSpreadSize(-Vector2.one * 0.1f);
            HandleCrosshair();
            HandleAttacking();
        }

        public void HandleCrosshair()
        {
            weaponModel.CrosshairController.UpdateCrosshairSize(spreadSize);
        }

        public void UpdateSpreadSize(Vector2 additiveValue)
        {
            float x = Mathf.Max(additiveSpread.x + additiveValue.x, 0);
            float y = Mathf.Max(additiveSpread.y + additiveValue.y, 0);
            additiveSpread = new Vector2(Mathf.Min(x,sprayPattern.MaxRadius), Mathf.Min(y, sprayPattern.MaxRadius));
        }

        #region Attack handlig
        protected virtual void HandleAttacking()
        {
            bool isAttacking = shootingMode == ShootingMode.Hold
                ? shootInputCommandSO.ReadValue<float>() != 0
                : aimInputCommandSO.WasPressedThisFrame();

            if (isAttacking)
            {
                Attack();
            }
            else
            {
                weaponModel.StopFire();
            }
        }

        public void PlayKillFeedbacks()
        {
           // weaponFeedbackHandler.KillFeedback.PlayFeedbacks();
        }

        public virtual void Attack()
        {
            if (!canShoot) return;
            UpdateSpreadSize(Vector2.one);
            lastTimeFired = Time.time;
            EventBus<PlayerShot>.Raise(new PlayerShot() { WeaponSO = this.weaponSO });
            weaponSFXHandler.PlayLocalSFX();
            weaponSFXHandler.CmdPlaySFX();
            HitStatus hitStatus = HitStatus.Miss;
            for (int i = 0; i < weaponSO.Burst; i++)
            {
                EventBus<PlayerShot>.Raise(new PlayerShot() { WeaponSO = this.weaponSO });
                var result = ShootRaycast(mainCamera.transform.position, spreadDirection);
                weaponVFXHandler.CmdHandleHitVFX(result);
                if ((int)result.HitStatus > (int)hitStatus)
                {
                    hitStatus = result.HitStatus;
                }
            }

            switch (hitStatus)
            {
                case HitStatus.Kill:
                    weaponModel.CrosshairController.ChangeState(typeof(HitmarkerKillScope));
                    EventBus<NotificationRequest>.Raise(new NotificationRequest()
                    {
                        MessageType = typeof(KillNotification),
                        Overflow = OverflowPolicy.DropOldest,
                        Priority = NotificationPriority.Normal,
                        Dedupe = DedupePolicy.None,
                        Text = "Kill"
                    });
                    PlayKillFeedbacks();
                    break;
                case HitStatus.Headshot:
                    weaponModel.CrosshairController.ChangeState(typeof(HitmarkerHeadshotScope));
                    break;
                case HitStatus.Hit:
                    weaponModel.CrosshairController.ChangeState(typeof(HitmarkerScope));
                    break;
                case HitStatus.HeadshotKill:
                    weaponModel.CrosshairController.ChangeState(typeof(HitmarkerHeadshotKillScope));
                    break;
            }

            weaponModel.OnFire();
        }

        public abstract ShotResult ShootRaycast(Vector3 from, Vector3 direction);

        public virtual bool TryShoot(Vector3 from, Vector3 direction, out RaycastHit raycastHit)
        {
            return Physics.Raycast(from, direction, out raycastHit, weaponSO.Distance, weaponModel.GetHitLayerMask());
        }


        public HitStatus ProcessHit(RaycastHit hit, float damage, float damageReduction)
        {
            if (hit.collider.TryGetComponent(out IDamagable damagable))
            {
                var itemSO = weaponModel.GetItemSO();
                float finalDamage = damage * damageReduction;
                damagable.DoDamage(new PhysicalDamageType (finalDamage));
            }
            if (hit.collider.TryGetComponent(out IHitbox hitbox))
            {
                return hitbox.GetHitStatus();
            }
            return HitStatus.Miss;
        }
        #endregion
    }
}
