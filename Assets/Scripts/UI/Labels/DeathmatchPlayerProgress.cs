using MyToolz.UI.Labels;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MyToolz.Networking.UI.Labels
{
    public class DeathmatchPlayerProgress : Label
    {
        [SerializeField, Required] private TMP_Text nicknameDisplay;
        [SerializeField, Required] private Slider progressDisplay;

        public void Construct(string nickname, int min, int current,int max)
        {
            nicknameDisplay.text = nickname;
            progressDisplay.maxValue = max;
            progressDisplay.minValue = min;
            progressDisplay.value = current;
        }
    }
}
