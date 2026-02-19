using Mirror;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using MyToolz.Networking.Scoreboards;
using Steamworks;
using MyToolz.Networking.Strategies;
using MyToolz.Networking.GameModes.Presenter;
using MyToolz.EditorToolz;

namespace MyToolz.Networking
{
    [System.Serializable]
    public class Team
    {
        public string Guid;
        [Range(0, 100)] public int maxPlayerAmount;

        public void Setup()
        {
            if (string.IsNullOrWhiteSpace(Guid) || string.IsNullOrEmpty(Guid)) Guid = System.Guid.NewGuid().ToString();
            // color = GenerateRandomColorAvoidingWhite();
        }

        private Color GenerateRandomColorAvoidingWhite()
        {
            const float minValue = 0.2f;
            const float maxValue = 0.8f;

            float r = UnityEngine.Random.Range(minValue, maxValue);
            float g = UnityEngine.Random.Range(minValue, maxValue);
            float b = UnityEngine.Random.Range(minValue, maxValue);

            return new Color(r, g, b);
        }
    }
}

namespace MyToolz.Networking.Strategies
{
    [System.Serializable]
    public abstract class TopPlayerSelectionStrategy
    {
        public abstract List<Core.NetworkPlayer> GetTopPlayers();
    }

    [System.Serializable]
    public class TopDeathMatchPlayerSelectionStrategy : TopPlayerSelectionStrategy
    {
        public override List<Core.NetworkPlayer> GetTopPlayers()
        {
            return Core.NetworkPlayer.NetworkPlayers
                .OrderByDescending(p => p.Kills)
                .ToList();
        }
    }

    [System.Serializable]
    public class TopTeamDeathMatchPlayerSelectionStrategy : TopPlayerSelectionStrategy
    {
        public override List<Core.NetworkPlayer> GetTopPlayers()
        {
            var teamGroups = Core.NetworkPlayer.NetworkPlayers
                .GroupBy(p => p.TeamGuid)
                .ToDictionary(g => g.Key, g => g.ToList());

            var topTeam = teamGroups
                .Select(kvp => new
                {
                    TeamGuid = kvp.Key,
                    TotalScore = kvp.Value.Sum(p => p.Kills),
                    Players = kvp.Value
                })
                .OrderByDescending(t => t.TotalScore)
                .FirstOrDefault();

            if (topTeam == null)
                return new List<Core.NetworkPlayer>();

            return topTeam.Players
                .OrderByDescending(p => p.Kills)
                .ToList();
        }
    }

    [System.Serializable]
    public class TopCaptureTheFlagPlayerSelectionStrategy : TopPlayerSelectionStrategy
    {
        public override List<Core.NetworkPlayer> GetTopPlayers()
        {
            return Core.NetworkPlayer.NetworkPlayers
                .OrderByDescending(p => p.Points)
                .ToList();
        }
    }
}

namespace MyToolz.Networking.ScriptableObjects
{
    [CreateAssetMenu(fileName = "GameMode", menuName = "NoSaints/UI/GameMode")]
    public class GameModeSO : ScriptableObject
    {
        [FoldoutGroup("Basic Info", true)]
        public string Title;

        [FoldoutGroup("Basic Info", true)]
        public Sprite Icon;

        [FoldoutGroup("Basic Info")]
        [TextArea] public string Objective;

        [FoldoutGroup("Basic Info")]
        [TextArea] public string Description;

        [FoldoutGroup("Basic Info")]
        [SerializeField] private ELobbyType lobbyType;
        public ELobbyType LobbyType { get => lobbyType; }

        [FoldoutGroup("Teams Settings")]
        public bool EnableTeamKill = false;
        [FoldoutGroup("Teams Settings")]
        public bool EnableSelfHarm = true;
        [FoldoutGroup("Teams Settings")]
        public List<Team> teams;

        [FoldoutGroup("Map Settings")]
        [Scene]
        public string[] maps;

        // Matchmaking settings
        [FoldoutGroup("Matchmaking Settings", true)]
        public int MinPlayers;
        [FoldoutGroup("Matchmaking Settings", true)]
        public bool StopIfNotEnoughPlayers = true;
        public int MaxPlayers
        {
            get => teams.Sum((t) => t.maxPlayerAmount);
        }

        [FoldoutGroup("Respawn Settings", true)]
        [Range(0,1000)] public float RespawnTime = 10f;
        [FoldoutGroup("Respawn Settings", true)]
        public bool EnableAutoRespawn = true;
        [FoldoutGroup("Respawn Settings", true)]
        [Range(0, 1000), ShowIf("@EnableAutoRespawn")] public float AutoRespawnTime = 10f;
        [FoldoutGroup("Loadout Settings", true)]
        public bool EnableInstantUpdate = false;
        [FoldoutGroup("Spectator Mode Settings", true)]
        public bool EnableSpectatorMode = true;
        [FoldoutGroup("Spectator Mode Settings", true)]
        [ShowIf("@EnableSpectatorMode")] public bool IncludeUnfriendly = true;
        /// <summary>
        /// Ignores team ownership(player will be able to respawn anywhere for e.g.)
        /// </summary>
        [SerializeField, Tooltip("Ignores team ownership(player will be able to respawn anywhere for e.g.)")] private bool enableCommunism;
        public bool EnableCommunism =>  enableCommunism;

        [FoldoutGroup("End game", true), SerializeReference, Required] private TopPlayerSelectionStrategy topPlayerSelectionStrategy;
        public TopPlayerSelectionStrategy TopPlayerSelectionStrategy => topPlayerSelectionStrategy;

        [SerializeReference] private RespawnDistributionStrategy respawnDistributionStrategy;
        public RespawnDistributionStrategy RespawnDistributionStrategy
        {
            get => respawnDistributionStrategy;
        }

        [SerializeReference] private TeamDistributionStrategy teamDistributionStrategy;
        public TeamDistributionStrategy TeamDistributionStrategy
        {
            get => teamDistributionStrategy;
        }

        [SerializeReference] private ScoreboardBackend scoreboard;
        public ScoreboardBackend Scoreboard
        {
            get => scoreboard;
        }

        [SerializeReference] private GameStateHandler[] gameStateHandlers;
        public GameStateHandler [] GameStateHandlers => gameStateHandlers;



#if UNITY_EDITOR
        private void OnEnable()
        {
            foreach (var team in teams)
            {
                team.Setup();
            }
        }
#endif

        public int GetTotalPlayerCount()
        {
            return teams.Sum((t) => t.maxPlayerAmount);
        }

        public string GetRandomMap ()
        {
            if (maps.Length == 0) return "";
            return maps[UnityEngine.Random.Range(0, maps.Length)];
        }

        public int GetPlayerCountForTeam(string teamGuid)
        {
            var team = teams.FirstOrDefault((t) => t.Guid.Equals(teamGuid));
            return team == null ? 0 : team.maxPlayerAmount;
        }
    }
}
