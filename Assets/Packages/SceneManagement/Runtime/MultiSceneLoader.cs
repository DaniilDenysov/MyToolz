using System;
using System.Collections.Generic;
using System.Threading;
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

        [Tooltip("Keeps the loading screen up for at least this many seconds (unscaled) so a fast load does not flash it. 0 = activate as soon as the group is ready.")]
        [SerializeField, Min(0f)] private float minimalLoadingDuration;

        private EventBinding<LoadSceneGroup> onLoadSceneBinding;
        private EventBinding<ReloadCurrentSceneGroup> onReloadCurrentSceneBinding;
        private SceneGroupSO currentSceneGroup;
        private bool isLoading;
        private SceneGroupSO queuedGroup;
        private List<AsyncLoadStep> queuedSteps;

        private void Start()
        {
            RegisterEvents();

            if (sceneGroups == null || sceneGroups.Length == 0)
            {
                DebugUtility.LogWarning(this, "No scene groups configured, skipping initial load.");
                return;
            }

            LoadSceneGroup(sceneGroups[0], null).Forget();
        }

        public async UniTask LoadSceneGroup(SceneGroupSO sceneGroupSO, List<AsyncLoadStep> steps)
        {
            if (sceneGroupSO == null)
            {
                DebugUtility.LogError(this, "SceneGroup event received with null scene group!");
                return;
            }

            if (isLoading)
            {
                queuedGroup = sceneGroupSO;
                queuedSteps = steps;
                return;
            }

            isLoading = true;

            try
            {
                await LoadNow(sceneGroupSO, steps);
            }
            finally
            {
                isLoading = false;
            }

            if (queuedGroup != null && this != null)
            {
                SceneGroupSO next = queuedGroup;
                List<AsyncLoadStep> nextSteps = queuedSteps;
                queuedGroup = null;
                queuedSteps = null;
                await LoadSceneGroup(next, nextSteps);
            }
        }

        private async UniTask LoadNow(SceneGroupSO sceneGroupSO, List<AsyncLoadStep> steps)
        {
            float startedAt = Time.realtimeSinceStartup;
            CancellationToken token = destroyCancellationToken;

            List<AsyncLoadStep> pipeline = new List<AsyncLoadStep>
            {
                progress => sceneGroupManager.LoadScenes(
                    sceneGroupSO,
                    progress,
                    () => WaitForMinimalDuration(startedAt, token))
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

                await WaitForMinimalDuration(startedAt, token);
                currentSceneGroup = sceneGroupSO;
                EventBus<SceneGroupLoaded>.Raise(new SceneGroupLoaded());
            }
            catch (OperationCanceledException)
            {
                DebugUtility.LogError(this, "Loading pipeline was cancelled.");
            }
            catch (Exception e)
            {
                // A failed scene or step must not leave the loading screen up forever, and must not
                // be reported as a successful load either.
                DebugUtility.LogError(this, $"Loading scene group '{sceneGroupSO.name}' failed: {e.Message}\n{e}");
                EventBus<SceneGroupLoadFailed>.Raise(new SceneGroupLoadFailed
                {
                    Group = sceneGroupSO,
                    Reason = e.Message
                });
            }
            finally
            {
                EventBus<LoadingScreenHide>.Raise(new LoadingScreenHide());
                masterProgress.Dispose();
            }
        }

        private UniTask WaitForMinimalDuration(float startedAt, CancellationToken token)
        {
            float remaining = minimalLoadingDuration - (Time.realtimeSinceStartup - startedAt);

            if (remaining <= 0f)
            {
                return UniTask.CompletedTask;
            }

            return UniTask.Delay(
                TimeSpan.FromSeconds(remaining),
                DelayType.UnscaledDeltaTime,
                PlayerLoopTiming.Update,
                token);
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
