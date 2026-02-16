using DG.Tweening;
using MyToolz.DesignPatterns.EventBus;
using MyToolz.Events;
using MyToolz.HealthSystem;
using MyToolz.HealthSystem.Interfaces;
using MyToolz.HealthSystem.Model;
using MyToolz.Projectiles;
using Sirenix.OdinInspector;
using UnityEngine;

namespace ProjectClyde.Projectiles
{
    public class Slash : Projectile
    {
        [BoxGroup("Slash"), MinValue(0.01f)][SerializeField] private float damage = 10f;
        [BoxGroup("Slash"), MinValue(0.01f)][SerializeField] private float height = 1f;
        [BoxGroup("Slash"), MinValue(0.01f)][SerializeField] private float maxDistance = 3f;
        [BoxGroup("Slash"), SerializeField] private Ease ease = Ease.OutQuad;
        [BoxGroup("Layers"), SerializeField] private LayerMask targetMask;

        private Vector2 lastCenter;
        private Vector2 lastSize;
        private float lastAngle;

        private Rigidbody2D rb;
        private Tween moveTween;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
        }

        public override void Initialize(ProjectileInit init)
        {
            Fire(init.Origin, init.Direction, init.DamageType);
        }


        public void Fire(Vector2 origin, Vector2 direction, DamageType damageType)
        {
            direction = direction.sqrMagnitude > 0f ? direction.normalized : Vector2.right;
            var distance = maxDistance;

            var hit = Physics2D.Raycast(origin, direction, maxDistance, hitMask);
            if (hit.collider != null) distance = hit.distance;

            var center = origin + direction * (distance * 0.5f);
            var size = new Vector2(Mathf.Max(distance, 0.01f), height);
            var angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            var destination = origin + direction * distance;

            var colliders = Physics2D.OverlapBoxAll(center, size, angle, targetMask);

            for (int i = 0; i < colliders.Length; i++)
            {
                var c = colliders[i];
                if (c == null) continue;
                if (c.TryGetComponent(out IDamagable damagable))
                {
                    damagable.DoDamage(damageType);
                }
                Log("Slash hit " + c.ToString());
            }

            Log("Slash cast from " + origin + " dir " + direction + " distance " + distance);

            lastCenter = center;
            lastSize = size;
            lastAngle = angle;

            if (moveTween != null && moveTween.IsActive()) moveTween.Kill();

            transform.rotation = Quaternion.Euler(0f, 0f, angle);

            var duration = Mathf.Approximately(speed, 0f) ? 0f : distance / speed;

            if (rb != null)
            {
                moveTween = rb.DOMove(destination, duration).SetEase(ease).SetUpdate(UpdateType.Fixed).OnComplete(() => Dispose());
            }
            else
            {
                moveTween = transform.DOMove(destination, duration).SetUpdate(UpdateType.Normal).SetEase(ease).OnComplete(() => Dispose());
            }
        }

        private void Dispose()
        {
            EventBus<ReleaseRequest<Projectile>>.Raise(new ReleaseRequest<Projectile>()
            {
                PoolObject = this
            });
        }

        private void OnDestroy()
        {
            if (moveTween != null && moveTween.IsActive()) moveTween.Kill();
        }

        private void OnDisable()
        {
            if (moveTween != null && moveTween.IsActive()) moveTween.Kill();
        }
#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.matrix = Matrix4x4.TRS(lastCenter, Quaternion.Euler(0f, 0f, lastAngle), Vector3.one);
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(lastSize.x, lastSize.y, 0.01f));
        }
#endif
    }
}
