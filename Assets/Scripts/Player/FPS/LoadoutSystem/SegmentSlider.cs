using MyToolz.Core;
using System.Collections.Generic;
using UnityEngine;

namespace MyToolz.Player.FPS.LoadoutSystem.View
{
    public class SegmentSlider : MonoBehaviourPlus
    {
        [Header("Value Settings")]
        [Tooltip("Minimum possible value (e.g. damage = 10)")]
        public float minValue = 0;

        [Tooltip("Maximum possible value (e.g. damage = 100)")]
        public float maxValue = 100;

        [Tooltip("Total number of segments shown in UI.")]
        [SerializeField] private int totalSegments = 10;

        [Tooltip("Current value (e.g. 65)")]
        [SerializeField] private float currentValue = 0;

        public float value
        {
            get => currentValue;
            set => SetValue(value);
        }

        [Header("Prefabs")]
        [SerializeField] private GameObject filledPrefab;
        [SerializeField] private GameObject emptyPrefab;
        [SerializeField] private GameObject morePrefab;
        [SerializeField] private GameObject lessPrefab;

        [Header("UI Settings")]
        [SerializeField] private RectTransform container;

        private List<GameObject> segments = new List<GameObject>();

        private float previousValue = -1;
        private bool showDifference = false;

        private void Start()
        {
            BuildSegments();
            UpdateSliderVisual();
        }

        /// <summary>
        /// Build empty placeholders initially.
        /// </summary>
        public void BuildSegments()
        {
            ClearSegments();

            for (int i = 0; i < totalSegments; i++)
            {
                var segmentObj = Instantiate(emptyPrefab, container);
                segments.Add(segmentObj);
            }
        }

        private void ClearSegments()
        {
            foreach (var obj in segments)
            {
                if (obj != null)
                    Destroy(obj);
            }
            segments.Clear();
        }

        private void UpdateSliderVisual()
        {
            float cellSize = (maxValue - minValue) / totalSegments;

            int prevSegments = ValueToSegments(previousValue, cellSize);
            int currSegments = ValueToSegments(currentValue, cellSize);

            // Clear current visuals
            foreach (Transform child in container)
            {
                Destroy(child.gameObject);
            }

            for (int i = 0; i < totalSegments; i++)
            {
                GameObject prefabToSpawn;

                if (showDifference)
                {
                    if (i < prevSegments && i >= currSegments)
                    {
                        prefabToSpawn = lessPrefab;
                    }
                    else if (i >= prevSegments && i < currSegments)
                    {
                        prefabToSpawn = morePrefab;
                    }
                    else if (i < currSegments)
                    {
                        prefabToSpawn = filledPrefab;
                    }
                    else
                    {
                        prefabToSpawn = emptyPrefab;
                    }
                }
                else
                {
                    prefabToSpawn = (i < currSegments) ? filledPrefab : emptyPrefab;
                }

                if (prefabToSpawn == null)
                {
                    LogError($"SegmentSlider: Missing prefab for segment at index {i}!");
                    continue;
                }

                var segmentObj = Instantiate(prefabToSpawn, container);
                segments.Add(segmentObj);
            }
        }

        /// <summary>
        /// Maps a numeric value to a number of segments to fill.
        /// </summary>
        private int ValueToSegments(float value, float cellSize)
        {
            if (value < minValue)
                return 0;

            if (value > maxValue)
                return totalSegments;

            float relative = value - minValue;
            int filled = Mathf.FloorToInt(relative / cellSize);

            filled = Mathf.Clamp(filled, 0, totalSegments);
            return filled;
        }

        private void SetValue(float newValue)
        {
            previousValue = currentValue;
            currentValue = Mathf.Clamp(newValue, minValue, maxValue);

            showDifference = false;
            UpdateSliderVisual();
        }

        /// <summary>
        /// Starts a comparison view against the current value.
        /// </summary>
        public void StartComparing(float newValue)
        {
            previousValue = currentValue;
            currentValue = Mathf.Clamp(newValue, minValue, maxValue);

            showDifference = true;
            UpdateSliderVisual();
        }

        /// <summary>
        /// Stops the comparison and resets to normal view.
        /// </summary>
        public void StopComparing()
        {
            showDifference = false;
            UpdateSliderVisual();
        }
    }
}
