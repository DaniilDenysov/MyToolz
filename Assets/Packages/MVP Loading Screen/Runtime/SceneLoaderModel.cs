using MyToolz.DesignPatterns.EventBus;
using MyToolz.Events;
using System;
using UnityEngine;

namespace MyToolz.UI.LoadingScreen
{
    [Serializable]
    public class SceneLoaderModel : ISceneLoaderModel
    {
        private const int LOADING_DELAY = 100;

        public event Action OnLoadingStarted;
        public event Action OnLoadingFinished;
        public event Action OnProgressChanged;

        public float CurrentProgress { get; private set; }
        public bool IsLoading { get; private set; }

        private EventBinding<SceneLoading> onSceneLoadingBinding;
        private EventBinding<SceneLoaded> onSceneLoadedBinding;

        public void RegisterEvents()
        {
            onSceneLoadingBinding = new EventBinding<SceneLoading>(OnSceneLoading);
            EventBus<SceneLoading>.Register(onSceneLoadingBinding);

            onSceneLoadedBinding = new EventBinding<SceneLoaded>(OnSceneLoaded);
            EventBus<SceneLoaded>.Register(onSceneLoadedBinding);
        }

        public void UnregisterEvents()
        {
            EventBus<SceneLoading>.Deregister(onSceneLoadingBinding);
            EventBus<SceneLoaded>.Deregister(onSceneLoadedBinding);
        }

        private async void OnSceneLoading(SceneLoading sceneLoading)
        {
            if (sceneLoading.AsyncOperation == null) return;

            IsLoading = true;
            SetProgress(0f);
            OnLoadingStarted?.Invoke();

            var operation = sceneLoading.AsyncOperation;

            while (operation.progress < 0.9f)
            {
                SetProgress(operation.progress / 0.9f);
                await System.Threading.Tasks.Task.Delay(LOADING_DELAY);
            }

            SetProgress(1f);
        }

        private void OnSceneLoaded(SceneLoaded sceneLoaded)
        {
            IsLoading = false;
            OnLoadingFinished?.Invoke();
        }

        private void SetProgress(float progress)
        {
            CurrentProgress = Mathf.Clamp01(progress);
            OnProgressChanged?.Invoke();
        }
    }
}
