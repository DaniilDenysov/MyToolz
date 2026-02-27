using UnityEngine;
using MyToolz.Extensions;
using MyToolz.Utilities.Debug;
using MyToolz.DesignPatterns.EventBus;
using MyToolz.Events;
using MyToolz.EditorToolz;
using TMPro;

namespace MyToolz.Demo
{
    public class SceneLabel : MonoBehaviour
    {
        [SerializeField, Required] private TMP_Text display;
        [SerializeField] private string sceneName;
        private bool isInitialized => SceneExtensions.IsSceneValid(sceneName);

        public void Initialize(string sceneName)
        {
            if (isInitialized)
            {
                DebugUtility.LogWarning(this, $"'{gameObject.name}' has already been initialized with scene '{this.sceneName}'. Ignoring reinitialization.");
                return;
            }

            if (!SceneExtensions.IsSceneValid(sceneName))
            {
                DebugUtility.LogError(this, $"'{gameObject.name}' received an invalid scene name: '{sceneName}'.");
                return;
            }

            this.sceneName = sceneName;
            display.SetText(sceneName);
        }

        public void LoadScene()
        {
            if (!isInitialized)
            {
                DebugUtility.LogError(this, $"'{gameObject.name}' hasn't already been initialized!");
                return;
            }

            EventBus<LoadScene>.Raise(new LoadScene()
            {
                SceneName = sceneName,
                LoadSceneMode = UnityEngine.SceneManagement.LoadSceneMode.Single
            });
        }
    }
}