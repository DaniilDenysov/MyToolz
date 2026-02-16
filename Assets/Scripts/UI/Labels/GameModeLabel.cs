using MyToolz.EditorToolz;
using MyToolz.Networking.ScriptableObjects;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace MyToolz.UI.Labels
{
    public class GameModeLabel : Label
    {
        [SerializeField, Required] private TMP_Text gameModeNameDisplay;
        [SerializeField, Required] private Image gameModeIconDisplay;
        [SerializeField, Required] private Button gameModeButton;

        public void Construct(GameModeSO gameModeSO, UnityAction onSelected)
        {
            gameModeNameDisplay.text = gameModeSO.Title;
            gameModeIconDisplay.sprite = gameModeSO.Icon;
            gameModeButton.onClick.AddListener(onSelected);
        }
    }
}
