using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;
#if MYTOOLZ_ADDRESSABLES
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
#endif

namespace MyToolz.SceneManagement
{
    public enum SceneLoadSource
    {
        /// <summary>Scenes in the Build Profile / build list, loaded through SceneManager.</summary>
        BuildSettings,
        /// <summary>Addressable scenes, loaded by asset GUID. Requires com.unity.addressables.</summary>
        Addressables
    }

    /// <summary>Scenes of a group that still need loading, in batch order, with the scene whose activation is deferred.</summary>
    public sealed class SceneLoadPlan
    {
        public readonly List<List<SceneData>> Batches = new();
        /// <summary>Last scene to load; it is activated only after the optional before-activation step.</summary>
        public SceneData DeferredScene;
        public int TotalScenes;
        public int SkippedScenes;
    }

    [Serializable]
    public class SceneGroupManager
    {
        [SerializeField] private List<string> blacklist = new();
        [SerializeField, Tooltip("Where scenes are loaded from. Addressables needs the com.unity.addressables package and the scenes marked Addressable.")]
        private SceneLoadSource loadSource = SceneLoadSource.BuildSettings;

#if MYTOOLZ_ADDRESSABLES
        // Addressable scenes this manager loaded, so they are unloaded (and their handles released)
        // through Addressables rather than SceneManager.
        private readonly List<AsyncOperationHandle<SceneInstance>> addressableScenes = new();
#endif

        public SceneLoadSource LoadSource
        {
            get => loadSource;
            set => loadSource = value;
        }

        /// <summary>
        /// Unloads the previous group, then loads <paramref name="sceneGroupSO"/> additively in priority batches.
        /// Returns false when any scene failed to load. Cancellation throws <see cref="OperationCanceledException"/>
        /// after making sure no scene is left waiting for activation.
        /// </summary>
        public async UniTask<bool> LoadScenes(
            SceneGroupSO sceneGroupSO,
            IProgress<float> progress,
            Func<UniTask> beforeActivation = null,
            bool reloadDupScenes = false,
            CancellationToken token = default)
        {
            if (sceneGroupSO == null)
            {
                throw new ArgumentNullException(nameof(sceneGroupSO));
            }

            await UnloadScenes(token);

            SceneLoadPlan plan = CreatePlan(sceneGroupSO, GetLoadedScenes(), reloadDupScenes, loadSource);
            int total = Mathf.Max(1, plan.TotalScenes);
            int completed = plan.SkippedScenes;
            bool success = true;
            PendingScene deferred = default;

            try
            {
                foreach (List<SceneData> batch in plan.Batches)
                {
                    var operations = new List<PendingScene>(batch.Count);
                    SceneData deferredData = null;
                    foreach (SceneData sceneData in batch)
                    {
                        if (sceneData == plan.DeferredScene)
                        {
                            // Started only after the rest of its batch: a scene parked before activation
                            // stalls every async operation queued behind it.
                            deferredData = sceneData;
                            continue;
                        }

                        PendingScene pending = StartLoad(sceneData, activateOnLoad: true);
                        if (!pending.IsValid)
                        {
                            success = false;
                            continue;
                        }

                        operations.Add(pending);
                    }

                    await WaitUntilReady(operations, progress, completed, batch.Count, total, token);

                    if (deferredData != null)
                    {
                        deferred = StartLoad(deferredData, activateOnLoad: false);
                        if (deferred.IsValid)
                        {
                            operations.Add(deferred);
                            await WaitUntilReady(operations, progress, completed, batch.Count, total, token);
                        }
                        else
                        {
                            success = false;
                        }
                    }

                    foreach (PendingScene operation in operations)
                    {
                        if (operation.Failed)
                        {
                            success = false;
                            Debug.LogError($"[SceneGroupManager] Failed to load scene '{operation.Scene.Name}'. {operation.Error}");
                            operation.ReleaseIfFailed();
                        }
                    }

                    completed += batch.Count;
                    progress?.Report(Mathf.Clamp01((float)completed / total));
                }

                if (deferred.IsValid && !deferred.Failed)
                {
                    if (beforeActivation != null)
                    {
                        await beforeActivation();
                    }

                    PendingScene toActivate = deferred;
                    deferred = default;
                    await Activate(toActivate, token);
                }
            }
            finally
            {
                // Cancelled or failed while the last scene waited for activation: never leave it
                // parked. A SceneManager operation with allowSceneActivation=false blocks every later
                // async load, and an Addressables scene would stay half-loaded.
                if (deferred.IsValid)
                {
                    deferred.ActivateAndForget();
                }
            }

            SetActiveScene(sceneGroupSO);
            return success;
        }

        public async UniTask UnloadScenes(CancellationToken token = default)
        {
            Scene bootstrapperScene = SceneManager.GetSceneAt(0);
            if (bootstrapperScene.IsValid() && bootstrapperScene.isLoaded)
            {
                SceneManager.SetActiveScene(bootstrapperScene);
            }

            Scene activeScene = SceneManager.GetActiveScene();
            var operations = new List<AsyncOperation>();
#if MYTOOLZ_ADDRESSABLES
            var addressableUnloads = new List<AsyncOperationHandle<SceneInstance>>();
#endif

            // Collect Scene handles (not names) so two loaded scenes sharing a name are both handled.
            var toUnload = new List<Scene>();
            for (int i = SceneManager.sceneCount - 1; i > 0; i--)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (!scene.IsValid() || !scene.isLoaded || scene == activeScene || blacklist.Contains(scene.name))
                {
                    continue;
                }

                toUnload.Add(scene);
            }

            foreach (Scene scene in toUnload)
            {
#if MYTOOLZ_ADDRESSABLES
                int index = addressableScenes.FindIndex(h => h.IsValid() && h.Status == AsyncOperationStatus.Succeeded && h.Result.Scene == scene);
                if (index >= 0)
                {
                    addressableUnloads.Add(Addressables.UnloadSceneAsync(addressableScenes[index]));
                    addressableScenes.RemoveAt(index);
                    continue;
                }
#endif
                AsyncOperation operation = SceneManager.UnloadSceneAsync(scene);
                if (operation != null)
                {
                    operations.Add(operation);
                }
            }

            // Frame-based polling keeps working while Time.timeScale is 0 (paused game).
            while (!operations.TrueForAll(o => o.isDone)
#if MYTOOLZ_ADDRESSABLES
                   || !addressableUnloads.TrueForAll(h => !h.IsValid() || h.IsDone)
#endif
                   )
            {
                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }

            await Resources.UnloadUnusedAssets().ToUniTask(cancellationToken: token);
        }

        /// <summary>
        /// Decides which scenes of a group still need loading. Scenes are identified by path when
        /// known (names can collide across folders) and by name otherwise.
        /// </summary>
        public static SceneLoadPlan CreatePlan(SceneGroupSO group, IReadOnlyCollection<(string path, string name)> loadedScenes,
            bool reloadDupScenes, SceneLoadSource source = SceneLoadSource.BuildSettings)
        {
            var plan = new SceneLoadPlan();
            var loadedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var loadedNames = new HashSet<string>(StringComparer.Ordinal);
            if (loadedScenes != null)
            {
                foreach (var (path, name) in loadedScenes)
                {
                    if (!string.IsNullOrEmpty(path)) loadedPaths.Add(path);
                    if (!string.IsNullOrEmpty(name)) loadedNames.Add(name);
                }
            }

            foreach (SceneData[] batch in group.GetBatchedByPriority())
            {
                var pending = new List<SceneData>(batch.Length);
                foreach (SceneData sceneData in batch)
                {
                    plan.TotalScenes++;

                    if (sceneData.Reference == null || (!sceneData.Reference.IsAssigned && string.IsNullOrEmpty(sceneData.Name)))
                    {
                        Debug.LogError($"[SceneGroupManager] Group '{group.GroupName}' contains an unassigned scene.");
                        plan.SkippedScenes++;
                        continue;
                    }

                    if (source == SceneLoadSource.Addressables && string.IsNullOrEmpty(sceneData.Reference.Guid))
                    {
                        Debug.LogError($"[SceneGroupManager] Scene '{sceneData.Name}' has no asset GUID. Re-save its SceneGroupSO so the SceneReference caches it.");
                        plan.SkippedScenes++;
                        continue;
                    }

                    bool alreadyLoaded = !string.IsNullOrEmpty(sceneData.Reference.Path)
                        ? loadedPaths.Contains(sceneData.Reference.Path)
                        : loadedNames.Contains(sceneData.Name);

                    if (alreadyLoaded && !reloadDupScenes)
                    {
                        plan.SkippedScenes++;
                        continue;
                    }

                    pending.Add(sceneData);
                    plan.DeferredScene = sceneData;
                }

                if (pending.Count > 0)
                {
                    plan.Batches.Add(pending);
                }
            }

            // Progress counts skipped scenes as done once, in SkippedScenes; batches only hold real work.
            return plan;
        }

        private static List<(string path, string name)> GetLoadedScenes()
        {
            var loaded = new List<(string path, string name)>(SceneManager.sceneCount);
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                loaded.Add((scene.path, scene.name));
            }
            return loaded;
        }

        private static void SetActiveScene(SceneGroupSO group)
        {
            SceneData activeData = group.FindSceneDataByType(SceneType.ActiveScene);
            if (activeData == null)
            {
                return;
            }

            Scene scene = !string.IsNullOrEmpty(activeData.Reference.Path)
                ? SceneManager.GetSceneByPath(activeData.Reference.Path)
                : default;
            if (!scene.IsValid())
            {
                scene = SceneManager.GetSceneByName(activeData.Name);
            }

            if (scene.IsValid() && scene.isLoaded)
            {
                SceneManager.SetActiveScene(scene);
            }
        }

        private PendingScene StartLoad(SceneData sceneData, bool activateOnLoad)
        {
#if MYTOOLZ_ADDRESSABLES
            if (loadSource == SceneLoadSource.Addressables)
            {
                AsyncOperationHandle<SceneInstance> handle =
                    Addressables.LoadSceneAsync(sceneData.Reference.Guid, LoadSceneMode.Additive, activateOnLoad);
                addressableScenes.Add(handle);
                return PendingScene.ForAddressable(sceneData, handle, this);
            }
#else
            if (loadSource == SceneLoadSource.Addressables)
            {
                Debug.LogError("[SceneGroupManager] Load Source is Addressables but the com.unity.addressables package is not installed.");
                return default;
            }
#endif

            // Load by path when it is in the build list (unique identity), otherwise by name: a stale
            // cached path (scene moved, group not re-saved) still resolves through the name.
            string key = !string.IsNullOrEmpty(sceneData.Reference.Path) && SceneUtility.GetBuildIndexByScenePath(sceneData.Reference.Path) >= 0
                ? sceneData.Reference.Path
                : sceneData.Name;

            AsyncOperation operation = SceneManager.LoadSceneAsync(key, LoadSceneMode.Additive);
            if (operation == null)
            {
                Debug.LogError($"[SceneGroupManager] Could not load scene '{sceneData.Name}'. " +
                    "It is not registered in the active Build Profile / build list. Check Build Profiles > Scene List.");
                return default;
            }

            operation.allowSceneActivation = activateOnLoad;
            return PendingScene.ForBuildScene(sceneData, operation);
        }

        private static async UniTask WaitUntilReady(List<PendingScene> operations, IProgress<float> progress,
            int completed, int batchSize, int total, CancellationToken token)
        {
            // Frame-based polling keeps working while Time.timeScale is 0 (paused game).
            while (!AllReady(operations))
            {
                ReportProgress(progress, completed, operations, batchSize, total);
                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }
        }

        private static bool AllReady(List<PendingScene> operations)
        {
            foreach (PendingScene operation in operations)
            {
                if (!operation.IsReady) return false;
            }
            return true;
        }

        private static void ReportProgress(IProgress<float> progress, int completed, List<PendingScene> operations, int batchSize, int total)
        {
            if (progress == null) return;

            float sum = 0f;
            foreach (PendingScene operation in operations) sum += operation.Progress;
            float batchProgress = operations.Count > 0 ? sum / operations.Count : 1f;
            progress.Report(Mathf.Clamp01((completed + batchProgress * batchSize) / total));
        }

        private static async UniTask Activate(PendingScene scene, CancellationToken token)
        {
            await scene.ActivateAsync(token);
        }

#if MYTOOLZ_ADDRESSABLES
        private void ForgetFailed(AsyncOperationHandle<SceneInstance> handle)
        {
            addressableScenes.Remove(handle);
            if (handle.IsValid())
            {
                Addressables.Release(handle);
            }
        }
#endif

        /// <summary>One in-flight scene load, over either SceneManager or Addressables.</summary>
        private readonly struct PendingScene
        {
            public readonly SceneData Scene;
            private readonly AsyncOperation operation;
#if MYTOOLZ_ADDRESSABLES
            private readonly AsyncOperationHandle<SceneInstance> handle;
            private readonly SceneGroupManager owner;
#endif

            private PendingScene(SceneData scene, AsyncOperation operation)
            {
                Scene = scene;
                this.operation = operation;
#if MYTOOLZ_ADDRESSABLES
                handle = default;
                owner = null;
#endif
            }

            public static PendingScene ForBuildScene(SceneData scene, AsyncOperation operation) => new PendingScene(scene, operation);

#if MYTOOLZ_ADDRESSABLES
            private PendingScene(SceneData scene, AsyncOperationHandle<SceneInstance> handle, SceneGroupManager owner)
            {
                Scene = scene;
                operation = null;
                this.handle = handle;
                this.owner = owner;
            }

            public static PendingScene ForAddressable(SceneData scene, AsyncOperationHandle<SceneInstance> handle, SceneGroupManager owner) =>
                new PendingScene(scene, handle, owner);

            private bool IsAddressable => owner != null;
#else
            private bool IsAddressable => false;
#endif

            public bool IsValid => Scene != null && (operation != null || IsAddressable);

            /// <summary>Loaded (or parked at 90% awaiting activation), or finished with an error.</summary>
            public bool IsReady
            {
                get
                {
#if MYTOOLZ_ADDRESSABLES
                    if (IsAddressable) return !handle.IsValid() || handle.IsDone;
#endif
                    return operation.isDone || (!operation.allowSceneActivation && operation.progress >= 0.9f);
                }
            }

            public bool Failed
            {
                get
                {
#if MYTOOLZ_ADDRESSABLES
                    if (IsAddressable) return !handle.IsValid() || handle.Status == AsyncOperationStatus.Failed;
#endif
                    return false;
                }
            }

            public string Error
            {
                get
                {
#if MYTOOLZ_ADDRESSABLES
                    if (IsAddressable && handle.IsValid()) return handle.OperationException?.Message ?? string.Empty;
#endif
                    return string.Empty;
                }
            }

            public float Progress
            {
                get
                {
#if MYTOOLZ_ADDRESSABLES
                    if (IsAddressable) return handle.IsValid() ? handle.PercentComplete : 1f;
#endif
                    return operation.allowSceneActivation ? operation.progress : Mathf.Clamp01(operation.progress / 0.9f);
                }
            }

            public async UniTask ActivateAsync(CancellationToken token)
            {
#if MYTOOLZ_ADDRESSABLES
                if (IsAddressable)
                {
                    if (handle.IsValid() && handle.Status == AsyncOperationStatus.Succeeded)
                    {
                        await handle.Result.ActivateAsync().ToUniTask(cancellationToken: token);
                    }
                    return;
                }
#endif
                operation.allowSceneActivation = true;
                await operation.ToUniTask(cancellationToken: token);
            }

            public void ReleaseIfFailed()
            {
#if MYTOOLZ_ADDRESSABLES
                if (IsAddressable && Failed)
                {
                    owner.ForgetFailed(handle);
                }
#endif
            }

            public void ActivateAndForget()
            {
#if MYTOOLZ_ADDRESSABLES
                if (IsAddressable)
                {
                    if (handle.IsValid() && handle.Status == AsyncOperationStatus.Succeeded)
                    {
                        handle.Result.ActivateAsync();
                    }
                    else
                    {
                        owner.ForgetFailed(handle);
                    }
                    return;
                }
#endif
                if (operation != null)
                {
                    operation.allowSceneActivation = true;
                }
            }
        }
    }
}
