using UnityEngine;

namespace MyToolz.UI.Layout
{
    /// <summary>
    /// Keeps a RectTransform inside the device safe area (notches, rounded corners, home bars).
    /// Baked onto a screen's content wrapper when the definition enables Use Safe Area. This is the
    /// one layout concern that cannot be baked - it depends on the device - so it stays tiny: a
    /// cheap comparison per frame, anchors rewritten only when the safe area actually changes.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    public class SafeAreaFitter : MonoBehaviour
    {
        private RectTransform rectTransform;
        private Rect appliedArea = Rect.zero;
        private Vector2Int appliedScreen = Vector2Int.zero;

        private void OnEnable()
        {
            rectTransform = (RectTransform)transform;
            Apply();
        }

        private void Update()
        {
            var area = Screen.safeArea;
            if (area == appliedArea && appliedScreen.x == Screen.width && appliedScreen.y == Screen.height)
                return;
            Apply();
        }

        private void Apply()
        {
            if (Screen.width <= 0 || Screen.height <= 0) return;

            var area = Screen.safeArea;
            var min = new Vector2(area.xMin / Screen.width, area.yMin / Screen.height);
            var max = new Vector2(area.xMax / Screen.width, area.yMax / Screen.height);

            rectTransform.anchorMin = min;
            rectTransform.anchorMax = max;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            appliedArea = area;
            appliedScreen = new Vector2Int(Screen.width, Screen.height);
        }
    }
}
