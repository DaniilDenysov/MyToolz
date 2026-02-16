using Mirror;
using MyToolz.DesignPatterns.EventBus;
using MyToolz.Events;
using MyToolz.Networking.Events;
using MyToolz.Player.FPS.Inventory;
using MyToolz.UI.Labels;
using NoSaints.UI.Labels;
using System.Collections.Generic;
using UnityEngine;

namespace MyToolz.Networking.Events
{
    public struct PlayerKilledEvent : IEvent
    {
        public string KillerName;
        public NetworkConnectionToClient KillerConn;
        public string MurderWeaponGUID;
        public string VictimName;
        public NetworkConnectionToClient VictimConn;
    }
}

namespace MyToolz.Networking.Killfeed
{
    public class KillfeedManager : NetworkBehaviour
    {
        [SerializeField] private Transform container;
        [SerializeField] private Label itemPrefab;
        [SerializeField] private Label itemPrefabWithIcon;

        //TODO: [DD] add handling for icons instead of phrases, us phrases as fallbacks
        [SerializeField] private string [] randomPhrases = { "slaughtered", "destroyed", "killed","demolished","erased" };

        private EventBinding<PlayerKilledEvent> playerEventBinding;

        private Dictionary<string, Sprite> guidToIcons = new Dictionary<string, Sprite>();

        private void Awake()
        {
            guidToIcons = LoadGuidToIcons();
        }

        public static Dictionary<string, Sprite> LoadGuidToIcons()
        {
            ItemSO[] itemSOs = Resources.LoadAll<ItemSO>("");
            Dictionary<string, Sprite> guidToIcons = new Dictionary<string, Sprite>();
            if (itemSOs == null || itemSOs.Length == 0)
            {
                Debug.LogWarning("No ItemSO assets found in the specified Resources folder.");
                return guidToIcons;
            }

            foreach (var item in itemSOs)
            {
                if (item != null && item.ItemGuid != null && item.ItemIcon != null)
                {
                    guidToIcons.TryAdd(item.ItemGuid, item.ItemIcon);
                }
                else
                {
                    Debug.LogWarning($"ItemSO '{item?.name}' is missing ItemName or ItemIcon.");
                }
            }
            return guidToIcons;
        }

        private void OnEnable()
        {
            playerEventBinding = new EventBinding<PlayerKilledEvent>(OnPlayerKilled);
            EventBus<PlayerKilledEvent>.Register(playerEventBinding);
        }

        private void OnDisable()
        {
            EventBus<PlayerKilledEvent>.Deregister(playerEventBinding);
        }

        public void OnPlayerKilled(PlayerKilledEvent playerKilledEvent)
        {
            if (!string.IsNullOrEmpty(playerKilledEvent.MurderWeaponGUID)) RPCPlayerKilled(playerKilledEvent.KillerName, playerKilledEvent.MurderWeaponGUID, playerKilledEvent.VictimName);
            else
            {
                Debug.LogWarning("Murder weapon is null, using fallback pharase!");
                RPCPlayerKilled(playerKilledEvent.KillerName, GetPhrase(), playerKilledEvent.VictimName);
            }
        }

        [ClientRpc]
        private void RPCPlayerKilled(string killer, string murderWeapon, string victim)
        {
            if (murderWeapon != null && guidToIcons.TryGetValue(murderWeapon, out Sprite sprite))
            {
                EventBus<PoolRequest<Label>>.Raise(new PoolRequest<Label>()
                {
                    Prefab = itemPrefabWithIcon,
                    Callback = (itm)=>
                    {
                        ((KillfeedLabel)itm).Construct(killer, sprite, victim);
                        itm.transform.SetParent(container);
                    }
                });

                //item = KillfeedObjectPool.Instance.Get(itemPrefabWithIcon); //Instantiate(itemPrefabWithIcon);
                //item.Construct(killer, sprite, victim);
            }
            else
            {
                EventBus<PoolRequest<Label>>.Raise(new PoolRequest<Label>()
                {
                    Prefab = itemPrefab,
                    Callback = (itm) =>
                    {
                        ((KillfeedLabel)itm).Construct(killer, murderWeapon, victim);
                        itm.transform.SetParent(container);
                    }
                });

                //item = KillfeedObjectPool.Instance.Get(itemPrefab);
                //item.Construct(killer, murderWeapon, victim);
            }
            //item.transform.SetParent(container);
        }


        private string GetPhrase() => randomPhrases[Random.Range(0, randomPhrases.Length)];
    }
}