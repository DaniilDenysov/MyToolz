using DG.Tweening;
using UnityEngine;

namespace MyToolz.UI.LoadingScreen
{
    [AddComponentMenu("MyToolz/Loading Screen/Indeterminate Loading Ring")]
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class IndeterminateLoadingRing : UnityEngine.UI.MaskableGraphic, IProgressBar
    {
        [SerializeField, Min(1f)] private float thickness = 8f;
        [SerializeField, Range(1f, 359f)] private float sweepAngle = 270f;
        [SerializeField, Range(-360f, 360f)] private float startAngle = 135f;
        [SerializeField, Range(8, 128)] private int segments = 64;
        [SerializeField] private Color gradientStart = new Color(0.43f, 0f, 0.25f, 1f);
        [SerializeField] private Color gradientEnd = new Color(0.93f, 0.05f, 0.55f, 1f);
        [SerializeField, Min(1f)] private float rotationSpeed = 150f;

        private float value;
        private Tween rotationTween;

        public float Value
        {
            get => value;
            set => this.value = Mathf.Clamp01(value);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            StartRotation();
        }

        protected override void OnDisable()
        {
            StopRotation();
            base.OnDisable();
        }

        protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper vertexHelper)
        {
            vertexHelper.Clear();

            Rect rect = GetPixelAdjustedRect();
            Vector2 center = rect.center;
            float halfThickness = thickness * 0.5f;
            float radius = Mathf.Max(halfThickness, Mathf.Min(rect.width, rect.height) * 0.5f - halfThickness);
            Vector2 startCenter = AddArc(vertexHelper, center, radius, halfThickness);
            Vector2 endCenter = PointOnCircle(center, radius, startAngle - sweepAngle);

            AddRoundCap(vertexHelper, startCenter, halfThickness, MultiplyColors(color, gradientStart));
            AddRoundCap(vertexHelper, endCenter, halfThickness, MultiplyColors(color, gradientEnd));
        }

        private Vector2 AddArc(
            UnityEngine.UI.VertexHelper vertexHelper,
            Vector2 center,
            float radius,
            float halfThickness)
        {
            float innerRadius = radius - halfThickness;
            float outerRadius = radius + halfThickness;

            for (int i = 0; i <= segments; i++)
            {
                float progress = (float)i / segments;
                float angle = startAngle - sweepAngle * progress;
                Vector2 direction = Direction(angle);
                Color32 vertexColor = MultiplyColors(color, Color.Lerp(gradientStart, gradientEnd, progress));
                AddVertex(vertexHelper, center + direction * innerRadius, vertexColor);
                AddVertex(vertexHelper, center + direction * outerRadius, vertexColor);

                if (i == 0)
                {
                    continue;
                }

                int index = i * 2;
                vertexHelper.AddTriangle(index - 2, index - 1, index);
                vertexHelper.AddTriangle(index, index - 1, index + 1);
            }

            return PointOnCircle(center, radius, startAngle);
        }

        private static void AddRoundCap(
            UnityEngine.UI.VertexHelper vertexHelper,
            Vector2 center,
            float radius,
            Color32 vertexColor)
        {
            const int capSegments = 12;
            int centerIndex = vertexHelper.currentVertCount;
            AddVertex(vertexHelper, center, vertexColor);

            for (int i = 0; i <= capSegments; i++)
            {
                float angle = 360f * i / capSegments;
                AddVertex(vertexHelper, center + Direction(angle) * radius, vertexColor);

                if (i > 0)
                {
                    vertexHelper.AddTriangle(centerIndex, centerIndex + i, centerIndex + i + 1);
                }
            }
        }

        private static Vector2 PointOnCircle(Vector2 center, float radius, float angle)
        {
            return center + Direction(angle) * radius;
        }

        private static Vector2 Direction(float angle)
        {
            float radians = angle * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
        }

        private static Color MultiplyColors(Color first, Color second)
        {
            return new Color(
                first.r * second.r,
                first.g * second.g,
                first.b * second.b,
                first.a * second.a);
        }

        private static void AddVertex(
            UnityEngine.UI.VertexHelper vertexHelper,
            Vector2 position,
            Color32 vertexColor)
        {
            vertexHelper.AddVert(position, vertexColor, Vector2.zero);
        }

        private void StartRotation()
        {
            StopRotation();
            rectTransform.localRotation = Quaternion.identity;
            rotationTween = rectTransform
                .DOLocalRotate(new Vector3(0f, 0f, -360f), 360f / rotationSpeed, RotateMode.FastBeyond360)
                .SetEase(Ease.Linear)
                .SetLoops(-1, LoopType.Restart)
                .SetUpdate(true);
        }

        private void StopRotation()
        {
            rotationTween?.Kill();
            rotationTween = null;
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            thickness = Mathf.Max(1f, thickness);
            segments = Mathf.Clamp(segments, 8, 128);
            rotationSpeed = Mathf.Max(1f, rotationSpeed);
            SetVerticesDirty();
        }
#endif
    }
}
