using Mirror;
using MyToolz.DesignPatterns.EventBus;
using MyToolz.Networking.Events;
using MyToolz.Networking.GameModes.Presenter;
using MyToolz.Networking.UI.Labels;
using MyToolz.Tweener.UI;
using MyToolz.Utilities.Debug;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MyToolz.Networking.GameModes.View
{
    public class CaptureTheFlagView : GameModeView<CaptureTheFlagNewRoundStarted>
    {
        [SerializeField, Required] private Transform container;
        [SerializeField, Required] private UITweener tweener;
        [SerializeField, Required] private TeamDeathmatchProgress [] teamProgressDisplays;
        private readonly SyncList<PlayerScoreDTO> teamScores = new SyncList<PlayerScoreDTO>();
        private EventBinding<PlayersStateChanged> playerStateChanged;
        private CaptureTheFlagNewRoundStarted model;

        public override void OnStartClient()
        {
            base.OnStartClient();
            teamScores.OnChange += OnTeamViewUpdated;
            playerStateChanged = new EventBinding<PlayersStateChanged>(OnPlayerStateChanged);
            EventBus<PlayersStateChanged>.Register(playerStateChanged);
        }

        public override void Initialize(CaptureTheFlagNewRoundStarted model)
        {
            this.model = model;
        }

        public override void OnStopClient()
        {
            base.OnStopClient();
            teamScores.OnChange -= OnTeamViewUpdated;
            EventBus<PlayersStateChanged>.Deregister(playerStateChanged);
        }

        private void RefreshUI()
        {
            string localPlayerTeamGuid = Core.NetworkPlayer.LocalPlayerInstance?.TeamGuid;

            if (string.IsNullOrEmpty(localPlayerTeamGuid))
            {
                DebugUtility.LogWarning(this, "Local player team GUID not found.");
                return;
            }

            var teams = new List<PlayerScoreDTO>(teamScores);


            teams = teams
                .OrderByDescending(dto => dto.Name == localPlayerTeamGuid)
                .ToList();

            foreach (var itm in teamProgressDisplays)
            {
                if (teams.Count == 0) return;
                var player = teams.FirstOrDefault();
                itm.Construct(player.Name, player.Min, player.Current, player.Max);
                teams.Remove(player);
            }
        }

        private void OnPlayerStateChanged()
        {
            UpdateView(model);
        }

        private void OnTeamViewUpdated(SyncList<PlayerScoreDTO>.Operation operation, int arg2, PlayerScoreDTO dTO)
        {
            RefreshUI();
        }

        public override void UpdateView(CaptureTheFlagNewRoundStarted model)
        {
            if (!isServer)
            {
                RefreshUI();
                return;
            }
            if (model == null) return;
            teamScores.Clear();
            foreach (var player in model.GetTeamScoresOrderedByKills(Core.NetworkPlayer.NetworkPlayers))
            {
                teamScores.Add(new PlayerScoreDTO()
                {
                    Name = player.Item1,
                    Current = player.Item2,
                    Max = model.PointsLimit,
                    Min = 0
                });
                DebugUtility.Log(this, $"{player.Item1}:{player.Item2}");
            }
        }
    }
}
