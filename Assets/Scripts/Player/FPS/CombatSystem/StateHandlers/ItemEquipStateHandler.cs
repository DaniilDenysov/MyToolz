namespace MyToolz.Player.FPS.CombatSystem.Presenter
{
    [System.Serializable]
    public class ItemEquipStateHandler : CombatSystemStateHandler
    {


        public override void Enter()
        {
            weaponSFXHandler.PlayEquipSFX();
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
