using MyToolz.HealthSystem.Interfaces;
using MyToolz.Utilities.Debug;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace MyToolz.HealthSystem.Model
{
    [System.Serializable]
    public class HealthSystemModel : IHealthModel, IKillable
    {
        public (float currentHealth, float min, float max) CurrentHealth
        {
            get => new(currentHealth, minHealth, maxHealth);
        }
        public event Action<(float currentHealth, float min, float max), float> HealthChanged;
        public event Action<(float oldHealth, float newHealth)> HealthChangedDiff;
        public event Action Died;
        [SerializeField, Range(0, 100000f)] protected float currentHealth;
        [SerializeField, Range(0, 100000f)] protected float maxHealth;
        [SerializeField, Range(0, 100000f)] protected float minHealth = 0f;
        [SerializeField, Range(1, 4)] protected int maxStack = 2;
        [SerializeField] protected bool ignoreIfExceeded = true;
        [SerializeField] protected bool isInvincible;

        protected readonly Dictionary<Type, DamageType> active = new Dictionary<Type, DamageType>();
        protected bool IsDead => currentHealth <= minHealth;

        public bool IsInvincible
        {
            get => isInvincible;
            set => isInvincible = value;
        }

        protected void UpdateHealth(float old)
        {
            HealthChanged?.Invoke((currentHealth, minHealth, maxHealth), old);
        }

        public virtual void DoDamage(float damage)
        {
            if (IsDead || IsInvincible) return;
            float old = currentHealth;
            currentHealth = Mathf.Max(currentHealth - damage, minHealth);
            HealthChangedDiff?.Invoke(new(old, currentHealth));
            UpdateHealth(old);
            if (IsDead) Died?.Invoke();
        }

        public virtual void Update()
        {
            if (active.Count == 0) return;

            var keys = new List<Type>(active.Keys);
            for (int i = 0; i < keys.Count; i++)
            {
                var k = keys[i];
                if (!active.TryGetValue(k, out var e)) continue;

                var keep = e.DoDamage(this);
                if (!keep)
                {
                    active.Remove(k);
                    DebugUtility.LogError(this, "DamageType removed: " + k.Name);
                }
            }
        }

        public virtual void RefreshModel()
        {
            UpdateHealth(currentHealth);
        }

        public void DoDamage(DamageType damageType)
        {
            if (damageType == null) return;
            var key = damageType.GetType();
            if (active.TryGetValue(key, out var entry))
            {
                active[key] = damageType.Clone();
                DebugUtility.LogError(this, "DamageType updated: " + key.Name);
                return;
            }

            if (active.Count >= maxStack)
            {
                if (ignoreIfExceeded)
                {
                    DebugUtility.LogError(this, "Stack exceeded, ignoring: " + key.Name);
                    return;
                }
                var idx = UnityEngine.Random.Range(0, active.Count);
                var i = 0;
                Type toRemove = null;
                foreach (var k in active.Keys)
                {
                    if (i == idx) { toRemove = k; break; }
                    i++;
                }
                if (toRemove != null)
                {
                    active.Remove(toRemove);
                    DebugUtility.LogError(this, "Stack exceeded, replaced: " + toRemove.Name + " with " + key.Name);
                }
            }

            var clone = damageType.Clone();
            active[key] = clone;
            DebugUtility.LogError(this, "DamageType added: " + key.Name);
        }

        public void Kill()
        {
            if (IsDead) return;
            var old = currentHealth;
            currentHealth = minHealth;
            UpdateHealth(old);
            if (IsDead) Died?.Invoke();
        }

        public void Initialize()
        {

        }


        public void DoHeal(float amount)
        {
            if (IsDead) return;
            if (amount < 0) return;
            var old = currentHealth;
            currentHealth = Mathf.Clamp(currentHealth + amount, minHealth, maxHealth);
            UpdateHealth(old);
        }
    }
}

