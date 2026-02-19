using MyToolz.Player.FPS.CombatSystem.Model;
using MyToolz.Utilities.Debug;
using UnityEngine;

namespace MyToolz.Player.FPS.CombatSystem.Presenter
{
    [System.Serializable]
    public class WeaponReloadingStateHandler : CombatSystemStateHandler
    {
        private float reloadStartTimeStamp;
        private float reloadTime;
        private bool reloadCompleted
        {
            get => reloadStartTimeStamp + reloadTime <= Time.time;
        }
        
        public override void Enter()
        {
            if (weaponSO.WeaponInfo == WeaponType.Melee)
            {
                DebugUtility.LogError(this, "Unable to handle reload state for melee!");
                return;
            }
            weaponSFXHandler.PlayReloadSFX();
            reloadTime = weaponSO.ReloadDuration;
            weaponModel.PlayReloadAnimation(); 
            reloadStartTimeStamp = Time.time;
        }

        public override void Update()
        {
            if (!reloadCompleted) return;
            weaponModel.Reload();
            if (weaponSO.Mag != null)
            {
                weaponVFXHandler.CmdPlayReloadingVFX();
            }
            combatSystemController.ExitState(GetType());
        }

        public override void Exit()
        {
           //weaponSFXHandler.CancelReloadSFX();
           weaponModel.StopAnimations();
            //CmdCancelReload();
        }
    }
}
