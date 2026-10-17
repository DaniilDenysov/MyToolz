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
        [SerializeField, Min(0f), Tooltip("Minimum seconds (real time) the loading screen stays up. " +
            "The last scene of a group is activated only after this, so it never pops in under the loading screen.")]
        private float minimalLoadingDuration = 0f;

        private EventBinding<LoadSceneGroup> onLoadSceneBinding;
        private EventBinding<ReloadCurrentSceneGroup> onReloadCurrentSceneBinding;
        private SceneGroupSO currentSceneGroup;
        private bool eventsRegistered;

        // One transition runs at a time. Requests arriving meanwhile are coalesced: only the latest
        // one runs after the current transition finishes.
        private bool isLoading;
        private SceneGroupSO queuedGroup;
        private List<AsyncLoadStep> queuedSteps;
        private CancellationTokenSource lifetime;

        public SceneGroupSO CurrentSceneGroup => currentSceneGroup;
        public bool IsLoading => isLoading;

        private void Awake()
        {
            lifetime = new CancellationTokenSource();
        }

        async void Start()
        {
            RegisterEvents();

            if (sceneGroups == null || sceneGroups.Length == 0)
            {
                DebugUtility.LogWarning(this, "No scene groups configured, skipping initial load.");
                return;
            }

            await LoadSceneGroup(sceneGroups[0], null);
        }

        /// <summary>
        /// Loads a group, or queues it when a transition is already running. Returns true when this
        /// request's group loaded successfully (false when it failed, was cancelled, or was superseded
        /// by a newer queued request).
        /// </summary>
        public async UniTask<bool> LoadSceneGroup(SceneGroupSO sceneGroupSO, List<AsyncLoadStep> steps)
        {
            if (sceneGroupSO == null)
            {
                DebugUtility.LogError(this, "SceneGroup event received with null scene group!");
                return false;
            }

            if (isLoading)
            {
                queuedGroup = sceneGroupSO;
                queuedSteps = steps;
                return false;
            }

            isLoading = true;
            bool loaded;
            try
            {
                loaded = await LoadNow(sceneGroupSO, steps);
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

            return loaded;
        }

        private async UniTask<bool> LoadNow(SceneGroupSO sceneGroupSO, List<AsyncLoadStep> steps)
        {
            float startedAt = Time.realtimeSinceStartup;
            CancellationToken token = lifetime != null ? lifetime.Token : CancellationToken.None;
            bool scenesLoaded = false;

            List<AsyncLoadStep> pipeline = new List<AsyncLoadStep>
            {
                async progress => scenesLoaded = await sceneGroupManager.LoadScenes(
                    sceneGroupSO,
                    progress,
                    () => WaitForMinimalDuration(startedAt, token),
                    token: token)
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

                if (!scenesLoaded)
                {
                    RaiseFailed(sceneGroupSO, false, "One or more scenes failed to load.");
                    return false;
                }

                currentSceneGroup = sceneGroupSO;
                EventBus<SceneGroupLoaded>.Raise(new SceneGroupLoaded { Group = sceneGroupSO });
                return true;
            }
            catch (OperationCanceledException)
            {
                DebugUtility.LogWarning(this, $"Loading of '{sceneGroupSO.GroupName}' was cancelled.");
                RaiseFailed(sceneGroupSO, true, "Cancelled.");
                return false;
            }
            catch (Exception e)
            {
                DebugUtility.LogError(this, $"Loading of '{sceneGroupSO.GroupName}' failed: {e}");
                RaiseFailed(sceneGroupSO, false, e.Message);
                return false;
            }
            finally
            {
                EventBus<LoadingScreenHide>.Raise(new LoadingScreenHide());
                masterProgress.Dispose();
            }
        }

        private static void RaiseFailed(SceneGroupSO group, bool cancelled, string error)
        {
            EventBus<SceneGroupLoadFailed>.Raise(new SceneGroupLoadFailed
            {
                Group = group,
                Cancelled = cancelled,
                Error = error
            });
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
                DelayType.Realtime,
                PlayerLoopTiming.Update,
                token);
        }

        public void OnDestroy()
        {
            UnregisterEvents();
            queuedGroup = null;
            queuedSteps = null;
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

            onLoadSceneBinding = new EventBinding<LoadSceneGroup>(OnLoadSceneRequested);
            EventBus<LoadSceneGroup>.Register(onLoadSceneBinding);

            onReloadCurrentSceneBinding = new EventBinding<ReloadCurrentSceneGroup>(OnReloadCurrentSceneRequested);
            EventBus<ReloadCurrentSceneGroup>.Register(onReloadCurrentSceneBinding);
            eventsRegistered = true;
        }

        public void UnregisterEvents()
        {
            if (!eventsRegistered)
            {
                return;
            }

            EventBus<LoadSceneGroup>.Deregister(onLoadSceneBinding);
            EventBus<ReloadCurrentSceneGroup>.Deregister(onReloadCurrentSceneBinding);
            eventsRegistered = false;
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
