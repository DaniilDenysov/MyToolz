using MyToolz.EditorToolz;
using UnityEngine;

namespace MyToolz.Player.FPS.CombatSystem.Model
{
    [CreateAssetMenu(fileName = "New Recoil Pattern", menuName = "NoSaints/CombatSystem/Recoil Pattern")]
    public class RecoilPatternSO : ScriptableObject
    {

        [SerializeField] private RecoilMode recoilMode = RecoilMode.Random;

        private enum RecoilMode
        {
            Fixed,
            Random,
            Controlled
        }
        [SerializeField, HideIf("@recoilMode!=RecoilMode.Fixed")] private Vector3 fixedRecoil;
        [SerializeField, Range(0, 180f), HideIf("@recoilMode!=RecoilMode.Random")] private float radius = 1;
        [SerializeField, Range(0, 180f), HideIf("@recoilMode!=RecoilMode.Controlled")] private float amount = 1;

        [SerializeField, HideIf("@recoilMode!=RecoilMode.Controlled")]
        private AnimationCurve vertical = new AnimationCurve(
            new Keyframe(0, 0),
            new Keyframe(1, 0));
        [SerializeField, HideIf("@recoilMode!=RecoilMode.Controlled")]
        private AnimationCurve horizontal = new AnimationCurve(
            new Keyframe(0, 0),
            new Keyframe(1, 0));
        [SerializeField, HideIf("@recoilMode!=RecoilMode.Controlled")]
        private AnimationCurve backward = new AnimationCurve(
    new Keyframe(0, 0),
    new Keyframe(1, 0));
        [Space]
        [Header("Camera settings")]
        [SerializeField, Range(0f, 100f)] private float cameraShakeMultipler = 1f;
        public float CameraShakeMultipler
        {
            get
            {
                return cameraShakeMultipler;
            }
        }

        /// <summary>
        /// returns random direction from a direction
        /// </summary>`
        /// <param name="percentage">The point of spray, considered as remaining ammo / mag size</param>
        /// <param name="direction">Where to randomize spray pattern</param>
        /// <returns></returns>
        public Vector3 GetPattern(float percentage, Vector3 direction)
        {
            Vector3 targetRecoil = Vector3.zero;
            if (recoilMode == RecoilMode.Fixed)
            {
                targetRecoil = fixedRecoil;
            }
            if (recoilMode == RecoilMode.Random)
            {
                targetRecoil += Random.insideUnitSphere * radius;
            }
            if (recoilMode == RecoilMode.Controlled)
            {
                targetRecoil.x += vertical.Evaluate(percentage) * amount;
                targetRecoil.y += horizontal.Evaluate(percentage) * amount;
                targetRecoil.z += backward.Evaluate(percentage) * amount;
            }
            return targetRecoil;
        }
    }
}
