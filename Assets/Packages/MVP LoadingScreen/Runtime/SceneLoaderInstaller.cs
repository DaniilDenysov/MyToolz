using MyToolz.DesignPatterns.MVP.View;
using MyToolz.SceneManagement;
using UnityEngine;
using Zenject;

namespace MyToolz.UI.LoadingScreen
{
    public class SceneLoaderInstaller : MonoInstaller
    {
        [SerializeField] private SceneLoaderPresenter presenter;
        [SerializeReference] private SceneLoaderModel model = new();
        [SerializeReference] private SceneLoaderView view = new();
        [SerializeField] private SceneLoader sceneLoader;

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
                .FromComponentInNewPrefab(presenter)
                .AsSingle()
                .NonLazy();

            Container.Bind<SceneLoader>()
                .FromInstance(sceneLoader)
                .AsSingle()
                .NonLazy();
        }
    }
}
