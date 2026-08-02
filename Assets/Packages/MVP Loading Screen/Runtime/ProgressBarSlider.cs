using UnityEngine;
using UnityEngine.UI;

namespace MyToolz.UI.LoadingScreen
{
    [AddComponentMenu("MyToolz/UI/Progress Bar Slider")]
    public class ProgressBarSlider : Slider, IProgressBar
    {
        public float Value
        {
            get => normalizedValue;
            set => normalizedValue = value;
        }
    }
}
