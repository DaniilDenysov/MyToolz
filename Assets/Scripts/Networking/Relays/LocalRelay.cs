using Mirror;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using UnityEngine;
using System.Linq;
using System.Net;
using MyToolz.Networking.ScriptableObjects;
using MyToolz.EditorToolz;
using MyToolz.Utilities.Debug;

namespace MyToolz.Networking.Relays
{
    public class LocalLobbyDTO : LobbyDTO
    {
        public Uri URI;
        public string IP;

        public override string GetAddress()
        {
            return URI.ToString();
        }
    }

    [System.Serializable]
    public class LocalRelay : Relay
    {
        [SerializeField, Required] protected CustomNetworkDiscovery networkDiscovery;

        public override async Task<ResultCode> JoinLobbyGameMode(LobbyDTO lobbyDTO, string gameMode, CancellationTokenSource tokenSource)
        {
            var result = await JoinLobby(lobbyDTO, tokenSource);
            return result;
        }

        public override void Initialize()
        {
            base.Initialize();
            networkDiscovery?.BeginSearching();
        }

        public override async Task<ResultCode> JoinLobby(LobbyDTO lobbyDTO, CancellationTokenSource tokenSource)
        {
            try
            {
                if (lobbyDTO == null)
                {
                    DebugUtility.LogError(this, "JoinLobby called with null lobbyDTO.");
                    return ResultCode.Error_Unknown;
                }

                var localLobbyDTO = (LocalLobbyDTO)lobbyDTO;

                if (localLobbyDTO == null)
                {
                    DebugUtility.LogError(this, "JoinLobby called with null lobbyDTO.");
                    return ResultCode.Error_Unknown;
                }

                networkDiscovery?.EndSearching();

                if (Transport.active == null)
                {
                    DebugUtility.LogError(this, "JoinLobby: no active transport present.");
                    return ResultCode.Error_Unknown;
                }

                CustomNetworkManager customNetworkManager = (CustomNetworkManager)NetworkManager.singleton;

                if (customNetworkManager == null)
                {
                    DebugUtility.LogError(this, "Network Manager is not initialized!");
                    return ResultCode.Error_Unknown;
                }

                DebugUtility.Log(this, $"JoinLobby: connecting to {localLobbyDTO.IP} ...");
                try
                {
                    customNetworkManager.GameModeSO = localLobbyDTO.GameModeSO;
                    customNetworkManager.onlineScene = localLobbyDTO.MapName;
                    customNetworkManager.networkAddress = localLobbyDTO.IP;
                    NetworkManager.singleton?.StartClient();
                }
                catch (Exception cx)
                {
                    DebugUtility.LogError(this, $"JoinLobby: ClientConnect/StartClient threw: {cx.Message}");
                    networkDiscovery?.BeginSearching();
                    return ResultCode.Error_Unknown;
                }

                DebugUtility.Log(this, "JoinLobby: connected successfully.");
                return ResultCode.Success_LobbyJoined;
            }
            catch (OperationCanceledException)
            {
                DebugUtility.LogWarning(this, "JoinLobby: cancelled by token.");
                try
                {
                    var nm = (CustomNetworkManager)NetworkManager.singleton;
                    nm?.StopClient();
                }
                catch 
                {

                }

                networkDiscovery?.BeginSearching();
                return ResultCode.Error_Unknown;
            }
            catch (Exception ex)
            {
                DebugUtility.LogError(this, $"JoinLobby: unexpected error: {ex.Message}");
                try
                {
                    var nm = (CustomNetworkManager)NetworkManager.singleton;
                    nm?.StopClient();
                }
                catch 
                {

                }

                networkDiscovery?.BeginSearching();
                return ResultCode.Error_Unknown;
            }
        }


        public override async Task<ResultCode> CreateLobby(GameModeSO gameModeSO, CancellationTokenSource tokenSource)
        {
            if (gameModeSO == null)
            {
                DebugUtility.LogError(this, "CreateLobby called with null gameModeSO.");
                return ResultCode.Error_Unknown;
            }

            string map = gameModeSO.GetRandomMap();
            if (string.IsNullOrEmpty(map))
            {
                DebugUtility.LogError(this, $"No maps found for game mode {gameModeSO.Title}");
                return ResultCode.Error_Unknown;
            }

            CustomNetworkManager customNetworkManager = (CustomNetworkManager)NetworkManager.singleton;
            if (customNetworkManager == null)
            {
                DebugUtility.LogError(this, "Network Manager is not initialized!");
                return ResultCode.Error_Unknown;
            }
            networkDiscovery?.EndSearching();
            customNetworkManager.GameModeSO = gameModeSO;
            customNetworkManager.onlineScene = map;
            customNetworkManager.networkAddress = NetUtils.GetLocalIPv4Address(); 
            customNetworkManager.StartHost();

            networkDiscovery?.AdvertiseServer();
            DebugUtility.Log(this, $"[Relay] Hosting:{customNetworkManager.networkAddress}");
            return ResultCode.Success_LobbyCreated;
        }

        public override async Task<IReadOnlyList<LobbyDTO>> GetLobbyList()
        {
            var hosts = networkDiscovery.GetAvailableHosts();
            DebugUtility.Log(this, $"Hosts:{hosts.Count}");
            return hosts.Select(host => new LocalLobbyDTO()
            {
                URI = host.URI,
                IP = host.IP,
                GameState = host.GameState,
                ResourceName = host.GameMode.Id,
                CurrentPlayers = host.TotalPlayers,
                GameModeName = host.GameMode.Name,
                MapName = host.Scene,
                MaxPlayers = host.GameMode.MaxPlayers
            }).Where((i)=>i.GameStateParsed == GameState.Available && i.GameModeSO.LobbyType != Steamworks.ELobbyType.k_ELobbyTypePrivate).ToList();
        }

        public override async Task<ResultCode> LeaveLobby(LobbyDTO lobbyDTO)
        {
            if (!(NetworkClient.active || NetworkServer.active)) return ResultCode.Error_LobbyIsClosed;
            CustomNetworkManager customNetworkManager = ((CustomNetworkManager)NetworkManager.singleton);
            if (customNetworkManager != null)
            {
                customNetworkManager.StopClient();
                customNetworkManager.StopServer();
            }
            networkDiscovery?.BeginSearching();
            return ResultCode.Success_LobbyLeft;
        }
    }

    public static class NetUtils
    {
        public static string GetLocalIPv4Address()
        {
            return Dns.GetHostAddresses(Dns.GetHostName())[0].ToString();
        }

    }

}

