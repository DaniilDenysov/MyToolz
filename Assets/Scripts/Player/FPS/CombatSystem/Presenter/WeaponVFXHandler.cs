using Mirror;
using MyToolz.DesignPatterns.EventBus;
using MyToolz.Events;
using MyToolz.Player.FPS.CombatSystem.Model;
using MyToolz.Player.FPS.DisposableObjects;
using UnityEngine;
using Zenject;

namespace MyToolz.Player.FPS.CombatSystem.Presenter
{
    [System.Serializable]
    public class MeleeVFXResolver : WeaponVFXResolver
    {
        public override void SpawnMag()
        {
            
        }
        public override void PlayShootingVFX()
        {

        }


        public override void HandleHitVFX(ShotResult hitResult)
        {
           
        }

        public override void SpawnBulletHole(PenetrationResult penetrationResult)
        {
          
        }

        public override void SpawnProjectile(ShotResult hitResult)
        {
          
        }
    }

    [System.Serializable]
    public class WeaponVFXResolver
    {
        #region Protected fields
        protected CombatSystemController combatSystemController;
        protected WeaponSO weaponSO => weaponModel.GetItemSO();
        protected WeaponModel weaponModel => combatSystemController.WeaponModel;
        #endregion

        public WeaponVFXResolver Construct(CombatSystemController combatSystemController)
        {
            this.combatSystemController = combatSystemController;
            return this;
        }

        public virtual void SpawnMag()
        {
            if (!weaponModel.GetItemSO().Mag.Prefab.TryGetComponent(out DisposableMag prefab) || weaponModel.MagPoint == null) return;

            EventBus<PoolRequest<DisposableMag>>.Raise(new PoolRequest<DisposableMag>()
            {
                Prefab = prefab,
                Rotation = Quaternion.Euler(-90, 0, 0),
                Position = weaponModel.MagPoint.position
            });
        }

        public virtual void PlayShootingVFX()
        {
            weaponModel.PlayBulletCase();
            weaponModel.PlayMuzzleFlash();
        }

        public virtual void HandleHitVFX(ShotResult hitResult)
        {
            SpawnProjectile(hitResult);
            foreach (var penetration in hitResult.PenetrationPoints)
            {
                SpawnBulletHole(penetration);
            }
        }

        public virtual void SpawnBulletHole(PenetrationResult penetrationResult)
        {
            Vector3 hitPosition = penetrationResult.HitPosition;
            Vector3 hitNormal = penetrationResult.NormalDirection;
            NetworkIdentity networkIdentity = penetrationResult.NetIdentity;
            if (weaponSO.BulletHole == null) return;
            if (networkIdentity != null && networkIdentity.gameObject.activeInHierarchy == false) return;
            EventBus<PoolRequest<DisposableBulletHole>>.Raise(new PoolRequest<DisposableBulletHole>()
            {
                Prefab = weaponSO.BulletHole.GetBulletHolePrefab(penetrationResult.PhysicsMaterialName),
                Callback = (bulletHole) =>
                {
                    if (networkIdentity != null)
                    {
                        bulletHole.transform.SetParent(networkIdentity.transform);
                    }
                    else
                    {
                        bulletHole.transform.SetParent(null);
                    }
                    bulletHole.transform.position = hitPosition;
                    bulletHole.transform.rotation = Quaternion.LookRotation(hitNormal);
                    bulletHole.Play();
                }
            });
        }

        public virtual void SpawnProjectile(ShotResult hitResult)
        {
            EventBus<PoolRequest<RaycastProjectile>>.Raise(new PoolRequest<RaycastProjectile>()
            {
                Prefab = weaponSO.Projectile,
                Callback = (projectile) =>
                {
                    if (projectile.TryGetComponent(out RaycastProjectile raycastProjectile))
                    {
                        raycastProjectile.Fire(weaponSO.BulletSpeed, hitResult.ShotOriginVec, hitResult.HitPosVec);
                    }
                }
            });
        }

        public virtual void OnEnable()
        {

        }

        public virtual void OnDisable()
        {

        }

    }

    public class WeaponVFXHandler : NetworkBehaviour
    {
        #region Protected fields
        protected WeaponVFXResolver vfxResolver
        {
            get
            {
                if (weaponModel == null) return null;
                return weaponModel.WeaponVFXResolver?.Construct(combatSystemController);
            }
        }

        protected CombatSystemController combatSystemController;

        protected WeaponSO weaponSO => weaponModel.GetItemSO();

        protected WeaponModel weaponModel => combatSystemController.WeaponModel;
        #endregion

        [Inject]
        public void Construct(CombatSystemController combatSystemController)
        {
            this.combatSystemController = combatSystemController;
        }

        private void OnEnable()
        {
            vfxResolver?.OnEnable();
        }

        private void OnDisable()
        {
            vfxResolver?.OnDisable();
        }

        #region Hit vfx handling
        [Command(requiresAuthority = false)]
        public void CmdHandleHitVFX(ShotResult hitResult)
        {
            RpcHandleHitVFX(hitResult);
        }

        [ClientRpc]
        public void RpcHandleHitVFX(ShotResult hitResult)
        {
            vfxResolver?.HandleHitVFX(hitResult);
        }
        #endregion

        #region Muzzle flash and bullet case
        [Command(requiresAuthority = false)]
        public void CmdPlayShootingVFX()
        {
            RpcPlayShootingVFX();
        }

        [ClientRpc]
        public void RpcPlayShootingVFX()
        {
            vfxResolver?.PlayShootingVFX();
        }
        #endregion

        #region Reload vfx handling
        [Command(requiresAuthority = false)]
        public void CmdPlayReloadingVFX()
        {
            RpcSpawnMag();
        }

        [ClientRpc]
        public void RpcSpawnMag()
        {
            SpawnMag();
        }

        public void SpawnMag()
        {
            vfxResolver?.SpawnMag();
        }
        #endregion
    }
}