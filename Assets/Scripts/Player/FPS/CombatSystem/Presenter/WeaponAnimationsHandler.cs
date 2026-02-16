using Mirror;
using MyToolz.Player.FPS.CombatSystem.Model;
using MyToolz.Player.FPS.LoadoutSystem.Model;
using Zenject;
namespace MyToolz.Player.FPS.CombatSystem.Presenter

{
    public class WeaponAnimationsHandler : NetworkBehaviour
    {
        #region Private fields

        private WeaponModel weaponModel
        {
            get
            {
                return combatSystemController.WeaponModel;
            }
        }

        private CombatSystemController combatSystemController;
        private WeaponLoadoutModel weaponLoadoutModel;

        #endregion

        #region Local

        [Inject]
        public void Construct(CombatSystemController combatSystemController, WeaponLoadoutModel weaponLoadoutModel)
        {
            this.combatSystemController = combatSystemController;
            this.weaponLoadoutModel = weaponLoadoutModel;
        }

        public void OnAim(bool isAimed, WeaponModel model)
        {
            //playerMovement.Lean(isAimed);
            //animationController.OnAim(isAimed, model);
        }

        public void OnEquip(WeaponModel weapon)
        {
            //animationController.OnEquip(weapon);
        }
        
        public float OnThrowGrenade(WeaponModel weapon)
        {
            //return animationController.OnThrowGrenade(weapon);
            return 0f;
        }

        #endregion

        #region Commands
        [Command(requiresAuthority = false)]
        public void CmdReload()
        {
            RpcReload();
        }
        [Command(requiresAuthority = false)]
        public void CmdCancelReload()
        {
            RpcCancelReload();
        }
        #endregion

        #region RPCs
        [ClientRpc(includeOwner = false)]
        public void RpcReload()
        {
            weaponModel.PlayReloadAnimation();

        }

        [ClientRpc(includeOwner = false)]
        public void RpcCancelReload()
        {
            weaponModel.StopAnimations();
        }
        #endregion
    }
}
