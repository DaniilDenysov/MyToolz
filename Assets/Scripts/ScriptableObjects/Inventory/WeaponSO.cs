using MyToolz.HealthSystem;
using MyToolz.ScriptableObjects.Audio;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MyToolz.ScriptableObjects.Inventory
{
    [CreateAssetMenu(fileName = "WeaponSO", menuName = "MyToolz/Inventory/Weapons/WeaponSO")]
    public class WeaponSO : ItemSO
    {
        [FoldoutGroup("Weapon"), SerializeField] protected float equipSpeed = 1f;
        [FoldoutGroup("Weapon"), SerializeField, HideIf("@knockBackForceModifier")] protected float knockBackForce = 3f;
        [FoldoutGroup("Weapon"), SerializeField] protected float attackRange = 3f;
        [FoldoutGroup("Weapon"), SerializeField] protected float pullForce = 3f;
        [FoldoutGroup("Weapon"), SerializeField] protected float distance = 2f;
        [FoldoutGroup("Weapon"), SerializeReference] protected DamageType damageType = new PhysicalDamageType();
        [FoldoutGroup("Weapon"), SerializeField] protected LayerMask layerMask;
        [FoldoutGroup("Weapon/Stun"), SerializeField] protected bool enableStun;
        [FoldoutGroup("Weapon/Stun"), SerializeField, ShowIf("@enableStun")] protected float stunDuration = 3f;
        [FoldoutGroup("Weapon/Audio"), SerializeField] protected AudioClipSO hitClip;
        [FoldoutGroup("Weapon/Audio"), SerializeField] protected AudioClipSO missClip;

        public bool EnableStun => enableStun;
        public float StunDuration => stunDuration;
        public AudioClipSO HitClip => hitClip;
        public AudioClipSO MissClip => missClip;
        public float EquipSpeed => equipSpeed;
        public float KnockBackForce => knockBackForce;
        public float AttackRange => attackRange;
        public float PullForce => pullForce;
        public float Distance => distance;
        public DamageType DamageType => damageType;
        public LayerMask LayerMask => layerMask;
    }
}
