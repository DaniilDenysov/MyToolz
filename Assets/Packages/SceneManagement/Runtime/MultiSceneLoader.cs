using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using MyToolz.DesignPatterns.EventBus;
using MyToolz.Events;
using MyToolz.Utilities.Debug;
using UnityEngine;

namespace MyToolz.SceneManagement
{
    public delegate UniTask AsyncLoadStep(LoadingProgress progress);

    public class MultiSceneLoader : MonoBehaviour, IEventListener
    {
        [SerializeField] private SceneGroupSO[] sceneGroups;
        [SerializeField] private SceneGroupManager sceneGroupManager = new();

        private EventBinding<LoadSceneGroup> onLoadSceneBinding;
        private EventBinding<ReloadCurrentSceneGroup> onReloadCurrentSceneBinding;
        private SceneGroupSO currentSceneGroup;

        async void Start()
        {
            RegisterEvents();
            await LoadSceneGroup(sceneGroups[0], null);
        }

        public async UniTask LoadSceneGroup(SceneGroupSO sceneGroupSO, List<AsyncLoadStep> steps)
        {
            if (sceneGroupSO == null)
            {
                DebugUtility.LogError(this, "SceneGroup event received with null scene group!");
                return;
            }

            List<AsyncLoadStep> pipeline = new List<AsyncLoadStep>
            {
                progress => sceneGroupManager.LoadScenes(sceneGroupSO, progress)
            };

            if (steps != null)
            {
                pipeline.AddRange(steps);
            }

            List<LoadingProgress> childProgresses = new List<LoadingProgress>();
            for (int i = 0; i < pipeline.Count; i++)
            {
                childProgresses.Add(new LoadingProgress());
            }

            MultiLoadingProgress masterProgress = new MultiLoadingProgress(childProgresses);

            EventBus<LoadingScreenShow>.Raise(new LoadingScreenShow
            {
                Progress = masterProgress
            });

            EventBus<SceneGroupLoading>.Raise(new SceneGroupLoading
            {
                Group = sceneGroupSO
            });

            try
            {
                for (int i = 0; i < pipeline.Count; i++)
                {
                    await pipeline[i](childProgresses[i]);
                }
                currentSceneGroup = sceneGroupSO;
                EventBus<SceneGroupLoaded>.Raise(new SceneGroupLoaded());
            }
            catch (OperationCanceledException)
            {
                DebugUtility.LogError(this, "Loading pipeline was cancelled.");
            }

            EventBus<LoadingScreenHide>.Raise(new LoadingScreenHide());
            masterProgress.Dispose();
        }

        public void OnDestroy()
        {
            UnregisterEvents();
        }

        public void RegisterEvents()
        {
            onLoadSceneBinding = new EventBinding<LoadSceneGroup>(OnLoadSceneRequested);
            EventBus<LoadSceneGroup>.Register(onLoadSceneBinding);

            onReloadCurrentSceneBinding = new EventBinding<ReloadCurrentSceneGroup>(OnReloadCurrentSceneRequested);
            EventBus<ReloadCurrentSceneGroup>.Register(onReloadCurrentSceneBinding);
        }

        public void UnregisterEvents()
        {
            EventBus<LoadSceneGroup>.Deregister(onLoadSceneBinding);
            EventBus<ReloadCurrentSceneGroup>.Deregister(onReloadCurrentSceneBinding);
        }

        private void OnReloadCurrentSceneRequested(ReloadCurrentSceneGroup loadScene)
        {
            if (currentSceneGroup == null)
            {
                DebugUtility.LogError(this, "No loaded scene group!");
                return;
            }

            LoadSceneGroup(currentSceneGroup, loadScene.Steps).Forget();
        }

        private void OnLoadSceneRequested(LoadSceneGroup loadScene)
        {
            if (loadScene.Group == null)
            {
                DebugUtility.LogError(this, "SceneGroup event received with null scene group!");
                return;
            }

            LoadSceneGroup(loadScene.Group, loadScene.Steps).Forget();
        }
    }
}
