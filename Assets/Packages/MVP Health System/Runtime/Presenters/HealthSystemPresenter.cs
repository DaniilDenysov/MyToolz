using MyToolz.Events;
using MyToolz.HealthSystem.Interfaces;
using MyToolz.Utilities.Debug;
using UnityEngine;
using Zenject;

namespace MyToolz.HealthSystem.Presenters
{
    public class HealthSystemPresenter : MonoBehaviour, IDamagable<IDamageArgs>, IEventListener, IKillable, IHealable
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
            DebugUtility.Log(this, $"Damaged [{model.currentHealth}]");
        }

        public virtual void OnUnitDied()
        {
            view.Hide();
            DebugUtility.Log(this, $"Died!");
        }


        public void DoDamage(IDamageArgs damageArgs)
        {
            model.DoDamage(damageArgs);
        }

        public virtual void RegisterEvents()
        {
            UnregisterEvents();
            if (model != null)
            {
                model.HealthChanged += OnUnitDamaged;
                model.Died += OnUnitDied;
                if (model is IEventListener) ((IEventListener)model)?.RegisterEvents();
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
                if (model is IEventListener) ((IEventListener)model)?.UnregisterEvents();
            }
            if (view != null)
            {
                model.HealthChanged -= view.UpdateView;
            }
        }

        private void Start()
        {    
            RegisterEvents();
            model.RefreshModel();
        }

        private void OnEnable()
        {
            RegisterEvents();
        }

        private void OnDisable()
        {
            UnregisterEvents();
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
