using DG.Tweening;
using MyToolz.DesignPatterns.EventBus;
using MyToolz.EditorToolz;
using MyToolz.Events;
using MyToolz.HealthSystem;
using MyToolz.HealthSystem.Interfaces;
using UnityEngine;

namespace MyToolz.Projectiles
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class DoTweenProjectile : Projectile
    {
        [FoldoutGroup("Config"), SerializeField, Min(0.05f)] private float disposeTime = 0.2f;
        [FoldoutGroup("Config"), SerializeField, Min(0.1f)] private float effectiveDistance = 100f;
        [FoldoutGroup("Config"), SerializeField] private Ease ease = Ease.InOutSine;
        [FoldoutGroup("Refs"), SerializeField, Required, ReadOnly] private Rigidbody2D rb;

        private Tween moveTween;
        private DamageType damageType;

        private void Awake()
        {
            OnValidate();
        }

        private void OnValidate()
        {
            if (rb == null)
            {
                rb = GetComponent<Rigidbody2D>();
                if (rb == null) rb = gameObject.AddComponent<Rigidbody2D>();
                rb.gravityScale = 0;
                rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                rb.interpolation = RigidbodyInterpolation2D.Interpolate;
                rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            }
        }

        public override void Initialize(ProjectileInit init)
        {
            if (moveTween != null && moveTween.IsActive()) moveTween.Kill();
            rb.position = init.Origin;
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
            transform.SetParent(null);

            damageType = init.DamageType;

            transform.position = init.Origin;

            var dir = init.Direction.normalized;
            var origin = (Vector2)init.Origin;
            var maxPoint = origin + (dir * effectiveDistance);

            var distance = Vector2.Distance(origin, maxPoint);
            var duration = distance / Mathf.Max(0.0001f, speed);

            moveTween = rb
                .DOMove(maxPoint, duration)
                .SetUpdate(UpdateType.Fixed)
                .SetEase(ease)
                .OnComplete(Dispose);
        }

        public override void OnDespawned()
        {
            base.OnDespawned();
            OnDispose();
        }

        public override void OnSpawned()
        {
            base.OnSpawned();
            OnDispose();
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (collision.gameObject.TryGetComponent(out IDamagable damagable))
            {
                if ((hitMask.value & (1 << collision.gameObject.layer)) == 0) return;
                damagable.DoDamage(damageType);
            }
            var contact = collision.GetContact(0);
            Dispose();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var layerMaskMatch = (hitMask.value & (1 << other.gameObject.layer)) != 0;
            if (!layerMaskMatch) return;
            if (other.gameObject.TryGetComponent(out IDamagable damagable)) damagable.DoDamage(damageType);
            Dispose();
        }


        private void Dispose()
        {
            EventBus<ReleaseRequest<Projectile>>.Raise(new ReleaseRequest<Projectile>()
            {
                PoolObject = this,
                Callback = (o) => OnDispose()
            });
        }

        private void OnDispose()
        {
            CancelInvoke(nameof(Dispose));
            if (moveTween != null && moveTween.IsActive()) moveTween.Kill();
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
    }
}
