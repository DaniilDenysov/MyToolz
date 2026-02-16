using MyToolz.Networking.GameModes.Presenter;
using TMPro;
using UnityEngine;

namespace MyToolz.Networking.GameModes.View
{
    public class WaitingForMorePlayersView : GameModeView<WaitingForMorePlayers>
    {
        [SerializeField] private TMP_Text statusDisplay;
        [SerializeField] private TMP_Text timeDisplay;

        public override void Initialize(WaitingForMorePlayers model)
        {
            statusDisplay.text = "Waiting for more players!";
        }


        public override void UpdateView(WaitingForMorePlayers model)
        {


        }
    }
}