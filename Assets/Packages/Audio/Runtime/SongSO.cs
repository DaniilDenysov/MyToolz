using System.Collections.Generic;
using UnityEngine;
using MyToolz.Audio;
using MyToolz.EditorToolz;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MyToolz.Audio
{
    [CreateAssetMenu(fileName = nameof(SongSO), menuName = "MyToolz/Audio/" + nameof(SongSO))]
    public class SongSO : ScriptableObject
    {
        [TitleGroup("Configuration")]
        [LabelText("Reverb Tail"), MinValue(0f), SuffixLabel("s", overlay: true)]
        [SerializeField] private float reverbTail;

        [SerializeField] private AudioSourceConfigSO audioSourceConfigSO;

        [TitleGroup("Intensity Layers")]
        [ListDrawerSettings(DraggableItems = true)]
        [SerializeField] private List<AudioClip> intensityClips = new();

        public float ReverbTail => reverbTail;
        public IReadOnlyList<AudioClip> IntensityClips => intensityClips;

        public AudioSourceConfigSO AudioSourceConfigSO => audioSourceConfigSO;

        public Data ToSongData() => new Data
        {
            reverbTail = reverbTail,
            intensityClips = new List<AudioClip>(intensityClips)
        };

        public struct Data
        {
            public float reverbTail;
            public List<AudioClip> intensityClips;
        }

#if UNITY_EDITOR
        private static readonly Color graphBackground = new Color(0.10f, 0.10f, 0.14f);
        private static readonly Color graphBorder = new Color(0.25f, 0.25f, 0.30f);
        private static readonly Color gridLine = new Color(1f, 1f, 1f, 0.04f);
        private static readonly Color tailOverlay = new Color(1f, 0.42f, 0.21f, 0.40f);
        private static readonly Color tailMarker = new Color(1f, 0.42f, 0.21f, 0.90f);
        private static readonly Color axisColor = new Color(1f, 1f, 1f, 0.25f);
        private static readonly Color dimLabel = new Color(1f, 1f, 1f, 0.50f);

        private static readonly Color[] layerPalette =
        {
            new Color(0.00f, 0.68f, 0.71f, 0.85f),
            new Color(0.22f, 0.55f, 0.87f, 0.85f),
            new Color(0.42f, 0.36f, 0.91f, 0.85f),
            new Color(0.70f, 0.32f, 0.86f, 0.85f),
            new Color(0.92f, 0.30f, 0.62f, 0.85f),
            new Color(0.95f, 0.52f, 0.30f, 0.85f),
        };

        [TitleGroup("Clip Timeline")]
        [OnInspectorGUI, PropertyOrder(100)]
        private void DrawClipTimeline()
        {
            if (intensityClips == null || intensityClips.Count == 0)
            {
                EditorGUILayout.HelpBox("Add intensity clips to visualize the timeline.", MessageType.Info);
                return;
            }

            float maxDuration = 0f;
            bool hasAnyClip = false;

            for (int i = 0; i < intensityClips.Count; i++)
            {
                if (intensityClips[i] != null)
                {
                    hasAnyClip = true;
                    if (intensityClips[i].length > maxDuration)
                    {
                        maxDuration = intensityClips[i].length;
                    }
                }
            }

            if (!hasAnyClip || maxDuration <= 0f)
            {
                EditorGUILayout.HelpBox("Assign audio clips to visualize the timeline.", MessageType.Info);
                return;
            }

            const float leftMargin = 62f;
            const float rightMargin = 14f;
            const float topMargin = 10f;
            const float rowHeight = 28f;
            const float rowGap = 5f;
            const float axisHeight = 28f;
            const float legendHeight = 22f;
            const float bottomPad = 10f;

            int layerCount = intensityClips.Count;
            float bodyHeight = layerCount * rowHeight + (layerCount - 1) * rowGap;
            float totalHeight = topMargin + bodyHeight + axisHeight + legendHeight + bottomPad;

            Rect canvas = GUILayoutUtility.GetRect(0f, totalHeight, GUILayout.ExpandWidth(true));

            EditorGUI.DrawRect(canvas, graphBackground);

            Rect border = new Rect(canvas.x, canvas.y, canvas.width, canvas.height);
            DrawRectBorder(border, graphBorder);

            Rect graphArea = new Rect(
                canvas.x + leftMargin,
                canvas.y + topMargin,
                canvas.width - leftMargin - rightMargin,
                bodyHeight
            );

            int divisions = ComputeTimeDivisions(maxDuration);
            DrawGrid(graphArea, divisions);

            for (int i = 0; i < layerCount; i++)
            {
                float rowY = graphArea.y + i * (rowHeight + rowGap);
                DrawLayerLabel(canvas.x, rowY, leftMargin, rowHeight, i);

                AudioClip clip = intensityClips[i];
                if (clip == null)
                {
                    DrawEmptySlot(graphArea.x, rowY, graphArea.width, rowHeight);
                    continue;
                }

                DrawClipBar(graphArea, rowY, rowHeight, clip, maxDuration, i);
            }

            float axisY = graphArea.y + bodyHeight + 4f;
            DrawTimeAxis(graphArea.x, axisY, graphArea.width, maxDuration, divisions);

            float legendY = axisY + axisHeight - 4f;
            DrawLegend(graphArea.x, legendY, graphArea.width);
        }

        private void DrawClipBar(Rect graphArea, float rowY, float rowHeight, AudioClip clip, float maxDuration, int index)
        {
            float barWidth = (clip.length / maxDuration) * graphArea.width;
            Rect barRect = new Rect(graphArea.x, rowY, barWidth, rowHeight);

            Color barColor = layerPalette[index % layerPalette.Length];
            EditorGUI.DrawRect(barRect, barColor);

            Rect topEdge = new Rect(barRect.x, barRect.y, barRect.width, 1f);
            EditorGUI.DrawRect(topEdge, new Color(1f, 1f, 1f, 0.12f));

            Rect bottomEdge = new Rect(barRect.x, barRect.yMax - 1f, barRect.width, 1f);
            EditorGUI.DrawRect(bottomEdge, new Color(0f, 0f, 0f, 0.20f));

            if (reverbTail > 0f && reverbTail < clip.length)
            {
                float tailStartNorm = (clip.length - reverbTail) / maxDuration;
                float tailStartX = tailStartNorm * graphArea.width;
                float tailWidth = barWidth - tailStartX;

                Rect tailRect = new Rect(graphArea.x + tailStartX, rowY, tailWidth, rowHeight);
                EditorGUI.DrawRect(tailRect, tailOverlay);

                Rect markerLine = new Rect(graphArea.x + tailStartX, rowY, 2f, rowHeight);
                EditorGUI.DrawRect(markerLine, tailMarker);

                for (int d = 0; d < 3; d++)
                {
                    float dotY = rowY + 4f + d * 8f;
                    if (dotY + 2f < rowY + rowHeight - 2f)
                    {
                        Rect dot = new Rect(graphArea.x + tailStartX - 1f, dotY, 4f, 2f);
                        EditorGUI.DrawRect(dot, tailMarker);
                    }
                }
            }

            GUIStyle barLabel = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleLeft,
                fontStyle = FontStyle.Bold,
                fontSize = 10,
                normal = { textColor = Color.white },
                padding = new RectOffset(6, 0, 0, 0)
            };

            string labelText = clip.name + "   " + clip.length.ToString("F2") + "s";

            if (barRect.width > 40f)
            {
                EditorGUI.LabelField(barRect, labelText, barLabel);
            }
        }

        private static void DrawLayerLabel(float x, float y, float width, float height, int index)
        {
            GUIStyle style = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleRight,
                normal = { textColor = new Color(1f, 1f, 1f, 0.55f) },
                fontSize = 10,
                padding = new RectOffset(0, 8, 0, 0)
            };

            Rect rect = new Rect(x, y, width - 4f, height);
            EditorGUI.LabelField(rect, "Layer " + index, style);
        }

        private static void DrawEmptySlot(float x, float y, float width, float height)
        {
            Rect slotBg = new Rect(x, y, width, height);
            EditorGUI.DrawRect(slotBg, new Color(1f, 1f, 1f, 0.02f));

            GUIStyle style = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Italic,
                fontSize = 10,
                normal = { textColor = new Color(1f, 1f, 1f, 0.18f) }
            };

            EditorGUI.LabelField(slotBg, "— no clip assigned —", style);
        }

        private static void DrawGrid(Rect area, int divisions)
        {
            for (int i = 1; i < divisions; i++)
            {
                float t = (float)i / divisions;
                float x = area.x + t * area.width;
                Rect line = new Rect(x, area.y, 1f, area.height);
                EditorGUI.DrawRect(line, gridLine);
            }
        }

        private static void DrawTimeAxis(float x, float y, float width, float maxDuration, int divisions)
        {
            Rect axisLine = new Rect(x, y, width, 1f);
            EditorGUI.DrawRect(axisLine, new Color(1f, 1f, 1f, 0.08f));

            GUIStyle tickLabel = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.UpperCenter,
                fontSize = 9,
                normal = { textColor = axisColor }
            };

            for (int i = 0; i <= divisions; i++)
            {
                float t = (float)i / divisions;
                float tickX = x + t * width;

                Rect tick = new Rect(tickX, y, 1f, 5f);
                EditorGUI.DrawRect(tick, axisColor);

                float seconds = t * maxDuration;
                string text;

                if (maxDuration >= 60f)
                {
                    int minutes = Mathf.FloorToInt(seconds / 60f);
                    int secs = Mathf.FloorToInt(seconds % 60f);
                    text = minutes + ":" + secs.ToString("D2");
                }
                else
                {
                    text = seconds.ToString("F1") + "s";
                }

                Rect labelRect = new Rect(tickX - 22f, y + 6f, 44f, 14f);
                EditorGUI.LabelField(labelRect, text, tickLabel);
            }
        }

        private void DrawLegend(float x, float y, float width)
        {
            GUIStyle legendText = new GUIStyle(EditorStyles.miniLabel)
            {
                fontSize = 9,
                normal = { textColor = dimLabel }
            };

            float cursor = x;

            Rect clipSwatch = new Rect(cursor, y + 4f, 10f, 10f);
            EditorGUI.DrawRect(clipSwatch, layerPalette[0]);
            cursor += 14f;

            Rect clipLabel = new Rect(cursor, y + 1f, 80f, 16f);
            EditorGUI.LabelField(clipLabel, "Clip Duration", legendText);
            cursor += 84f;

            if (reverbTail > 0f)
            {
                Rect tailSwatch = new Rect(cursor, y + 4f, 10f, 10f);
                EditorGUI.DrawRect(tailSwatch, tailOverlay);

                Rect tailLine = new Rect(cursor + 3f, y + 4f, 2f, 10f);
                EditorGUI.DrawRect(tailLine, tailMarker);
                cursor += 14f;

                string tailText = "Reverb Tail (" + reverbTail.ToString("F2") + "s)";
                Rect tailLabel = new Rect(cursor, y + 1f, 140f, 16f);
                EditorGUI.LabelField(tailLabel, tailText, legendText);
                cursor += 144f;
            }

            string totalText = "Max Duration: " + ComputeMaxDuration().ToString("F2") + "s";
            float totalWidth = 120f;
            Rect totalLabel = new Rect(x + width - totalWidth, y + 1f, totalWidth, 16f);

            GUIStyle rightAlign = new GUIStyle(legendText)
            {
                alignment = TextAnchor.MiddleRight
            };

            EditorGUI.LabelField(totalLabel, totalText, rightAlign);
        }

        private float ComputeMaxDuration()
        {
            float max = 0f;

            for (int i = 0; i < intensityClips.Count; i++)
            {
                if (intensityClips[i] != null && intensityClips[i].length > max)
                {
                    max = intensityClips[i].length;
                }
            }

            return max;
        }

        private static int ComputeTimeDivisions(float maxDuration)
        {
            if (maxDuration <= 5f) return 5;
            if (maxDuration <= 15f) return Mathf.Max(3, Mathf.CeilToInt(maxDuration / 2f));
            if (maxDuration <= 60f) return Mathf.CeilToInt(maxDuration / 5f);
            if (maxDuration <= 180f) return Mathf.CeilToInt(maxDuration / 15f);
            if (maxDuration <= 600f) return Mathf.CeilToInt(maxDuration / 30f);
            return 10;
        }

        private static void DrawRectBorder(Rect rect, Color color)
        {
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 1f), color);
            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), color);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, 1f, rect.height), color);
            EditorGUI.DrawRect(new Rect(rect.xMax - 1f, rect.y, 1f, rect.height), color);
        }
#endif
    }
}
