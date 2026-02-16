using MyToolz.EditorToolz;
using MyToolz.Networking.Relays;
using MyToolz.UI.Labels;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace MyToolz.Networking.Matchmaking.View
{
    public class LobbyLabel : Label
    {
        [SerializeField, Required] private TMP_Text gameModeNameDisplay;
        [SerializeField, Required] private TMP_Text mapNameDisplay;
        [SerializeField, Required] private TMP_Text playersInLobbyDisplay;
        [SerializeField, Required] private Button button;
        public void Construct(LobbyDTO lobbyDTO, UnityAction onSelected)
        {
            gameModeNameDisplay.text = lobbyDTO.GameModeName;
            mapNameDisplay.text = lobbyDTO.MapName;
            playersInLobbyDisplay.text = $"{lobbyDTO.CurrentPlayers}/{lobbyDTO.MaxPlayers}";
            button.onClick.AddListener(onSelected);
        }
    }
}
