using MyToolz.DesignPatterns.EventBus;
using MyToolz.Events;
using MyToolz.Networking.UI.Labels;
using MyToolz.UI.Labels;
using NoSaints.UI.Labels;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace MyToolz.Networking.Scoreboards
{
    public class ScoreboardTeamLabel : Label
    {
        protected List<ScoreboardLabel> scoreboardLabels = new List<ScoreboardLabel>();
        [SerializeField] protected TMP_Text teamNameDisplay;
        [SerializeField] protected Transform root;

        public void Construct(string teamName)
        {
            Clear();
            teamNameDisplay.text = teamName;
        }

        public void Add(List<ScoreboardLabel> scoreboardLabels)
        {
            if (scoreboardLabels == null) return;
            scoreboardLabels.ForEach(l => Add(l));
        }

        public void Add(ScoreboardLabel scoreboardLabel)
        {
            if (scoreboardLabel == null) return;
            scoreboardLabels.Add(scoreboardLabel);
            scoreboardLabel.transform.SetParent(root);
        }

        public void Clear()
        {
            foreach (ScoreboardLabel child in scoreboardLabels)
            {
                EventBus<ReleaseRequest<Label>>.Raise(new ReleaseRequest<Label>()
                {
                    PoolObject = child
                });
            }
            scoreboardLabels.Clear();
        }
    }
}
