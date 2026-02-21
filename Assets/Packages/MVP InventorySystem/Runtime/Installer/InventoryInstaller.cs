using MyToolz.InventorySystem.Models;
using MyToolz.InventorySystem.Presenters;
using MyToolz.InventorySystem.Settings;
using UnityEngine;
using Zenject;

namespace MyToolz.InventorySystem.Installers
{
    public abstract class InventoryInstaller<T, Model> : MonoInstaller where T : ScriptableObject where Model : InventoryModel<T>
    {
        [SerializeField] private InventorySettingsSO<T> settings;
        [SerializeReference, SubclassSelector] protected Model model;
        [SerializeField] protected InventoryPresenter<T> presenter;

        public override void InstallBindings()
        {
            Container.Bind<IInventoryModel<T>>()
                .FromInstance(model)
                .AsSingle();

            Container.Bind<InventorySettingsSO<T>>()
                .FromInstance(settings)
                .AsSingle();

            Container.Bind<IInventoryPresenter<T>>().FromInstance(presenter).AsSingle();
            Container.QueueForInject(presenter);
        }
    }
}
