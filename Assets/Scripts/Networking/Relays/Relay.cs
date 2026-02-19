using MyToolz.Networking.ScriptableObjects;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace MyToolz.Networking.Relays
{
    public enum ResultCode
    {
        Cancelled,
        Success,
        Success_LobbyCreated,
        Success_LobbyJoined,
        Success_LobbyLeft,
        Error_Unknown,
        Error_LobbyNotFound,
        Error_LobbyIsFull,
        Error_LobbyIsClosed,
        Error_LobbyIsUnavailable
    }

    public enum ConnectionStatus
    {
        Connected,
        NotConnected
    }

    public enum GameState
    {
        NotAvailable,
        Available
    }

    public class LobbyDTO
    {
        public string ResourceName;
        public string GameModeName;
        public string MapName;
        public GameModeSO GameModeSO => Resources.Load<GameModeSO>($"GameModes/{GameModeName}");
        public GameState GameStateParsed => Enum.Parse<GameState>(GameState);
        public int CurrentPlayers;
        public int MaxPlayers;
        public string GameState;
        public virtual string GetAddress()
        {
            return "localhost";
        }
        public bool Contains(string str)
        {
            return GameModeName.Contains(str) || MapName.Contains(str);
        }
    }

    public abstract class Relay
    {
        protected LobbyDTO currentLobby;
       
        public LobbyDTO CurrentLobby
        {
            get => currentLobby; 
        }

        public virtual void Initialize()
        {
            
        }

        public Relay() { }

        public virtual async Task<ResultCode> JoinLobby(LobbyDTO lobbyDTO, CancellationTokenSource tokenSource)
        {  
            return ResultCode.Success_LobbyJoined;
        }

        public virtual (ConnectionStatus, string) GetRelayStatus() => new (ConnectionStatus.Connected,"Successfully connected!");

        public virtual string GetPersonalName()
        {
            var newName = $"Player{UnityEngine.Random.Range(1000, 9999)}";
            return newName;
        }
        public virtual void UpdateStatus(GameState status)
        {
            
        }

        public virtual async Task<ResultCode> JoinLobbyGameMode(LobbyDTO lobbyDTO, string gameMode, CancellationTokenSource tokenSource)
        {
            return ResultCode.Success_LobbyJoined;
        }

        public virtual async Task<ResultCode> CreateLobby(GameModeSO gameModeSO, CancellationTokenSource tokenSource)
        {
            return ResultCode.Success_LobbyCreated;
        }

        public virtual async Task<IReadOnlyList<LobbyDTO>> GetLobbyList()
        {
            return null;
        }

        public virtual async Task<ResultCode> LeaveLobby(LobbyDTO lobbyDTO)
        {
            return ResultCode.Success_LobbyLeft;
        }
    }
}
