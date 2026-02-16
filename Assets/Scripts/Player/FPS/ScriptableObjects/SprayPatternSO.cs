using Sirenix.OdinInspector;
using UnityEngine;

namespace MyToolz.Player.FPS.CombatSystem.Model
{
    [CreateAssetMenu(fileName = "New Spray Pattern", menuName = "NoSaints/CombatSystem/Spray Pattern")]
    public class SprayPatternSO : ScriptableObject
    {
        [SerializeField] private bool random = true;
        [SerializeField, Range(0, 180f), HideIf("random")] private float amount= 1;
        [SerializeField, Range(0,180f), HideIf("@!random")] private float radius = 1;
        [SerializeField, HideIf("@!random")]
        [PropertyRange("radius", 180f)]
        private float maxRadius = 1f;

        public float MaxRadius
        {
            get => maxRadius;
        }

        public float Amount
        {
            get
            {
                if (random)
                {
                    return radius;
                }
                else
                {
                    return amount;
                }
            }
        }
        [SerializeField, HideIf("random")] private AnimationCurve vertical = new AnimationCurve(
            new Keyframe(0, 0),
            new Keyframe(1, 0));
        [SerializeField, HideIf("random")] private AnimationCurve horizontal = new AnimationCurve(
            new Keyframe(0, 0),
            new Keyframe(1, 0));

        private Vector3 GetTargetSpread (float percentage)
        {
            Vector3 targetSpread = Vector3.zero;
            if (random) targetSpread += Random.insideUnitSphere * radius;
            else
            {
                targetSpread.x += vertical.Evaluate(percentage) * amount;
                targetSpread.y += horizontal.Evaluate(percentage) * amount;
            }
            return targetSpread;
        }

        /// <summary>
        /// returns random direction from a direction
        /// </summary>`
        /// <param name="percentage">The point of spray, considered as remaining ammo / mag size</param>
        /// <param name="direction">Where to randomize spray pattern</param>
        /// <returns></returns>
        public Vector3 GetPattern(float percentage, Vector3 direction)
        {
            Vector3 targetSpread = GetTargetSpread(percentage);
            return Vector3.Slerp(direction, targetSpread, Vector3.Distance(direction, targetSpread) / 180f);
        }
    }
}
