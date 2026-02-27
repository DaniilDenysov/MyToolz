using MyToolz.DesignPatterns.ObjectPool;
using MyToolz.EditorToolz;
using MyToolz.HealthSystem;
using UnityEngine;

namespace MyToolz.Projectiles
{
    public struct ProjectileInit
    {
        public Vector3 Origin;
        public Vector2 Direction;
        public DamageType DamageType;
    }

    public abstract class Projectile : MonoBehaviour, IPoolable
    {
        [FoldoutGroup("Config"), SerializeField, Min(0.1f)] protected float speed = 10f;
        [FoldoutGroup("Config"), SerializeField] protected LayerMask hitMask;

        public virtual void Initialize(ProjectileInit init)
        {

        }

        public virtual void OnDespawned()
        {

        }

        public virtual void OnSpawned()
        {

        }
    }
}
