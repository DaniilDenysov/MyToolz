using UnityEngine;
using Sirenix.OdinInspector;
using DG.Tweening;
using MyToolz.Core;

namespace MyToolz.ScriptableObjects.AI.Platformer
{
    public enum EnabledKnockoffAxis { XOnly, YOnly, Both, None }

    [CreateAssetMenu(fileName = "EnemyMovementSO", menuName = "Enemies/EnemyMovementSO")]
    public class EnemyMovementSO : ScriptableObjectPlus
    {
        [FoldoutGroup("Navigation"), SerializeField, MinValue(0f)] private float arriveThreshold;
        [FoldoutGroup("Navigation"), SerializeField, MinValue(0f)] private float canMoveDirCheckDistance;

        [FoldoutGroup("Friction"), SerializeField, MinValue(0f)] private float groundFriction = 10f;
        [FoldoutGroup("Friction"), SerializeField, MinValue(0f)] private float airFriction = 10f;
        [FoldoutGroup("Movement"), SerializeField] private LayerMask layerMask;
        [FoldoutGroup("Movement"), SerializeField] private LayerMask wallLayerMask;
        [FoldoutGroup("Movement"), SerializeField, MinValue(0f)] private float maxSpeed = 5f;

        [FoldoutGroup("Acceleration"), SerializeField]
        private AnimationCurve accelerationCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        [FoldoutGroup("Acceleration"), SerializeField, MinValue(0.01f)]
        private float accelerationDuration = 0.25f;

        [FoldoutGroup("Deceleration"), SerializeField]
        private AnimationCurve decelerationCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        [FoldoutGroup("Deceleration"), SerializeField, MinValue(0.01f)]
        private float decelerationDuration = 0.2f;

        [FoldoutGroup("Ground Check"), SerializeField, MinValue(0f)]
        private float groundProbeRadius = 0.1f;

        [FoldoutGroup("Edges"), SerializeField, ToggleLeft]
        private bool stopAtEdges = true;

        [FoldoutGroup("Edges"), SerializeField, ShowIf("@stopAtEdges"), MinValue(0.01f)]
        private float edgeProbeDepth = 0.3f;

        [FoldoutGroup("Edges"), SerializeField, MinValue(0.01f), ShowIf("@useGravity")]
        private float maxFallSpeed = 26f;

        [FoldoutGroup("Edges"), SerializeField, MinValue(0.01f)]
        private bool useGravity = true;

        [FoldoutGroup("Visuals"), SerializeField, ToggleLeft]
        private bool invertFlip = false;

        [FoldoutGroup("Layers"), SerializeField]
        private LayerMask groundLayer;

        [FoldoutGroup("Charge"), SerializeField]
        private bool enableCharge = false;
        [FoldoutGroup("Charge"), ShowIf("@enableCharge"), SerializeField]
        private bool flipDuringCharge = false;

        [FoldoutGroup("Charge"), ShowIf("@enableCharge"), SerializeField]
        private float chargeSpeed = 20f;

        [FoldoutGroup("Charge"), ShowIf("@enableCharge"), SerializeField]
        private Ease chargeEase = Ease.InOutSine;

        [FoldoutGroup("Charge"), ShowIf("@enableCharge"), SerializeField]
        private float chargeDuration = 2f;

        [FoldoutGroup("Charge"), ShowIf("@enableCharge"), SerializeField]
        private EnabledKnockoffAxis chargeTargetAxis = EnabledKnockoffAxis.XOnly;

        [FoldoutGroup("Patrol"), SerializeField, MinValue(0.01f)] 
        private float arriveDistance = 0.2f;

        [FoldoutGroup("Patrol"), SerializeField, MinValue(0f)] 
        private float waitAtPoint = 0.25f;

        [FoldoutGroup("Search"), SerializeField, MinValue(0.1f)] 
        private float searchDuration = 3f;

        [FoldoutGroup("Search"), SerializeField, MinValue(0.01f)] 
        private float completeDistance = 0.4f;

        [FoldoutGroup("Follow"), SerializeField, MinValue(0.01f)] 
        private float stopDistance = 1.25f;

        [FoldoutGroup("Follow"), SerializeField, MinValue(0.01f)] 
        private float loseSightDistance = 12f;

        [FoldoutGroup("Jump"), SerializeField]
        private bool enableJump = false;

        [FoldoutGroup("Jump"), SerializeField, MinValue(0.01f), ShowIf("@enableJump")]
        private float jumpForce = 12f;

        [FoldoutGroup("KnockOff"), SerializeField] private EnabledKnockoffAxis enabledKnockoffAxis = EnabledKnockoffAxis.Both;
        public EnabledKnockoffAxis EnabledKnockoffAxis => enabledKnockoffAxis;
        public float MaxFallSpeed => maxFallSpeed;
        public float ArriveDistance => arriveDistance;
        public bool UseGravity => useGravity;
        public float WaitAtPoint => waitAtPoint;
        public float SearchDuration => searchDuration;
        public float CompleteDistance => completeDistance;
        public float ArriveThreshold => arriveThreshold;
        public float CanMoveDirCheckDistance => canMoveDirCheckDistance;
        public float StopDistance => stopDistance;
        public float LoseSightDistance => loseSightDistance;
        public EnabledKnockoffAxis ChargeTargetAxis => enabledKnockoffAxis;
        public Ease ChargeEase => chargeEase;
        public float GroundFriction => groundFriction;
        public float AirFriction => airFriction;
        public float MaxSpeed => maxSpeed;
        public AnimationCurve AccelerationCurve => accelerationCurve;
        public float AccelerationDuration => accelerationDuration;
        public AnimationCurve DecelerationCurve => decelerationCurve;
        public float DecelerationDuration => decelerationDuration;
        public float GroundProbeRadius => groundProbeRadius;
        public bool StopAtEdges => stopAtEdges;
        public float EdgeProbeDepth => edgeProbeDepth;
        public bool InvertFlip => invertFlip;
        public LayerMask GroundLayer => groundLayer;
        public LayerMask LayerMask => layerMask;
        public LayerMask WallLayerMask => wallLayerMask;
        public bool EnableJump => enableJump;
        public float JumpForce => jumpForce;
        public bool EnableCharge => enableCharge;
        public bool FlipDuringCharge => flipDuringCharge;
        public float ChargeSpeed => chargeSpeed;
        public float ChargeDuration => chargeDuration;
    }
}