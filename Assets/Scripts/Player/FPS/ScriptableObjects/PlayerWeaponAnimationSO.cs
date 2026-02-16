//using KINEMATION.FPSAnimationFramework.Runtime.Camera;
//using KINEMATION.FPSAnimationFramework.Runtime.Layers.IkMotionLayer;
//using KINEMATION.FPSAnimationFramework.Runtime.Playables;
using UnityEngine;

namespace MyToolz.Player.FPS.CombatSystem.Model
{
    [CreateAssetMenu(fileName = "create new player weapon animations", menuName = "NoSaints/CombatSystem/PlayerAnimation")]
    public class PlayerWeaponAnimationSO : ScriptableObject
    {
        //public FPSCameraShake cameraShake;
        public RuntimeAnimatorController overrideController;

        //public IkMotionLayerSettings equipMotion;
        //public IkMotionLayerSettings unEquipMotion;

        //public FPSAnimationAsset lethalEquipmentThrowClip;
        //public FPSCameraAnimation lethalEquiopmentThrowCameraAnimation;
    }
}
