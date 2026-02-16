using UnityEngine;

namespace MyToolz.Player.FPS.CombatSystem.Presenter
{
    public class LethalEquipmentStateHandler : CombatSystemStateHandler
    {
        float _throwDuration;
        float _throwTimeStamp;
        private bool throwCompleted
        {
            get => _throwTimeStamp + _throwDuration <= Time.time;
        }
        
        public override void Enter()
        {
            _throwDuration = weaponAnimationsHandler.OnThrowGrenade(weaponModel);
            if (_throwDuration < 0)
            {
                combatSystemController.ExitState(GetType());
            }
            
            _throwTimeStamp = Time.time;
        }

        public override void Update()
        {
            if (!throwCompleted) return;
            
            combatSystemController.ExitState(GetType());
        }

        public override void Exit()
        {
            weaponModel.StopAnimations();
        }
    }
}