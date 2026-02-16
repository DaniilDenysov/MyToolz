using Mirror;
using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Zenject;
using System.Threading.Tasks;
using MyToolz.Networking.ScriptableObjects;
using MyToolz.DesignPatterns.EventBus;
using NoSaints.Relays;
using MyToolz.Utilities.Debug;
using MyToolz.Networking.Relays;
using MyToolz.Networking.Strategies;
using MyToolz.Networking.Events;
using MyToolz.Networking.Core;
using MyToolz.Networking.RespawnSystem;
using MyToolz.Events;
using MyToolz.UI.Events;

namespace MyToolz.Networking.Events
{
    public struct SessionEvent : IEvent
    {
        public string Message;
    }
}

namespace MyToolz.Networking.Strategies
{
    [System.Serializable]
    public abstract class TeamDistributionStrategy : DistributionStrategy<string, IReadOnlyDictionary<string, List<Core.NetworkPlayer>>>
    {

    }

    [System.Serializable]
    public class SoloTeamStrategy : TeamDistributionStrategy
    {
        public override string Get(IReadOnlyDictionary<string, List<Core.NetworkPlayer>> formedTeams)
        {
            foreach (var team in formedTeams)
            {
                if (team.Value.Count == 0)
                    return team.Key;
            }
            return null;
        }
    }

    //[System.Serializable]
    //public class EqualityTeamDistributionStrategy : TeamDistributionStrategy
    //{
    //    public override string Get(IReadOnlyDictionary<string, List<NetworkPlayer>> formedTeams)
    //    {
    //        string smallestTeam = null;
    //        int minPlayerCount = int.MaxValue;

    //        foreach (var team in formedTeams)
    //        {
    //            int currentCount = team.Value.Count;
    //            if (currentCount < minPlayerCount)
    //            {
    //                minPlayerCount = currentCount;
    //                smallestTeam = team.Key;
    //            }
    //        }

    //        return smallestTeam;
    //    }
    //}

    [System.Serializable]
    public class RoundRobinTeamDistributionStrategy : TeamDistributionStrategy
    {
        private int roundRobinLastIndex;

        public override void Construct(GameModeSO gameModeSO)
        {
            base.Construct(gameModeSO);
            roundRobinLastIndex = 0;
        }

        public override string Get(IReadOnlyDictionary<string, List<Core.NetworkPlayer>> formedTeams)
        {
            var teamKeys = new List<string>(formedTeams.Keys);

            if (roundRobinLastIndex >= teamKeys.Count) roundRobinLastIndex = 0;

            for (; roundRobinLastIndex < teamKeys.Count;)
            {
                if (gameModeSO.GetPlayerCountForTeam(teamKeys[roundRobinLastIndex]) > formedTeams[teamKeys[roundRobinLastIndex]].Count)
                {
                    return teamKeys[roundRobinLastIndex++];
                }
                roundRobinLastIndex++;
            }
            return null;
        }
    }
}

namespace MyToolz.Networking
{ 
    public class CustomNetworkManager : NetworkManager
    {
        [SerializeField] private Core.NetworkPlayer networkPlayerPrefab;
        //public CSteamID SteamID { get; set; }
        public GameModeSO GameModeSO;

        private Relay relay;

        public Relay? Relay
        {
            get => relay;
        }

        private TeamDistributionStrategy teamDistributionStrategy
        {
            get => GameModeSO?.TeamDistributionStrategy;
        }
        private EventBinding<PlayersStateChanged> playerStateChangedBinding;

        private DiContainer projectContextContainer;
        private DiContainer? sceneContextContainer;

        [Inject]
        public void Construct(Relay relay, DiContainer container)
        {
            this.relay = relay;
            this.projectContextContainer = container;
        }

        private void OnEnable()
        {
            playerStateChangedBinding = new EventBinding<PlayersStateChanged>(OnPlayersStateChanged);
            EventBus<PlayersStateChanged>.Register(playerStateChangedBinding);
        }

        private void OnDisable()
        {
            EventBus<PlayersStateChanged>.Deregister(playerStateChangedBinding);
        }

        private void OnPlayersStateChanged(PlayersStateChanged playersStateChanged)
        {
            if (!NetworkServer.active) return;
            if (playersStateChanged.NetworkPlayer == null) return;
            if (formedTeamsReverseMapping.ContainsKey(playersStateChanged.NetworkPlayer)) return;
            if (playersStateChanged.PlayerState == PlayerState.Updated)
            {
                if (formedTeamsReverseMapping.TryGetValue(playersStateChanged.NetworkPlayer, out string teamGuid)) return;
                playersStateChanged.NetworkPlayer.AddToTeam();
            }
            else if (playersStateChanged.PlayerState == PlayerState.Removed)
            {
                if (formedTeamsReverseMapping.TryGetValue(playersStateChanged.NetworkPlayer, out string teamGuid))
                {
                    formedTeamsReverseMapping.Remove(playersStateChanged.NetworkPlayer);
                }
                if (!string.IsNullOrEmpty(teamGuid) && formedTeams.TryGetValue(teamGuid, out var list))
                {
                    list.Remove(playersStateChanged.NetworkPlayer);
                }
            }
        }


        public enum TeamDistributionMode
        {
            RoundRobin,
            Equality
        }


        /// <summary>
        /// key - guid/name  value - list of nicknames
        /// </summary>
        /// 
        private Dictionary<string, List<Core.NetworkPlayer>> formedTeams = new Dictionary<string, List<Core.NetworkPlayer>>();
        private Dictionary<Core.NetworkPlayer, string> formedTeamsReverseMapping = new Dictionary<Core.NetworkPlayer, string>();

        public List<string> TeamGuids => formedTeams.Keys.ToList();

        public override void OnClientConnect()
        {
            base.OnClientConnect();
            EventBus<SessionEvent>.Raise(new SessionEvent() { Message = "Connected to server!" });
        }


        public override void OnStartClient()
        {
            //NetworkClient.RegisterHandler<GameModeMessage>(OnReceiveGameMode);

            base.OnStartClient();

            foreach (var prefab in spawnPrefabs)
            {
                if (prefab == null) continue;
                NetworkClient.UnregisterPrefab(prefab);
                NetworkClient.RegisterPrefab(
                    prefab,
                    (msg) =>
                    {
                        var containerToUse = RespawnManager.Instance.Container;

                        var go = containerToUse.InstantiatePrefab(
                            prefab,
                            msg.position,
                            msg.rotation,
                            null
                        );

                        DebugUtility.Log(this, $"Spawned object via Zenject on CLIENT: {prefab.name}");

                        return go;
                    },
                    Destroy
                );
            }

        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            if (formedTeams.Count > 0)
            {
                DebugUtility.Log(this, "Teams are already formed!");
                return;
            }
            if (GameModeSO == null)
            {
                DebugUtility.Log(this, "Game mode is null!");
                return;
            }
            teamDistributionStrategy?.Construct(GameModeSO);
            FormTeams();

        }

        #region scene loading handling
        public override async void OnClientChangeScene(string newSceneName, SceneOperation sceneOperation, bool customHandling)
        {

            if (newSceneName != offlineScene)
            {
                EventBus<StartedSceneLoading>.Raise(new StartedSceneLoading()
                {
                    GameModeSO = GameModeSO,
                    SceneName = newSceneName
                });

                while (loadingSceneAsync == null)
                    await Task.Yield();

                EventBus<SceneLoading>.Raise(new SceneLoading()
                {
                    AsyncOperation = loadingSceneAsync,
                    SceneName = newSceneName,
                    SceneOperation = sceneOperation
                });
            }

            base.OnClientChangeScene(newSceneName, sceneOperation, customHandling);
        }

        public override async void OnServerChangeScene(string newSceneName)
        {

            if (newSceneName != offlineScene)
            {
                EventBus<StartedSceneLoading>.Raise(new StartedSceneLoading()
                {
                    GameModeSO = GameModeSO,
                    SceneName = newSceneName
                });

                while (loadingSceneAsync == null)
                    await Task.Yield();

                EventBus<SceneLoading>.Raise(new SceneLoading()
                {
                    AsyncOperation = loadingSceneAsync,
                    SceneName = newSceneName
                });
            }

            base.OnServerChangeScene(newSceneName);
        }

        public override void OnClientSceneChanged()
        {
            EventBus<SceneLoaded>.Raise(new SceneLoaded());
            base.OnClientSceneChanged();
        }

        public override void OnServerSceneChanged(string sceneName)
        {
            EventBus<SceneLoaded>.Raise(new SceneLoaded());
            base.OnServerSceneChanged(sceneName);
        }
        #endregion


        #region TeamHandling

        public bool TryAddToTeam(Core.NetworkPlayer networkPlayer, out string teamGuid)
        {
            Core.NetworkPlayer.PlayerSnapshots.TryGetValue(networkPlayer.Nickname, out PlayerSnapshot playerSnapshot);
            teamGuid = playerSnapshot?.Team;
            if (string.IsNullOrEmpty(teamGuid) || string.IsNullOrWhiteSpace(teamGuid))     
            {
                teamGuid = FindAvailableTeam();

                if (string.IsNullOrEmpty(teamGuid)) return false;

                if (!formedTeams.ContainsKey(teamGuid))
                {
                    Debug.LogError($"Team with GUID {teamGuid} does not exist.");
                    return false;
                }

                if (formedTeams[teamGuid].Count >= GameModeSO.GetPlayerCountForTeam(teamGuid))
                {
                    Debug.LogWarning($"Team {teamGuid} is full.");
                    return false;
                }

                formedTeams[teamGuid].Add(networkPlayer);
                formedTeamsReverseMapping.Add(networkPlayer,teamGuid);
                EventBus<PlayersAddedToTeam>.Raise(new PlayersAddedToTeam() { @NetworkPlayer = networkPlayer, TeamName = teamGuid });
                return true;
            }
            else if (formedTeams.TryGetValue(playerSnapshot.Team, out var list))
            {
                list.Add(networkPlayer);
                formedTeamsReverseMapping.Add(networkPlayer, teamGuid);
                EventBus<PlayersAddedToTeam>.Raise(new PlayersAddedToTeam() { @NetworkPlayer = networkPlayer, TeamName = teamGuid });
                return true;
            }
            return false;
        }


        public string FindAvailableTeam()
        {
            if (formedTeams == null || formedTeams.Count == 0)
            {
                throw new InvalidOperationException("No teams are available.");
            }
            return teamDistributionStrategy?.Get(formedTeams);
        }

    


        private void FormTeams()
        {
            if (GameModeSO == null || GameModeSO.teams == null || GameModeSO.teams.Count == 0)
            {
                Debug.LogError("gameModeSO or teams are not configured properly.");
                return;
            }

            foreach (var team in GameModeSO.teams)
            {
                if (!formedTeams.ContainsKey(team.Guid))
                {
                    formedTeams[team.Guid] = new List<Core.NetworkPlayer>();
                }
            }
        }

        #endregion

        public struct GameModeMessage : NetworkMessage
        {
            public string GameModeName;
        }


        public override void OnServerConnect(NetworkConnectionToClient conn)
        {
            //conn.Send(new GameModeMessage
            //{
            //    GameModeName = GameModeSO.name
            //});

            base.OnServerConnect(conn);

            DebugUtility.Log(this, "Adding player");

            if (!NetworkServer.active)
            {
                DebugUtility.LogWarning(this, "Trying to spawn player but server is not active!");
                return;
            }

            EventBus<PoolRequest<Core.NetworkPlayer>>.Raise(new PoolRequest<Core.NetworkPlayer>()
            {
                Prefab = networkPlayerPrefab,
                Callback = (obj) =>
                {
                    NetworkServer.AddPlayerForConnection(conn, obj.gameObject);

                    EventBus<PlayersStateChanged>.Raise(new PlayersStateChanged()
                    {
                        NetworkPlayer = obj,
                        PlayerState = PlayerState.Added
                    });
                }
            });
        }


        public override void OnServerDisconnect(NetworkConnectionToClient conn)
        {
            if (conn.identity != null)
            {
                var player = conn.identity.GetComponent<Core.NetworkPlayer>();
                if (player != null)
                {
                    string teamGuid = player.TeamGuid;
                    if (!string.IsNullOrEmpty(teamGuid) && formedTeams.ContainsKey(teamGuid))
                    {
                        formedTeams[teamGuid].Remove(player);
                    }
                }
            }
            EventBus<SessionEvent>.Raise(new SessionEvent() { Message = "Player disconnected!" });
            base.OnServerDisconnect(conn);
        }

        public override void OnClientDisconnect()
        {
            base.OnClientDisconnect();
            relay.LeaveLobby(relay.CurrentLobby);
            EventBus<SessionEvent>.Raise(new SessionEvent() { Message = "Disconnected from server!" });
        }
    }
}