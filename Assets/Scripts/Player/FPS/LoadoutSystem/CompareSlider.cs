using MyToolz.EditorToolz;
using MyToolz.Player.FPS.LoadoutSystem.View;
using MyToolz.UI.Labels;
using TMPro;
using UnityEngine;

namespace MyToolz.Player.FPS.LoadoutSystem
{
    public class CompareSlider : Label
    {
        [SerializeField, Required] private TMP_Text displayNameField;
        [SerializeField, Required] private SegmentSlider slider;

        public string Name => displayNameField.text;

        public void Construct(string fieldName,(float min, float max) range)
        {
            displayNameField.text = fieldName.ToUpper();
            slider.minValue = (int)range.min;
            slider.maxValue = (int)range.max;
        }
        

        public void SetComparison(float previous, float current)
        {
            slider.value = (int)previous;
            slider.StartComparing((int)current);
        }
    }
}
