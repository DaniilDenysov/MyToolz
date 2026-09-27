using System.Collections.Generic;
using UnityEngine;

namespace MyToolz.UI.Kit
{
    [ExecuteAlways]
    public class ArcLayout : MonoBehaviour
    {
        [Tooltip("Radius of the circle the children sit on. Larger means a flatter arch.")]
        [SerializeField, Min(1f)] private float radius = 420f;

        [Tooltip("Total angle the children are spread across.")]
        [SerializeField, Range(0f, 180f)] private float spanDegrees = 70f;

        [Tooltip("Turns each child to follow the curve, so the outer ones tilt outward.")]
        [SerializeField] private bool rotateChildren = true;

        [Tooltip("Scale of the children at the ends of the arch.")]
        [SerializeField, Min(0.01f)] private float sideScale = 1f;

        [Tooltip("Extra scale at the middle of the arch, tapering to none at the ends.")]
        [SerializeField, Min(0f)] private float centerBoost = 0.35f;

        [Tooltip("Flips the arch so it bulges down instead of up.")]
        [SerializeField] private bool invert;

        [Tooltip("Lay out hidden children too, so the arch keeps its shape when lives are spent.")]
        [SerializeField] private bool includeInactive = true;

        private readonly List<RectTransform> items = new List<RectTransform>();

        private void OnEnable() => Apply();

        private void OnTransformChildrenChanged() => Apply();

        private void OnValidate() => Apply();

        public void Apply()
        {
            if (this == null || transform == null)
            {
                return;
            }

            Collect();

            if (items.Count == 0)
            {
                return;
            }

            float step = items.Count > 1 ? spanDegrees / (items.Count - 1) : 0f;
            float start = 90f + (spanDegrees * 0.5f);
            float sign = invert ? -1f : 1f;

            for (int i = 0; i < items.Count; i++)
            {
                float angle = start - (step * i);
                float radians = angle * Mathf.Deg2Rad;

                float x = radius * Mathf.Cos(radians);
                float y = sign * ((radius * Mathf.Sin(radians)) - radius);

                items[i].anchoredPosition = new Vector2(x, y);

                if (rotateChildren)
                {
                    items[i].localRotation = Quaternion.Euler(0f, 0f, sign * (angle - 90f));
                }

                float position = items.Count > 1 ? i / (float)(items.Count - 1) : 0.5f;
                float fromCenter = Mathf.Abs((position * 2f) - 1f);
                float scale = sideScale + (centerBoost * (1f - fromCenter));

                items[i].localScale = new Vector3(scale, scale, 1f);
            }
        }

        private void Collect()
        {
            items.Clear();

            for (int i = 0; i < transform.childCount; i++)
            {
                RectTransform child = transform.GetChild(i) as RectTransform;

                if (child == null || (!includeInactive && !child.gameObject.activeSelf))
                {
                    continue;
                }

                items.Add(child);
            }
        }
    }
}
