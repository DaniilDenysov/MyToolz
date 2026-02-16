using Mirror;
using MyToolz.DesignPatterns.EventBus;
using MyToolz.Events;
using MyToolz.Networking.Events;
using MyToolz.Networking.GameModes.Presenter;
using MyToolz.Networking.UI.Labels;
using MyToolz.UI.Labels;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MyToolz.Networking.GameModes.View
{
    public class TeamDeathmatchView : GameModeView<NewTeamDeathmatchRoundStarted>
    {
        [SerializeField, Required] private Transform container;
        [SerializeField, Required] private TeamDeathmatchProgress teamProgressPrefab;

        private EventBinding<PlayersStateChanged> playerStateChanged;
        private readonly SyncList<PlayerScoreDTO> teamScores = new SyncList<PlayerScoreDTO>();

        private List<Label> containerItems = new List<Label>();
        private NewTeamDeathmatchRoundStarted model;

        public override void OnStartClient()
        {
            base.OnStartClient();
            teamScores.OnChange += OnTeamViewUpdated;
            playerStateChanged = new EventBinding<PlayersStateChanged>(OnPlayerStateChanged);
            EventBus<PlayersStateChanged>.Register(playerStateChanged);
        }

        public override void OnStopClient()
        {
            base.OnStopClient();
            teamScores.OnChange -= OnTeamViewUpdated;
            EventBus<PlayersStateChanged>.Deregister(playerStateChanged);
        }

        public override void Initialize(NewTeamDeathmatchRoundStarted model)
        {
            this.model = model;
        }

        private void OnPlayerStateChanged()
        {
            UpdateView(model);
        }

        public void OnTeamViewUpdated(SyncList<PlayerScoreDTO>.Operation operation, int arg2, PlayerScoreDTO dTO)
        {
            ClearContainer();
            foreach (var player in teamScores)
            {
                EventBus<PoolRequest<Label>>.Raise(new PoolRequest<Label>()
                {
                    Prefab = teamProgressPrefab,
                    Parent = container,
                    Callback = (obj) =>
                    {
                        var status = (TeamDeathmatchProgress)obj;
                        status.Construct(player.Name, player.Min, player.Current, player.Max);
                        containerItems.Add(obj);
                        obj.transform.localScale = Vector3.one;
                    }
                });
            }
        }

        private void ClearContainer()
        {
            var itemsToClear = containerItems.ToList();

            foreach (var playerDisplay in itemsToClear)
            {
                EventBus<ReleaseRequest<Label>>.Raise(new ReleaseRequest<Label>()
                {
                    PoolObject = playerDisplay,
                    Callback = (obj) =>
                    {
                        containerItems.Remove(obj);
                    }
                });
            }
        }

        public override void UpdateView(NewTeamDeathmatchRoundStarted model)
        {
            if (!isServer) return;
            if (model == null) return;
            teamScores.Clear();
            foreach (var player in model.GetTeamScoresOrderedByKills(Core.NetworkPlayer.NetworkPlayers))
            {
                teamScores.Add(new PlayerScoreDTO()
                {
                    Name = player.Item1,
                    Current = player.Item2,
                    Max = model.KillCountLimit,
                    Min = 0
                });
            }
        }

    }
}
