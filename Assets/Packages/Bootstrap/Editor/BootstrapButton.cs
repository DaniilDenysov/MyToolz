#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Toolbars;
using UnityEngine;

namespace MyToolz.Bootstrap.Editor
{
    public static class BootstrapButton
    {
        private const string BootstrapperSceneName = "Bootstrapper";
        private const string LaunchedKey = "BootstrapButton_Launched";
        private const string PreviousSceneKey = "BootstrapButton_PreviousScene";
        private const string ElementPath = "MyToolz/BootLauncher";
        private const string SetupHintKey = "BootstrapButton_HintShown";

        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

            if (!SessionState.GetBool(SetupHintKey, false))
            {
                SessionState.SetBool(SetupHintKey, true);
                EditorApplication.delayCall += ShowSetupHintIfNeeded;
            }
        }

        private static void ShowSetupHintIfNeeded()
        {
            if (EditorPrefs.GetBool(ElementPath + "_enabled", false))
            {
                return;
            }

            Debug.Log("[BootstrapButton] If the Boot button is not visible, right-click the toolbar \u2192 enable \"MyToolz\" \u2192 \"BootLauncher\". This is a one-time setup.");
        }

        [MainToolbarElement(ElementPath, defaultDockPosition = MainToolbarDockPosition.Middle, defaultDockIndex = -1000)]
        public static MainToolbarElement CreateBootButton()
        {
            EditorPrefs.SetBool(ElementPath + "_enabled", true);
            Texture2D icon = EditorGUIUtility.IconContent("PlayButton").image as Texture2D;
            MainToolbarContent content = new MainToolbarContent("Boot", icon, "Play from " + BootstrapperSceneName + " scene");
            return new MainToolbarButton(content, LaunchBootstrapper);
        }

        private static void LaunchBootstrapper()
        {
            if (EditorApplication.isPlaying)
            {
                return;
            }

            string bootstrapperPath = FindBootstrapperScene();
            if (string.IsNullOrEmpty(bootstrapperPath))
            {
                EditorUtility.DisplayDialog(
                    BootstrapperSceneName + " Not Found",
                    "Could not find a scene named '" + BootstrapperSceneName + "' in the project.",
                    "OK");
                return;
            }

            if (EditorSceneManager.GetActiveScene().isDirty)
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    return;
                }
            }

            SessionState.SetString(PreviousSceneKey, EditorSceneManager.GetActiveScene().path);
            SessionState.SetBool(LaunchedKey, true);

            EditorSceneManager.OpenScene(bootstrapperPath);
            EditorApplication.EnterPlaymode();
        }

        private static string FindBootstrapperScene()
        {
            string[] guids = AssetDatabase.FindAssets(BootstrapperSceneName + " t:Scene");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.EndsWith(BootstrapperSceneName + ".unity", StringComparison.OrdinalIgnoreCase))
                {
                    return path;
                }
            }
            return null;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredEditMode)
            {
                return;
            }

            if (!SessionState.GetBool(LaunchedKey, false))
            {
                return;
            }

            SessionState.SetBool(LaunchedKey, false);

            string previousScene = SessionState.GetString(PreviousSceneKey, string.Empty);
            if (!string.IsNullOrEmpty(previousScene))
            {
                EditorSceneManager.OpenScene(previousScene);
                SessionState.EraseString(PreviousSceneKey);
            }
        }
    }
}
#endif
