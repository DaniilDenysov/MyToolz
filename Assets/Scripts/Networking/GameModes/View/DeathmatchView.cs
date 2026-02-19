using Mirror;
using MyToolz.DesignPatterns.EventBus;
using MyToolz.EditorToolz;
using MyToolz.Events;
using MyToolz.Networking.Events;
using MyToolz.Networking.GameModes.Presenter;
using MyToolz.Networking.UI.Labels;
using MyToolz.UI.Labels;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MyToolz.Networking
{
    [System.Serializable]
    public struct PlayerScoreDTO
    {
        public string Name;
        public int Current;
        public int Min;
        public int Max;
    }
}

namespace MyToolz.Networking.GameModes.View
{
    public class DeathmatchView : GameModeView<NewDeathmatchRoundStarted>
    {
        [SerializeField, Required] private Transform container;
        [SerializeField, Required] private DeathmatchPlayerProgress playerProgressPrefab;

        private List<Label> containerItems = new List<Label>();
        private EventBinding<PlayersStateChanged> playerStateChanged;
        private readonly SyncList<PlayerScoreDTO> playerScores = new SyncList<PlayerScoreDTO>();
        private NewDeathmatchRoundStarted model;

        private void OnScoresUpdated(SyncList<PlayerScoreDTO>.Operation operation, int arg2, PlayerScoreDTO dTO)
        {
            RefreshUI();
        }

        public override void Initialize(NewDeathmatchRoundStarted model)
        {
            this.model = model;
        }

        private void OnPlayerStateChanged()
        {
            UpdateView(model);
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            playerScores.OnChange += OnScoresUpdated;
            playerStateChanged = new EventBinding<PlayersStateChanged>(OnPlayerStateChanged);
            EventBus<PlayersStateChanged>.Register(playerStateChanged);
        }

        public override void OnStopClient()
        {
            base.OnStopClient();
            playerScores.OnChange -= OnScoresUpdated;
            EventBus<PlayersStateChanged>.Deregister(playerStateChanged);
        }

        private void RefreshUI()
        {
            ClearContainer();
            if (playerScores == null) return;
            foreach (var player in playerScores)
            {
                EventBus<PoolRequest<Label>>.Raise(new PoolRequest<Label>()
                {
                    Prefab = playerProgressPrefab,
                    Parent = container,
                    Callback = (obj) =>
                    {
                        var status = (DeathmatchPlayerProgress)obj;
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

        public override void UpdateView(NewDeathmatchRoundStarted model)
        {
            if (!isServer)
            {
                RefreshUI();
                return;
            }
            if (model == null) return;
            playerScores.Clear();
            foreach (var player in model.GetTopPlayers(Core.NetworkPlayer.NetworkPlayers))
            {
                playerScores.Add(new PlayerScoreDTO()
                {
                    Name = player.Nickname,
                    Current = player.Kills,
                    Max = model.KillCountLimit,
                    Min = 0
                });
            }
        }
    }
}
