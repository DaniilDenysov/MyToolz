using MyToolz.UI.Labels;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

namespace MyToolz.Networking.UI.Labels
{
    public class ScoreboardLabel : Label
    {
        [SerializeField, Required] private TMP_Text playerNameDisplay, killCountDisplay, assistsCountDisplay, deathsCountDisplay, pointsCountDisplay;

        public void Construct(string playerName, int killCount, int assistsCount, int deathsCount, int pointsCount)
        {
            playerNameDisplay.text = playerName;
            killCountDisplay.text = $"{killCount}";
            assistsCountDisplay.text = $"{assistsCount}";
            deathsCountDisplay.text = $"{deathsCount}";
            pointsCountDisplay.text = $"{pointsCount}";
        }
    }
}
