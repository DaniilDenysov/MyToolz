using System.Collections.Generic;
using UnityEngine;
using MyToolz.Extensions;
using MyToolz.Utilities.Debug;
using MyToolz.EditorToolz;

namespace MyToolz.Demo
{
    public class DemoManager : MonoBehaviour
    {
        [SerializeField, Required] private SceneLabel sceneLabelPrefab;
        [SerializeField, Required] private Transform container;

        void Start()
        {
            if (container == null)
            {
                DebugUtility.LogError(this, "Container transform is not assigned.");
                return;
            }

            int sceneCount = SceneExtensions.GetBuildSceneCount();

            if (sceneCount == 0)
            {
                DebugUtility.LogWarning(this, "No scenes found in Build Settings.");
                return;
            }

            List<string> validSceneNames = new List<string>();

            for (int i = 0; i < sceneCount; i++)
            {
                string sceneName = SceneExtensions.GetSceneNameByBuildIndex(i);

                if (!SceneExtensions.IsSceneValid(sceneName))
                {
                    DebugUtility.LogWarning(this, $"Skipping invalid scene at build index {i}.");
                    continue;
                }

                validSceneNames.Add(sceneName);
            }

            validSceneNames.Sort(System.StringComparer.OrdinalIgnoreCase);

            foreach (string sceneName in validSceneNames)
            {
                var label = Instantiate(sceneLabelPrefab, container);
                label.Initialize(sceneName);
            }
        }
    }
}