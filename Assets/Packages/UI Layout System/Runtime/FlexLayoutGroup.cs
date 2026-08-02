using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MyToolz.UI.Layout
{
    /// <summary>
    /// Per-child settings for a <see cref="FlexLayoutGroup"/> parent. Optional - children without it
    /// get grow 0 / shrink 1 / auto basis / no margins.
    /// </summary>
    [DisallowMultipleComponent]
    public class FlexChild : MonoBehaviour
    {
        [Tooltip("Share of free space this child takes along the main axis.")]
        [SerializeField] private float grow;
        [Tooltip("How much this child gives up when space runs short.")]
        [SerializeField] private float shrink = 1f;
        [Tooltip("Starting main-axis size. -1 = auto (the child's preferred size).")]
        [SerializeField] private float basis = -1f;
        [SerializeField] private Margins margins;
        [SerializeField] private bool overrideAlignment;
        [SerializeField] private FlexAlign alignSelf = FlexAlign.Stretch;

        public float Grow => grow;
        public float Shrink => shrink;
        public float Basis => basis;
        public Margins Margins => margins;
        public bool OverridesAlignment => overrideAlignment;
        public FlexAlign AlignSelf => alignSelf;

        public void Configure(float grow, float shrink, Margins margins)
        {
            this.grow = grow;
            this.shrink = shrink;
            this.margins = margins;
        }
    }

    /// <summary>
    /// A CSS-flexbox-inspired layout group: direction, wrap, justify-content, align-items, gap, and
    /// per-child grow/shrink/basis/margins via <see cref="FlexChild"/>. Lives in UILS (not a MyToolz
    /// package change) and only recalculates when the layout system marks it dirty, like any built-in
    /// group. v1 simplifications: no align-content (wrapped lines stack from the start edge), and the
    /// reported preferred size assumes a single line.
    /// </summary>
    [DisallowMultipleComponent]
    public class FlexLayoutGroup : LayoutGroup
    {
        [SerializeField] private FlexDirection direction = FlexDirection.Row;
        [SerializeField] private bool wrap;
        [SerializeField] private FlexJustify justifyContent = FlexJustify.Start;
        [SerializeField] private FlexAlign alignItems = FlexAlign.Stretch;
        [Tooltip("x = gap along the main axis, y = gap between wrapped lines.")]
        [SerializeField] private Vector2 gap = new Vector2(8f, 8f);

        public FlexDirection Direction { get => direction; set { direction = value; SetDirty(); } }
        public bool Wrap { get => wrap; set { wrap = value; SetDirty(); } }
        public FlexJustify JustifyContent { get => justifyContent; set { justifyContent = value; SetDirty(); } }
        public FlexAlign AlignItems { get => alignItems; set { alignItems = value; SetDirty(); } }
        public Vector2 Gap { get => gap; set { gap = value; SetDirty(); } }

        private int MainAxis => direction == FlexDirection.Row ? 0 : 1;

        private struct Item
        {
            public RectTransform rect;
            public float basis;      // main-axis starting size (margins excluded)
            public float grow;
            public float shrink;
            public float crossPref;  // cross-axis preferred size (margins excluded)
            public Margins margins;
            public FlexAlign align;
            public float mainSize;   // resolved main-axis size
        }

        public override void CalculateLayoutInputHorizontal()
        {
            base.CalculateLayoutInputHorizontal();
            SetInputsForAxis(0);
        }

        public override void CalculateLayoutInputVertical() => SetInputsForAxis(1);

        public override void SetLayoutHorizontal() => DoLayout(applyAxis: 0);

        public override void SetLayoutVertical() => DoLayout(applyAxis: 1);

        /// <summary>Reports this group's own min/preferred size along an axis (single-line estimate).</summary>
        private void SetInputsForAxis(int axis)
        {
            float pad = axis == 0 ? padding.horizontal : padding.vertical;
            float totalMin = pad, totalPreferred = pad;

            bool axisIsMain = axis == MainAxis;
            for (int i = 0; i < rectChildren.Count; i++)
            {
                var child = rectChildren[i];
                var flex = child.GetComponent<FlexChild>();
                float marginSum = MarginSum(flex != null ? flex.Margins : default, axis);
                float min = LayoutUtility.GetMinSize(child, axis) + marginSum;
                float pref = LayoutUtility.GetPreferredSize(child, axis) + marginSum;

                if (axisIsMain)
                {
                    totalMin += min;
                    totalPreferred += pref;
                    if (i > 0) { totalMin += gap.x; totalPreferred += gap.x; }
                }
                else
                {
                    totalMin = Mathf.Max(totalMin, min + pad);
                    totalPreferred = Mathf.Max(totalPreferred, pref + pad);
                }
            }

            SetLayoutInputForAxis(totalMin, totalPreferred, -1, axis);
        }

        private void DoLayout(int applyAxis)
        {
            int mainAxis = MainAxis;
            int crossAxis = 1 - mainAxis;

            float innerMain = rectTransform.rect.size[mainAxis] - (mainAxis == 0 ? padding.horizontal : padding.vertical);
            float innerCross = rectTransform.rect.size[crossAxis] - (crossAxis == 0 ? padding.horizontal : padding.vertical);
            float mainStart = mainAxis == 0 ? padding.left : padding.top;
            float crossStart = crossAxis == 0 ? padding.left : padding.top;

            // Measure.
            var items = new List<Item>(rectChildren.Count);
            foreach (var child in rectChildren)
            {
                var flex = child.GetComponent<FlexChild>();
                var margins = flex != null ? flex.Margins : default;
                float basis = flex != null && flex.Basis >= 0f
                    ? flex.Basis
                    : LayoutUtility.GetPreferredSize(child, mainAxis);
                items.Add(new Item
                {
                    rect = child,
                    basis = Mathf.Max(0f, basis),
                    grow = flex != null ? Mathf.Max(0f, flex.Grow) : 0f,
                    shrink = flex != null ? Mathf.Max(0f, flex.Shrink) : 1f,
                    crossPref = LayoutUtility.GetPreferredSize(child, crossAxis),
                    margins = margins,
                    align = flex != null && flex.OverridesAlignment ? flex.AlignSelf : alignItems,
                    mainSize = 0f
                });
            }

            // Break into lines.
            var lines = new List<List<int>>();
            var line = new List<int>();
            float lineUsed = 0f;
            for (int i = 0; i < items.Count; i++)
            {
                float itemMain = items[i].basis + MarginSum(items[i].margins, mainAxis);
                float withGap = line.Count > 0 ? gap.x + itemMain : itemMain;
                if (wrap && line.Count > 0 && lineUsed + withGap > innerMain)
                {
                    lines.Add(line);
                    line = new List<int>();
                    lineUsed = itemMain;
                }
                else
                {
                    lineUsed += withGap;
                }
                line.Add(i);
            }
            if (line.Count > 0) lines.Add(line);

            // Resolve sizes & positions line by line.
            float lineCrossPos = crossStart;
            for (int li = 0; li < lines.Count; li++)
            {
                var indices = lines[li];

                // Grow / shrink along the main axis.
                float used = 0f, growSum = 0f, shrinkWeight = 0f;
                foreach (int i in indices)
                {
                    used += items[i].basis + MarginSum(items[i].margins, mainAxis);
                    growSum += items[i].grow;
                    shrinkWeight += items[i].shrink * items[i].basis;
                }
                used += gap.x * (indices.Count - 1);
                float free = innerMain - used;

                foreach (int i in indices)
                {
                    var item = items[i];
                    if (free > 0f && growSum > 0f)
                        item.mainSize = item.basis + free * (item.grow / growSum);
                    else if (free < 0f && shrinkWeight > 0f)
                        item.mainSize = Mathf.Max(0f, item.basis + free * (item.shrink * item.basis / shrinkWeight));
                    else
                        item.mainSize = item.basis;
                    items[i] = item;
                }

                // Leftover after growth (for justification).
                float usedAfter = gap.x * (indices.Count - 1);
                foreach (int i in indices)
                    usedAfter += items[i].mainSize + MarginSum(items[i].margins, mainAxis);
                float leftover = Mathf.Max(0f, innerMain - usedAfter);

                GetJustification(leftover, indices.Count, out float offset, out float extraGap);

                // Line cross size: single un-wrapped line fills the container; wrapped lines fit content.
                float lineCross;
                if (lines.Count == 1 && !wrap)
                    lineCross = innerCross;
                else
                {
                    lineCross = 0f;
                    foreach (int i in indices)
                        lineCross = Mathf.Max(lineCross, items[i].crossPref + MarginSum(items[i].margins, crossAxis));
                }

                // Place.
                float mainPos = mainStart + offset;
                foreach (int i in indices)
                {
                    var item = items[i];
                    float marginLead = MarginLeading(item.margins, mainAxis);
                    float marginTrail = MarginTrailing(item.margins, mainAxis);
                    float crossMarginLead = MarginLeading(item.margins, crossAxis);
                    float crossMarginSum = MarginSum(item.margins, crossAxis);

                    float crossSize = item.align == FlexAlign.Stretch
                        ? Mathf.Max(0f, lineCross - crossMarginSum)
                        : item.crossPref;

                    float crossOffset;
                    switch (item.align)
                    {
                        case FlexAlign.Center: crossOffset = (lineCross - crossSize - crossMarginSum) * 0.5f; break;
                        case FlexAlign.End: crossOffset = lineCross - crossSize - crossMarginSum; break;
                        default: crossOffset = 0f; break;
                    }

                    if (applyAxis == mainAxis)
                        SetChildAlongAxis(item.rect, mainAxis, mainPos + marginLead, item.mainSize);
                    else
                        SetChildAlongAxis(item.rect, crossAxis, lineCrossPos + crossMarginLead + crossOffset, crossSize);

                    mainPos += marginLead + item.mainSize + marginTrail + gap.x + extraGap;
                }

                lineCrossPos += lineCross + gap.y;
            }
        }

        private void GetJustification(float leftover, int count, out float offset, out float extraGap)
        {
            offset = 0f;
            extraGap = 0f;
            switch (justifyContent)
            {
                case FlexJustify.Center: offset = leftover * 0.5f; break;
                case FlexJustify.End: offset = leftover; break;
                case FlexJustify.SpaceBetween:
                    if (count > 1) extraGap = leftover / (count - 1);
                    break;
                case FlexJustify.SpaceAround:
                    extraGap = leftover / count;
                    offset = extraGap * 0.5f;
                    break;
                case FlexJustify.SpaceEvenly:
                    extraGap = leftover / (count + 1);
                    offset = extraGap;
                    break;
            }
        }

        private static float MarginSum(Margins m, int axis) => axis == 0 ? m.left + m.right : m.top + m.bottom;
        private static float MarginLeading(Margins m, int axis) => axis == 0 ? m.left : m.top;
        private static float MarginTrailing(Margins m, int axis) => axis == 0 ? m.right : m.bottom;
    }
}
