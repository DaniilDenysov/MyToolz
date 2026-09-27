using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace MyToolz.UI.Kit
{
    public class WorldSparkleBurst : MonoBehaviour
    {
        [Tooltip("Sprite used for each shard.")]
        [SerializeField] private Sprite shard;

        [Tooltip("Total shards in the pool. Bursts recycle the oldest past this.")]
        [SerializeField, Min(1)] private int capacity = 64;

        [Tooltip("Shards thrown by one burst.")]
        [SerializeField, Min(1)] private int perBurst = 9;

        [Tooltip("Sorting order for the shards. Keep it above the tiles.")]
        [SerializeField] private int sortingOrder = 20;

        [SerializeField, Min(0f)] private float minDistance = 0.7f;
        [SerializeField, Min(0f)] private float maxDistance = 2.1f;

        [SerializeField, Min(0.05f)] private float duration = 0.55f;

        [Tooltip("World size of a shard at its peak.")]
        [SerializeField, Min(0.01f)] private float size = 0.55f;

        [Tooltip("How far the shards drift down as they fade, so the burst settles instead of hanging.")]
        [SerializeField] private float gravity = -0.9f;

        [SerializeField] private float spin = 220f;

        private readonly List<SpriteRenderer> shards = new List<SpriteRenderer>();
        private readonly List<Sequence> tweens = new List<Sequence>();
        private int next;

        public void Burst(Vector3 origin, Color tint)
        {
            Build();

            if (shards.Count == 0)
            {
                return;
            }

            float step = 360f / perBurst;
            float offset = Random.Range(0f, 360f);

            for (int i = 0; i < perBurst; i++)
            {
                int index = next;
                next = (next + 1) % shards.Count;

                SpriteRenderer shardRenderer = shards[index];

                if (shardRenderer == null)
                {
                    continue;
                }

                tweens[index]?.Kill();

                float angle = (offset + (step * i) + Random.Range(-step * 0.35f, step * 0.35f)) * Mathf.Deg2Rad;
                float distance = Random.Range(minDistance, maxDistance);

                Vector3 target = origin + new Vector3(
                    Mathf.Cos(angle) * distance,
                    (Mathf.Sin(angle) * distance) + gravity,
                    0f);

                Transform t = shardRenderer.transform;
                t.position = origin;
                t.localScale = Vector3.zero;
                t.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));

                Color start = tint;
                start.a = 1f;
                shardRenderer.color = start;
                shardRenderer.gameObject.SetActive(true);

                float life = duration * Random.Range(0.8f, 1.2f);
                float peak = size * Random.Range(0.6f, 1.25f);

                Sequence sequence = DOTween.Sequence();
                sequence.Append(t.DOScale(Vector3.one * peak, life * 0.22f).SetEase(Ease.OutBack));
                sequence.Join(t.DOMove(target, life).SetEase(Ease.OutCubic));
                sequence.Join(t.DORotate(new Vector3(0f, 0f, Random.Range(-spin, spin)), life, RotateMode.LocalAxisAdd));
                sequence.Insert(life * 0.3f, t.DOScale(Vector3.zero, life * 0.7f).SetEase(Ease.InQuad));
                sequence.Insert(life * 0.3f, shardRenderer.DOFade(0f, life * 0.7f));

                SpriteRenderer captured = shardRenderer;
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

                if (shards[i] != null)
                {
                    shards[i].gameObject.SetActive(false);
                }
            }
        }

        private void OnDisable() => Clear();

        private void Build()
        {
            if (shards.Count > 0)
            {
                return;
            }

            for (int i = 0; i < capacity; i++)
            {
                GameObject go = new GameObject("Shard" + i);
                go.transform.SetParent(transform, false);

                SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
                renderer.sprite = shard;
                renderer.sortingOrder = sortingOrder;

                go.SetActive(false);

                shards.Add(renderer);
                tweens.Add(null);
            }
        }
    }
}
