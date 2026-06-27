using MyToolz.DesignPatterns.MVP.View;
using System;
using UnityEngine;
using Zenject;

namespace MyToolz.UI.LoadingScreen
{
    public class SceneLoaderInstaller : MonoInstaller
    {
        [SerializeField] private SceneLoaderPresenter presenter;
        [SerializeReference] private SceneLoaderModel model = new();
        [SerializeField] private SceneLoaderView view = new();

        public override void InstallBindings()
        {
            Container.Bind<ISceneLoaderModel>()
                .FromInstance(model)
                .AsSingle();

            Container.Bind<IReadOnlyView<ISceneLoaderModel>>()
                .FromInstance(view)
                .AsSingle()
                .NonLazy();

            Container.Bind<SceneLoaderPresenter>()
                .FromInstance(presenter)
                .AsSingle()
                .NonLazy();
        }
    }
}
