using MyToolz.DesignPatterns.EventBus;
using MyToolz.Events;
using MyToolz.Networking.Events;
using MyToolz.Networking.UI.Labels;
using MyToolz.UI.Labels;
using NoSaints.UI.Labels;
using UnityEngine;

namespace MyToolz.Networking.Scoreboards
{
    public class DeathmatchScoreboard : ScoreboardBackend
    {
        [SerializeField] protected Label scoreboardLabel;
        public static Core.NetworkPlayer KillLeader
        {
            get
            {
                Core.NetworkPlayer newKillLeader = null;
                foreach (var player in Core.NetworkPlayer.NetworkPlayers)
                {
                    if (!player) continue;
                    if (!newKillLeader)
                    {
                        newKillLeader = player;
                        continue;
                    }
                    if (newKillLeader.Kills < player.Kills)
                    {
                        newKillLeader = player;
                    }
                }
                return newKillLeader;
            }
        }

        private EventBinding<PlayerKilledEvent> playerKilledEventBinding;
        private EventBinding<PlayersStateChanged> playersStateChangedEventBinding;

        public override void RegisterEvents()
        {
            base.RegisterEvents();
            playerKilledEventBinding = new EventBinding<PlayerKilledEvent>(OnPlayerKilled);
            EventBus<PlayerKilledEvent>.Register(playerKilledEventBinding);
            playersStateChangedEventBinding = new EventBinding<PlayersStateChanged>(OnPlayerStateChanged);
            EventBus<PlayersStateChanged>.Register(playersStateChangedEventBinding);
        }

        public override void UnregisterEvents()
        {
            base.UnregisterEvents();
            EventBus<PlayerKilledEvent>.Deregister(playerKilledEventBinding);
            EventBus<PlayersStateChanged>.Deregister(playersStateChangedEventBinding);
        }

        private void OnPlayerKilled(PlayerKilledEvent _)
        {
            Debug.Log("Player killed!");
            if (isDirty) return;
            SetIsDirty();
        }
        private void OnPlayerStateChanged(PlayersStateChanged @event)
        {
            if (isDirty) return;
            SetIsDirty();
        }

        public override void Refresh()
        {
            isDirty = false;
            ClearContainer();
            foreach (var player in Core.NetworkPlayer.NetworkPlayers)
            {
                CreateLabel(player);
            }
        }

        public override void ClearContainer()
        {
            foreach (Label child in labelContainer.GetComponentsInChildren<Label>())
            {
                EventBus<ReleaseRequest<Label>>.Raise(new ReleaseRequest<Label>()
                {
                    PoolObject = child,
                });
            }
        }

        public virtual void CreateLabel(Core.NetworkPlayer networkPlayer)
        {
            EventBus<PoolRequest<Label>>.Raise(new PoolRequest<Label>()
            {
                Prefab = scoreboardLabel,
                Callback = (label) =>
                {
                    label.transform.SetParent(labelContainer);
                    ((ScoreboardLabel)label).Construct(networkPlayer.Nickname, networkPlayer.Kills, networkPlayer.Assists, networkPlayer.Deaths, networkPlayer.Points);
                    label.transform.localScale = Vector3.one;
                }
            });
        }
    }
}