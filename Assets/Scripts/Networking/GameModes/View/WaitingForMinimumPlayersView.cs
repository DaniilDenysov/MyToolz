using MyToolz.Networking.GameModes.Presenter;
using TMPro;
using UnityEngine;

namespace MyToolz.Networking.GameModes.View
{
    public class WaitingForMinimumPlayersView : GameModeView<WaitingForMinimumPlayers>
    {
        [SerializeField] private TMP_Text statusDisplay;
        [SerializeField] private TMP_Text timeDisplay;

        public override void Initialize(WaitingForMinimumPlayers model)
        {
            statusDisplay.text = "Waiting for minimum players!";
        }

        public override void UpdateView(WaitingForMinimumPlayers model)
        {
           
        }
    }
}
