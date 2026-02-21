using MyToolz.DesignPatterns.MVP.View;
using MyToolz.Events;
using UnityEngine;
using Zenject;

namespace MyToolz.UI.LoadingScreen
{
    public class SceneLoaderPresenter : MonoBehaviour, IEventListener
    {
        private ISceneLoaderModel model;
        private IReadOnlyView<ISceneLoaderModel> view;

        [Inject]
        public void Construct(ISceneLoaderModel model, IReadOnlyView<ISceneLoaderModel> view)
        {
            this.model = model;
            this.view = view;
        }

        private void Start()
        {
            view.Initialize(model);

            model.OnLoadingStarted += OnLoadingStarted;
            model.OnLoadingFinished += OnLoadingFinished;
            model.OnProgressChanged += OnProgressChanged;

            RegisterEvents();
        }

        private void OnDestroy()
        {
            model.OnLoadingStarted -= OnLoadingStarted;
            model.OnLoadingFinished -= OnLoadingFinished;
            model.OnProgressChanged -= OnProgressChanged;

            UnregisterEvents();

            view.Destroy(model);
        }

        public void RegisterEvents()
        {
            model.RegisterEvents();
        }

        public void UnregisterEvents()
        {
            model.UnregisterEvents();
        }

        private void OnLoadingStarted()
        {
            view.Show();
            view.UpdateView(model);
        }

        private void OnLoadingFinished()
        {
            view.UpdateView(model);
            view.Hide();
        }

        private void OnProgressChanged()
        {
            view.UpdateView(model);
        }
    }
}
