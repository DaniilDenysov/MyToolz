using MyToolz.HealthSystem.Interfaces;
using System;
using UnityEngine;

namespace MyToolz.HealthSystem
{
    [Serializable]
    public abstract class DamageType
    {
        [SerializeField, Min(0f)] protected float damage = 25f;
        public float Damage => damage;
        public void SetDamage(float damage)
        {
            if (damage < 0) return;
            this.damage = damage;
        }
        public DamageType() { }
        public DamageType(float damage) { this.damage = damage; }
        public abstract DamageType Clone();
        public abstract bool DoDamage(IDamagable damagable);
    }

    [Serializable]
    public class PhysicalDamageType : DamageType
    {
        public PhysicalDamageType() : base() { }
        public PhysicalDamageType(float damage) : base(damage) { }

        public override DamageType Clone()
        {
            var n = new PhysicalDamageType(damage);
            return n;
        }

        public override bool DoDamage(IDamagable damagable)
        {
            damagable.DoDamage(damage);
            return false;
        }
    }

    public abstract class TickBasedDamageType : DamageType
    {
        [SerializeField, Min(0.1f)] protected float duration = 5f;
        [SerializeField, Min(0.05f)] protected float tickInterval = 1f;
        protected float tickTimer;
        protected float elapsed;

        public TickBasedDamageType() : base() { }
        public TickBasedDamageType(float damage) : base(damage) { }

        public override bool DoDamage(IDamagable damagable)
        {
            elapsed += Time.deltaTime;
            tickTimer += Time.deltaTime;
            if (elapsed >= duration) return false;
            if (tickTimer >= tickInterval)
            {
                tickTimer = 0f;
                damagable.DoDamage(damage * Time.deltaTime / tickInterval * tickInterval);
            }
            return true;
        }
    }

    [Serializable]
    public class PoisonDamageType : TickBasedDamageType
    {
        public PoisonDamageType() : base() { }
        public PoisonDamageType(float damage) : base(damage) { }

        public override DamageType Clone()
        {
            var c = new PoisonDamageType(damage);
            c.duration = duration;
            c.tickInterval = tickInterval;
            return c;
        }
    }

    [Serializable]
    public class FireDamageType : TickBasedDamageType
    {
        public FireDamageType() : base() { }
        public FireDamageType(float damage) : base(damage) { }

        public override DamageType Clone()
        {
            var c = new FireDamageType(damage);
            c.duration = duration;
            c.tickInterval = tickInterval;
            return c;
        }

        public override bool DoDamage(IDamagable damagable)
        {
            elapsed += Time.deltaTime;
            tickTimer += Time.deltaTime;
            if (elapsed >= duration) return false;
            if (tickTimer >= tickInterval)
            {
                tickTimer = 0f;
                damagable.DoDamage(damage);
            }
            return true;
        }
    }
}