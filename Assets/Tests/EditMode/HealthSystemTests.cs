using System.Reflection;
using MyToolz.HealthSystem;
using MyToolz.HealthSystem.Interfaces;
using MyToolz.HealthSystem.Model;
using NUnit.Framework;

namespace MyToolz.Tests.EditMode
{
    public class HealthSystemTests : SilentLogTest
    {
        private sealed class DamageRecorder : IDamagable<IDamageArgs>
        {
            public float Total;
            public int Hits;

            public void DoDamage(IDamageArgs damageArgs)
            {
                Total += damageArgs.Damage;
                Hits++;
            }
        }

        private static HealthSystemModel Model(float current, float max)
        {
            var model = new HealthSystemModel();
            Set(model, "currentHealth", current);
            Set(model, "maxHealth", max);
            model.Initialize();
            return model;
        }

        private static void Set(object target, string field, object value) =>
            typeof(HealthSystemModel).GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);

        private static float Run(TickBasedDamageType effect, float frameTime, float seconds)
        {
            var recorder = new DamageRecorder();
            for (float t = 0f; t < seconds; t += frameTime)
            {
                effect.Tick(recorder, frameTime);
            }
            return recorder.Total;
        }

        [Test]
        public void TickDamage_TotalIsIndependentOfFrameRate()
        {
            float at60 = Run(new PoisonDamageType(10f, 5f, 1f), 1f / 60f, 6f);
            float at10 = Run(new PoisonDamageType(10f, 5f, 1f), 0.1f, 6f);

            Assert.AreEqual(50f, at60, 1e-3, "damage per tick x five ticks");
            Assert.AreEqual(at60, at10, 1e-3);
        }

        [Test]
        public void LongFrame_DeliversEveryTickItCovered()
        {
            var effect = new FireDamageType(4f, 5f, 1f);
            var recorder = new DamageRecorder();

            bool active = effect.Tick(recorder, 3.5f);

            Assert.AreEqual(3, recorder.Hits);
            Assert.IsTrue(active);
        }

        [Test]
        public void Effect_EndsAfterItsLastTick()
        {
            var effect = new FireDamageType(4f, 2f, 1f);
            var recorder = new DamageRecorder();

            Assert.IsTrue(effect.Tick(recorder, 1f));
            Assert.IsFalse(effect.Tick(recorder, 1f));
            Assert.IsFalse(effect.Tick(recorder, 5f));
            Assert.AreEqual(2, recorder.Hits, "no damage after the duration");
        }

        [Test]
        public void InvalidDamage_IsIgnored()
        {
            var model = Model(100f, 100f);

            model.DoDamage(new DefaultDamageArgs { Damage = -20f });
            model.DoDamage(new DefaultDamageArgs { Damage = float.NaN });
            model.DoDamage(new DefaultDamageArgs { Damage = float.PositiveInfinity });

            Assert.AreEqual(100f, model.CurrentHealth.currentHealth);
        }

        [Test]
        public void InvalidHeal_IsIgnored()
        {
            var model = Model(50f, 100f);

            model.DoHeal(float.NaN);
            model.DoHeal(-5f);

            Assert.AreEqual(50f, model.CurrentHealth.currentHealth);
        }

        [Test]
        public void ModelUpdate_AppliesAndRetiresDamageOverTime()
        {
            var model = Model(100f, 100f);
            model.DoDamage(new PoisonDamageType(10f, 2f, 1f));

            model.Update(1f);
            model.Update(1f);
            model.Update(1f);

            Assert.AreEqual(80f, model.CurrentHealth.currentHealth, 1e-3);
        }

        [Test]
        public void Initialize_ClampsCurrentHealthIntoRange()
        {
            var model = Model(250f, 100f);
            Assert.AreEqual(100f, model.CurrentHealth.currentHealth);
        }
    }
}
