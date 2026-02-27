using Mirror;
using MyToolz.Clock.Interfaces;
using MyToolz.Clock.Model;
using MyToolz.Clock.Presenter;
using MyToolz.DesignPatterns.EventBus;
using MyToolz.EditorToolz;
using MyToolz.Events;
using MyToolz.InputManagement.Commands;
using MyToolz.Networking.DesignPatterns.Singleton;
using MyToolz.Networking.Events;
using MyToolz.Networking.Killfeed;
using MyToolz.Networking.Ownership;
using MyToolz.Networking.ScriptableObjects;
using MyToolz.Networking.Strategies;
using MyToolz.Tweener.UI;
using MyToolz.UI.Management;
using MyToolz.Utilities.Debug;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace MyToolz.Networking.Strategies
{
    [System.Serializable]
    public abstract class DistributionStrategy<T, Data>
    {
        protected GameModeSO gameModeSO;
        public virtual void Construct(GameModeSO gameModeSO)
        {
            this.gameModeSO = gameModeSO;
        }

        public abstract T Get(Data spawnpoints);
    }

    [System.Serializable]
    public abstract class RespawnDistributionStrategy : DistributionStrategy<CharacterSpawn, IReadOnlyList<CharacterSpawn>>
    {

    }

    [System.Serializable]
    public class RandomRespawnDistributionStrategy : RespawnDistributionStrategy
    {
        public override CharacterSpawn Get(IReadOnlyList<CharacterSpawn> spawnpoints)
        {
            if (spawnpoints.Count > 0)
            {
                return spawnpoints[UnityEngine.Random.Range(0, spawnpoints.Count)];
            }
            return null;
        }
    }

    [System.Serializable]
    public class RoundRobinRespawnDistributionStrategy : RespawnDistributionStrategy
    {
        private int currentIndex;

        public override void Construct(GameModeSO gameModeSO)
        {
            base.Construct(gameModeSO);
            currentIndex = 0;
        }

        public override CharacterSpawn Get(IReadOnlyList<CharacterSpawn> spawnpoints)
        {
            if (spawnpoints.Count == 0)
                return null;

            CharacterSpawn spawn = spawnpoints[currentIndex];
            currentIndex = (currentIndex + 1) % spawnpoints.Count;
            return spawn;
        }
    }


    [System.Serializable]
    public class RandomTimeBasedRespawnDistributionStrategy : RespawnDistributionStrategy
    {
        private Dictionary<CharacterSpawn, float> lastUsedTimes;

        public override void Construct(GameModeSO gameModeSO)
        {
            base.Construct(gameModeSO);
            lastUsedTimes = new Dictionary<CharacterSpawn, float>();
        }

        public override CharacterSpawn Get(IReadOnlyList<CharacterSpawn> spawnpoints)
        {
            if (spawnpoints.Count == 0)
                return null;

            float currentTime = Time.time;
            CharacterSpawn leastRecentlyUsed = null;
            float oldestTime = float.MaxValue;

            foreach (var spawn in spawnpoints)
            {
                if (!lastUsedTimes.TryGetValue(spawn, out float usedTime))
                {
                    usedTime = float.MinValue;
                }

                if (usedTime < oldestTime)
                {
                    oldestTime = usedTime;
                    leastRecentlyUsed = spawn;
                }
            }

            if (leastRecentlyUsed != null)
            {
                lastUsedTimes[leastRecentlyUsed] = currentTime;
            }

            return leastRecentlyUsed;
        }
    }
}

namespace MyToolz.Networking.RespawnSystem
{
    //TODO: [DD] use state machine for view of the respawn manager
    public class RespawnManager : NetworkSingleton<RespawnManager>, IEventListener
    {
        private float respawnTime => gameModeSO.RespawnTime;
        private float autoRespawnTime => gameModeSO.AutoRespawnTime;
        [SerializeField, Required] private GameObject characterPrefab;
        [Header("Killer info")]
        [SerializeField] private UITweener killerInfoTween;
        [SerializeField] private Image weaponIcon;
        [SerializeField] private TMP_Text killerNickname;
        [Header("Respawn info")]
        [SerializeField] private TMP_Text display;
        [SerializeField] private CharacterSpawn [] characterSpawnPoints;
        //change to UITweener
        [SerializeField] private TMP_Text respawnNotification;
        private bool blockRespawn;

        [SerializeField]  ClockModel clockModel = new();
        [SerializeField] private ClockModel autoRespawnClockModel = new();

        private InterfaceReference<IClockView> clockView;
        private InterfaceReference<IClockView> autoRespawnClockView;

        private IClockPresenter clock = new ClockPresenter();
        private IClockPresenter autoRespawnClock = new ClockPresenter();

        private Dictionary<string, HashSet<CharacterSpawn>> characterSpawnPointsMap = new Dictionary<string, HashSet<CharacterSpawn>>();

        [SerializeField, Required] private UIScreen respawnScreen;
        [SerializeField, Required] private InputCommandSO respawnInputCommandSO;
        private DiContainer container;


        [Inject]
        private void Construct(DiContainer container, GameModeSO gameModeSO)
        {
            this.container = container;
            this.gameModeSO = gameModeSO;
        }

        private GameModeSO gameModeSO;
        private RespawnDistributionStrategy respawnDistributionStrategy
        {
            get
            {
                return gameModeSO?.RespawnDistributionStrategy;
            }
        }
        private EventBinding<PlayerKilledEvent> playerEventBinding;
        private EventBinding<GameReseted> gameResetedEventBinding;
        private EventBinding<GameOverEvent> gameOverEventBinding;
        private EventBinding<PlayersAddedToTeam> playerStateChangedEventBinding;

        private Dictionary<string,Sprite> guidToIcons = new Dictionary<string,Sprite>();

        public override void Awake()
        {
            base.Awake();
            clock.Initialize(clockModel, clockView.Value);
            autoRespawnClock.Initialize(autoRespawnClockModel, autoRespawnClockView.Value);
            guidToIcons = KillfeedManager.LoadGuidToIcons();
            respawnDistributionStrategy?.Construct(gameModeSO);
        }

        private void MapCharacterSpawnPoints()
        {
            if (characterSpawnPointsMap.Keys.Count > 0) return;

            foreach (var spawn in characterSpawnPoints)
            {
                if (spawn == null) continue;

                string teamGuid = spawn.GetTeamGuid();

                if (!characterSpawnPointsMap.ContainsKey(teamGuid))
                {
                    characterSpawnPointsMap[teamGuid] = new HashSet<CharacterSpawn>();
                }

                characterSpawnPointsMap[teamGuid].Add(spawn);
            }
        }


        private void OnEnable()
        {
            RegisterEvents();
        }

        private void OnGameOver(GameOverEvent @event)
        {
            blockRespawn = true;
        }

        private void OnDisable()
        {
            UnregisterEvents();
        }

        [ClientRpc]
        private void OnGameReseted(GameReseted gameReseted)
        {
            StopAllCoroutines();
            autoRespawnClock.Stop();
            clock.Stop();
            OnRespawn();
        }

        private void OnPLayerStateChanged(PlayersAddedToTeam @event)
        {
            if (!NetworkServer.active) return;
            MapCharacterSpawnPoints();
            Transform spawnPoint = GetNextSpawnPoint(@event.TeamName);
            SpawnPlayer(@event.NetworkPlayer.connectionToClient,spawnPoint.position);
        }

        private void OnPlayerKilled(PlayerKilledEvent @event)
        {
            if (!NetworkServer.active) return;
            Vector3 victimPosition = Vector3.zero;
            Vector3 killerPosition = Vector3.zero;
            Vector3 victimCameraPosition = Vector3.zero;
            Quaternion victimCameraRotation = Quaternion.identity;
            bool killerUndefined = false;
            try
            {
                if (@event.VictimConn.identity.TryGetComponent(out Core.NetworkPlayer networkCharacter))
                {
                    GameObject player = networkCharacter.Character.gameObject;
                    victimPosition = player.transform.position;
                    //TODO: [DD] refactor with player data script later 
                    //if (player.TryGetComponent(out HealthSystem hpSystem))
                    //{
                    //    victimCameraPosition = hpSystem.PlayerCamera.position;
                    //    victimCameraRotation = hpSystem.PlayerCamera.rotation;
                    //}
                }
                if (@event.KillerConn.identity.TryGetComponent(out Core.NetworkPlayer killerNetworkPlayer))
                {
                    GameObject player = killerNetworkPlayer.Character.gameObject;
                    killerPosition = player.transform.position;
                }
            }
            catch (NullReferenceException e)
            {
                DebugUtility.LogWarning("Unable to retrive player data!");
                killerUndefined = true;
            }

            //,new KillCamData(victimCameraPosition,victimCameraRotation,victimPosition, killerPosition, killerUndefined)
            ActivateRespawnMenu(@event.VictimConn, @event.MurderWeaponGUID, @event.KillerName);
        }


        [TargetRpc]
        public void ActivateRespawnMenu(NetworkConnectionToClient conn, string murderWeaponGuid, string killerName)
        {
            //deathCamera.ShowCamera(killCamData);
            respawnNotification.text = "Waiting for respawn";
            if (!string.IsNullOrEmpty(murderWeaponGuid) && !string.IsNullOrEmpty(killerName))
            {
                killerInfoTween.SetActive(true);
                killerNickname.text = killerName;
                if (guidToIcons.TryGetValue(murderWeaponGuid, out Sprite sprite))
                {
                    weaponIcon.sprite = sprite;
                }
            }
            clock.Start();
            respawnScreen.Open();
            //if (spectatorManager != null) spectatorManager.StartSpectatorMode();
        }

        private void StartAutoRespawnClock()
        {
            if (!gameModeSO.EnableAutoRespawn) return;
            autoRespawnClock.Start();
        }

        private void Respawn()
        {
            OnRespawn();
        }

        private void OnRespawn()
        {
            if (blockRespawn) return;
            autoRespawnClock.Stop();
            clock.Stop();
            respawnInputCommandSO.OnPerformed -= OnRespawn;
            //TODO: [DD] decouple DeathCameraController here
            //if (deathCamera != null) deathCamera.HideCamera();
            respawnNotification.text = "";
            respawnScreen.Close();
            //if (spectatorManager != null) spectatorManager.StopSpectatorMode();
            RespawnPlayer();
        }

        [Command(requiresAuthority = false)]
        public void RespawnPlayer(NetworkConnectionToClient conn = null)
        {
            Transform spawnPoint = GetNextSpawnPoint(conn.identity.GetComponent<Core.NetworkPlayer>().TeamGuid);
            if (spawnPoint != null)
            {   
                SpawnPlayer(conn,spawnPoint.position);
            }
            else
            {
                DebugUtility.LogWarning(this, "Spawnpoint is null!");
            }
        }

        public DiContainer Container => container;

        private void SpawnPlayer(NetworkConnectionToClient conn, Vector3 spawnPoint)
        {
            StartCoroutine(SpawnPlayerCoroutine(conn, spawnPoint));
            //GameObject newPlayer = container.InstantiatePrefab(characterPrefab.gameObject, spawnPoint, Quaternion.identity, null);
            //NetworkServer.Spawn(newPlayer, conn);
            //EventBus<SpawnRequest<NetworkCharacter>>.Raise(new SpawnRequest<NetworkCharacter>()
            //{
            //    Prefab = characterPrefab,
            //    Position = spawnPoint,
            //    Rotation = Quaternion.identity
            //});
        }

        private IEnumerator SpawnPlayerCoroutine(NetworkConnectionToClient conn, Vector3 spawnPoint)
        {
            yield return new WaitWhile(()=>container == null);
            GameObject newPlayer = container.InstantiatePrefab(characterPrefab.gameObject, spawnPoint, Quaternion.identity, null);
            NetworkServer.Spawn(newPlayer, conn);
        }

        public Transform GetNextSpawnPoint(string teamGuid)
        {
            CharacterSpawn spawn = null;

            if (((CustomNetworkManager)NetworkManager.singleton).GameModeSO.EnableCommunism)
            {
                var allSpawns = characterSpawnPointsMap.Values.SelectMany(set => set).ToList();
                spawn = respawnDistributionStrategy.Get(allSpawns);
                if (allSpawns.Count > 0)
                {
                    spawn = allSpawns[UnityEngine.Random.Range(0, allSpawns.Count)];
                }
            }
            else
            {
                if (characterSpawnPointsMap.TryGetValue(teamGuid, out var spawns) && spawns.Count > 0)
                {
                    spawn = respawnDistributionStrategy.Get(spawns.ToList());
                }
            }

            return spawn?.transform;
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            UnregisterEvents();
            respawnInputCommandSO.OnPerformed -= OnRespawn;
        }


        public override RespawnManager GetInstance()
        {
            return this;
        }

        public void RegisterEvents()
        {
            gameOverEventBinding = new EventBinding<GameOverEvent>(OnGameOver);
            EventBus<GameOverEvent>.Register(gameOverEventBinding);
            playerEventBinding = new EventBinding<PlayerKilledEvent>(OnPlayerKilled);
            EventBus<PlayerKilledEvent>.Register(playerEventBinding);
            playerStateChangedEventBinding = new EventBinding<PlayersAddedToTeam>(OnPLayerStateChanged);
            EventBus<PlayersAddedToTeam>.Register(playerStateChangedEventBinding);
            gameResetedEventBinding = new EventBinding<GameReseted>(OnGameReseted);
            EventBus<GameReseted>.Register(gameResetedEventBinding);

            clock.Elapsed += StartAutoRespawnClock;
            autoRespawnClock.Elapsed += Respawn;
        }

        public void UnregisterEvents()
        {
            EventBus<GameOverEvent>.Deregister(gameOverEventBinding);
            EventBus<PlayerKilledEvent>.Deregister(playerEventBinding);
            EventBus<PlayersAddedToTeam>.Deregister(playerStateChangedEventBinding);
            EventBus<GameReseted>.Deregister(gameResetedEventBinding);

            clock.Elapsed -= StartAutoRespawnClock;
            autoRespawnClock.Elapsed -= Respawn;
        }
    }
}
