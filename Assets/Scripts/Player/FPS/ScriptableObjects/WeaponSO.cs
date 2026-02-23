using System;
using UnityEngine;
using UnityEngine.Serialization;
using MyToolz.ScriptableObjects.Audio;
using MyToolz.Player.FPS.LoadoutSystem.View;
using MyToolz.EditorToolz;
using MyToolz.InventorySystem.Models;

namespace MyToolz.Player.FPS.CombatSystem.Model
{
    public enum WeaponType
    {
        Firearm,
        Melee
    }

    public enum ShootingMode
    {
        Press,
        Hold
    }

    [CreateAssetMenu(fileName = "create new weapon", menuName = "NoSaints/CombatSystem/Weapon")]
    public class WeaponSO : ItemSO
    {
        //Displayable fields

        // [DisplayAs("Equip speed", 0, 100)]
        public float EquipSpeedDisplayValue
        {
            get
            {
                return 100 - Mathf.Clamp(((equipSpeed + unequipSpeed) / 5) * 100, 0, 100);
            }
        }

        [DisplayAs("DAMAGE", 0, 100)]
        public float DamageDisplayValue
        {
            get
            {
                return damage;
            }
        }

        [DisplayAs("FIRE RATE", 0, 100)]
        public float FireRateDisplayValue
        {
            get
            {
                return (1f - (fireRate / 3f)) * 100f;
            }
        }

        [DisplayAs("MOBILITY", 0, 100)]
        public float SpeedReductionDisplayValue
        {
            get
            {
                return 100 - speedReduction;
            }
        }

        [DisplayAs("ADS", 0, 100)]
        public float ADSSpeedDisplayValue
        {
            get
            {
                return 100 - speedReduction;
            }
        }

        [DisplayAs("RECOIL", 0, 100)]
        public float RecoilHandlingDisplayValue
        {
            get
            {
                return 100 - speedReduction;
            }
        }

        [DisplayAs("MAG SIZE", 0, 100)]
        public float MagSizeDisplayValue
        {
            get
            {
                if (mag == null) return -1;
                return Mathf.Clamp(mag.GetMaxBullets(), 0, 100);
            }
        }


        [Header("View")]
        [SerializeField] private WeaponBulletHoleMappingSO bulletHoleMapping;
        public WeaponBulletHoleMappingSO BulletHole { get => bulletHoleMapping; }
        [SerializeField] private GameObject pickUp;
        public GameObject PickUp { get => pickUp; }

        public WeaponModel Model { get => weaponModel; }
        [SerializeField] private WeaponModel weaponModel;

        [SerializeField] private RaycastProjectile projectile;
        public RaycastProjectile Projectile { get => projectile; }

        [Header("Stats")]
        [SerializeField, Range(0, 5)] private float equipSpeed = 1f;
        public float EquipSpeed { get => equipSpeed; }

        [SerializeField, Range(0, 100), Tooltip("Reduction is in percentages")] private int speedReduction = 10;
        public float SpeedReduction { get => speedReduction / 100f; }

        [SerializeField, Range(0, 5)] private float unequipSpeed = 1f;
        public float UnequipSpeed { get => equipSpeed; }

        [SerializeField] private SprayPatternSO defaultSprayPattern;
        public SprayPatternSO DefaultSprayPattern { get => defaultSprayPattern; }
        [SerializeField] private SprayPatternSO aimingSprayPattern;
        public SprayPatternSO AimingSprayPattern { get => aimingSprayPattern; }

        [SerializeField] private SprayPatternSO runningSprayPattern;
        public SprayPatternSO RunningSprayPattern { get => runningSprayPattern; }

        [SerializeField] private SprayPatternSO crouchingSprayPattern;
        public SprayPatternSO CrouchingSprayPattern { get => crouchingSprayPattern; }

        [SerializeField] private WeaponType weaponType;
        public WeaponType WeaponInfo { get => weaponType; }

        [SerializeField, Range(0, 1000)] private float bulletSpeed;
        public float BulletSpeed { get => bulletSpeed; }

        [SerializeField, Range(0, 3)] private float fireRate;
        public float FireRate { get => fireRate; }

        [SerializeField, Range(0, 100)] private float damage = 10f;
        public float Damage { get => damage; }

        [SerializeField, Range(0, 10000)] private float distance = 1000f;
        public float Distance { get => distance; }

        [SerializeField, Range(0, 10000), HideIf("@weaponType==WeaponType.Melee")] private float zoom = 20f;
        public float Zoom { get => zoom; }

        [SerializeField, Range(0, 1000)] private int burst = 1;
        public float Burst { get => burst; }

        [SerializeField, Range(0, 10), HideIf("@weaponType==WeaponType.Melee")] private float reloadDuration = 1;
        public float ReloadDuration { get => reloadDuration; }

        [SerializeField, Range(0, 1000)] private int bulletsPerShot = 1;
        public int BulletsPerShot { get => bulletsPerShot; }

        [SerializeField] private ShootingMode mode;
        public ShootingMode Mode { get => mode; }

        [SerializeField, Range(0, 1000), HideIf("@weaponType==WeaponType.Melee")] private float scopeTime;
        public float ScopeTime { get => scopeTime; }
        [SerializeField, HideIf("@weaponType==WeaponType.Melee")] private WeaponMagSO mag;
        public WeaponMagSO Mag { get => mag; }
        [SerializeField, Range(0, 1000), HideIf("@weaponType==WeaponType.Melee")] private int mags = 4;
        public int Mags { get => mags; }
        [Space]
        [Header("Bash")]
        [SerializeField, Range(0, 100)] private float bashDamage = 30f;
        public float BashDamage
        {
            get => bashDamage;
        }
        [SerializeField, Range(0, 100)] private float bashDelay = 5f;
        public float BashDelay
        {
            get => bashDelay;
        }
        [SerializeField, Range(0, 100)] private float bashRange = 3f;
        public float BashRange
        {
            get => bashRange;
        }
        [Header("Sounds")]
        [Header("Hitmarker")]
        [SerializeField] private AudioClipSO hitmarkerAudioClip;
        public AudioClipSO HitmarkerAudioClip { get => hitmarkerAudioClip; }
        [SerializeField] private AudioClipSO killmarkerAudioClip;
        public AudioClipSO KillmarkerAudioClip { get => killmarkerAudioClip; }
        [SerializeField] private AudioClipSO headshotmarkerAudioClip;
        public AudioClipSO HeadshotmarkerAudioClip { get => headshotmarkerAudioClip; }


        [SerializeField] private AudioClipSO shotAudioClip;
        public AudioClipSO ShotAudioClip { get => shotAudioClip; }
        [SerializeField] private AudioClipSO shellCasingAudioClip;
        public AudioClipSO ShellCasingAudioClip { get => shellCasingAudioClip; }
        [SerializeField] private AudioClipSO equipAudioClip;
        public AudioClipSO EquipAudioClip { get => equipAudioClip; }
        [SerializeField] private AudioClipSO unequipAudioClip;
        public AudioClipSO UnequipAudioClip { get => unequipAudioClip; }
        [SerializeField] private AudioClipSO movingAudioClip;
        public AudioClipSO MovingAudioClip { get => movingAudioClip; }
        [SerializeField] private AudioClipSO reloadAudioClip;
        public AudioClipSO ReloadAudioClip { get => reloadAudioClip; }

        [SerializeField] private AudioClipSO emptyMagAudioClip;
        public AudioClipSO EemptyMagAudioClip { get => emptyMagAudioClip; }

        [Header("Penetration")]
        [SerializeField, HideIf("@weaponType==WeaponType.Melee")] private LayerMask penetrationLayers;
        public LayerMask PenetrationLayers { get => penetrationLayers; }
        [SerializeField, HideIf("@weaponType==WeaponType.Melee")] private float penetrationDepth;
        public float PenetrationDepth { get => penetrationDepth; }
        [Range(0f, 1f), SerializeField, HideIf("@weaponType==WeaponType.Melee")] private float damageReductionPerpenetration = 0.2f;
        public float DamageReductionPerpenetration { get => damageReductionPerpenetration; }
        [SerializeField, HideIf("@weaponType==WeaponType.Melee")] private int maximumPenetrationCount;
        public int MaximumPenetrationCount { get => maximumPenetrationCount; }

        [Header("Aim")]
        [Min(0f)] public float aimFovZoom = 1f;
        [Range(0f, 2f)] public float sensitivityMultiplier = 1f;

        [Header("Recoil")]
        //public RecoilAnimData recoilData;
        //public RecoilPatternSettings recoilPatternSettings;

        [Header("Animations")]
        //[SerializeField] private CameraShakeSO shakeSO;
        //public CameraShakeSO ShakeSO
        //{
        //    get => shakeSO;
        //}
        [SerializeField][Range(0f, 120f)] public float fieldOfView = 90f;
        [FormerlySerializedAs("_playerWeaponAnimationSo")] public PlayerWeaponAnimationSO playerWeaponAnimationSo;
        //public FPSAnimationAsset fireClip;
        //public FPSAnimationAsset reloadClip;
        //public FPSCameraAnimation cameraReloadAnimation;
    }
}