using Mirror;
using MyToolz.DesignPatterns.EventBus;
using MyToolz.Networking.Events;
using MyToolz.Networking.Relays;
using MyToolz.Utilities.Debug;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;
using Zenject;

namespace MyToolz.Networking
{
    [System.Serializable]
    public class Player
    {
        public string IP;
        public string Nickname;
        public string CharacterGUID;
        public int ConnectionId;
        public bool IsReady, IsPartyOwner;

        public Player()
        {

        }

        public Player(Player player)
        {
            IP = player.IP;
            Nickname = player.Nickname;
            CharacterGUID = player.CharacterGUID;
            ConnectionId = player.ConnectionId;
            IsReady = player.IsReady;
            IsPartyOwner = player.IsPartyOwner;
        }
    }
}

namespace MyToolz.Networking.Events
{
    public struct PlayersStateChanged : IEvent
    {
        public Core.PlayerState PlayerState;
        public Core.NetworkPlayer NetworkPlayer;
    }

    public struct PlayersAddedToTeam : IEvent
    {
        public Core.NetworkPlayer @NetworkPlayer;
        public string TeamName;
    }
}

namespace MyToolz.Networking.Core
{
    public enum PlayerState
    {
        Added,
        Updated,
        Removed
    }

    public class PlayerSnapshot
    {
        public string Team;
        public int Kills;
        public int Assists;
        public int Deaths;
        public int Points;
    }

    public class NetworkPlayer : NetworkBehaviour
    {
        #region Fields
        private static HashSet<NetworkPlayer> networkPlayers = new HashSet<NetworkPlayer>();
        private static Dictionary<string, PlayerSnapshot> playerSnapshots = new Dictionary<string, PlayerSnapshot>();

        public static IReadOnlyDictionary<string, PlayerSnapshot> PlayerSnapshots => playerSnapshots;

        private Relay relay;

        public static IReadOnlyList<NetworkPlayer> NetworkPlayers
        {
            get
            {
                if (networkPlayers == null)
                {
                    networkPlayers = new HashSet<NetworkPlayer>();
                }
                else
                {
                    networkPlayers.RemoveWhere(player => player == null);
                }
                return new ReadOnlyCollection<NetworkPlayer>(networkPlayers.ToList());
            }
        }

        public static NetworkPlayer LocalPlayerInstance;
        [SerializeField, SyncVar] private NetworkCharacter character;
        public NetworkCharacter Character
        {
            get => character;
        }
        [SerializeField, SyncVar(hook = nameof(OnNicknameChanged))] private string nickname;
        public string Nickname
        {
            get => nickname;
        }
        [SerializeField, SyncVar(hook = nameof(OnTeamGuidChanged))] private string teamGuid;
        public string TeamGuid
        {
            get => teamGuid;
        }
        [SerializeField, SyncVar(hook = nameof(OnKillsUpdated))] private int kills;
        public int Kills
        {
            get => kills;
        }
        [SerializeField, SyncVar(hook = nameof(OnAssistsUpdated))] private int assists;
        public int Assists
        {
            get => assists;
        }
        [SerializeField, SyncVar(hook = nameof(OnPointsUpdated))] private int points;
        public int Points
        {
            get => points;
        }
        [SerializeField, SyncVar(hook = nameof(OnDeathsUpdated))] private int deaths;
        public int Deaths
        {
            get => deaths;
        }
        #endregion

        [Inject]
        private void Construct(Relay relay)
        {
            this.relay = relay;
        }

        private void Start()
        {
            networkPlayers.Add(this);
        }
        #region Callbacks
        private void OnKillsUpdated(int oldValue, int newValue)
        {
            NotifyUpdate();
        }

        private void OnDeathsUpdated(int oldValue, int newValue)
        {
            NotifyUpdate();
        }

        private void OnAssistsUpdated(int oldValue, int newValue)
        {
            NotifyUpdate();
        }
        private void OnPointsUpdated(int oldValue, int newValue)
        {
            NotifyUpdate();
        }

        private void NotifyUpdate()
        {
            EventBus<PlayersStateChanged>.Raise(CreateEvent(PlayerState.Updated));
        }

        private void OnNicknameChanged(string oldName, string newName)
        {
            Debug.Log($"Nickname changed from {oldName} to {newName}");
            if (NetworkServer.active)
            {
                if (string.IsNullOrEmpty(oldName) || string.IsNullOrWhiteSpace(oldName))
                {
                    if (playerSnapshots.TryGetValue(newName, out PlayerSnapshot snapshot))
                    {
                        if (snapshot != null) LoadSnapshot(snapshot);
                        else snapshot = new PlayerSnapshot();
                    }
                }
            }
            NotifyUpdate();
        }

        private void OnTeamGuidChanged(string oldGuid, string newGuid)
        {
            NotifyUpdate();
        }
        #endregion

        public bool IsFriendly(string sendersTeamGuid)
        {
            return teamGuid.Equals(sendersTeamGuid);
        }

        private void OnDestroy()
        {
            if (playerSnapshots.TryGetValue(nickname, out var snapshot))
            {
                playerSnapshots.Remove(nickname);
            }
            playerSnapshots.Add(nickname, CreateSnapshot());
            networkPlayers.Remove(this);
            if (isOwned)
            {
                relay.LeaveLobby(relay.CurrentLobby);
            }
            EventBus<PlayersStateChanged>.Raise(CreateEvent(PlayerState.Removed));
        }

        private PlayersStateChanged CreateEvent(PlayerState state)
        {
            return new PlayersStateChanged() { PlayerState = state, NetworkPlayer = this };
        }


        [Server]
        public void AddPoints(int amount)
        {
            points += amount;
            NotifyUpdate();
        }

        [Server]
        public void AddKills(int kills)
        {
            this.kills += kills;
            points += kills * 10;
        }

        [Server]
        public void AddAssists(int assists)
        {
            this.assists += assists;
            points += assists * 5;
            NotifyUpdate();
        }

        [Server]
        public void AddDeaths(int deaths)
        {
            this.deaths += deaths;
        }

        [Server]
        public void ResetStats()
        {
            kills = 0;
            assists = 0;
            deaths = 0;
            points = 0;
        }

        [Server]
        private PlayerSnapshot CreateSnapshot()
        {
            return new PlayerSnapshot() { Team = teamGuid, Kills = kills, Assists = assists, Deaths = deaths, Points = points };
        }

        [Server]
        private void LoadSnapshot(PlayerSnapshot snapshot)
        {
            AddKills(snapshot.Kills);
            AddAssists(snapshot.Assists);
            AddDeaths(snapshot.Deaths);
            AddPoints(snapshot.Points);
        }

        [Server]
        public void SetNickname(string v)
        {
            nickname = v;
        }

        public void SetCharacter(NetworkCharacter nCharacter)
        {
            character = nCharacter;
        }


        [Command(requiresAuthority = false)]
        public void CmdSetNickname(string nickname)
        {
            if (nickname.Equals(this.nickname)) return;
            this.nickname = nickname;
        }

        [Server]
        public void AddToTeam()
        {
            if (!((CustomNetworkManager)NetworkManager.singleton).TryAddToTeam(this, out var teamGuid))
            {
                DebugUtility.LogError(this, "Unable to join team!");
            }
            else
            {
                this.teamGuid = teamGuid;
            }
        }

        [Command(requiresAuthority = false)]
        public void CmdSetUpPlayer(string nickname)
        {
            if (nickname.Equals(this.nickname)) return;
            this.nickname = nickname;
        }

        public override void OnStopServer()
        {
            base.OnStopServer();
            playerSnapshots.Clear();
        }


        public override void OnStartLocalPlayer()
        {
            base.OnStartLocalPlayer();

            if (!isLocalPlayer) return;
            DebugUtility.Log(this, "Local player initialized!");

            LocalPlayerInstance = this;
            CmdSetUpPlayer(relay.GetPersonalName());

            DebugUtility.Log(this, "Local player initialized successfully.");
        }
    }
}