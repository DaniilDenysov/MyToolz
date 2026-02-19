using MyToolz.EditorToolz;
using TMPro;
using UnityEngine;

namespace MyToolz.UI.ToolTip
{
    public class Tooltip : MonoBehaviour
    {
        [SerializeField, Required, FoldoutGroup("Config")] private TMP_Text description;
        [SerializeField, FoldoutGroup("Config")] private float screenMargin = 8f;

        private RectTransform rectTransform;

        public void Initialize(string description)
        {
            this.description.text = description;

            rectTransform = transform as RectTransform;
            if (rectTransform == null) return;

            rectTransform.pivot = new Vector2(0f, 0.1f);
            UpdatePosition();
        }

        private void Update()
        {
            if (rectTransform == null) return;
            UpdatePosition();
        }

        private void UpdatePosition()
        {
            Vector2 pointerPosition = GetPointerPosition();
            Vector2 size = rectTransform.rect.size;
            Vector2 position = pointerPosition;

            if (position.x + size.x + screenMargin > Screen.width)
                position.x = Screen.width - size.x - screenMargin;

            if (position.y - size.y - screenMargin < 0f)
                position.y = size.y + screenMargin;

            rectTransform.position = position;
        }

        private Vector2 GetPointerPosition()
        {
            return UnityEngine.Input.mousePosition;
        }
    }
}
