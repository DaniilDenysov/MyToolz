using UnityEngine;
using UnityEngine.SceneManagement;

namespace MyToolz.Extensions
{
    public static class SceneExtensions
    {
        public static bool IsSceneInBuildSettings(string sceneName)
        {
            int sceneCount = SceneManager.sceneCountInBuildSettings;
            for (int i = 0; i < sceneCount; i++)
            {
                string path = SceneUtility.GetScenePathByBuildIndex(i);
                string buildSceneName = System.IO.Path.GetFileNameWithoutExtension(path);
                if (buildSceneName == sceneName)
                    return true;
            }
            return false;
        }

        public static bool IsSceneValid(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
                return false;

            if (string.IsNullOrWhiteSpace(sceneName))
                return false;

            if (!IsSceneInBuildSettings(sceneName))
                return false;

            return true;
        }

        public static string GetSceneNameByBuildIndex(int buildIndex)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(buildIndex);
            return System.IO.Path.GetFileNameWithoutExtension(path);
        }

        public static int GetBuildSceneCount()
        {
            return SceneManager.sceneCountInBuildSettings;
        }
    }
}
