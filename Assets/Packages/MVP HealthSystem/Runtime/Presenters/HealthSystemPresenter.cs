using MyToolz.HealthSystem.Interfaces;
using MyToolz.Utilities.Debug;
using UnityEngine;
using Zenject;

namespace MyToolz.HealthSystem.Presenters
{
    public class HealthSystemPresenter : MonoBehaviour, IDamagable, IEventListener, IKillable, IHealable
    {
        protected IHealthView view;
        protected IHealthModel model;

        [Inject]
        protected virtual void Construct(IHealthModel model, IHealthView view, DiContainer container)
        {
            this.model = model;
            container.Inject(model);
            model.Initialize();
            this.view = view;
            container.Inject(view);
        }

        protected virtual void FixedUpdate()
        {
            model?.Update();
        }

        protected virtual void OnUnitDamaged((float currentHealth, float min, float max) model, float oldHealth)
        {
            view.Show();
            DebugUtility.LogError(this, $"Damaged [{model.currentHealth}]");
        }

        public virtual void OnUnitDied()
        {
            view.Hide();
            DebugUtility.LogError(this, $"Died!");
        }

        public virtual void DoDamage(float damage)
        {
            model.DoDamage(damage);
        }

        public void DoDamage(DamageType damageType)
        {
            model.DoDamage(damageType);
        }

        public virtual void RegisterEvents()
        {
            if (model != null)
            {
                model.HealthChanged += OnUnitDamaged;
                model.Died += OnUnitDied;
                ((IEventListener)model)?.RegisterEvents();
            }
            if (view != null)
            {
                model.HealthChanged += view.UpdateView;
            }
        }

        public virtual void UnregisterEvents()
        {
            if (model != null)
            {
                model.HealthChanged -= OnUnitDamaged;
                model.Died -= OnUnitDied;
                ((IEventListener)model)?.UnregisterEvents();
            }
            if (view != null)
            {
                model.HealthChanged -= view.UpdateView;
            }
        }

        private void Start()
        {     
            model.RefreshModel();
        }

        private void OnDestroy()
        {
            UnregisterEvents();
        }

        public void Kill()
        {
            var killable = (IKillable)model;
            if (killable == null) return;
            killable.Kill();
        }

        public void DoHeal(float amount)
        {
            var heallable = (IHealable)model;
            if (heallable == null) return;
            heallable.DoHeal(amount);
        }
    }
}
