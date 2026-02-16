using MyToolz.HealthSystem.Interfaces;
using MyToolz.UI.Management;
using UnityEngine;

namespace MyToolz.HealthSystem.View
{
    [System.Serializable]
    public abstract class HealthSystemViewAbstract : IHealthView
    {
        //Example of what could be added
        //TODO: create own health bar solution for it
        //[SerializeField, Required] protected MMProgressBar progressBar;
        [SerializeField] protected bool showHideHP = true;

        public void Hide()
        {
            //if (showHideHP) progressBar.HideBar(0f);
        }

        public virtual void Initialize((float currentHealth, float min, float max) model)
        {
            // progressBar.SetBar(model.currentHealth, model.min, model.max);
        }

        public void Show()
        {
            //if (showHideHP) progressBar.ShowBar();
        }

        public virtual void UpdateView((float currentHealth, float min, float max) model, float oldHealth)
        {
            // progressBar.UpdateBar(model.currentHealth, model.min, model.max);
        }
    }

    [System.Serializable]
    public class PlayerHealthSystemView : HealthSystemViewAbstract
    {
        //Example of what could be added
        //[SerializeField, Required] private MMF_Player damagedFeedback;
        [SerializeField] private UIScreenBase deathScreen;

        public override void UpdateView((float currentHealth, float min, float max) model, float oldHealth)
        {
            base.UpdateView(model, oldHealth);
            //if (model.currentHealth < oldHealth) damagedFeedback.PlayFeedbacks();
            if (model.currentHealth <= model.min)
            {
                deathScreen?.Open();
            }
        }
    }

    [System.Serializable]
    public class HealthSystemView : HealthSystemViewAbstract
    {

    }
}