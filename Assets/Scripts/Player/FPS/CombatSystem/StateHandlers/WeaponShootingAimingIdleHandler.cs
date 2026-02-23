using Mirror;
using MyToolz.DesignPatterns.EventBus;
using MyToolz.EditorToolz;
using MyToolz.InputManagement.Commands;
using MyToolz.Networking.Events;
using MyToolz.Networking.Utilities;
using MyToolz.Player.FPS.CombatSystem.Model;
using MyToolz.Player.FPS.CombatSystem.View;
using MyToolz.UI.Events;
using MyToolz.UI.Notifications.Model;
using System.Collections.Generic;
using UnityEngine;

namespace MyToolz.Player.FPS.CombatSystem.Presenter
{
    public struct PenetrationResult
    {
        public Vector3 HitPosition;
        public Vector3 NormalDirection;
        public string PhysicsMaterialName;
        public NetworkIdentity NetIdentity;
    }

    public struct ShotResult
    {
        public HitStatus HitStatus;
        public List<PenetrationResult> PenetrationPoints;
        public Vector3 HitPosVec;
        public Vector3 ShotDirectionVec;
        public Vector3 ShotOriginVec;
        public float Distance => Vector3.Distance(HitPosVec, ShotDirectionVec);
        public Vector3 NormalVec;
    }

    public enum HitStatus
    {
        Miss = 0,
        Headshot = 2,
        Hit = 1,
        Kill = 3,
        HeadshotKill = 4,
    }

    //WILL BE RETURNED BY SCRIPTABLE OBJECT
    //FOR FIREARMS
    //aiming and shooting and just shooting
    /// <summary>
    /// Responsible for handling shooting and aiming
    /// </summary>
    [System.Serializable]
    public class WeaponShootingAimingIdleHandler : PlayerAttackHandler
    {
        [SerializeField, Required] protected InputCommandSO aimInputCommandSO;
        protected bool wasAimPressedLastFrame = false;

        public override void Update()
        {
            UpdateSpreadSize(-Vector2.one*0.1f);
            HandleCrosshair();
            HandleAiming();
            HandleAttacking();
        }

        public override void Exit()
        {
            OnAim(false);
            weaponModel.StopFire();
        }

        #region Shooting handling
        public override void Attack()
        {
            int bulletsShooted = 1;
            if (needReload)
            {
                if (weaponModel.CanReload()) combatSystemController.EnterState(typeof(WeaponReloadingStateHandler));
                else weaponModel.StopFire();
                weaponSFXHandler.PlayEmptyMagSFX();
                return;
            }
            if (!canShoot) return;
            UpdateSpreadSize(Vector2.one);
            lastTimeFired = Time.time;
            if (weaponSO.Mag)
            {
                bulletsShooted = Mathf.Min(weaponModel.CurrentBullets, weaponSO.BulletsPerShot);
                weaponModel.CurrentBullets -= bulletsShooted;
            }

            //TODO: [DD] encapsulate to weapon view
            weaponVFXHandler.CmdPlayShootingVFX();
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
                        MessageType = typeof(WeaponShootingAimingIdleHandler),
                        Overflow = OverflowPolicy.DropOldest,
                        Priority = NotificationPriority.Normal,
                        Dedupe = DedupePolicy.None,
                        Text = "Kill"
                    });
                    weaponSFXHandler.PlayHitmarkerSFX(weaponSO.KillmarkerAudioClip);
                    PlayKillFeedbacks();
                    break;
                case HitStatus.Headshot:
                    weaponModel.CrosshairController.ChangeState(typeof(HitmarkerHeadshotScope));
                    weaponSFXHandler.PlayHitmarkerSFX(weaponSO.HeadshotmarkerAudioClip);
                    break;
                case HitStatus.Hit:
                    weaponModel.CrosshairController.ChangeState(typeof(HitmarkerScope));
                    weaponSFXHandler.PlayHitmarkerSFX(weaponSO.HitmarkerAudioClip);
                    break;
                case HitStatus.HeadshotKill:
                    weaponModel.CrosshairController.ChangeState(typeof(HitmarkerHeadshotKillScope));
                    EventBus<NotificationRequest>.Raise(new NotificationRequest()
                    {
                        MessageType = typeof(WeaponShootingAimingIdleHandler),
                        Overflow = OverflowPolicy.DropOldest,
                        Priority = NotificationPriority.Normal,
                        Dedupe = DedupePolicy.None,
                        Text = "Kill"
                    });
                    weaponSFXHandler.PlayHitmarkerSFX(weaponSO.KillmarkerAudioClip);
                    PlayKillFeedbacks();
                    break;
            }
            weaponModel.OnFire();
        }

        private PenetrationResult CreatePenetrationResult(Vector3 hitPoint,Vector3 hitNormal, GameObject hitObject)
        {
            NetworkIdentity networkIdentity = null;
            string materialName = "";
            if (hitObject != null)
            {
                hitObject.TryGetComponent(out networkIdentity);
                if (hitObject.TryGetComponent(out Collider hitCollider))
                {
                   materialName = MappingUtility.PhysicsMaterialToName(hitCollider.sharedMaterial);
                }    
            }
            return new PenetrationResult()
            {
                HitPosition = hitPoint,
                NormalDirection = hitNormal,
                PhysicsMaterialName = materialName,
                NetIdentity = networkIdentity
            };
        }

        public override ShotResult ShootRaycast(Vector3 from, Vector3 direction)
        {
            ShotResult hitResult = new ShotResult();
            var shootingPoint = weaponModel.GetShootingPoint();
            bool isFirearm = weaponSO.WeaponInfo == WeaponType.Firearm;
            float damageReduction = 1f;
            hitResult.ShotDirectionVec = direction.normalized;
            hitResult.ShotOriginVec = weaponModel.GetShootingPoint().position;
            hitResult.PenetrationPoints = new List<PenetrationResult>();
            hitResult.HitPosVec = hitResult.ShotDirectionVec * weaponSO.Distance;
            if (!TryShoot(from, direction.normalized, out RaycastHit hit))
            {
                hitResult.HitStatus = HitStatus.Miss;
                return hitResult;
            }

            if (IsPenetratable(hit.collider.gameObject, weaponSO.PenetrationLayers))
            {
                hitResult.PenetrationPoints.Add(CreatePenetrationResult(hit.point, hit.normal, hit.collider.gameObject));
                for (int i = 0; i < weaponSO.MaximumPenetrationCount; i++)
                {
                    Vector3 penetrationPoint = hit.point + (direction * weaponSO.PenetrationDepth);
                    if (!TryShoot(penetrationPoint, -direction.normalized, out hit)) break;
                    hitResult.PenetrationPoints.Add(CreatePenetrationResult(hit.point, hit.normal, hit.collider.gameObject));
                    if (TryShoot(hit.point + (direction * 0.1f), direction, out hit))
                    {
                        hitResult.PenetrationPoints.Add(CreatePenetrationResult(hit.point, hit.normal, hit.collider.gameObject));
                        if (!IsPenetratable(hit.collider.gameObject, weaponSO.PenetrationLayers)) break;
                    }
                    else break;
                }
                damageReduction -= weaponSO.DamageReductionPerpenetration;
            }

            if (hit.collider)
            {
                hitResult.HitStatus = ProcessHit(hit, weaponSO.Damage / weaponSO.Burst, damageReduction);
                hitResult.HitPosVec = hit.point;
                hitResult.PenetrationPoints.Add(CreatePenetrationResult(hit.point, hit.normal, hit.collider.gameObject));
            }
            hitResult.NormalVec = hit.normal;
            return hitResult;
        }


        public bool IsPenetratable(GameObject gameObject, LayerMask layerMask)
        {
            return (layerMask.value & (1 << gameObject.layer)) != 0;
        }

        #endregion
        #region Aim handling

        private void HandleAiming()
        {
            bool isToggleMode = weaponModel.IsZoomPressModeOn();
            bool aimInputActive = aimInputCommandSO.ReadValue<float>() != 0;

            if (isToggleMode)
            {
                if (aimInputActive && !wasAimPressedLastFrame)
                {
                    bool newAimState = !weaponModel.IsAiming();
                    OnAim(newAimState);
                }
                wasAimPressedLastFrame = aimInputActive;
            }
            else
            {
                OnAim(aimInputActive);
            }
        }

        private void OnAim(bool isAiming)
        {
            if (weaponModel.IsAiming() != isAiming)
            {
                weaponModel.SetAim(isAiming);
                weaponAnimationsHandler.OnAim(isAiming, weaponModel);
                UpdateFov();
            }
        }

        //TODO: [DD] encapsulate to weapon view
        private void UpdateFov()
        {
            //mainCamera.UpdateFov(weaponModel.CurrentFov, weaponModel.CurrentSensitivityMultiplier);
        }
        #endregion
    }
}
