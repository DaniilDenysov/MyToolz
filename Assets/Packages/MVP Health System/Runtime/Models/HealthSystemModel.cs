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
        private readonly List<Type> activeKeysBuffer = new List<Type>();
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

        /// <summary>True for a usable amount: finite and not negative.</summary>
        protected static bool IsValidAmount(float amount) => !float.IsNaN(amount) && !float.IsInfinity(amount) && amount >= 0f;

        public virtual void DoDamage(IDamageArgs damageArgs)
        {
            if (damageArgs == null || IsDead || IsInvincible) return;

            float amount = damageArgs.Damage;
            if (!IsValidAmount(amount))
            {
                // A negative amount would heal and NaN would corrupt the health value.
                DebugUtility.LogWarning(this, $"Ignored invalid damage amount: {amount}");
                return;
            }
            if (amount == 0f) return;

            float old = currentHealth;
            currentHealth = Mathf.Max(currentHealth - amount, minHealth);
            HealthChangedDiff?.Invoke(new(old, currentHealth));
            UpdateHealth(old);
            if (IsDead) Died?.Invoke();
        }

        public virtual void Update() => Update(Time.deltaTime);

        /// <summary>Advances active damage-over-time effects by <paramref name="deltaTime"/> seconds.</summary>
        public virtual void Update(float deltaTime)
        {
            if (active.Count == 0) return;

            activeKeysBuffer.Clear();
            activeKeysBuffer.AddRange(active.Keys);
            for (int i = 0; i < activeKeysBuffer.Count; i++)
            {
                var k = activeKeysBuffer[i];
                if (!active.TryGetValue(k, out var e)) continue;

                var keep = e.Tick(this, deltaTime);
                if (!keep)
                {
                    active.Remove(k);
                    DebugUtility.Log(this, "DamageType removed: " + k.Name);
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
                DebugUtility.Log(this, "DamageType updated: " + key.Name);
                return;
            }

            if (active.Count >= maxStack)
            {
                if (ignoreIfExceeded)
                {
                    DebugUtility.Log(this, "Stack exceeded, ignoring: " + key.Name);
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
                    DebugUtility.Log(this, "Stack exceeded, replaced: " + toRemove.Name + " with " + key.Name);
                }
            }

            var clone = damageType.Clone();
            active[key] = clone;
            DebugUtility.Log(this, "DamageType added: " + key.Name);
        }

        public void Kill()
        {
            if (IsDead) return;
            var old = currentHealth;
            currentHealth = minHealth;
            HealthChangedDiff?.Invoke(new(old, currentHealth));
            UpdateHealth(old);
            if (IsDead) Died?.Invoke();
        }

        /// <summary>Normalizes serialized limits: max is never below min and current health lies between them.</summary>
        public void Initialize()
        {
            if (maxHealth < minHealth)
            {
                DebugUtility.LogWarning(this, $"Max health ({maxHealth}) is below min health ({minHealth}); using min as max.");
                maxHealth = minHealth;
            }

            if (float.IsNaN(currentHealth))
            {
                currentHealth = maxHealth;
            }

            currentHealth = Mathf.Clamp(currentHealth, minHealth, maxHealth);
        }


        public void DoHeal(float amount)
        {
            if (IsDead) return;
            if (!IsValidAmount(amount)) return;
            var old = currentHealth;
            currentHealth = Mathf.Clamp(currentHealth + amount, minHealth, maxHealth);
            UpdateHealth(old);
        }
    }
}

