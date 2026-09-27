using UnityEngine;
using UnityEngine.UI;

namespace MyToolz.UI.Kit
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    [AddComponentMenu("MyToolz/UI Kit/Fit Content To Height")]
    public class FitContentToHeight : MonoBehaviour
    {
        [Tooltip("Smallest the content may be scaled to on very short screens.")]
        [SerializeField, Range(0.3f, 1f)] private float minScale = 0.6f;

        private RectTransform rect;
        private float applied = -1f;

        private void Awake() => rect = (RectTransform)transform;

        private void OnDisable() => applied = -1f;

        private void LateUpdate()
        {
            float available = rect.rect.height;
            float needed = LayoutUtility.GetPreferredHeight(rect);

            if (available <= 0f || needed <= 0f)
            {
                return;
            }

            float scale = Mathf.Clamp(available / needed, minScale, 1f);

            if (Mathf.Approximately(scale, applied))
            {
                return;
            }

            applied = scale;
            rect.localScale = new Vector3(scale, scale, 1f);
        }
    }
}
