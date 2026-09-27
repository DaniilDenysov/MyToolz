using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace MyToolz.UI.Kit
{
    public class UISparkleBurst : MonoBehaviour
    {
        [Tooltip("Sprite used for each spark. The four-point sparkle from the tile icon sheet suits it.")]
        [SerializeField] private Sprite sparkle;

        [Tooltip("How many sparks exist in total. Bursts recycle the oldest past this.")]
        [SerializeField, Min(1)] private int capacity = 30;

        [Tooltip("Sparks thrown by a single burst.")]
        [SerializeField, Min(1)] private int perBurst = 10;

        [SerializeField] private Color tint = new Color(1f, 0.86f, 0.35f, 1f);

        [SerializeField, Min(1f)] private float size = 48f;

        [SerializeField, Min(0f)] private float minDistance = 80f;
        [SerializeField, Min(0f)] private float maxDistance = 210f;

        [SerializeField, Min(0.05f)] private float duration = 0.75f;

        [Tooltip("Degrees a spark turns through on its way out.")]
        [SerializeField] private float spin = 220f;

        private readonly List<Image> sparks = new List<Image>();
        private readonly List<Tween> tweens = new List<Tween>();
        private int next;

        public void Burst(Vector2 origin) => Burst(origin, tint);

        public void Burst(Vector2 origin, Color color)
        {
            Build();

            if (sparks.Count == 0)
            {
                return;
            }

            float step = 360f / perBurst;
            float offset = Random.Range(0f, 360f);

            for (int i = 0; i < perBurst; i++)
            {
                int index = next;
                next = (next + 1) % sparks.Count;

                Image spark = sparks[index];

                if (spark == null)
                {
                    continue;
                }

                tweens[index]?.Kill();

                float angle = (offset + (step * i) + Random.Range(-step * 0.3f, step * 0.3f)) * Mathf.Deg2Rad;
                float distance = Random.Range(minDistance, maxDistance);
                Vector2 target = origin + (new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance);

                RectTransform rect = spark.rectTransform;
                rect.anchoredPosition = origin;
                rect.localScale = Vector3.zero;
                rect.localRotation = Quaternion.identity;

                spark.color = color;
                spark.gameObject.SetActive(true);

                float life = duration * Random.Range(0.75f, 1.15f);
                float peak = Random.Range(0.7f, 1.2f);

                Sequence sequence = DOTween.Sequence().SetUpdate(true);
                sequence.Append(rect.DOScale(Vector3.one * peak, life * 0.25f).SetEase(Ease.OutBack));
                sequence.Join(rect.DOAnchorPos(target, life).SetEase(Ease.OutCubic));
                sequence.Join(rect.DOLocalRotate(new Vector3(0f, 0f, Random.Range(-spin, spin)), life, RotateMode.LocalAxisAdd));
                sequence.Insert(life * 0.35f, rect.DOScale(Vector3.zero, life * 0.65f).SetEase(Ease.InQuad));
                sequence.Insert(life * 0.35f, spark.DOFade(0f, life * 0.65f));

                Image captured = spark;
                sequence.OnComplete(() => captured.gameObject.SetActive(false));

                tweens[index] = sequence;
            }
        }

        public void Clear()
        {
            for (int i = 0; i < tweens.Count; i++)
            {
                tweens[i]?.Kill();
                tweens[i] = null;

                if (sparks[i] != null)
                {
                    sparks[i].gameObject.SetActive(false);
                }
            }
        }

        private void OnDisable() => Clear();

        private void Build()
        {
            if (sparks.Count > 0)
            {
                return;
            }

            for (int i = 0; i < capacity; i++)
            {
                GameObject go = new GameObject("Spark" + i, typeof(RectTransform));
                go.transform.SetParent(transform, false);

                RectTransform rect = (RectTransform)go.transform;
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(size, size);

                Image image = go.AddComponent<Image>();
                image.sprite = sparkle;
                image.color = tint;
                image.raycastTarget = false;
                image.preserveAspect = true;

                go.SetActive(false);

                sparks.Add(image);
                tweens.Add(null);
            }
        }
    }
}
