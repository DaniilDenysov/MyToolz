using MyToolz.DesignPatterns.EventBus;
using MyToolz.Events;
using MyToolz.Networking.UI.Labels;
using MyToolz.UI.Labels;
using System.Collections.Generic;
using UnityEngine;

namespace MyToolz.Networking.Scoreboards
{
    public class TeamDeathmatchScoreboard : DeathmatchScoreboard
    {
        [SerializeField] protected Label teamScoreboardLabel;

        public override void Refresh()
        {
            isDirty = false;
            ClearContainer();
            Dictionary<string, List<Core.NetworkPlayer>> teams = new Dictionary<string, List<Core.NetworkPlayer>>();
            foreach (var player in Core.NetworkPlayer.NetworkPlayers)
            {
                if (!teams.ContainsKey(player.TeamGuid)) teams[player.TeamGuid] = new List<Core.NetworkPlayer>();
                teams[player.TeamGuid].Add(player);
            }

            foreach (var team in teams)
            {
                EventBus<PoolRequest<Label>>.Raise(new PoolRequest<Label>()
                {
                    Prefab = teamScoreboardLabel,
                    Callback = (label) =>
                    {
                        label.transform.SetParent(labelContainer);
                        var teamScoreLabel = (ScoreboardTeamLabel)label;
                        teamScoreLabel.Construct(team.Key);
                        label.transform.localScale = Vector3.one;
                        foreach (var player in team.Value)
                        {
                            EventBus<PoolRequest<Label>>.Raise(new PoolRequest<Label>()
                            {
                                Prefab = scoreboardLabel,
                                Callback = (playerLabel) =>
                                {
                                    label.transform.SetParent(labelContainer);

                                    var playerScoreBoardLabel = (ScoreboardLabel)playerLabel;
                                    playerLabel.transform.SetParent(labelContainer);
                                    playerScoreBoardLabel.Construct(player.Nickname, player.Kills, player.Assists, player.Deaths, player.Points);
                                    playerLabel.transform.localScale = Vector3.one;
                                }
                            });
                        }
                    }
                });
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
    }
}
