using MyToolz.EditorToolz;
using UnityEngine;
using UnityEngine.UI;

namespace MyToolz.UI.LoadingScreen
{
    [AddComponentMenu("MyToolz/UI/Custom Slider")]
    public class CustomSlider : MonoBehaviour, IProgressBar
    {
        [SerializeField, Required] private Image fillImage;

        public float Value
        {
            get => fillImage != null ? fillImage.fillAmount : 0f;
            set
            {
                if (fillImage != null)
                    fillImage.fillAmount = Mathf.Clamp01(value);
            }
        }

#if UNITY_EDITOR
        private void Reset()
        {
            fillImage = GetComponent<Image>();
        }

        private void OnValidate()
        {
            if (fillImage != null && fillImage.type != Image.Type.Filled)
                fillImage.type = Image.Type.Filled;
        }
#endif
    }
}
