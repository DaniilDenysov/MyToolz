using Cysharp.Threading.Tasks;
using MyToolz.DesignPatterns.EventBus;
using MyToolz.DesignPatterns.Singleton;
using MyToolz.Events;
using MyToolz.Extensions;
using MyToolz.Utilities.Debug;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MyToolz.SceneManagement
{
    public class SceneLoader : Singleton<SceneLoader>, IEventListener
    {
        private EventBinding<LoadScene> onLoadSceneBinding;

        private void Start()
        {
            RegisterEvents();
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            UnregisterEvents();
        }

        public void RegisterEvents()
        {
            onLoadSceneBinding = new EventBinding<LoadScene>(OnLoadSceneRequested);
            EventBus<LoadScene>.Register(onLoadSceneBinding);
        }

        public void UnregisterEvents()
        {
            EventBus<LoadScene>.Deregister(onLoadSceneBinding);
        }

        private void OnLoadSceneRequested(LoadScene loadScene)
        {
            if (string.IsNullOrWhiteSpace(loadScene.SceneName))
            {
                DebugUtility.LogError(this, "LoadScene event received with null or empty SceneName.");
                return;
            }

            string sceneName = UIUtilities.ExtractSceneName(loadScene.SceneName);

            if (SceneManager.GetActiveScene().name == sceneName)
            {
                DebugUtility.LogError(this, $"Scene '{sceneName}' is already active. Skipping load.");
                return;
            }

            LoadSceneAsync(sceneName, loadScene.LoadSceneMode, destroyCancellationToken).Forget();
        }

        private async UniTaskVoid LoadSceneAsync(string sceneName, LoadSceneMode mode, CancellationToken token)
        {
            AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName, mode);

            if (operation == null)
            {
                DebugUtility.LogError(this, $"Failed to start async load for scene '{sceneName}'.");
                return;
            }

            operation.allowSceneActivation = false;

            EventBus<SceneLoading>.Raise(new SceneLoading
            {
                SceneName = sceneName,
                AsyncOperation = operation
            });

            while (operation.progress < 0.9f)
            {
                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }

            operation.allowSceneActivation = true;

            await operation.ToUniTask(cancellationToken: token);

            EventBus<SceneLoaded>.Raise(new SceneLoaded
            {
                SceneName = sceneName
            });
        }
    }
}