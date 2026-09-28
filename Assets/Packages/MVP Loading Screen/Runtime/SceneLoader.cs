using Cysharp.Threading.Tasks;
using MyToolz.DesignPatterns.EventBus;
using MyToolz.DesignPatterns.Singleton;
using MyToolz.Events;
using MyToolz.Extensions;
using MyToolz.Utilities.Debug;
using System;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MyToolz.SceneManagement
{
    public class SceneLoader : PrivateSingleton<SceneLoader>, IEventListener
    {
        [SerializeField, Min(0f), Tooltip("Minimum seconds (real time) the loading screen stays up before the scene activates.")]
        private float minimalLoadingDuration = 0f;

        private EventBinding<LoadScene> onLoadSceneBinding;
        private bool eventsRegistered;
        private bool isLoading;
        private CancellationTokenSource lifetime;

        protected override void OnSingletonAwake()
        {
            lifetime = new CancellationTokenSource();
        }

        private void Start()
        {
            RegisterEvents();
        }

        protected override void OnSingletonDestroy()
        {
            UnregisterEvents();
            lifetime?.Cancel();
            lifetime?.Dispose();
            lifetime = null;
        }

        public void RegisterEvents()
        {
            if (eventsRegistered)
            {
                return;
            }

            onLoadSceneBinding = new EventBinding<LoadScene>(OnLoadSceneRequested);
            EventBus<LoadScene>.Register(onLoadSceneBinding);
            eventsRegistered = true;
        }

        public void UnregisterEvents()
        {
            if (!eventsRegistered)
            {
                return;
            }

            EventBus<LoadScene>.Deregister(onLoadSceneBinding);
            eventsRegistered = false;
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

            if (isLoading)
            {
                // A second async load would queue behind the parked activation of the first one.
                DebugUtility.LogWarning(this, $"A scene is already loading; ignoring the request for '{sceneName}'.");
                return;
            }

            LoadSceneAsync(sceneName, loadScene.LoadSceneMode, lifetime != null ? lifetime.Token : CancellationToken.None).Forget();
        }

        private async UniTaskVoid LoadSceneAsync(string sceneName, LoadSceneMode mode, CancellationToken token)
        {
            float startedAt = Time.realtimeSinceStartup;
            AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName, mode);

            if (operation == null)
            {
                DebugUtility.LogError(this, $"Failed to start async load for scene '{sceneName}'.");
                return;
            }

            isLoading = true;
            operation.allowSceneActivation = false;

            var progress = new LoadingProgress();

            // Raise unified loading screen event
            EventBus<LoadingScreenShow>.Raise(new LoadingScreenShow
            {
                Progress = progress
            });

            // Raise legacy event for backward compatibility
            EventBus<SceneLoading>.Raise(new SceneLoading
            {
                SceneName = sceneName,
                AsyncOperation = operation
            });

            bool activated = false;
            try
            {
                while (operation.progress < 0.9f)
                {
                    progress.Report(operation.progress / 0.9f);
                    await UniTask.Yield(PlayerLoopTiming.Update, token);
                }

                progress.Report(1f);

                float remaining = minimalLoadingDuration - (Time.realtimeSinceStartup - startedAt);
                if (remaining > 0f)
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(remaining), DelayType.Realtime, PlayerLoopTiming.Update, token);
                }

                operation.allowSceneActivation = true;
                activated = true;

                // Not cancellable: with LoadSceneMode.Single activation destroys this loader's scene
                // (and this component) before the operation reports done.
                await operation.ToUniTask();

                EventBus<SceneLoaded>.Raise(new SceneLoaded
                {
                    SceneName = sceneName
                });
            }
            catch (OperationCanceledException)
            {
                DebugUtility.LogWarning(this, $"Loading of scene '{sceneName}' was cancelled.");
            }
            finally
            {
                // A parked operation (allowSceneActivation = false) blocks every later async load.
                if (!activated)
                {
                    operation.allowSceneActivation = true;
                }

                isLoading = false;
                EventBus<LoadingScreenHide>.Raise(new LoadingScreenHide());
            }
        }
    }
}
