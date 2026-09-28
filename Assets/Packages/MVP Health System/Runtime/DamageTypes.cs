using MyToolz.HealthSystem.Interfaces;
using System;
using UnityEngine;

namespace MyToolz.HealthSystem
{
    public struct DefaultDamageArgs : IDamageArgs
    {
        public float Damage { get; set; }
        public DamageType DamageType;
    }

    public interface IDamageArgs
    {
        public float Damage { get; set; }
    }

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

        /// <summary>Applies this frame's damage using <see cref="Time.deltaTime"/>. Returns true while the effect lasts.</summary>
        public abstract bool DoDamage(IDamagable<IDamageArgs> damagable);

        /// <summary>
        /// Advances the effect by <paramref name="deltaTime"/> seconds and applies any damage due.
        /// Returns true while the effect lasts. Instant damage types ignore the time step.
        /// </summary>
        public virtual bool Tick(IDamagable<IDamageArgs> damagable, float deltaTime) => DoDamage(damagable);
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

        public override bool DoDamage(IDamagable<IDamageArgs> damagable)
        {
            damagable?.DoDamage(new DefaultDamageArgs() { Damage = damage, DamageType = this });
            return false;
        }
    }

    /// <summary>
    /// Damage over time: <see cref="DamageType.Damage"/> is dealt once per <see cref="TickInterval"/>,
    /// at t = interval, 2·interval, … up to <see cref="Duration"/>. Ticks are counted from accumulated
    /// time, so the total dealt is Damage × floor(Duration / TickInterval) at any frame rate, and a
    /// long frame delivers every tick it covered.
    /// </summary>
    public abstract class TickBasedDamageType : DamageType
    {
        // Tolerance for float accumulation (e.g. ten 0.1 s frames summing to 0.99999994 s).
        private const float TickEpsilon = 1e-4f;

        [SerializeField, Min(0.1f)] protected float duration = 5f;
        [SerializeField, Min(0.05f)] protected float tickInterval = 1f;
        protected float elapsed;
        protected int ticksApplied;

        public float Duration => duration;
        public float TickInterval => tickInterval;
        public int TotalTicks => tickInterval > 0f ? Mathf.FloorToInt(duration / tickInterval + TickEpsilon) : 0;
        public bool IsFinished => ticksApplied >= TotalTicks;

        public TickBasedDamageType() : base() { }
        public TickBasedDamageType(float damage) : base(damage) { }
        public TickBasedDamageType(float damage, float duration, float tickInterval) : base(damage)
        {
            this.duration = Mathf.Max(0f, duration);
            this.tickInterval = Mathf.Max(0.0001f, tickInterval);
        }

        public override bool DoDamage(IDamagable<IDamageArgs> damagable) => Tick(damagable, Time.deltaTime);

        public override bool Tick(IDamagable<IDamageArgs> damagable, float deltaTime)
        {
            if (float.IsNaN(deltaTime) || float.IsInfinity(deltaTime) || deltaTime < 0f)
            {
                deltaTime = 0f;
            }

            int totalTicks = TotalTicks;
            elapsed = Mathf.Min(elapsed + deltaTime, duration);
            int due = Mathf.Min(totalTicks, Mathf.FloorToInt(elapsed / tickInterval + TickEpsilon));

            while (ticksApplied < due)
            {
                ticksApplied++;
                damagable?.DoDamage(new DefaultDamageArgs() { Damage = damage, DamageType = this });
            }

            return ticksApplied < totalTicks;
        }

        protected T CopySettingsTo<T>(T copy) where T : TickBasedDamageType
        {
            copy.duration = duration;
            copy.tickInterval = tickInterval;
            return copy;
        }
    }

    [Serializable]
    public class PoisonDamageType : TickBasedDamageType
    {
        public PoisonDamageType() : base() { }
        public PoisonDamageType(float damage) : base(damage) { }

        public PoisonDamageType(float damage, float duration, float tickInterval) : base(damage, duration, tickInterval) { }

        public override DamageType Clone() => CopySettingsTo(new PoisonDamageType(damage));
    }

    [Serializable]
    public class FireDamageType : TickBasedDamageType
    {
        public FireDamageType() : base() { }
        public FireDamageType(float damage) : base(damage) { }

        public FireDamageType(float damage, float duration, float tickInterval) : base(damage, duration, tickInterval) { }

        public override DamageType Clone() => CopySettingsTo(new FireDamageType(damage));
    }
}
