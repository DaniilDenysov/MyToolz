using System;
using UnityEngine;
//using Demo.Scripts.Runtime.Character;
//using KINEMATION.FPSAnimationFramework.Runtime.Camera;
//using KINEMATION.FPSAnimationFramework.Runtime.Core;
//using KINEMATION.FPSAnimationFramework.Runtime.Layers.IkMotionLayer;
//using KINEMATION.FPSAnimationFramework.Runtime.Playables;
//using KINEMATION.FPSAnimationFramework.Runtime.Recoil;
//using KINEMATION.KAnimationCore.Runtime.Input;
using TMPro;
using Zenject;
using MyToolz.DesignPatterns.EventBus;
using MyToolz.Player.FPS.CombatSystem.Presenter;
using MyToolz.DesignPatterns.MVP.View;
using MyToolz.Utilities.Debug;
using MyToolz.UI.Events;
using MyToolz.Player.FPS.CombatSystem.View;
using MyToolz.ScriptableObjects.GameSettings;
using MyToolz.EditorToolz;
using MyToolz.Extensions;
using MyToolz.UI.Notifications.Model;

namespace MyToolz.Player.FPS.CombatSystem.Events
{
    public struct OnWeaponPickedUp : IEvent
    {
        public string WeaponGuid;
        public Vector3 Position;
        public int CurrentBullets;
    }
}
namespace MyToolz.Player.FPS.CombatSystem.Model
{
    [Serializable]
    public class MagDisplay
    {
        [SerializeField] private TMP_Text currentBulletsDisplay;
        [SerializeField] private TMP_Text totalBulletsDisplay;

        public void Update(string current, string total)
        {
            if (currentBulletsDisplay != null) currentBulletsDisplay.text = current;
            if (totalBulletsDisplay != null) totalBulletsDisplay.text = total;
        }
    }
}
namespace MyToolz.Player.FPS.CombatSystem.Model
{
    public class WeaponModel : ItemModel<WeaponSO>
    {
        [SerializeReference] private WeaponVFXResolver weaponVFXResolver;
        public WeaponVFXResolver WeaponVFXResolver => weaponVFXResolver;
        [SerializeField] private Transform shootingPoint;
        [SerializeField] private LayerMask hitLayers;
        [SerializeField] private ParticleSystem bulletCase;
        [SerializeField] private ParticleSystem magazine;
        [SerializeField] private ParticleSystem muzzleLightClient, muzzleLightLocal;
        [SerializeField] private Transform magPoint;
        [SerializeField] private GameObject localInterface;
        [SerializeField] private GameObject localModel;
        [SerializeField] private GameObject clientModel;
        [SerializeField] private CrosshairController crosshairController;
        [SerializeField] private CanvasGroup ammoNotificationCanvasGroup;
        public CrosshairController CrosshairController { get => crosshairController; }
        public Transform MagPoint { get => magPoint; }  
        [SerializeField] private Animator weaponAnimator;
        public Animator WeaponAnimator { get => weaponAnimator; }

        private TMP_Text itemMagDisplay;
        [SerializeField] private TMP_Text weaponResponse;
        [SerializeField, Required] private BoolSettingSO zoomModeSO;
        [SerializeField, Required] private FloatSettingSO currentFovSO;
        [SerializeField, Range(0,31)] private int overlayLayer;
        [SerializeField] private GameObject overlayModel;

        private bool _isAiming;
        private string key => nameof(WeaponModel);
        private Type msgType => GetType();
        private Animator _controllerAnimator;
        
        private RecoilPatternSO _recoilPattern;
        private Animator _weaponAnimator;

        private IReadOnlyView<WeaponModel> view;

        [Inject]
        private void Construct(IReadOnlyView<WeaponModel> view)
        {
            this.view = view;
            view.Initialize(this);
        }

        public int MaxBullets
        {
            get
            {
                if (weaponMag != null)
                {
                    return weaponMag.GetMaxBullets();
                }
                else
                {
                    return 0;
                }
            }
        }

        private WeaponMagSO weaponMag
        {
            get
            {
               return itemSO.Mag;
            }
        }

        public int CurrentBullets 
        {
            set
            {
                currentBullets = value;
                UpdateMagDisplay();
            }
            get => currentBullets;
        }

        public bool IsZoomPressModeOn()
        {
            return zoomModeSO.CurrentValue;
        }

        /// <summary>
        /// How many bullets left in the mag
        /// </summary>
        public float RemainingMagPercentage
        {
            get
            {
                if (itemSO == null) return 0f;
                float magazineCapacity = MaxBullets;
                float res = (float)(magazineCapacity - currentBullets) /magazineCapacity;
                return res;
            }
        }

        public float CurrentFov
        {
            get
            {
                var fov = currentFovSO.CurrentValue.ToFloat();
                return fov * (_isAiming ? itemSO.aimFovZoom : 1f);
            }
           
         }

        public float CurrentSensitivityMultiplier => _isAiming ? itemSO.sensitivityMultiplier : 1f;
            
        [SerializeField] private int currentBullets;
        
        public int BulletsTotal
        {
            set
            {
                bulletsTotal = value;
                UpdateMagDisplay();
            }
            get => bulletsTotal;
        }
        private int bulletsTotal;

        private int currentSpreadIndex = 0;

        public void UpdateMagDisplay()
        {
            if (itemSO == null)
            {
                DebugUtility.LogWarning(this, "WeaponSO is null!");
                return;
            }
            if (itemSO.Mag == null) return;
            int maxBullets = itemSO.Mag.GetMaxBullets();
            if (weaponResponse != null)
            {
                float percent = ((float)currentBullets / maxBullets);
                if (percent > 0.25f)
                {
                    DisposeHUDNotification();
                }
                if (percent <= 0.25f)
                {
                    DisplayHUDNotification("Low ammo");
                }
                if (percent <= 0.1f)
                {
                    DisplayHUDNotification("Very low ammo");
                }
                if (percent == 0f)
                {
                    DisplayHUDNotification("Mag is empty");
                }
            }
            view.UpdateView(this);
            if (itemMagDisplay != null) itemMagDisplay.text = $"{bulletsTotal+currentBullets}";
        }

        public void SetMagDisplay (TMP_Text magDisplay)
        {
            this.itemMagDisplay = magDisplay;
            UpdateMagDisplay();
        }

        public bool CanReload ()
        {
            return bulletsTotal > 0 && currentBullets < MaxBullets;
        }

        public void LocalMode ()
        {
            localInterface.SetActive(true);
            localModel.SetActive (true);
        }

        public void ClientMode()
        {
            localInterface.SetActive(false);
            if (clientModel) clientModel.SetActive(true);
        }

        public void Reload()
        {
            if (itemSO.Mag == null) return;
            int maxBullets = itemSO.Mag.GetMaxBullets();
            int bulletsNeeded = Mathf.Clamp(maxBullets - currentBullets, 0, maxBullets);
            int bulletsToReload = Mathf.Min(bulletsNeeded, bulletsTotal);
            bulletsTotal -= bulletsToReload;
            CurrentBullets += bulletsToReload;
        }

        public void ResetBullets()
        {
            if (itemSO.Mag == null) return;
            bulletsTotal = itemSO.Mags * itemSO.Mag.GetMaxBullets();
            Reload();
        }

        public void RetreatSpreadPoint()
        {
            if (currentSpreadIndex > 0)
            {
                currentSpreadIndex--;
            }
        }

        protected void DisposeHUDNotification()
        {
            EventBus<NotificationClearRequest>.Raise(new NotificationClearRequest()
            {
                Key = key,
                MessageType = msgType
            });
        }

        protected void DisplayHUDNotification(string msg)
        {
            DisposeHUDNotification();
            EventBus<NotificationRequest>.Raise(new NotificationRequest()
            {
                Key = key,
                MessageType = msgType,
                Overflow = OverflowPolicy.ReplaceSameKeyOrDropNew,
                Priority = NotificationPriority.High,
                Dedupe = DedupePolicy.ReplaceIfSameKeyExists,
                Text = msg
            });
        }

        public void OnEquip(GameObject parent)
        {
            _weaponAnimator = GetComponentInChildren<Animator>();
            
            _controllerAnimator = parent.GetComponent<Animator>();
            
           
            UpdateMagDisplay();
            view.UpdateView(this);
            view.Show();
        }

        public override void OnAccessibleChanged(bool state)
        {
            if (!state) view?.Destroy(this);
            else view?.Initialize(this);
        }

        public void OnUnEquip()
        {
            //ChangeAmmoState(new DefaultAmmo());
            view.Hide();
        }
        
        public void OnFire()
        {
            if (_weaponAnimator != null )
            {
                _weaponAnimator.SetFloat("SpeedMultiplier", 1.0f / itemSO.FireRate);
                _weaponAnimator.Play("Fire", 0, 0f);
            }
        }

        public void StopFire()
        {
            if (_weaponAnimator != null && itemSO.Mode != ShootingMode.Press)
            {
                _weaponAnimator.Play("Idle", 0, 0f);
            }
           
        }
        
        public void PlayReloadAnimation()
        {

            var reloadMultiplier = itemSO.ReloadDuration;// / itemSO.reloadClip.clip.length;

            if (_weaponAnimator != null)
            {
                _weaponAnimator.Rebind();
                _weaponAnimator.CrossFade("Reload", 0.1f);
                _weaponAnimator.SetFloat("ReloadSpeed", reloadMultiplier);
            }
        }
        

        private float baseVFXLifetimeMax;
        private float baseVFXLifetimeMin;

        private void Start()
        {
            if (muzzleLightLocal != null)
            {
                var mainModule = muzzleLightLocal.main;
                baseVFXLifetimeMax = muzzleLightClient.main.startLifetime.constantMax;
                baseVFXLifetimeMin = muzzleLightClient.main.startLifetime.constantMin;
            }
            
        }


        public void StopAnimations()
        {
            if (_weaponAnimator != null)
            {
                _weaponAnimator.Rebind();
            }
        }
        
        public bool OnAimPressed()
        {
            
            return true;
        }

        public bool OnAimReleased()
        {
            return true;
        }

        public void ResetSpreadIndex()
        {
            currentSpreadIndex = 0;
        }
        public void PlayMuzzleFlash()
        {
            if (muzzleLightClient != null)
            {
                muzzleLightClient.Play();
            }

            if (muzzleLightLocal != null)
            {
                var mainModule = muzzleLightLocal.main;

                mainModule.startLifetime = new ParticleSystem.MinMaxCurve(
                    _isAiming ? baseVFXLifetimeMin / 20 : baseVFXLifetimeMin,
                    _isAiming ? baseVFXLifetimeMax / 20 : baseVFXLifetimeMax);
                muzzleLightLocal.Play();
            }
        }

        public void PlayBulletCase()
        {
            if (bulletCase != null) bulletCase?.Play();
        }

        public void PlayMagazineDrop()
        {
           if (magazine!=null) magazine.Play();
        }

        public void ChangeLocalLayer()
        {
            overlayModel.layer = overlayLayer;
            ChangeLocalLayerInner(overlayModel.transform);
        }

        public void ChangeLocalLayerInner(Transform loaclTransform)
        {
            for (int i = 0; i < loaclTransform.childCount; i++)
            {
                var child = loaclTransform.GetChild(i);
                if (child.childCount != 0) ChangeLocalLayerInner(child);
                child.gameObject.layer = overlayLayer;
            }
        }

        public LayerMask GetHitLayerMask() => hitLayers;
        public Transform GetShootingPoint() => shootingPoint;

        public void SetAim(bool isAim)
        {
            _isAiming = isAim;
            DebugUtility.Log($"Aiming{_isAiming}");
            if (isAim) crosshairController.Hide();
            else crosshairController.Show();
        }

        public bool IsAiming()
        {
            return _isAiming;
        }
    }
}
