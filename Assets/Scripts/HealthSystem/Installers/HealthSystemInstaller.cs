using MyToolz.HealthSystem.Interfaces;
using MyToolz.HealthSystem.Model;
using MyToolz.HealthSystem.View;
using UnityEngine;
using Zenject;

namespace MyToolz.HealthSystem.Installers
{
    public class HealthSystemInstaller : MonoInstaller
    {
        [SerializeReference] private Model.HealthSystemModel healthModel = new();
        [SerializeReference] private HealthSystemViewAbstract healthView;

        public override void InstallBindings()
        {
            Container.Bind<IHealthModel>().FromInstance(healthModel).AsSingle();
            Container.Bind<IHealthView>().FromInstance(healthView).AsSingle();
        }
    }
}
