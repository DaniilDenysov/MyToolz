using MyToolz.HealthSystem.Interfaces;

namespace MyToolz.HealthSystem
{
    public class HealableHitBoxPresenter : HitBoxPresenter, IHealable
    {
        public void DoHeal(float amount)
        {
            healthModel.DoHeal(amount);
        }
    }
}
