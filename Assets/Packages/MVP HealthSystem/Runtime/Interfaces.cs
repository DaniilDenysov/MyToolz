using System;

namespace MyToolz.HealthSystem.Interfaces
{
    public interface IKillable
    {
        public void Kill();
    }

    public interface IDamagable
    {
        public void DoDamage(DamageType damageType);
        public void DoDamage(float damage);
    }

    public interface IHealable
    {
        public void DoHeal(float amount);
    }

    public interface IHealthView
    {
        public void Initialize((float currentHealth, float min, float max) model);
        public void UpdateView((float currentHealth, float min, float max) model, float oldHealth);
        public void Show();
        public void Hide();
    }

    public interface IHealthModel : IDamagable, IHealable
    {
        public (float currentHealth, float min, float max) CurrentHealth { get; }
        public event Action<(float currentHealth, float min, float max), float> HealthChanged;
        public event Action<(float oldHealth, float newHealth)> HealthChangedDiff;
        public event Action Died;
        public void Initialize();
        public void Update();
        public void RefreshModel();
    }
}