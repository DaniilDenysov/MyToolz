#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using MyToolz.SceneManagement;
using UnityEditor;
using UnityEditor.Overlays;
using UnityEditor.SceneManagement;
using UnityEditor.Toolbars;
using UnityEngine;

namespace MyToolz.Bootstrap.Editor
{
    /// <summary>
    /// Main-toolbar controls for playing through the boot scene:
    /// <list type="bullet">
    /// <item><b>Boot</b> plays from the Bootstrapper scene with the default flow.</item>
    /// <item><b>Boot Scene</b> plays from the Bootstrapper scene but starts with the <see cref="SceneGroupSO"/> that
    /// contains the scene being edited (read by <see cref="MultiSceneLoader"/>).</item>
    /// <item><b>Scenes</b> opens any scene listed in a <see cref="SceneGroupSO"/>, grouped by scene group.</item>
    /// </list>
    /// Leaving play mode reopens the scene that was being edited.
    /// </summary>
    public static class BootstrapButton
    {
        private const string BootstrapperSceneName = "Bootstrapper";
        private const string LaunchedKey = "BootstrapButton_Launched";
        private const string PreviousSceneKey = "BootstrapButton_PreviousScene";
        private const string BootElementPath = "MyToolz/BootLauncher";
        private const string BootSceneElementPath = "MyToolz/BootCurrentScene";
        private const string SceneNavigatorElementPath = "MyToolz/SceneNavigator";
        private const string SetupHintKey = "BootstrapButton_HintShown";
        private const int ShowRetries = 3;

        private static readonly string[] ManagedElementPaths =
        {
            BootElementPath,
            BootSceneElementPath,
            SceneNavigatorElementPath,
        };

        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.delayCall += () => EnsureDefaultElementsVisible(ShowRetries);
        }

        [MainToolbarElement(BootElementPath, defaultDockPosition = MainToolbarDockPosition.Middle, defaultDockIndex = -1000)]
        public static MainToolbarElement CreateBootButton()
        {
            Texture2D icon = EditorGUIUtility.IconContent("PlayButton").image as Texture2D;
            MainToolbarContent content = new MainToolbarContent("Boot", icon, "Play from the " + BootstrapperSceneName + " scene (default flow)");
            return new MainToolbarButton(content, LaunchDefault);
        }

        [MainToolbarElement(BootSceneElementPath, defaultDockPosition = MainToolbarDockPosition.Middle, defaultDockIndex = -999)]
        public static MainToolbarElement CreateBootCurrentSceneButton()
        {
            Texture2D icon = EditorGUIUtility.IconContent("PlayButton On").image as Texture2D;
            MainToolbarContent content = new MainToolbarContent("Boot Scene", icon, "Boot the active scene's " + nameof(SceneGroupSO) + " through the full " + BootstrapperSceneName + " flow");
            return new MainToolbarButton(content, LaunchCurrentScene);
        }

        [MainToolbarElement(SceneNavigatorElementPath, defaultDockPosition = MainToolbarDockPosition.Middle, defaultDockIndex = -998)]
        public static MainToolbarElement CreateSceneNavigator()
        {
            Texture2D icon = EditorGUIUtility.IconContent("SceneAsset Icon").image as Texture2D;
            MainToolbarContent content = new MainToolbarContent("Scenes", icon, "Open a scene from any " + nameof(SceneGroupSO));
            return new MainToolbarDropdown(content, ShowSceneMenu);
        }

        // New toolbar elements start hidden. Show each one once; the user can hide it again from the toolbar menu.
        // MainToolbar.TryGetOverlay is internal, so this falls back to a hint when the API is not there.
        private static void EnsureDefaultElementsVisible(int retriesLeft)
        {
            bool allResolved = true;

            try
            {
                Type mainToolbar = typeof(MainToolbarContent).Assembly.GetType("UnityEditor.Toolbars.MainToolbar");
                MethodInfo tryGetOverlay = mainToolbar?.GetMethod("TryGetOverlay", BindingFlags.NonPublic | BindingFlags.Static);
                MethodInfo refresh = mainToolbar?.GetMethod("Refresh", BindingFlags.Public | BindingFlags.Static);

                if (tryGetOverlay == null)
                {
                    ShowManualEnableHint();
                    return;
                }

                foreach (string path in ManagedElementPaths)
                {
                    string shownKey = path + "_autoShown";
                    if (EditorPrefs.GetBool(shownKey, false))
                    {
                        continue;
                    }

                    object[] args = { path, null };
                    bool found = (bool)tryGetOverlay.Invoke(null, args);
                    if (!found || args[1] is not Overlay overlay)
                    {
                        allResolved = false;
                        continue;
                    }

                    overlay.displayed = true;
                    EditorPrefs.SetBool(shownKey, true);
                    refresh?.Invoke(null, new object[] { path });
                }
            }
            catch (Exception)
            {
                ShowManualEnableHint();
                return;
            }

            if (!allResolved && retriesLeft > 0)
            {
                EditorApplication.delayCall += () => EnsureDefaultElementsVisible(retriesLeft - 1);
            }
            else if (!allResolved)
            {
                ShowManualEnableHint();
            }
        }

        private static void ShowManualEnableHint()
        {
            if (SessionState.GetBool(SetupHintKey, false))
            {
                return;
            }

            SessionState.SetBool(SetupHintKey, true);
            Debug.Log("[BootstrapButton] If the Boot / Boot Scene / Scenes controls are not visible, right-click the main toolbar → \"MyToolz\" → enable \"BootLauncher\", \"BootCurrentScene\" and \"SceneNavigator\". This is a one-time setup.");
        }

        private static void LaunchDefault() => Launch(null);

        private static void LaunchCurrentScene()
        {
            if (EditorApplication.isPlaying)
            {
                return;
            }

            UnityEngine.SceneManagement.Scene activeScene = EditorSceneManager.GetActiveScene();
            string sceneGroupPath = FindSceneGroupPathForScene(activeScene.path);
            if (string.IsNullOrEmpty(sceneGroupPath))
            {
                EditorUtility.DisplayDialog(
                    "No Scene Group Found",
                    "The active scene '" + activeScene.name + "' is not part of any " + nameof(SceneGroupSO) + ".\n\n" +
                    "Add it to a scene group, or use the Boot button to launch the default flow.",
                    "OK");
                return;
            }

            Launch(sceneGroupPath);
        }

        private static void Launch(string sceneGroupOverridePath)
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

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            SessionState.SetString(PreviousSceneKey, EditorSceneManager.GetActiveScene().path);
            SessionState.SetBool(LaunchedKey, true);

            if (string.IsNullOrEmpty(sceneGroupOverridePath))
            {
                SessionState.EraseString(MultiSceneLoader.EditorSceneGroupOverrideKey);
            }
            else
            {
                SessionState.SetString(MultiSceneLoader.EditorSceneGroupOverrideKey, sceneGroupOverridePath);
            }

            EditorSceneManager.OpenScene(bootstrapperPath);
            EditorApplication.EnterPlaymode();
        }

        private static void ShowSceneMenu(Rect rect)
        {
            GenericMenu menu = new GenericMenu();

            string bootstrapperPath = FindBootstrapperScene();
            if (!string.IsNullOrEmpty(bootstrapperPath))
            {
                menu.AddItem(new GUIContent(BootstrapperSceneName), false, () => OpenScene(bootstrapperPath));
                menu.AddSeparator(string.Empty);
            }

            string activeScenePath = EditorSceneManager.GetActiveScene().path;
            bool anyScene = false;

            IOrderedEnumerable<SceneGroupSO> groups = AssetDatabase.FindAssets("t:" + nameof(SceneGroupSO))
                .Select(guid => AssetDatabase.LoadAssetAtPath<SceneGroupSO>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(group => group != null)
                .OrderBy(GroupLabel);

            foreach (SceneGroupSO group in groups)
            {
                if (group.Scenes == null)
                {
                    continue;
                }

                string groupName = GroupLabel(group);

                foreach (SceneData scene in group.Scenes)
                {
                    string scenePath = ResolveScenePath(scene);
                    if (string.IsNullOrEmpty(scenePath))
                    {
                        continue;
                    }

                    string sceneName = Path.GetFileNameWithoutExtension(scenePath);
                    string label = groupName + "/" + sceneName + "  (" + scene.SceneType + ")";
                    bool isActive = scenePath == activeScenePath;
                    string captured = scenePath;
                    menu.AddItem(new GUIContent(label), isActive, () => OpenScene(captured));
                    anyScene = true;
                }
            }

            if (!anyScene)
            {
                menu.AddDisabledItem(new GUIContent("No scenes found in any " + nameof(SceneGroupSO)));
            }

            menu.DropDown(rect);
        }

        private static string GroupLabel(SceneGroupSO group) => string.IsNullOrEmpty(group.GroupName) ? group.name : group.GroupName;

        private static void OpenScene(string scenePath)
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("Cannot Open Scene", "Exit Play Mode before switching scenes.", "OK");
                return;
            }

            if (string.IsNullOrEmpty(scenePath))
            {
                return;
            }

            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorSceneManager.OpenScene(scenePath);
            }
        }

        /// <summary>
        /// The scene group to boot for <paramref name="scenePath"/>: one where the scene is the
        /// <see cref="SceneType.ActiveScene"/>, otherwise the first group that lists it.
        /// </summary>
        private static string FindSceneGroupPathForScene(string scenePath)
        {
            if (string.IsNullOrEmpty(scenePath))
            {
                return null;
            }

            string sceneGuid = AssetDatabase.AssetPathToGUID(scenePath);
            if (string.IsNullOrEmpty(sceneGuid))
            {
                return null;
            }

            string containingGroupPath = null;

            foreach (string groupGuid in AssetDatabase.FindAssets("t:" + nameof(SceneGroupSO)))
            {
                string groupPath = AssetDatabase.GUIDToAssetPath(groupGuid);
                SceneGroupSO group = AssetDatabase.LoadAssetAtPath<SceneGroupSO>(groupPath);
                if (group == null || group.Scenes == null)
                {
                    continue;
                }

                foreach (SceneData scene in group.Scenes)
                {
                    if (ResolveSceneGuid(scene) != sceneGuid)
                    {
                        continue;
                    }

                    if (scene.SceneType == SceneType.ActiveScene)
                    {
                        return groupPath;
                    }

                    containingGroupPath ??= groupPath;
                }
            }

            return containingGroupPath;
        }

        // SceneReference caches the GUID on serialization; fall back to its path for assets saved before it did.
        private static string ResolveSceneGuid(SceneData scene)
        {
            SceneReference reference = scene?.Reference;
            if (reference == null)
            {
                return null;
            }

            if (!string.IsNullOrEmpty(reference.Guid))
            {
                return reference.Guid;
            }

            return string.IsNullOrEmpty(reference.Path) ? null : AssetDatabase.AssetPathToGUID(reference.Path);
        }

        private static string ResolveScenePath(SceneData scene)
        {
            string guid = ResolveSceneGuid(scene);
            return string.IsNullOrEmpty(guid) ? null : AssetDatabase.GUIDToAssetPath(guid);
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
            SessionState.EraseString(MultiSceneLoader.EditorSceneGroupOverrideKey);

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
