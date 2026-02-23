using Mirror;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System;
using System.Threading;
using Zenject;
using MyToolz.Player.FPS.CombatSystem.Model;
using MyToolz.Networking.ScriptableObjects;
using MyToolz.IO;
using MyToolz.DesignPatterns.EventBus;
using MyToolz.Events;
using MyToolz.Utilities.Debug;
using MyToolz.Player.FPS.CombatSystem;
using MyToolz.Player.FPS.CombatSystem.View;
using MyToolz.UI.Events;
using MyToolz.Networking.Core;
using MyToolz.EditorToolz;
using MyToolz.Player.FPS.CombatSystem.Events;
using MyToolz.Player.FPS.CombatSystem.Presenter;
using MyToolz.UI.Notifications.Model;
using MyToolz.InputManagement.Commands;

namespace MyToolz.Player.FPS.LoadoutSystem.Model
{
    public class WeaponLoadoutModel : NetworkBehaviour, IEventListener
    {
        public static Action OnLocalWeaponLoadoutUpdated;

        [SerializeField] private WeaponView weaponView;
        [SerializeField, SyncVar(hook = nameof(OnCurrentWeaponChanged))] private string currentWeaponGuid;
        [SerializeField] private NetworkCharacter networkCharacter;
        [SerializeField] private CombatSystemController weaponController;
        [SerializeField] private UnityEvent onLoadoutChanged;
        [SerializeField] private MagDisplay mainMagDisplay;
        [SerializeField] private AssetDatabaseMappingSO assetDatabaseMappingSO;
        private string previousWeaponGuid; //temp var 
        private List<WeaponModel> currentLoadout = new List<WeaponModel>();
        private Dictionary<string, string> guidToInstance = new Dictionary<string, string>(); //PrefabGuid -> InstanceId
        private Dictionary<string, WeaponModel> localLoadoutWeapons = new Dictionary<string, WeaponModel>(); // InstanceId -> WeaponModel
        private SyncDictionary<string, string> instanceToGuidMappings = new SyncDictionary<string, string>(); // InstanceId -> PrefabGuid
        [SerializeField] private bool enableClientMode = false;
        [SerializeField] private Transform _loadoutParent;

        private CancellationTokenSource equipCancellationTokenSource;
        private bool initializing;
        public WeaponModel[] GetWeapons() => localLoadoutWeapons.Values.ToArray(); 
        public string GetCurrentWeaponGuid() => currentWeaponGuid;

        private EventBinding<OnWeaponPickedUp> onPlayerPickUpEventBinding;
        private ISaver<Loadout> saver;
        private DiContainer container;
        private GameModeSO gameModeSO;

        [SerializeField, Required] private InputCommandSO mainWeapon;
        [SerializeField, Required] private InputCommandSO weaponCycle;
        [SerializeField, Required] private InputCommandSO secondaryWeapon;
        [SerializeField, Required] private InputCommandSO thirdWeapon;

        [Inject]
        private void Construct(GameModeSO gameModeSO,ISaver<Loadout> saver,DiContainer container)
        {
            this.saver = saver;
            this.container = container;
            this.gameModeSO = gameModeSO;
        }

        private void Start()
        {
            if (isOwned)
            {
                RegisterEvents();
            }
        }

        private void OnWeaponPickUp(OnWeaponPickedUp onPlayerPickUp)
        {
            var guid = onPlayerPickUp.WeaponGuid;
            if (HasWeapon(guid))
            {
                string instanceGuid = guidToInstance[guid];
                EventBus<NotificationRequest>.Raise(new NotificationRequest()
                {
                    MessageType = typeof(WeaponLoadoutModel),
                    Overflow = OverflowPolicy.DropOldest,
                    Priority = NotificationPriority.Low,
                    Dedupe = DedupePolicy.None,
                    Text = $"{onPlayerPickUp.CurrentBullets}"
                });
                AddBullets(instanceGuid, onPlayerPickUp.CurrentBullets);    
            }
            else
            {

                if (!assetDatabaseMappingSO.TryGetPrefab(guid, out GameObject defaultWeapon)) return;
                if (!defaultWeapon.TryGetComponent(out WeaponModel defaultWeaponModel)) return;
                var newWeaponSO = defaultWeaponModel.GetItemSO();
                var currentWeapon = currentLoadout.ElementAt(0);//(int)newWeaponSO.LoadoutCategory);
                currentWeapon.IsAccessible = false;
                var currentWeaponSO = currentWeapon.GetItemSO();
                currentLoadout.Remove(currentWeapon);
                CmdDropWeapon(currentWeaponGuid, localLoadoutWeapons[currentWeaponGuid].CurrentBullets, onPlayerPickUp.Position);
                WeaponModel instance = null;
                if (!guidToInstance.TryGetValue(guid, out string instanceId) || !localLoadoutWeapons.ContainsKey(instanceId))
                {
                    instance = CreateWeapon(defaultWeaponModel, guid);
                }
                else
                {
                    instance = localLoadoutWeapons[instanceId];
                    instance.ResetBullets();
                    instance.gameObject.SetActive(false);
                    instance.IsAccessible = true;
                }   
                //currentLoadout.Insert((int)newWeaponSO.LoadoutCategory, instance);
                //if (newWeaponSO.LoadoutCategory == currentWeaponSO.LoadoutCategory)
                //{
                //    EquipWeapon((int)newWeaponSO.LoadoutCategory, false);
                //}
                onLoadoutChanged?.Invoke();
            }
        }

        [Command(requiresAuthority = false)]
        public void CmdDropWeapon(string currentWeaponGuid, int currentBullets,Vector3 position)
        {
            if (localLoadoutWeapons == null) return;
            if (!localLoadoutWeapons.TryGetValue(currentWeaponGuid, out var weaponModel)) return;
            PickableWeapon prefab = weaponModel.GetItemSO().PickUp.GetComponent<PickableWeapon>();
            if (prefab == null) return;
            if (weaponModel == null) return;
            EventBus<PoolRequest<PickableWeapon>>.Raise(new PoolRequest<PickableWeapon>()
            {
                Prefab = prefab,
                Position = position,
                Rotation = Quaternion.identity,
                Callback = (pickup) =>
                {
                    if (pickup.TryGetComponent(out PickableWeapon model))
                    {
                        WeaponSO weaponSO = weaponModel.GetItemSO();
                        model.Construct(currentBullets, instanceToGuidMappings[currentWeaponGuid]);
                    }
                }
            });
        }

        public void SelectWeaponByIndex(int index)
        {
            if (isOwned)
            {
                if (localLoadoutWeapons.Count == 0) return;
                var weaponModel = currentLoadout[index];
                if (assetDatabaseMappingSO.TryGetGuid(weaponModel.GetItemSO().Model.gameObject, out string guid))
                {
                    if (guidToInstance.TryGetValue(guid,out string instanceGuid))
                    {
                        DeselectWeapon(currentWeaponGuid);
                        currentWeaponGuid = instanceGuid;
                        SelectWeapon(currentWeaponGuid);
                    }
                }
            }
        }

        private bool AreWeaponsMismatch(int weaponIndexOrOffset, bool isOffset)
        {
            if (!isOwned || currentLoadout == null || currentLoadout.Count == 0)
                return false;

            int targetIndex = -1;

            if (isOffset)
            {
                if (!localLoadoutWeapons.TryGetValue(currentWeaponGuid, out WeaponModel currentWeapon))
                    return false;

                int currentIndex = currentLoadout.IndexOf(currentWeapon);
                if (currentIndex == -1) currentIndex = 0;
                targetIndex = (currentIndex + weaponIndexOrOffset + currentLoadout.Count) % currentLoadout.Count;
            }
            else
            {
                if (weaponIndexOrOffset < 0 || weaponIndexOrOffset >= currentLoadout.Count)
                    return false;

                targetIndex = weaponIndexOrOffset;
            }

            var desiredWeaponModel = currentLoadout[targetIndex];
            if (assetDatabaseMappingSO.TryGetGuid(desiredWeaponModel.GetItemSO().Model.gameObject, out string desiredGuid))
            {
                if (guidToInstance.TryGetValue(desiredGuid, out string desiredInstanceGuid))
                {
                    return currentWeaponGuid != desiredInstanceGuid;
                }
            }

            return false;
        }

        private async void EquipWeapon(int weaponIndexOrOffset, bool isOffset)
        {
            if (!isOwned) return;

            if (equipCancellationTokenSource != null)
            {
                equipCancellationTokenSource.Cancel();
                equipCancellationTokenSource = null;
            }

            if (!AreWeaponsMismatch(weaponIndexOrOffset,isOffset)) return;
            var equipState = typeof(ItemEquipStateHandler);
            weaponController.EnterState(equipState);
            equipCancellationTokenSource = new CancellationTokenSource();

            try
            {
               // await Task.Delay((int)(animationController.getEquipDelay() * 1000), equipCancellationTokenSource.Token);

                if (isOffset)
                    SelectWeaponByOffset(weaponIndexOrOffset);
                else
                    SelectWeaponByIndex(weaponIndexOrOffset);

               // await Task.Delay((int)(animationController.getEquipDelay() * 1000), equipCancellationTokenSource.Token);

                weaponController.ExitState(equipState);
                equipCancellationTokenSource = null;
            }
            catch (Exception ex)
            {
                weaponController.ExitState(equipState);
                equipCancellationTokenSource = null;
            }
        }

        private void OnMainWeaponSelected(InputCommandSO inputCommandSO) => EquipWeapon(0, false);
        private void OnSecondaryWeaponSelected(InputCommandSO inputCommandSO) => EquipWeapon(1, false);
        private void OnThirdWeaponSelected(InputCommandSO inputCommandSO) => EquipWeapon(2, false);

        private void OnWeaponCycle(InputCommandSO inputCommandSO)
        {
            float scrollInput = inputCommandSO.ReadValue<float>();
            int direction = scrollInput > 0 ? -1 : (scrollInput < 0 ? 1 : 0);
            if (direction != 0)
            {
                EquipWeapon(direction, true);
            }
        }
        public void HideWeapon()
        {
            if (isOwned)
            {
                DeselectWeapon(currentWeaponGuid);
                previousWeaponGuid = currentWeaponGuid;
                currentWeaponGuid = string.Empty;
            }
        }

        public void SelectHiddenWeapon()
        {
            if (isOwned)
            {
                if (previousWeaponGuid != null) {
                    currentWeaponGuid = previousWeaponGuid;
                    SelectWeapon(previousWeaponGuid);
                    previousWeaponGuid = null;
                }
            }
        }

        public string GetWeaponInstanceId(WeaponModel weaponModel)
        {
            foreach (var pair in localLoadoutWeapons)
            {
                if (pair.Value == weaponModel)
                {
                    return pair.Key;
                }
            }
            return null;
        }


        public void EnableClientMode()
        {
            enableClientMode = true;
            DebugUtility.Log(this, "Enabled client mode");
            foreach (var wpn in _loadoutParent.GetComponentsInChildren<WeaponModel>())
            {
                DebugUtility.Log(this, wpn.name);
                InitializeView(wpn);
            }
        }


        public bool HasWeapon(string weaponGuid)
        {
            if (!guidToInstance.TryGetValue(weaponGuid, out string instanceGuid)) return false;
            if (!localLoadoutWeapons.TryGetValue(instanceGuid, out var model)) return false;
            return model.IsAccessible;
        }

        public void AddBullets(string weaponGuid, int bullets)
        {
            if (!localLoadoutWeapons.TryGetValue(weaponGuid, out WeaponModel model)) return;
            model.BulletsTotal += bullets;
        }

        public void SelectWeaponByOffset(int offset)
        {
            if (localLoadoutWeapons.Count == 0) return;
            if (!localLoadoutWeapons.TryGetValue(currentWeaponGuid, out WeaponModel currentWeapon)) return;
            int currentIndex = currentLoadout.IndexOf(currentWeapon);
            if (currentIndex == -1) currentIndex = 0;
            int newIndex = (currentIndex + offset + currentLoadout.Count) % currentLoadout.Count;
            DeselectWeapon(currentWeaponGuid);
            currentWeaponGuid = GetWeaponInstanceId(currentLoadout[newIndex]);
            SelectWeapon(currentWeaponGuid);
        }

        public void InitializeLoadout()
        {
            if (initializing) return;
            initializing = true;
            assetDatabaseMappingSO.InitializeAssetMap();
            foreach (var defaultWeapon in currentLoadout)
            {
                defaultWeapon.IsAccessible = false;
            }
            currentLoadout.Clear();
            var loadout = saver.Load()?.ToWeaponSOs<WeaponSO>().OrderBy(w => (int)w.Key);
            if (loadout == null)
            {
                initializing = false;
                return;
            }
            foreach (var defaultWeapon in loadout)
            {
                if (assetDatabaseMappingSO.TryGetGuid(defaultWeapon.Value.Model.gameObject, out string guid))
                {
                        if (!instanceToGuidMappings.Values.Contains(guid))
                        {
                            currentLoadout.Add(CreateWeapon(defaultWeapon.Value.Model, guid));
                        }
                        else if (guidToInstance.TryGetValue(guid, out string instanceId))
                        {
                            if (localLoadoutWeapons.TryGetValue(instanceId, out WeaponModel model))
                            {
                                if (model == null)
                                {
                                  localLoadoutWeapons.Remove(instanceId);
                                  continue;
                                }
                                currentLoadout.Add(model);
                                model.ResetBullets();
                                model.gameObject.SetActive(false);
                                model.IsAccessible = true;
                            }
                        }
                }
            }
            onLoadoutChanged?.Invoke();
            if (localLoadoutWeapons.Any())
            {
                SelectWeaponByIndex(0);
            }
            initializing = false;
        }

        private WeaponModel CreateWeapon(WeaponModel defaultWeapon, string guid)
        {
            DebugUtility.Log(this, $"PICKED UP WEAPON");
            DebugUtility.Log(this, $"{_loadoutParent}");
            //var instance = Instantiate(defaultWeapon, _loadoutParent);
            var instance = container.InstantiatePrefab(defaultWeapon, _loadoutParent).GetComponent<WeaponModel>();
            var uniqueId = System.Guid.NewGuid().ToString();
            instanceToGuidMappings[uniqueId] = guid;
            localLoadoutWeapons[uniqueId] = instance;
            guidToInstance[guid] = uniqueId;
            instance.CurrentBullets = instance.GetItemSO().Mag != null ? instance.GetItemSO().Mag.GetMaxBullets() : 0;
            instance.BulletsTotal = instance.CurrentBullets * instance.GetItemSO().Mags;
            instance.gameObject.SetActive(false);
            return instance;
        }

        private void SelectWeapon(string instanceId)
        {
            if (localLoadoutWeapons.TryGetValue(instanceId, out WeaponModel weaponModel))
            {
                SelectWeapon(weaponModel);
            }
        }

        private void SelectWeapon(WeaponModel weaponModel)
        {
                InitializeView(weaponModel);
                weaponModel.gameObject.SetActive(true);
                weaponController.OnNewWeaponEquiped(weaponModel);
        }

        private void DeselectWeapon(string instanceId)
        {
            if (localLoadoutWeapons.TryGetValue(instanceId, out WeaponModel weaponModel))
            {
                DeselectWeapon(weaponModel);
            }
        }

        private void DeselectWeapon(WeaponModel weaponModel)
        {
            weaponModel.gameObject.SetActive(false);
        }

        public void InitializeView(WeaponModel obj)
        {
            if (enableClientMode)
            {
                obj.ClientMode();
            }
            else
            {
                obj.LocalMode();
            }
        }

        public void AddWeapon(string instanceGuid, WeaponModel prefab)
        {
            var instance = container.InstantiatePrefab(prefab, _loadoutParent).GetComponent<WeaponModel>();
            InitializeView(instance);
            localLoadoutWeapons[instanceGuid] = instance;
            instance.CurrentBullets = instance.CurrentBullets = instance.GetItemSO().Mag != null ? instance.GetItemSO().Mag.GetMaxBullets() : 0;
            //SelectWeapon(currentWeaponGuid);
        }


        [Server]
        public void SpawnCurrentWeapon()
        {
            if (localLoadoutWeapons == null) return;
            if (!localLoadoutWeapons.TryGetValue(currentWeaponGuid, out var weaponModel)) return;
            var pickUp = weaponModel.GetItemSO().PickUp;
            if (pickUp == null) return;
            if (!pickUp.TryGetComponent(out PickableWeapon prefab)) return;
            EventBus<PoolRequest<PickableWeapon>>.Raise(new PoolRequest<PickableWeapon>()
            {
                Prefab = prefab,
                Position = weaponModel.transform.position,
                Rotation = Quaternion.identity,
                Callback = (pickup) =>
                {
                    if (pickup.TryGetComponent(out PickableWeapon model))
                    {
                        if (model == null) return;
                        WeaponSO weaponSO = weaponModel.GetItemSO();
                        model.Construct(weaponSO.Mags * weaponSO.Mag.GetMaxBullets(), instanceToGuidMappings[currentWeaponGuid]);
                    }
                }
            });
        }


        public void ChangeWeapon(string weaponGUID)
        {
            currentWeaponGuid = weaponGUID;
        }

        private void OnCurrentWeaponChanged(string oldValue, string newValue)
        {
            if (!localLoadoutWeapons.TryGetValue(newValue, out WeaponModel weaponModel))
            {
                if (instanceToGuidMappings.TryGetValue(newValue, out string guid)) {
                    if (assetDatabaseMappingSO.TryGetPrefab(guid, out GameObject prefab))
                    {
                        if (prefab.TryGetComponent(out weaponModel))
                        {
                            AddWeapon(newValue, weaponModel);
                        }
                    }
                }
            }
            
            DeselectWeapon(oldValue);
            SelectWeapon(newValue);
        }

        private void OnDisable()
        {
            UnregisterEvents();
        }

        private void OnDestroy()
        {
            OnDisable();
        }

        public void SetParent(Transform parent)
        {
           // _loadoutParent = parent;
        }

        public void RegisterEvents()
        {
            onPlayerPickUpEventBinding = new EventBinding<OnWeaponPickedUp>(OnWeaponPickUp);
            EventBus<OnWeaponPickedUp>.Register(onPlayerPickUpEventBinding);
            if (gameModeSO.EnableInstantUpdate) OnLocalWeaponLoadoutUpdated += InitializeLoadout;
            mainWeapon.OnPerformed += OnMainWeaponSelected;
            weaponCycle.OnPerformed += OnWeaponCycle;
            secondaryWeapon.OnPerformed += OnSecondaryWeaponSelected;
            thirdWeapon.OnPerformed += OnThirdWeaponSelected;
        }

        public void UnregisterEvents()
        {
            OnLocalWeaponLoadoutUpdated -= InitializeLoadout;
            if (!isOwned) return;
            EventBus<OnWeaponPickedUp>.Deregister(onPlayerPickUpEventBinding);
            if (equipCancellationTokenSource != null)
            {
                if (!equipCancellationTokenSource.IsCancellationRequested)
                {
                    equipCancellationTokenSource.Cancel();
                }

                equipCancellationTokenSource.Dispose();
                equipCancellationTokenSource = null;
            }
        }
    }
}