using System.Collections.Generic;
using MyToolz.DesignPatterns.EventBus;
using MyToolz.Networking.Events;
using MyToolz.Player.FPS.CombatSystem.Model;
using MyToolz.Player.FPS.CombatSystem.View;
using MyToolz.UI.Events;
using MyToolz.UI.Notifications.Model;
using UnityEngine;

namespace MyToolz.Player.FPS.CombatSystem.Presenter
{
    public class MeleeAttackingStateHandler : PlayerAttackHandler
    {
        private bool _isCurrentlyAttacking;
        
        public override void Enter()
        {
            lastTimeFired = -weaponSO.FireRate;
            _isCurrentlyAttacking = false;
        }

        public override void Exit()
        {
            weaponModel.StopFire();
        }

        public override void Update()
        {
           HandleAttacking();
        }
        
        #region Attack handlig

        public override void Attack()
        {
            if (!canShoot) return;
            EventBus<PlayerShot>.Raise(new PlayerShot() { WeaponSO = this.weaponSO });
            //combatSystemController.IsFiring = true;
            weaponSFXHandler.CmdPlaySFX();
            weaponVFXHandler.CmdPlayShootingVFX();
            HitStatus hitStatus = HitStatus.Miss;
            for (int i = 0; i < weaponSO.Burst; i++)
            {
                EventBus<PlayerShot>.Raise(new PlayerShot() { WeaponSO = this.weaponSO });
                var result = ShootRaycast(mainCamera.transform.position, spreadDirection);
                ShootRaycast(mainCamera.transform.position, spreadDirection);
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
                        MessageType = typeof(PlayerAttackHandler),
                        Overflow = OverflowPolicy.DropOldest,
                        Priority = NotificationPriority.Normal,
                        Dedupe = DedupePolicy.None,
                        Text = "Kill"
                    });
                    break;
                case HitStatus.Headshot:
                    weaponModel.CrosshairController.ChangeState(typeof(HitmarkerHeadshotScope));
                    break;
                case HitStatus.Hit:
                    weaponModel.CrosshairController.ChangeState(typeof(HitmarkerScope));
                    break;
            }
        }

        public void PlayAnimation()
        {
            lastTimeFired = Time.time;
            weaponModel.OnFire();
        }
        
        protected override void HandleAttacking()
        {
            bool isAttacking = shootInputCommandSO.WasPressedThisFrame();
            
            if (isAttacking && !_isCurrentlyAttacking)
            {
                _isCurrentlyAttacking = true;
                PlayAnimation();
            }
            
            if (_isCurrentlyAttacking && canShoot)
            {
                Attack();
                
                _isCurrentlyAttacking = false;
            }
        }

        public override ShotResult ShootRaycast(Vector3 from, Vector3 direction)
        {
            ShotResult hitResult = new ShotResult();
            //var shootingPoint = weaponModel.GetShootingPoint();
            bool isFirearm = weaponSO.WeaponInfo == WeaponType.Firearm;
            float damageReduction = 1f;
            hitResult.ShotDirectionVec = direction.normalized;
            hitResult.ShotOriginVec = from;
            hitResult.PenetrationPoints = new List<PenetrationResult>();
            if (!TryShoot(from, direction, out RaycastHit hit))
            {
                hitResult.HitStatus = HitStatus.Miss;
                return hitResult;
            }

            //TODO: [DD] separate handling to different handlers for melee and firearm 
            if (hit.collider)
            {
                hitResult.HitStatus = ProcessHit(hit, weaponSO.Damage, damageReduction);
                hitResult.HitPosVec = hit.point;
            }
            else
            {
                hitResult.HitPosVec = hitResult.ShotDirectionVec * weaponSO.Distance;
            }
            hitResult.NormalVec = hit.normal;           
            return hitResult;
        }

        public override bool TryShoot(Vector3 from, Vector3 direction, out RaycastHit raycastHit)
        {
            raycastHit = default;

            RaycastHit[] hits = new RaycastHit[1];
            int hitCount = Physics.SphereCastNonAlloc(
                from,
                0.5f,
                direction,
                hits,
                weaponSO.Distance,
                weaponModel.GetHitLayerMask()
            );

            for (int i = 0; i < hitCount; i++)
            {
                var hit = hits[i];
                if (hit.collider != null)
                {
                    raycastHit = hit;
                    return true;
                }
            }

            return false;
        }


        #endregion

    }
}
