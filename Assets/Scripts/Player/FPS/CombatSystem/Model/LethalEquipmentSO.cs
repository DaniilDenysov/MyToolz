using Mirror;
using MyToolz.EditorToolz;
using MyToolz.Player.FPS.CombatSystem.Model;
using MyToolz.Player.FPS.CombatSystem.Presenter;
using MyToolz.Player.FPS.Inventory;
using UnityEngine;

namespace MyToolz.Player.FPS.CombatSystem.Model
{
    [CreateAssetMenu(fileName = "LethalEquipmentSO", menuName = "create new Equipment")]

    public class LethalEquipmentSO : ItemSO
    {
        [Header("General")]
        [SerializeField] protected LethalEquipmentPresenterAbstract prefab;
        public LethalEquipmentPresenterAbstract Prefab { get => prefab; }
        [SerializeField] protected float timeToLive = 30f;
        public float TimeToLive { get => timeToLive; }
        [SerializeField] protected TTLPreset ttlPreset = TTLPreset.OnCall;
        public TTLPreset TTLPreset { get => ttlPreset; }
        [SerializeField, Range(0, 100)] protected float damageAmount = 100f;
        public float DamageAmount { get => damageAmount; }
        [SerializeField] protected bool enableDamageOnHit = false;
        [SerializeField, HideIf("@enableDamageOnHit")] protected float damageOnHit = 2f;
        public float DamageOnHit { get => damageOnHit; }
        [SerializeField, HideIf("@!enableDamageOnHit")] protected float damageOnHitThreshold = 0.6f;
        public float DamageOnHitThreshold { get => damageOnHitThreshold; }

        [SerializeField, Range(1, 100f)] protected float throwForce = 10f;
        public float ThrowForce { get => throwForce; }
        //[DisplayAs("THROW", 1, 100)]
        public float ThrowForceDisplay
        {
            get
            {
                return (1f - (throwForce / 100f)) * 100f;
            }
        }

        [SerializeField, Range(1, 10000f)] protected float throwRate = 1f;
        [SerializeField] protected bool enableAmmoLimit = true;

        //[DisplayAs("AMMO", 1, 10)]
        public float AmmoLimit
        {
            get
            {
                return (1f - (ammoLimit / 10f)) * 10f;
            }
        }

        [SerializeField, HideIf("@!enableAmmoLimit")] protected int ammoLimit = 10;
        public float ThrowRate { get => throwRate; }
        [SerializeField] protected TouchDownMode touchDownMode = TouchDownMode.Inertial;
        public TouchDownMode TouchDownMode { get => touchDownMode; }

        public bool TryGetAmmoLimit (out int limit)
        {
            limit = enableAmmoLimit ? ammoLimit : int.MaxValue;
            return enableAmmoLimit;
        }

        public bool IsDamageOnHitEnabled() => enableDamageOnHit;
    }

    public enum TouchDownMode
    {
        Inertial,
        NonInertial,
        Sticky
    }

    public enum TTLPreset
    {
        FirstCollision,
        OnStart,
        OnCall
    }
}