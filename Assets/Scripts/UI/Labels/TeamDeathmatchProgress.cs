using MyToolz.EditorToolz;
using MyToolz.UI.Labels;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MyToolz.Networking.UI.Labels
{
    public class TeamDeathmatchProgress : Label
    {
        [SerializeField, Required] private TMP_Text teamNameDisplay;
        [SerializeField, Required] private TMP_Text scoreDisplay;
        [SerializeField, Required] private Slider progressDisplay;
        [SerializeField, Required] private Image sliderImage;
        [SerializeField] private Color friendly, notFirendly; 

        public void Construct(string nickname, int min, int current, int max)
        {
            teamNameDisplay.text = nickname;
            scoreDisplay.text = $"{current}";
            progressDisplay.maxValue = max;
            progressDisplay.minValue = min;
            progressDisplay.value = current;
            sliderImage.color = MyToolz.Networking.Core.NetworkPlayer.LocalPlayerInstance.IsFriendly(nickname) ? friendly : notFirendly;

        }
    }
}
