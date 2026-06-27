using Cysharp.Threading.Tasks;
using MyToolz.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MyToolz.SceneManagement
{
    [Serializable]
    public class SceneGroupManager
    {
        [SerializeField] private List<string> blacklist = new();

        private SceneGroupSO ActiveSceneGroup;

        public async UniTask LoadScenes(SceneGroupSO sceneGroupSO, IProgress<float> progress, bool reloadDupScenes = false)
        {
            ActiveSceneGroup = sceneGroupSO;
            var loadedScenes = new List<string>();

            await UnloadScenes();

            int sceneCount = SceneManager.sceneCount;

            for (var i = 0; i < sceneCount; i++)
            {
                loadedScenes.Add(SceneManager.GetSceneAt(i).name);
            }

            var batches = ActiveSceneGroup.GetBatchedByPriority();
            int totalScenes = ActiveSceneGroup.Scenes.Length;
            int scenesLoaded = 0;

            for (int b = 0; b < batches.Count; b++)
            {
                var batch = batches[b];
                var operationGroup = new AsyncOperationGroup(batch.Length);

                for (var i = 0; i < batch.Length; i++)
                {
                    var sceneData = batch[i];
                    if (!reloadDupScenes && loadedScenes.Contains(sceneData.Name))
                    {
                        scenesLoaded++;
                        continue;
                    }

                    var operation = SceneManager.LoadSceneAsync(sceneData.Reference.Path, LoadSceneMode.Additive);
                    operationGroup.Operations.Add(operation);
                }

                while (!operationGroup.IsDone)
                {
                    float batchProgress = operationGroup.Operations.Count > 0 ? operationGroup.Progress : 1f;
                    float overallProgress = (scenesLoaded + batchProgress * batch.Length) / totalScenes;
                    progress?.Report(Mathf.Clamp01(overallProgress));
                    await UniTask.Delay(100);
                }

                scenesLoaded += batch.Length;
                progress?.Report(Mathf.Clamp01((float)scenesLoaded / totalScenes));
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
        }

        public async UniTask UnloadScenes()
        {
            Scene bootstrapperScene = SceneManager.GetSceneAt(0);
            if (bootstrapperScene.IsValid() && bootstrapperScene.isLoaded)
            {
                SceneManager.SetActiveScene(bootstrapperScene);
            }

            var scenes = new List<string>();
            var activeScene = SceneManager.GetActiveScene().name;
            int sceneCount = SceneManager.sceneCount;

            for (var i = sceneCount - 1; i > 0; i--)
            {
                var sceneAt = SceneManager.GetSceneAt(i);
                if (!sceneAt.isLoaded) continue;

                var sceneName = sceneAt.name;
                if (sceneName.Equals(activeScene) || blacklist.Contains(sceneName) || !SceneExtensions.IsSceneValid(sceneName)) continue;
                scenes.Add(sceneName);
            }

            var operationGroup = new AsyncOperationGroup(scenes.Count);

            foreach (var scene in scenes)
            {
                var operation = SceneManager.UnloadSceneAsync(scene);
                if (operation == null) continue;
                operationGroup.Operations.Add(operation);
            }

            while (!operationGroup.IsDone)
            {
                await UniTask.Delay(100);
            }

            await Resources.UnloadUnusedAssets();
        }
    }
}
