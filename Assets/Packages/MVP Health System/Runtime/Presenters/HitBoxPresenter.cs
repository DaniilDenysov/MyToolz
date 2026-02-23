using MyToolz.HealthSystem.Interfaces;
using UnityEngine;
using Zenject;

namespace MyToolz.HealthSystem
{
    public class HitBoxPresenter : MonoBehaviour, IDamagable
    {
        [SerializeField, Range(0, 100)] protected float multiplier = 2f;

        protected IHealthModel healthModel;

        [Inject]
        public void Construct(IHealthModel healthModel)
        {
            this.healthModel = healthModel;
        }

        public void DoDamage(DamageType damageType)
        {
            damageType.SetDamage(damageType.Damage * multiplier);
            healthModel.DoDamage(damageType);
        }

        public void DoDamage(float damage)
        {
            damage *= multiplier;
            healthModel.DoDamage(damage);
        }
    }
}
