using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MyToolz.SceneManagement
{
    /// <summary>
    /// Serializable reference to a scene that survives player builds. In the editor it
    /// is authored by assigning a <see cref="UnityEditor.SceneAsset"/>; the resolved
    /// asset path and name are cached into plain strings so they remain available at
    /// runtime where <c>SceneAsset</c> does not exist.
    /// </summary>
    [Serializable]
    public class SceneReference : ISerializationCallbackReceiver
    {
#if UNITY_EDITOR
        [SerializeField] private SceneAsset sceneAsset;
#endif
        [SerializeField] private string scenePath;
        [SerializeField] private string sceneName;

        /// <summary>Project-relative asset path, e.g. "Assets/Scenes/Main.unity".</summary>
        public string Path => scenePath;

        /// <summary>Scene name without extension, e.g. "Main".</summary>
        public string Name => sceneName;

        /// <summary>True when a scene has been assigned.</summary>
        public bool IsAssigned => !string.IsNullOrEmpty(scenePath);

        public void OnBeforeSerialize()
        {
#if UNITY_EDITOR
            if (sceneAsset != null)
            {
                scenePath = AssetDatabase.GetAssetPath(sceneAsset);
                sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
            }
            else
            {
                scenePath = string.Empty;
                sceneName = string.Empty;
            }
#endif
        }

        public void OnAfterDeserialize() { }
    }
}
