using MyToolz.EditorToolz;
using MyToolz.HealthSystem;
using MyToolz.Projectiles;
using UnityEngine;

namespace MyToolz.ScriptableObjects.AI.Platformer
{
    public enum ProximityCastType { Circle, Box }
    public enum AttackType { Melee, Ranged, Both }
    public enum RangedAttackDirection { Horizontal, Vertical, Directional }
    public enum AttackCastType { Ray, Circle, Box }
    public enum SightCastType { Ray, Circle,  Box }

    [CreateAssetMenu(fileName = "EnemyCombatSO", menuName = "Enemies/EnemyCombatSO")]
    public class EnemyCombatSO : ScriptableObject
    {
        [FoldoutGroup("Sight"), SerializeField, Range(0.1f, 1000f)] private float viewDistance;
        [FoldoutGroup("Sight"), SerializeField, HideIf(nameof(NotUsingBoxSight))] private Vector2 sightBoxSize = Vector2.one;
        [FoldoutGroup("Sight"), SerializeField, HideIf(nameof(NotUsingBoxSight))] private Vector3 sightBoxOffset = Vector3.zero;
        [FoldoutGroup("Sight"), SerializeField, Range(5f, 360f), HideIf(nameof(NotUsingRaySight))] private float coneViewSize;
        [FoldoutGroup("Sight"), SerializeField, Range(1, 50), HideIf(nameof(NotUsingRaySight))] private uint precision = 1;
        [FoldoutGroup("Sight"), SerializeField] private SightCastType sightCastType = SightCastType.Ray;
        [FoldoutGroup("Sight"), SerializeField] private LayerMask viewLayerMask;
        [FoldoutGroup("Sight"), SerializeField] private bool checkObstracted;

        [FoldoutGroup("Attack"), SerializeField] private AttackType attackType = AttackType.Melee;
        [FoldoutGroup("Attack"), SerializeField] private bool shouldFaceTarget = false;
        [FoldoutGroup("Attack"), SerializeField, HideIf("@attackType == AttackType.Melee"), Required] private Projectile projectile;
        [FoldoutGroup("Attack"), SerializeField, HideIf("@attackType == AttackType.Melee")] private RangedAttackDirection direction = RangedAttackDirection.Directional;
        [FoldoutGroup("Attack"), SerializeField] private LayerMask layerMask;
        [FoldoutGroup("Attack"), SerializeField, HideIf("@attackType == AttackType.Ranged")] private AttackCastType castType = AttackCastType.Ray;

        [FoldoutGroup("Proximity Sight"), SerializeField] private ProximityCastType proximityCastType = ProximityCastType.Circle;
        [FoldoutGroup("Proximity Sight"), SerializeField, Range(0.1f, 1000f), ShowIf("@proximityCastType == ProximityCastType.Circle")] private float proximityRadius = 3f;
        [FoldoutGroup("Proximity Sight"), SerializeField, ShowIf("@proximityCastType == ProximityCastType.Box")] private Vector2 proximityBoxSize = Vector2.one;
        [FoldoutGroup("Proximity Sight"), SerializeField, ShowIf("@proximityCastType == ProximityCastType.Box")] private Vector3 proximityBoxOffset = Vector3.zero;

        [FoldoutGroup("Cooldown"), SerializeField, Range(0f, 1000f)] private float attackDuration;
        [FoldoutGroup("Cooldown"), SerializeField, Range(0f, 1000f), HideIf("@randomizeCoolDown")] private float coolDown;
        [FoldoutGroup("Cooldown"), SerializeField, ShowIf("@randomizeCoolDown")] private Vector2 randomCoolDown;
        [FoldoutGroup("Cooldown"), SerializeField] private bool randomizeCoolDown;

        [FoldoutGroup("Damage"), SerializeReference] private DamageType damageType = new PhysicalDamageType();

        [FoldoutGroup("Range"), SerializeField, Range(0f, 1000f), HideIf("@randomizeRange")] private float attackRange;
        [FoldoutGroup("Range"), SerializeField, Range(0f, 1000f), HideIf("@randomizeRange")] private float effectiveOutOfRangeDistance = 10f;
        [FoldoutGroup("Range"), SerializeField, ShowIf("@randomizeRange")] private Vector2 randomAttackRange;
        [FoldoutGroup("Range"), SerializeField] private bool randomizeRange;

        [FoldoutGroup("Geometry"), SerializeField, ShowIf("@castType == AttackCastType.Circle"), Min(0f)] private float circleRadius = 0.5f;
        [FoldoutGroup("Geometry"), SerializeField, ShowIf("@castType == AttackCastType.Box")] private Vector2 boxSize = Vector2.one;

        [FoldoutGroup("KnockOff"), SerializeField] private bool enableKnockOff = true;
        [FoldoutGroup("KnockOff"), SerializeField, Range(0f, 1000f), ShowIf("@enableKnockOff")] private float knockOffForce = 10f;
        [FoldoutGroup("KnockOff"), SerializeField, Range(0f, 1000f), ShowIf("@enableKnockOff")] private float closeCombatKnockOffForce = 100f;
        [FoldoutGroup("KnockOff"), SerializeField, ShowIf("@randomizeKnockOff")] private Vector2 randomKnockOffForce = new Vector2(5f, 15f);
        [FoldoutGroup("KnockOff"), SerializeField, ShowIf("@enableKnockOff")] private bool randomizeKnockOff;

        public float EffectiveOutOfRangeDistance => effectiveOutOfRangeDistance;
        public bool ShouldFaceTarget => shouldFaceTarget;
        public Projectile Projectile => projectile;
        public AttackType Attack => attackType;
        public RangedAttackDirection RangedAttackDirection => direction;
        public ProximityCastType ProximityCheckCastType => proximityCastType;
        public Vector2 ProximityBoxSize => proximityBoxSize;
        public Vector3 ProximityBoxOffset => proximityBoxOffset;

        public Vector2 SightBoxSize => sightBoxSize;
        public Vector3 SightBoxOffset => sightBoxOffset;

        public float ProximityRadius => proximityRadius;
        public float AttackDuration => attackDuration;
        public float ConeViewSize => coneViewSize;
        public float ViewDistance => viewDistance;
        public uint Precision => precision;
        public SightCastType SightCastType => sightCastType;
        public LayerMask ViewLayerMask => viewLayerMask;
        public bool CheckObstracted => checkObstracted;

        public float CoolDown => coolDown;
        public Vector2 RandomCoolDown => randomCoolDown;
        public bool RandomizeCoolDown => randomizeCoolDown;

        public DamageType DamageType => damageType;

        public float AttackRange => attackRange;
        public Vector2 RandomAttackRange => randomAttackRange;
        public bool RandomizeRange => randomizeRange;

        public LayerMask LayerMask => layerMask;
        public AttackCastType CastType => castType;

        public float CircleRadius => circleRadius;
        public Vector2 BoxSize => boxSize;

        public bool EnableKnockOff => enableKnockOff;
        public float KnockOffForce => knockOffForce;
        public float CloseCombatKnockOffForce => closeCombatKnockOffForce;
        public Vector2 RandomKnockOffForce => randomKnockOffForce;
        public bool RandomizeKnockOff => randomizeKnockOff;

        public float GetAttackRange() => randomizeRange ? Random.Range(randomAttackRange.x, randomAttackRange.y) : attackRange;
        public float GetKnockOffForce() => randomizeKnockOff ? Random.Range(randomKnockOffForce.x, randomKnockOffForce.y) : knockOffForce;

        private bool NotUsingRaySight() => sightCastType != SightCastType.Ray;
        private bool NotUsingBoxSight() => sightCastType != SightCastType.Box;
    }
}
