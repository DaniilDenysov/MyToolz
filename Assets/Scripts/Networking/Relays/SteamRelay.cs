using Mirror;
using MyToolz.Extensions;
using MyToolz.Networking;
using MyToolz.Networking.Relays;
using MyToolz.Networking.ScriptableObjects;
using MyToolz.Utilities.Debug;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace NoSaints.Relays
{
    public class SteamLobbyDTO : LobbyDTO
    {
        public CSteamID SteamID;

        public bool IsOwner => SteamMatchmaking.GetLobbyOwner(SteamID).Equals(SteamUser.GetSteamID());

        public override string GetAddress()
        {
            return SteamMatchmaking.GetLobbyData(SteamID, "address");
        }
        public bool IsInLobby()
        {
            return SteamID.IsValid() && SteamID.IsLobby();
        }

        public void SetLobbyData(string pchKey, string pchValue)
        {
            if (!IsOwner)
            {
                DebugUtility.LogError(this, "Not an owner!");
                return;
            }
            SteamMatchmaking.SetLobbyData(SteamID, pchKey, pchValue);
        }

        public string GetLobbyData(string pchKey)
        {
            return SteamMatchmaking.GetLobbyData(SteamID, pchKey);
        }
    }

    public class SteamAPIRequest<T> where T : struct
    {
        private CallResult<T> callResult;
        private T storedResult;

        private TaskCompletionSource<T> tcs;

        public T Result => storedResult;

        private Func<T, bool> validation;

        private CancellationTokenSource cancellationTokenSource;

        public SteamAPIRequest(CancellationTokenSource cancellationTokenSource, Func<T, bool> validation = null)
        {
            this.validation = validation ?? (_ => true);
            this.cancellationTokenSource = cancellationTokenSource;
        }

        ~SteamAPIRequest()
        {
            callResult?.Dispose();
        }

        protected virtual void OnCallbackReceived(T result, bool bIOFailure)
        {
            if (bIOFailure)
            {
                tcs?.TrySetException(new Exception("Steam call I/O failure."));
                return;
            }

            if (!validation(result))
            {
                tcs?.TrySetException(new Exception("Steam call validation failed."));
                return;
            }

            storedResult = result;
            tcs?.TrySetResult(result);
        }

        public void Cancel()
        {
            if (callResult != null)
            {
                callResult.Dispose();
                callResult = null;
            }

            tcs?.TrySetCanceled();
        }

        public async Task<T> Request(SteamAPICall_t handle)
        {
            tcs = new TaskCompletionSource<T>();
            storedResult = default;

            callResult = CallResult<T>.Create(OnCallbackReceived);
            callResult.Set(handle);

            using (cancellationTokenSource.Token.Register(() => tcs.TrySetCanceled()))
            {
                try
                {
                    return await tcs.Task;
                }
                catch (TaskCanceledException ex)
                {
                    DebugUtility.LogWarning("SteamAPIRequest was cancelled.");
                    throw;
                }
                finally
                {
                    Cancel();
                }
            }
        }

    }

    [System.Serializable]
    public class SteamRelay : Relay
    {
        private List<SteamLobbyDTO> availableLobbies = new List<SteamLobbyDTO>();
        private CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

        private const string gameIDValue = "NoSaints";
        private const string gameStateID = "game_state";
        private const string addressID = "address";
        private const string gameID = "game_id";
        private const string gameModeID = "game_mode";
        private const string gameModeSOID = "game_mode_so";
        private const string gameMapID = "game_map";


        public SteamRelay() : base()
        {

        }

        public override void Initialize()
        {
            base.Initialize();
            var init = SteamManager.Initialized;
        }

        public override (ConnectionStatus, string) GetRelayStatus()
        {
            if (SteamManager.Initialized) return base.GetRelayStatus();
            else return (ConnectionStatus.NotConnected, "Steam not initialized, please open steam and try again!");
        }

        private bool IsInLobby()
        {
            var lobby = (SteamLobbyDTO)currentLobby;
            return lobby != null && lobby.IsInLobby();
        }

        public override void UpdateStatus(GameState status)
        {
            if (currentLobby == null) return;
            var steamLobby = (SteamLobbyDTO)currentLobby;
            if (steamLobby == null) return;
            steamLobby.SetLobbyData(gameStateID,status.ToString());

        }

        public override async Task<ResultCode> JoinLobbyGameMode(LobbyDTO lobbyDTO, string gameMode, CancellationTokenSource tokenSource)
        {
            SteamLobbyDTO steamLobbyDTO = (SteamLobbyDTO)lobbyDTO;
            if (steamLobbyDTO == null)
            {
                return ResultCode.Error_LobbyNotFound;
            }
            if (IsInLobby())
            {
                return ResultCode.Error_Unknown;
            }
            if (!IsLobbyValid(steamLobbyDTO.SteamID))
            {
                return ResultCode.Error_LobbyIsUnavailable;
            }
            if (gameMode != steamLobbyDTO.GetLobbyData(gameModeID))
            {
                return ResultCode.Error_LobbyIsUnavailable;
            }
            var result = await JoinLobby(lobbyDTO, tokenSource);
            return result;
        }

        public override async Task<ResultCode> JoinLobby(LobbyDTO lobbyDTO, CancellationTokenSource tokenSource)
        {
            try
            {
                SteamLobbyDTO steamLobbyDTO = (SteamLobbyDTO)lobbyDTO;
                if (steamLobbyDTO == null)
                {
                    return ResultCode.Error_LobbyNotFound;
                }

                SteamAPICall_t handle = SteamMatchmaking.JoinLobby(steamLobbyDTO.SteamID);

                var request = new SteamAPIRequest<LobbyEnter_t>(tokenSource);
                LobbyEnter_t result = await request.Request(handle);

                var id = new CSteamID(result.m_ulSteamIDLobby);
                if (SteamMatchmaking.GetLobbyOwner(id) == SteamUser.GetSteamID())
                {
                    DebugUtility.Log("Entered own lobby as owner. Skipping client connection.");
                    return ResultCode.Success;
                }

                CustomNetworkManager customNetworkManager = ((CustomNetworkManager)NetworkManager.singleton);
                if (customNetworkManager == null)
                {
                    DebugUtility.LogError("Network Manager is not initialized!");
                    return ResultCode.Error_Unknown;
                }


                currentLobby = steamLobbyDTO;
                ((CustomNetworkManager)NetworkManager.singleton).GameModeSO = Resources.Load<GameModeSO>($"GameModes/{steamLobbyDTO.GetLobbyData(gameModeSOID)}");
                NetworkManager.singleton.networkAddress = steamLobbyDTO.GetLobbyData(addressID);
                customNetworkManager.onlineScene = steamLobbyDTO.GetLobbyData(gameMapID);
                customNetworkManager.StartClient();

                DebugUtility.Log("Joined lobby successfully");

                return ResultCode.Success_LobbyJoined;
            }
            catch (TaskCanceledException ex)
            {
                return ResultCode.Cancelled;
            }
        }

        public override async Task<ResultCode> CreateLobby(GameModeSO gameModeSO, CancellationTokenSource tokenSource)
        {
            try 
            { 
                if (gameModeSO == null)
                {
                    DebugUtility.LogError("CreateLobby called with null gameModeSO.");
                    return ResultCode.Error_Unknown;
                }

                string map = gameModeSO.GetRandomMap();
                if (string.IsNullOrEmpty(map))
                {
                    DebugUtility.LogError($"No maps found for game mode {gameModeSO.Title}");
                    return ResultCode.Error_Unknown;
                }

                SteamAPICall_t handle = SteamMatchmaking.CreateLobby(gameModeSO.LobbyType, gameModeSO.MaxPlayers);

                var request = new SteamAPIRequest<LobbyCreated_t>(tokenSource, result => result.m_eResult == EResult.k_EResultOK);

                LobbyCreated_t result;
                try
                {
                    result = await request.Request(handle);
                }
                catch (TaskCanceledException)
                {
                    DebugUtility.LogError("CreateLobby request was canceled.");
                    return ResultCode.Error_Unknown;
                }
                catch (Exception ex)
                {
                    DebugUtility.LogError($"Error during CreateLobby: {ex}");
                    return ResultCode.Error_Unknown;
                }

                var id = new CSteamID(result.m_ulSteamIDLobby);

                if (result.m_eResult != EResult.k_EResultOK)
                {
                    DebugUtility.LogError("Failed to create lobby. Result: " + result.m_eResult);
                    return ResultCode.Error_Unknown;
                }

                DebugUtility.Log($"Lobby created successfully with ID {id}.");

                var steamLobbyDTO = new SteamLobbyDTO();
                steamLobbyDTO.SteamID = id;
                steamLobbyDTO.SetLobbyData(addressID, SteamUser.GetSteamID().ToString());
                steamLobbyDTO.SetLobbyData(gameID, gameIDValue);
                steamLobbyDTO.SetLobbyData(gameModeID, gameModeSO.Title);
                steamLobbyDTO.SetLobbyData(gameModeSOID, gameModeSO.name);
                steamLobbyDTO.SetLobbyData(gameMapID, map);
                steamLobbyDTO.SetLobbyData(gameStateID, GameState.Available.ToString());

                currentLobby = steamLobbyDTO;

                CustomNetworkManager customNetworkManager = (CustomNetworkManager)NetworkManager.singleton;
                if (customNetworkManager == null)
                {
                    DebugUtility.LogError("Network Manager is not initialized!");

                    return ResultCode.Error_Unknown;
                }

                customNetworkManager.GameModeSO = gameModeSO;
                customNetworkManager.onlineScene = map;
                customNetworkManager.StartHost();

                DebugUtility.Log("Hosting started successfully.");

                return ResultCode.Success_LobbyCreated;
            }
            catch (TaskCanceledException ex)
            {
                return ResultCode.Cancelled;
            }
        }

        public override string GetPersonalName()
        {
            return SteamFriends.GetPersonaName();
        }

        public override async Task<IReadOnlyList<LobbyDTO>> GetLobbyList()
        {
            SteamMatchmaking.AddRequestLobbyListStringFilter(gameID, gameIDValue, ELobbyComparison.k_ELobbyComparisonEqual);
            SteamMatchmaking.AddRequestLobbyListStringFilter(gameStateID, GameState.Available.ToString(), ELobbyComparison.k_ELobbyComparisonEqual);
            SteamAPICall_t handle = SteamMatchmaking.RequestLobbyList();

            var request = new SteamAPIRequest<LobbyMatchList_t>(cancellationTokenSource,
                result => result.m_nLobbiesMatching >= 0
            );

            LobbyMatchList_t result = await request.Request(handle);

            DebugUtility.Log("Lobbies found: " + result.m_nLobbiesMatching);

            availableLobbies.Clear();
            if (!IsInLobby())
            {
                if (result.m_nLobbiesMatching > 0)
                {
                    for (int i = 0; i < result.m_nLobbiesMatching; i++)
                    {
                        CSteamID lobbyID = SteamMatchmaking.GetLobbyByIndex(i);
                        if (!IsLobbyValid(lobbyID)) continue;
                        availableLobbies.Add(GetLobbyDTO(lobbyID));
                    }
                }
            }
            return availableLobbies.AsReadOnly();
        }

        public override async Task<ResultCode> LeaveLobby(LobbyDTO lobbyDTO)
        {
            if (lobbyDTO == null) return ResultCode.Error_Unknown;
            SteamLobbyDTO steamLobbyDTO = (SteamLobbyDTO)lobbyDTO;
            if (steamLobbyDTO == null) return ResultCode.Error_LobbyNotFound;
            SteamMatchmaking.LeaveLobby(steamLobbyDTO.SteamID);
            if (NetworkClient.active || NetworkServer.active)
            {
                CustomNetworkManager customNetworkManager = ((CustomNetworkManager)NetworkManager.singleton);
                if (customNetworkManager != null)
                {
                    customNetworkManager.StopClient();
                    customNetworkManager.StopServer();
                }
            }
            currentLobby = null;
            return ResultCode.Success_LobbyLeft;
        }

        public SteamLobbyDTO GetLobbyDTO(CSteamID lobby)
        {
            var dto = new SteamLobbyDTO();
            if (!IsLobbyValid(lobby))
            {
                DebugUtility.LogError("Lobby is not valid!");
                return dto;
            }
            dto.SteamID = lobby;
            dto.ResourceName = SteamMatchmaking.GetLobbyData(lobby, gameModeSOID);
            dto.CurrentPlayers = SteamMatchmaking.GetNumLobbyMembers(lobby);
            dto.MaxPlayers = SteamMatchmaking.GetLobbyMemberLimit(lobby);
            dto.GameModeName = SteamMatchmaking.GetLobbyData(lobby, gameModeID);
            dto.MapName = UIUtilities.ExtractSceneName(SteamMatchmaking.GetLobbyData(lobby, gameMapID));
            dto.GameState = SteamMatchmaking.GetLobbyData(lobby,gameStateID);
            return dto;
        }

        private bool IsLobbyValid(CSteamID lobby)
        {
            if (lobby == null) return false;
            if (!lobby.IsValid() || !lobby.IsLobby()) return false;
            return true;
        }
    }
}
