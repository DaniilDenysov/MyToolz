using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MyToolz.UI.Kit
{
    [DisallowMultipleComponent]
    [AddComponentMenu("MyToolz/UI Kit/Text Box Fitter")]
    public class TextBoxFitter : MonoBehaviour
    {
        [Tooltip("Box that grows with the text. Defaults to this object.")]
        [SerializeField] private RectTransform box;

        [SerializeField] private TMP_Text text;

        [Tooltip("Font size the text is set at while it fits. It only shrinks below this once the box has reached its tallest.")]
        [SerializeField, Min(1f)] private float readableSize = 50f;

        [Tooltip("Smallest font size the text may shrink to when the box cannot grow any taller.")]
        [SerializeField, Min(1f)] private float smallestSize = 36f;

        [Tooltip("Shortest the box gets. Zero keeps the height it was authored at.")]
        [SerializeField, Min(0f)] private float minHeight;

        [Tooltip("Tallest the box gets before the text starts shrinking instead.")]
        [SerializeField, Min(0f)] private float maxHeight = 600f;

        private LayoutElement layout;
        private float padding;
        private float authoredHeight;
        private string fittedText;
        private float fittedWidth = -1f;
        private bool captured;

        public float Height => box != null ? box.rect.height : 0f;

        private void Awake() => Capture();

        private void OnEnable() => fittedText = null;

        private void LateUpdate()
        {
            if (text == null || box == null)
            {
                return;
            }

            float width = text.rectTransform.rect.width;

            if (fittedText == text.text && Mathf.Approximately(fittedWidth, width))
            {
                return;
            }

            Fit();
        }

        public void Fit()
        {
            Capture();

            if (text == null || box == null)
            {
                return;
            }

            fittedText = text.text;
            fittedWidth = text.rectTransform.rect.width;

            float floor = minHeight > 0f ? minHeight : authoredHeight;
            float ceiling = Mathf.Max(floor, maxHeight);

            text.enableAutoSizing = false;
            text.fontSize = readableSize;

            float needed = text.GetPreferredValues(fittedText, fittedWidth, 0f).y + padding;
            SetHeight(Mathf.Clamp(needed, floor, ceiling));

            if (needed <= ceiling)
            {
                return;
            }

            text.fontSizeMax = readableSize;
            text.fontSizeMin = Mathf.Min(smallestSize, readableSize);
            text.enableAutoSizing = true;
        }

        private void SetHeight(float height)
        {
            if (layout != null)
            {
                layout.minHeight = height;
                layout.preferredHeight = height;
            }

            box.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
        }

        private void Capture()
        {
            if (captured)
            {
                return;
            }

            if (box == null)
            {
                box = transform as RectTransform;
            }

            if (box == null || text == null)
            {
                return;
            }

            layout = box.GetComponent<LayoutElement>();
            authoredHeight = box.rect.height;
            padding = Mathf.Max(0f, authoredHeight - text.rectTransform.rect.height);
            captured = true;
        }
    }
}
