using Cysharp.Threading.Tasks;
using MyToolz.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace MyToolz.SceneManagement
{
    /// <summary>How a <see cref="SceneGroupManager"/> loads the scenes of a group.</summary>
    public enum SceneLoadBackend
    {
        /// <summary>Scenes marked Addressable load through Addressables; the rest from the build scene list.</summary>
        Auto,
        /// <summary>Every scene loads through Addressables by its GUID.</summary>
        Addressables,
        /// <summary>Every scene loads by name from the build scene list.</summary>
        BuildSettings
    }

    /// <summary>Thrown when one or more scenes of a group could not be loaded.</summary>
    public sealed class SceneGroupLoadException : Exception
    {
        public IReadOnlyList<string> FailedScenes { get; }

        public SceneGroupLoadException(IReadOnlyList<string> failedScenes, string message)
            : base(message)
        {
            FailedScenes = failedScenes;
        }
    }

    [Serializable]
    public class SceneGroupManager
    {
        [SerializeField] private List<string> blacklist = new();

        [Tooltip("Auto loads scenes marked Addressable through Addressables and every other scene from the build scene list.")]
        [SerializeField] private SceneLoadBackend backend = SceneLoadBackend.Auto;

        private SceneGroupSO ActiveSceneGroup;
        private readonly Dictionary<string, AsyncOperationHandle<SceneInstance>> loadedSceneHandles = new();
        private readonly HashSet<string> buildListScenes = new();

        public SceneLoadBackend Backend
        {
            get => backend;
            set => backend = value;
        }

        /// <summary>One scene being loaded, through either backend.</summary>
        private sealed class PendingLoad
        {
            public SceneData Scene;
            public AsyncOperationHandle<SceneInstance> Handle;
            public AsyncOperation Operation;
            public bool Addressable;
            public bool HoldActivation;

            public bool IsDone
            {
                get
                {
                    if (Addressable)
                    {
                        return !Handle.IsValid() || Handle.IsDone;
                    }

                    // A held build-list load stops at 0.9 and never reports done until activated.
                    return Operation == null || Operation.isDone || (HoldActivation && Operation.progress >= 0.9f);
                }
            }

            public float Progress
            {
                get
                {
                    if (Addressable)
                    {
                        return Handle.IsValid() ? Handle.PercentComplete : 1f;
                    }

                    return Operation == null ? 1f : Mathf.Clamp01(Operation.progress / 0.9f);
                }
            }

            public bool Failed => Addressable
                ? !Handle.IsValid() || Handle.Status == AsyncOperationStatus.Failed
                : Operation == null;

            public string FailureReason => Addressable
                ? (Handle.IsValid() ? Handle.OperationException?.Message : null) ?? "the Addressables operation failed"
                : "it is not in the build scene list";
        }

        public async UniTask LoadScenes(
            SceneGroupSO sceneGroupSO,
            IProgress<float> progress,
            Func<UniTask> beforeActivation = null,
            bool reloadDupScenes = false)
        {
            ActiveSceneGroup = sceneGroupSO;
            var loadedScenes = new List<string>();
            var failures = new List<string>();

            await UnloadScenes();

            int sceneCount = SceneManager.sceneCount;

            for (var i = 0; i < sceneCount; i++)
            {
                loadedScenes.Add(SceneManager.GetSceneAt(i).name);
            }

            var batches = ActiveSceneGroup.GetBatchedByPriority();
            int totalScenes = Mathf.Max(1, ActiveSceneGroup.Scenes.Length);
            int scenesLoaded = 0;

            // Decide each scene's backend up front so the one held back for activation is known.
            var routes = new Dictionary<SceneData, bool>();
            foreach (var batch in batches)
            {
                foreach (var sceneData in batch)
                {
                    if (!reloadDupScenes && loadedScenes.Contains(sceneData.Name))
                    {
                        continue;
                    }

                    bool? addressable = await ResolveRoute(sceneData);
                    if (addressable == null)
                    {
                        failures.Add(sceneData.Name);
                        continue;
                    }

                    routes[sceneData] = addressable.Value;
                }
            }

            SceneData deferredScene = FindLastSceneToLoad(batches, routes);
            PendingLoad deferred = null;

            for (int b = 0; b < batches.Count; b++)
            {
                var batch = batches[b];
                var pending = new List<PendingLoad>(batch.Length);
                bool batchHoldsDeferredScene = false;

                for (var i = 0; i < batch.Length; i++)
                {
                    var sceneData = batch[i];
                    if (!routes.TryGetValue(sceneData, out bool addressable))
                    {
                        continue;
                    }

                    if (sceneData == deferredScene)
                    {
                        batchHoldsDeferredScene = true;
                        continue;
                    }

                    pending.Add(StartLoad(sceneData, addressable, holdActivation: false));
                }

                await WaitFor(pending, progress, scenesLoaded, batch.Length, totalScenes);

                if (batchHoldsDeferredScene)
                {
                    deferred = StartLoad(deferredScene, routes[deferredScene], holdActivation: true);
                    pending.Add(deferred);
                    await WaitFor(pending, progress, scenesLoaded, batch.Length, totalScenes);
                }

                CollectFailures(pending, failures);

                scenesLoaded += batch.Length;
                progress?.Report(Mathf.Clamp01((float)scenesLoaded / totalScenes));
            }

            if (deferred != null && !deferred.Failed)
            {
                if (beforeActivation != null)
                {
                    await beforeActivation();
                }

                await Activate(deferred);
            }

            string activeSceneName = ActiveSceneGroup.FindSceneByType(SceneType.ActiveScene);
            if (!string.IsNullOrEmpty(activeSceneName))
            {
                Scene activeScene = SceneManager.GetSceneByName(activeSceneName);
                if (activeScene.IsValid() && activeScene.isLoaded)
                {
                    SceneManager.SetActiveScene(activeScene);
                }
            }

            if (failures.Count > 0)
            {
                throw new SceneGroupLoadException(failures,
                    $"Scene group '{sceneGroupSO.name}' could not load: {string.Join(", ", failures)}.");
            }
        }

        public async UniTask UnloadScenes()
        {
            Scene bootstrapperScene = SceneManager.GetSceneAt(0);
            if (bootstrapperScene.IsValid() && bootstrapperScene.isLoaded)
            {
                SceneManager.SetActiveScene(bootstrapperScene);
            }

            var activeScene = SceneManager.GetActiveScene().name;
            var handles = new List<AsyncOperationHandle<SceneInstance>>();
            var operations = new List<AsyncOperation>();

            foreach (var sceneName in loadedSceneHandles.Keys.ToList())
            {
                if (sceneName.Equals(activeScene) || blacklist.Contains(sceneName))
                {
                    continue;
                }

                var handle = loadedSceneHandles[sceneName];
                loadedSceneHandles.Remove(sceneName);

                if (!handle.IsValid())
                {
                    continue;
                }

                handles.Add(Addressables.UnloadSceneAsync(handle));
            }

            // Build-list scenes: everything loaded that is in the build list, not the active scene,
            // not blacklisted and not owned by an Addressables handle above.
            for (var i = SceneManager.sceneCount - 1; i > 0; i--)
            {
                Scene sceneAt = SceneManager.GetSceneAt(i);
                if (!sceneAt.isLoaded)
                {
                    continue;
                }

                string sceneName = sceneAt.name;
                if (sceneName.Equals(activeScene) || blacklist.Contains(sceneName) || loadedSceneHandles.ContainsKey(sceneName))
                {
                    continue;
                }

                if (!buildListScenes.Contains(sceneName) && !SceneExtensions.IsSceneValid(sceneName))
                {
                    continue;
                }

                AsyncOperation operation = SceneManager.UnloadSceneAsync(sceneAt);
                if (operation != null)
                {
                    operations.Add(operation);
                }

                buildListScenes.Remove(sceneName);
            }

            while (!handles.All(h => h.IsDone) || !operations.All(o => o.isDone))
            {
                await UniTask.Delay(100, DelayType.UnscaledDeltaTime);
            }

            await Resources.UnloadUnusedAssets();
        }

        /// <summary>True: Addressables, false: build list, null: this scene cannot be loaded at all.</summary>
        private async UniTask<bool?> ResolveRoute(SceneData sceneData)
        {
            string guid = sceneData.Reference != null ? sceneData.Reference.Guid : null;

            switch (backend)
            {
                case SceneLoadBackend.Addressables:
                    if (string.IsNullOrEmpty(guid))
                    {
                        Debug.LogError($"[SceneGroupManager] Scene '{sceneData.Name}' has no GUID. " +
                            "Re-save its SceneGroupSO so the SceneReference caches the GUID.");
                        return null;
                    }

                    return true;

                case SceneLoadBackend.BuildSettings:
                    return BuildListRoute(sceneData);

                default:
                    if (await IsAddressable(guid))
                    {
                        return true;
                    }

                    return BuildListRoute(sceneData);
            }
        }

        private static bool? BuildListRoute(SceneData sceneData)
        {
            if (SceneExtensions.IsSceneValid(sceneData.Name))
            {
                return false;
            }

            Debug.LogError($"[SceneGroupManager] Scene '{sceneData.Name}' is neither Addressable nor in the build scene list. " +
                "Mark it Addressable or add it to Build Profiles > Scene List.");
            return null;
        }

        private static async UniTask<bool> IsAddressable(string guid)
        {
            if (string.IsNullOrEmpty(guid))
            {
                return false;
            }

            AsyncOperationHandle<IList<UnityEngine.ResourceManagement.ResourceLocations.IResourceLocation>> handle;
            try
            {
                handle = Addressables.LoadResourceLocationsAsync(guid, typeof(SceneInstance));
            }
            catch (Exception)
            {
                return false;
            }

            while (!handle.IsDone)
            {
                await UniTask.Yield();
            }

            bool found = handle.Status == AsyncOperationStatus.Succeeded && handle.Result != null && handle.Result.Count > 0;
            Addressables.Release(handle);
            return found;
        }

        private PendingLoad StartLoad(SceneData sceneData, bool addressable, bool holdActivation)
        {
            var load = new PendingLoad
            {
                Scene = sceneData,
                Addressable = addressable,
                HoldActivation = holdActivation
            };

            if (addressable)
            {
                load.Handle = Addressables.LoadSceneAsync(sceneData.Reference.Guid, LoadSceneMode.Additive, !holdActivation);
                loadedSceneHandles[sceneData.Name] = load.Handle;
            }
            else
            {
                // Load by name (not path): scene names survive asset moves, whereas a cached path can
                // go stale until the SceneReference is re-serialized.
                load.Operation = SceneManager.LoadSceneAsync(sceneData.Name, LoadSceneMode.Additive);
                if (load.Operation != null)
                {
                    load.Operation.allowSceneActivation = !holdActivation;
                    buildListScenes.Add(sceneData.Name);
                }
            }

            return load;
        }

        private void CollectFailures(List<PendingLoad> loads, List<string> failures)
        {
            foreach (var load in loads)
            {
                if (!load.Failed)
                {
                    continue;
                }

                failures.Add(load.Scene.Name);
                Debug.LogError($"[SceneGroupManager] Scene '{load.Scene.Name}' failed to load: {load.FailureReason}");

                if (load.Addressable)
                {
                    loadedSceneHandles.Remove(load.Scene.Name);
                    if (load.Handle.IsValid())
                    {
                        Addressables.Release(load.Handle);
                    }
                }
                else
                {
                    buildListScenes.Remove(load.Scene.Name);
                }
            }
        }

        private static SceneData FindLastSceneToLoad(List<SceneData[]> batches, Dictionary<SceneData, bool> routes)
        {
            SceneData last = null;

            foreach (var batch in batches)
            {
                foreach (var sceneData in batch)
                {
                    if (routes.ContainsKey(sceneData))
                    {
                        last = sceneData;
                    }
                }
            }

            return last;
        }

        private static async UniTask WaitFor(
            List<PendingLoad> loads,
            IProgress<float> progress,
            int scenesLoaded,
            int batchSize,
            int totalScenes)
        {
            while (!loads.All(l => l.IsDone))
            {
                float batchProgress = loads.Count > 0 ? loads.Average(l => l.Progress) : 1f;
                float overallProgress = (scenesLoaded + batchProgress * batchSize) / totalScenes;
                progress?.Report(Mathf.Clamp01(overallProgress));
                await UniTask.Delay(100, DelayType.UnscaledDeltaTime);
            }
        }

        private static async UniTask Activate(PendingLoad load)
        {
            if (load.Addressable)
            {
                if (load.Handle.Status != AsyncOperationStatus.Succeeded)
                {
                    return;
                }

                await load.Handle.Result.ActivateAsync();
                return;
            }

            if (load.Operation == null)
            {
                return;
            }

            load.Operation.allowSceneActivation = true;
            while (!load.Operation.isDone)
            {
                await UniTask.Yield();
            }
        }
    }
}
