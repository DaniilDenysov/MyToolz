using Sirenix.OdinInspector;
using UnityEngine;

namespace MyToolz.ScriptableObjects.Player.Platformer.Movement
{
    [CreateAssetMenu(fileName = "PlayerMovementStats", menuName = "ProjectClyde/Player/Movement/Stats")]
    public class PlayerMovementStats : ScriptableObject
    {
        [FoldoutGroup("Walk")]
        [SerializeField, Range(1f, 50f), Tooltip("Rate of speed buildup on ground")] private float groundAcceleration = 5f;
        [FoldoutGroup("Walk")]
        [SerializeField, Range(1f, 50f), Tooltip("Rate of speed buildup in air")] private float airAcceleration = 5f;
        [FoldoutGroup("Walk")]
        [SerializeField, Range(1f, 50f), Tooltip("How quickly movement slows on ground when input stops")] private float groundDeceleration = 20f;
        [FoldoutGroup("Walk")]
        [SerializeField, Range(1f, 50f), Tooltip("How quickly movement slows in air when input stops")] private float airDeceleration = 5f;

        [FoldoutGroup("Grounded")]
        [SerializeField, Tooltip("Layers considered solid ground")] private LayerMask groundLayer;
        [FoldoutGroup("Grounded")]
        [SerializeField, Range(0.001f, 0.1f), Tooltip("Vertical ray length beneath collider for ground check")] private float groundDetectionRayLength = 0.02f;
        [FoldoutGroup("Grounded")]
        [SerializeField, Range(0.001f, 0.1f), Tooltip("Vertical ray length above collider for ceiling check")] private float headDetectionRayLength = 0.02f;
        [FoldoutGroup("Grounded")]
        [SerializeField, Range(0.1f, 2f), Tooltip("Horizontal span of the ceiling check area")] private float headWidth = 0.75f;
        [FoldoutGroup("Jump")]
        [SerializeField, Range(1f, 50f), Tooltip("Maximum downward velocity")] private float maxFallSpeed = 26f;

        public float GroundAcceleration => groundAcceleration;
        public float AirAcceleration => airAcceleration;
        public float GroundDeceleration => groundDeceleration;
        public float AirDeceleration => airDeceleration;
        public LayerMask GroundLayer => groundLayer;
        public float GroundDetectionRayLength => groundDetectionRayLength;
        public float HeadDetectionRayLength => headDetectionRayLength;
        public float HeadWidth => headWidth;

        public float MaxFallSpeed => maxFallSpeed;
        public float Gravity { get; private set; }
        public float InitialJumpVelocity { get; private set; }
        public float AdjustmentFactorHeight { get; private set; }

        public void Calculate(float jumpHeight, float jumpHeightCompensationFactor, float timeTillJumpApex)
        {
            AdjustmentFactorHeight = jumpHeight * jumpHeightCompensationFactor;
            Gravity = -(2f * AdjustmentFactorHeight) / Mathf.Pow(timeTillJumpApex, 2f);
            InitialJumpVelocity = Mathf.Abs(Gravity) * timeTillJumpApex;
        }
    }
}
