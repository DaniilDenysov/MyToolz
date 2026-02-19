using MyToolz.HealthSystem;
using MyToolz.HealthSystem.Interfaces;
using MyToolz.Player.FPS.CombatSystem.View;
using MyToolz.Utilities.Debug;
using System.Threading.Tasks;
using UnityEngine;

namespace MyToolz.Player.FPS.CombatSystem.Presenter
{
    [System.Serializable]
    public class WeaponBashingStateHandler : CombatSystemStateHandler
    {

        public override void Enter()
        {
            Bash();
        }

        private async void Bash()
        {
            DebugUtility.Log(this, "Bashing");
            if (Physics.Raycast(mainCamera.transform.position, mainCamera.transform.forward, out var raycastHit, weaponSO.BashRange, weaponModel.GetHitLayerMask()))
            {
                 ProcessHit(raycastHit, weaponSO.BashDamage,1f);
            }
            //TODO: [MP] add animations
            await Task.Delay((int)(weaponSO.BashDelay * 1000));
            DebugUtility.Log(this, "Ended bashing");
            Exit();
        }

        public void ProcessHit(RaycastHit hit, float damage, float damageReduction)
        {
            if (hit.collider.TryGetComponent(out IDamagable damagable))
            {
                float finalDamage = damage * damageReduction;
                damagable.DoDamage(new PhysicalDamageType(finalDamage));
            }
            if (hit.collider.TryGetComponent(out IHitbox hitbox))
            {
                var status = hitbox.GetHitStatus();
                if (status == HitStatus.Kill) weaponModel.CrosshairController.ChangeState(typeof(HitmarkerKillScope));
                else if (status == HitStatus.Headshot) weaponModel.CrosshairController.ChangeState(typeof(HitmarkerHeadshotScope));
                else weaponModel.CrosshairController.ChangeState(typeof(HitmarkerScope));
            }
        }

        public override void Update()
        {

        }

        public override void Exit()
        {
            combatSystemController.ResetStateStack();
        }
    }
}
