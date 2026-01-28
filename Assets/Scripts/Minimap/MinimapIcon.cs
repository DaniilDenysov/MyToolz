using UnityEngine;
using UnityEngine.UI;

namespace MyToolz.MiniMap
{
    public class MinimapIcon : MonoBehaviour
    {
        public Image Image;
        public RectTransform RectTransform;
        public RectTransform IconRectTransform;

        public void Hide()
        {
            Image.enabled = false;
        }

        public void Show()
        {
            Image.enabled = true;
        }
    }
}
