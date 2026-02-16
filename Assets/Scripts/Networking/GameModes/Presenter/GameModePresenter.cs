using Mirror;
using MyToolz.DesignPatterns.EventBus;
using MyToolz.DesignPatterns.MVP.View;
using MyToolz.Networking.Core;
using MyToolz.Networking.Events;
using MyToolz.Networking.GameModes.Events;
using MyToolz.Networking.GameModes.Model;
using MyToolz.Networking.GameModes.Presenter;
using MyToolz.Networking.Relays;
using MyToolz.Networking.Scoreboards;
using MyToolz.Networking.ScriptableObjects;
using MyToolz.Networking.SynchronizedClock;
using MyToolz.UI.Management;
using MyToolz.Utilities.Debug;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using Zenject;

namespace MyToolz.Networking.Events
{
    public struct GameStateEvent : IEvent
    {
        public GameModeSO GameMode;
        public int StateTime;
    }

    public struct GameReseted : IEvent
    {

    }
    public struct GameMessageEvent : IEvent
    {
        public string Description;
    }
    public struct GameOverEvent : IEvent
    {

    }
}

namespace MyToolz.Networking.GameModes.Presenter
{
    [System.Serializable]
    public abstract class GameStateHandler
    {
        protected GameModeSO gameModeSO;
        protected GameModePresenter gameModeManager;
        protected Relay relay;

        [SerializeField] protected string stateDisplayName;
        public string StateDisplayName => stateDisplayName;

        [Inject]
        private void Construct(Relay relay, GameModePresenter gameModeManager, GameModeSO gameModeSO)
        {
            this.gameModeManager = gameModeManager;
            this.relay = relay;
            this.gameModeSO = gameModeSO;
        }

        public virtual void Enter() { }
        public virtual void Exit() { }
    }

    [System.Serializable]
    public abstract class ClockBasedGameStateHandler : GameStateHandler
    {

        [SerializeField, Range(0, 1000f)] protected float duration;

        protected SyncedClock syncedClock;

        [Inject]
        private void Construct(SyncedClock syncedClock)
        {
            this.syncedClock = syncedClock;
        }

        public override void Enter()
        {
            if (NetworkServer.active) relay?.UpdateStatus(GameState.Available);
            if (!NetworkServer.active) return;
            syncedClock.StartTimer(duration, OnClockTick, OnClockStopped, OnClockElapsed);
        }

        public override void Exit()
        {
            if (!NetworkServer.active) return;
            syncedClock.StopTimer();
        }

        public virtual void OnClockStopped()
        {

        }

        public virtual void OnClockElapsed()
        {
            gameModeManager.EnterNextState();
        }

        public virtual void OnClockTick(float time)
        {

        }
    }

    [System.Serializable]
    public class DelayBeforeMatchmaking : ClockBasedGameStateHandler
    {

    }

    [System.Serializable]
    public class WaitingForMinimumPlayers : ClockBasedGameStateHandler
    {
        [SerializeField] protected bool stopIfNotEnoughPlayers;

        public bool EnoughPlayersInLobby() => Core.NetworkPlayer.NetworkPlayers.Count >= gameModeSO.MinPlayers;
        public bool LobbyIsFull() => Core.NetworkPlayer.NetworkPlayers.Count == gameModeSO.MaxPlayers;

        private IReadOnlyView<WaitingForMinimumPlayers> view;

        [Inject]
        private void Construct([InjectOptional] IReadOnlyView<WaitingForMinimumPlayers> view)
        {
            this.view = view;
            view?.Initialize(this);
        }

        public override void Enter()
        {
            base.Enter();
            view?.Show();
        }

        public override void Exit()
        {
            base.Exit();
            view?.Hide();
        }

        public override void OnClockElapsed()
        {
            if (!EnoughPlayersInLobby())
            {
                if (stopIfNotEnoughPlayers)
                {
                    GameModeModel model = new GameModeModel(gameModeManager.GameModeModel);
                    model.GameStatus = GameStatus.NotEnoughPlayers;
                    model.Result = "Not enough players!";
                    gameModeManager.GameModeModel = model;
                    gameModeManager.ChangeState(new RoundEnded());
                }
            }
            else
            {
                gameModeManager.EnterNextState();
            }
        }

        public override void OnClockTick(float time)
        {
            if (EnoughPlayersInLobby())
            {
                gameModeManager.EnterNextState();
            }
        }
    }

    [System.Serializable]
    public class WaitingForMorePlayers : ClockBasedGameStateHandler
    {
        public bool LobbyIsFull() => Core.NetworkPlayer.NetworkPlayers.Count == gameModeSO.MaxPlayers;
        public bool EnoughPlayersInLobby() => Core.NetworkPlayer.NetworkPlayers.Count >= gameModeSO.MinPlayers;

        public float TimeLeft
        {
            get;
            private set;
        }

        private EventBinding<PlayersStateChanged> playerStateChanged;
        private IReadOnlyView<WaitingForMorePlayers> view;

        [Inject]
        private void Construct([InjectOptional] IReadOnlyView<WaitingForMorePlayers> view)
        {
            this.view = view;
            view?.Initialize(this);
        }



        public override void Enter()
        {
            base.Enter();
            view?.Show();
            if (!gameModeManager.isServer) return;
            playerStateChanged = new EventBinding<PlayersStateChanged>(OnPlayerStateChanged);
            EventBus<PlayersStateChanged>.Register(playerStateChanged);
        }

        public override void Exit()
        {
            base.Exit();
            view?.Hide();
            if (!gameModeManager.isServer) return;
            EventBus<PlayersStateChanged>.Deregister(playerStateChanged);
        }

        private void OnPlayerStateChanged()
        {
            if (EnoughPlayersInLobby()) return;
            gameModeManager.ChangeState(new WaitingForMinimumPlayers());
        }

        public override void OnClockTick(float time)
        {
            TimeLeft = time;
            if (LobbyIsFull())
            {
                gameModeManager.EnterNextState();
            }
        }
    }

    [System.Serializable]
    public class NewRoundStarted : ClockBasedGameStateHandler
    {
        private int lastPublishedMinute = -1;

        public override void Enter()
        {
            if (NetworkServer.active) relay?.UpdateStatus(GameState.Available);
            base.Enter();
            if (!NetworkServer.active) return;
            foreach (Core.NetworkPlayer networkPlayer in Core.NetworkPlayer.NetworkPlayers)
            {
                NetworkCharacter networkCharacter = networkPlayer.Character;
                if (networkCharacter != null)
                {
                    NetworkServer.Destroy(networkCharacter.gameObject);
                }
                networkPlayer.ResetStats();
            }

            EventBus<GameReseted>.Raise(new GameReseted());
        }

        public override void OnClockElapsed()
        {
            gameModeManager.PublsihMessage($"Time is up!");
            base.OnClockElapsed();
        }

        public override void OnClockTick(float time)
        {
            int minutesRemaining = Mathf.RoundToInt(time / 60);

            if (minutesRemaining != lastPublishedMinute)
            {
                lastPublishedMinute = minutesRemaining;
                gameModeManager.PublsihMessage($"{minutesRemaining} minutes remaining!");
                gameModeManager.OneMinuteElapsed();
            }
        }
    }

    [System.Serializable]
    public class TrainingGroundsRound : ClockBasedGameStateHandler
    {
        public override void Enter()
        {
            if (NetworkServer.active) relay?.UpdateStatus(GameState.NotAvailable);
            else return;
            syncedClock.StartStopWatch(OnClockStopped);

        }
    }

    public class RoundEndStats
    {
        public uint OldPoints;
        public uint NewPoints => OldPoints + PointsGained;
        public uint Kills;
        public uint Deaths;
        public uint Assists;
        public uint PointsGained;
        public uint PointsPerLevel;
    }

    [System.Serializable]
    public class RoundEnded : GameStateHandler
    {

        private IReadOnlyView<RoundEnded> fallbackView;
        private IReadOnlyView<RoundEnded> mainView;

        public string Purpose
        {
            get => gameModeSO.Objective;
        }

        public GameStatus Status => gameModeManager.GameModeModel.ClientRelativeStatus;
        public string Reason => gameModeManager.GameModeModel.Result;

        public RoundEndStats RoundEndStats;

        [Inject]
        private void Construct([Inject(Id = "Fallback")] IReadOnlyView<RoundEnded> fallbackView, [Inject(Id = "Main")] IReadOnlyView<RoundEnded> mainView)
        {
            this.fallbackView = fallbackView;
            this.mainView = mainView;
            fallbackView.Initialize(this);
        }

        public override void Enter()
        {
            base.Enter();
            if (NetworkServer.active) relay?.UpdateStatus(GameState.NotAvailable);
            bool wonGame = gameModeManager.GameModeModel.ClientRelativeStatus == GameStatus.Win;
            bool lostGame = gameModeManager.GameModeModel.ClientRelativeStatus == GameStatus.Defeat;
            if (wonGame || lostGame)
            {
                if (NetworkServer.active)
                {
                    foreach (Core.NetworkPlayer networkPlayer in Core.NetworkPlayer.NetworkPlayers)
                    {
                        NetworkCharacter networkCharacter = networkPlayer.Character;
                        if (networkCharacter != null)
                        {
                            NetworkServer.Destroy(networkCharacter.gameObject);
                        }
                    }
                }
                var player = Core.NetworkPlayer.LocalPlayerInstance;
                mainView.UpdateView(this);
                mainView.Show();
            }
            else
            {
                fallbackView.UpdateView(this);
                fallbackView.Show();
            }
            EventBus<GameOverEvent>.Raise(new GameOverEvent());
        }
    }

    [System.Serializable]
    public class NewDeathmatchRoundStarted : NewRoundStarted
    {
        private EventBinding<PlayerKilledEvent> playerKilledBinding;
        [SerializeField, Range(1, 100)] protected int killCountLimit;
        public int KillCountLimit
        {
            get => killCountLimit;
        }
        private IReadOnlyView<NewDeathmatchRoundStarted> view;

        [Inject]
        private void Construct(IReadOnlyView<NewDeathmatchRoundStarted> view)
        {
            this.view = view;
            view?.Initialize(this);
        }

        public override void Enter()
        {
            base.Enter();
            view?.Show();
            playerKilledBinding = new EventBinding<PlayerKilledEvent>(OnPlayerKilled);
            UpdateView();
            EventBus<PlayerKilledEvent>.Register(playerKilledBinding);
        }

        public virtual void UpdateView()
        {
            view?.UpdateView(this);
        }

        public override void Exit()
        {
            view?.Hide();
            EventBus<PlayerKilledEvent>.Deregister(playerKilledBinding);
        }

        public int GetKillsLeft() => killCountLimit - DeathmatchScoreboard.KillLeader.Kills;

        public List<Core.NetworkPlayer> GetTopPlayers(IEnumerable<Core.NetworkPlayer> players)
        {
            return players
                .OrderByDescending(p => p.Kills)
                .ToList();
        }

        public virtual bool IsWinConditionFullfiled()
        {
            var teamGroups = Core.NetworkPlayer.NetworkPlayers
                .GroupBy(player => player.TeamGuid)
                .ToDictionary(
                    group => group.Key,
                    group => group.ToList()
                );

            var teamScores = teamGroups
                .Select(kvp => new
                {
                    TeamGuid = kvp.Key,
                    TotalScore = kvp.Value.Sum(player => player.Kills),
                    Players = kvp.Value
                })
                .OrderByDescending(team => team.TotalScore)
                .ToList();

            return teamScores.Any(s => s.TotalScore >= killCountLimit);
        }



        public override void OnClockElapsed()
        {
            var teamScores = Core.NetworkPlayer.NetworkPlayers
                .Select(player => new
                {
                    Nickname = player.Nickname,
                    TeamGuid = player.TeamGuid,
                    Kills = player.Kills
                })
                .OrderByDescending(team => team.Kills)
                .ToList();

            //Move to the round end state
            var result = teamScores.First();
            GameModeModel model = new GameModeModel(gameModeManager.GameModeModel);
            model.GameStatus = GameStatus.None;
            model.WinningTeam = result.TeamGuid;
            model.Result = $"{result.Nickname} won by timeout with {result.Kills} kills!";
            gameModeManager.GameModeModel = model;
            base.OnClockElapsed();
        }

        public void OnPlayerKilled(PlayerKilledEvent @event)
        {
            UpdateView();
            bool gameEnded = IsWinConditionFullfiled();
            if (!gameEnded)
            {
                if (GetKillsLeft() <= 2) gameModeManager.PublsihMessage($"{GetKillsLeft()} kill remaining!");
            }
            else
            {
                var teamScores = Core.NetworkPlayer.NetworkPlayers
                .Select(player => new
                {
                    Nickname = player.Nickname,
                    TeamGuid = player.TeamGuid,
                    Kills = player.Kills
                })
                .OrderByDescending(team => team.Kills)
                .ToList();

                var result = teamScores.First();
                GameModeModel model = new GameModeModel(gameModeManager.GameModeModel);
                model.GameStatus = GameStatus.None;
                model.WinningTeam = result.TeamGuid;
                model.Result = $"{result.Nickname} won by killing {result.Kills}!";
                gameModeManager.GameModeModel = model;
                gameModeManager.EnterNextState();
            }
        }
    }

    [System.Serializable]
    public class CaptureTheFlagNewRoundStarted : NewRoundStarted
    {
        [SerializeField, Range(0, 1000)] private int pointsLimit = 100;
        public int PointsLimit
        {
            get => pointsLimit;
        }

        private EventBinding<OnCrateDelivered> onCrateDelivered;

        private IReadOnlyView<CaptureTheFlagNewRoundStarted> view;

        [Inject]
        private void Construct(IReadOnlyView<CaptureTheFlagNewRoundStarted> view)
        {
            this.view = view;
            this.view?.Initialize(this);
            UpdateModel();
        }

        public virtual void UpdateModel()
        {
            view?.UpdateView(this);
        }

        public int GetPointsLeft() => pointsLimit - GetTeamScoresOrderedByKills(Core.NetworkPlayer.NetworkPlayers).Max((g) => g.TotalScore);

        public List<(string TeamGuid, int TotalScore)> GetTeamScoresOrderedByKills(IEnumerable<Core.NetworkPlayer> players)
        {
            var teamGroups = players
                .GroupBy(player => player.TeamGuid)
                .ToDictionary(
                    group => group.Key,
                    group => group.ToList()
                );

            var teamScores = teamGroups
                .Select(kvp => (
                    TeamGuid: kvp.Key,
                    TotalScore: kvp.Value.Sum(p => p.Points)
                ))
                .OrderByDescending(t => t.TotalScore)
                .ToList();

            return teamScores;
        }

        public override void Enter()
        {
            base.Enter();
            view?.Show();
            onCrateDelivered = new EventBinding<OnCrateDelivered>(OnCrateDelivered);
            EventBus<OnCrateDelivered>.Register(onCrateDelivered);
            EventBus<OnRoundStart>.Raise(new());
        }

        public override void Exit()
        {
            base.Exit();
            view?.Hide();
            EventBus<OnCrateDelivered>.Deregister(onCrateDelivered);
        }

        private void OnCrateDelivered(OnCrateDelivered @event)
        {
            if (!@event.Success) return;
            DebugUtility.Log("Delivered package successfully!");
            UpdateModel();
            bool gameEnded = IsWinConditionFullfiled();
            if (gameEnded)
            {
                var players = Core.NetworkPlayer.NetworkPlayers;

                if (players == null || !players.Any())
                {
                    Debug.LogError("No players found!");
                    return;
                }

                var teamScores = players
                    .GroupBy(player => player.TeamGuid)
                    .Select(group => new
                    {
                        TeamGuid = group.Key,
                        TotalScore = group.Sum(player => player.Points)
                    })
                    .OrderByDescending(team => team.TotalScore)
                .ToList();

                var result = teamScores.FirstOrDefault();
                GameModeModel model = new GameModeModel(gameModeManager.GameModeModel);
                model.GameStatus = GameStatus.None;
                model.Result = $"{result.TeamGuid} won the game by collecting {result.TotalScore} points!";
                model.WinningTeam = result.TeamGuid;
                gameModeManager.GameModeModel = model;
                DebugUtility.Log($"{result.TeamGuid} won the game by collecting {result.TotalScore} points!");
                gameModeManager.EnterNextState();
            }
        }

        public virtual bool IsWinConditionFullfiled()
        {
            return GetTeamScoresOrderedByKills(Core.NetworkPlayer.NetworkPlayers)
                   .Max(t => t.TotalScore) >= pointsLimit;
        }

        public override void OnClockElapsed()
        {
            var players = Core.NetworkPlayer.NetworkPlayers;

            if (players == null || !players.Any())
            {
                Debug.LogError("No players found!");
                return;
            }

            var teamScores = players
                .GroupBy(player => player.TeamGuid)
                .Select(group => new
                {
                    TeamGuid = group.Key,
                    TotalScore = group.Sum(player => player.Points)
                })
                .OrderByDescending(team => team.TotalScore)
            .ToList();

            var result = teamScores.FirstOrDefault();
            GameModeModel model = new GameModeModel(gameModeManager.GameModeModel);
            model.GameStatus = GameStatus.None;
            model.Result = $"{result.TeamGuid} won the game by collecting {result.TotalScore} points!";
            model.WinningTeam = result.TeamGuid;
            gameModeManager.GameModeModel = model;
            base.OnClockElapsed();
        }
    }

    [System.Serializable]
    public class NewTeamDeathmatchRoundStarted : NewRoundStarted
    {
        [SerializeField, Range(1, 100)] protected int killCountLimit;
        public int KillCountLimit
        {
            get => killCountLimit;
        }
        private EventBinding<PlayerKilledEvent> playerKilledBinding;
        private IReadOnlyView<NewTeamDeathmatchRoundStarted> view;

        [Inject]
        private void Construct(IReadOnlyView<NewTeamDeathmatchRoundStarted> view)
        {
            this.view = view;
            view?.Initialize(this);
        }

        public override void Enter()
        {
            base.Enter();
            view?.Show();
            playerKilledBinding = new EventBinding<PlayerKilledEvent>(OnPlayerKilled);
            UpdateView();
            EventBus<PlayerKilledEvent>.Register(playerKilledBinding);
        }

        public virtual void UpdateView()
        {
            view?.UpdateView(this);
        }

        public int GetKillsLeft() => killCountLimit - GetTeamScoresOrderedByKills(Core.NetworkPlayer.NetworkPlayers).First().TotalScore;

        public List<(string TeamGuid, int TotalScore)> GetTeamScoresOrderedByKills(IEnumerable<Core.NetworkPlayer> players)
        {
            var teamGroups = players
                .GroupBy(player => player.TeamGuid)
                .ToDictionary(
                    group => group.Key,
                    group => group.ToList()
                );

            var teamScores = teamGroups
                .Select(kvp => (
                    TeamGuid: kvp.Key,
                    TotalScore: kvp.Value.Sum(p => p.Kills)
                ))
                .OrderByDescending(t => t.TotalScore)
                .ToList();

            return teamScores;
        }

        public override void Exit()
        {
            base.Exit();
            view?.Hide();
            EventBus<PlayerKilledEvent>.Deregister(playerKilledBinding);
        }

        public bool IsWinConditionFullfiled()
        {
            return killCountLimit - GetKillsLeft() <= 0;
        }

        public override void OnClockElapsed()
        {
            var teamScores = Core.NetworkPlayer.NetworkPlayers
            .Select(player => new
            {
                Nickname = player.Nickname,
                TeamGuid = player.TeamGuid,
                Kills = player.Kills
            })
            .OrderByDescending(team => team.Kills)
            .ToList();

            var result = teamScores.First();
            GameModeModel model = new GameModeModel(gameModeManager.GameModeModel);
            model.GameStatus = GameStatus.None;
            model.WinningTeam = result.TeamGuid;
            model.Result = $"{result.TeamGuid} team won by timeout with {result.Kills} kills!";
            gameModeManager.GameModeModel = model;
            base.OnClockElapsed();
        }

        public void OnPlayerKilled(PlayerKilledEvent @event)
        {
            UpdateView();
            bool gameEnded = IsWinConditionFullfiled();
            if (!gameEnded)
            {
                if (GetKillsLeft() <= 2) gameModeManager.PublsihMessage($"{GetKillsLeft()} kill remaining!");
            }
            else
            {
                var teamScores = Core.NetworkPlayer.NetworkPlayers
               .GroupBy(player => player.TeamGuid)
               .Select(group => new
               {
                   TeamGuid = group.Key,
                   TotalScore = group.Sum(player => player.Kills)
               })
               .OrderByDescending(team => team.TotalScore)
               .ToList();

                var result = teamScores.First();
                GameModeModel model = new GameModeModel(gameModeManager.GameModeModel);
                model.GameStatus = GameStatus.None;
                model.WinningTeam = result.TeamGuid;
                model.Result = $"{result.TeamGuid} won by killing {result.TotalScore}!";
                gameModeManager.GameModeModel = model;
                gameModeManager.EnterNextState();
            }
        }
    }

    public enum GameStatus
    {
        None,
        NotEnoughPlayers,
        Defeat,
        Win
    }
}

namespace MyToolz.Networking.Serialization
{
    public static class GameModeModelSerializer
    {
        public static void WriteGameModeModel(this NetworkWriter writer, GameModeModel model)
        {
            writer.WriteInt(model?.CurrentStateIndex ?? 0);
            writer.WriteString(model?.WinningTeam ?? "");
            writer.WriteInt((int)(model?.gameStatus ?? GameStatus.None));
            writer.WriteString(model?.Result ?? "");
        }

        public static GameModeModel ReadGameModeModel(this NetworkReader reader)
        {
            GameModeModel model = new GameModeModel();
            model.CurrentStateIndex = reader.ReadInt();
            model.WinningTeam = reader.ReadString();
            model.gameStatus = (GameStatus)reader.ReadInt();
            model.Result = reader.ReadString();
            return model;
        }
    }
}

namespace MyToolz.Networking.GameModes.Presenter
{
    public class GameModePresenter : NetworkBehaviour
    {
        [SerializeField] protected UIScreenBase screen;
        [SerializeField] protected UnityEvent onOneMinuteElapsed;
        protected GameModeSO gameModeSO;

        protected GameStateHandler[] gameStatesStack
        {
            get => gameModeSO.GameStateHandlers;
        }

        protected GameStateHandler currentGameStateHandler;

        [SyncVar(hook = nameof(OnGameModeModelChanged))] public GameModeModel GameModeModel;


        private EventBinding<PlayersStateChanged> playerStateBinding;

        private DiContainer diContainer;

        [Inject]
        private void Construct(DiContainer diContainer, GameModeSO gameModeSO)
        {
            this.diContainer = diContainer;
            this.gameModeSO = gameModeSO;
        }

        private void Start()
        {
            if (!NetworkServer.active) return;
            if (gameStatesStack.Length > 0) ChangeState(gameStatesStack[0]);
        }

        private void OnDestroy()
        {
            currentGameStateHandler?.Exit();
        }

        #region StateHandling

        [Server]
        public void EnterNextState()
        {
            if (!NetworkServer.active) return;
            int i = GetIndexOfCurrentState(currentGameStateHandler?.GetType());
            if (i < 0 || i + 1 >= gameStatesStack.Length) return;
            ChangeState(gameStatesStack[i + 1]);
        }


        private int GetIndexOfCurrentState(Type type)
        {
            return Array.FindIndex(gameStatesStack, h => h.GetType() == type);
        }

        private void OnGameModeModelChanged(GameModeModel oldModel, GameModeModel newModel)
        {
            if (NetworkServer.active) return;
            screen?.Open();
            int i = newModel.CurrentStateIndex;
            if (i < 0 || i >= gameStatesStack.Length) return;
            GameStateHandler gameStateHandler = gameStatesStack[i];
            currentGameStateHandler?.Exit();
            DebugUtility.Log(this, $"Changed state from {currentGameStateHandler?.GetType()}");
            diContainer.Inject(gameStateHandler);
            currentGameStateHandler = gameStateHandler;
            DebugUtility.Log(this, $"Changed state to {gameStateHandler.GetType()}");
            currentGameStateHandler?.Enter();
        }

        [Server]
        public void ChangeState(GameStateHandler gameStateHandler)
        {
            if (!NetworkServer.active) return;
            screen?.Open();
            if (gameStateHandler == null) return;
            int oldI = GetIndexOfCurrentState(currentGameStateHandler?.GetType());
            int newI = GetIndexOfCurrentState(gameStateHandler?.GetType());
            if (newI == -1) return;
            currentGameStateHandler?.Exit();
            DebugUtility.Log(this, $"Changed state from {currentGameStateHandler?.GetType()} [{oldI}]");
            currentGameStateHandler = gameStatesStack[newI];
            diContainer.Inject(currentGameStateHandler);
            currentGameStateHandler?.Enter();
            DebugUtility.Log(this, $"Changed state to {currentGameStateHandler?.GetType()} [{newI}]");
            GameModeModel = new GameModeModel(GameModeModel)
            {
                CurrentStateIndex = newI
            };
        }
        #endregion

        [Command(requiresAuthority = false)]
        public void OneMinuteElapsed()
        {
            onOneMinuteElapsed?.Invoke();
        }

        [ClientRpc]
        public void PublsihMessage(string message)
        {
            EventBus<GameMessageEvent>.Raise(new GameMessageEvent() { Description = message });
        }
    }
}