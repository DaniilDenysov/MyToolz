using Mirror;
using MyToolz.DesignPatterns.EventBus;
using MyToolz.DesignPatterns.Singleton;
using MyToolz.EditorToolz;
using MyToolz.Extensions;
using MyToolz.HealthSystem.Interfaces;
using MyToolz.Networking.ScriptableObjects;
using MyToolz.UI.Events;
using MyToolz.UI.Management;
using MyToolz.Utilities.Debug;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MyToolz.UI.Events
{
    public struct StartedSceneLoading : IEvent
    {
        public string SceneName;
        public GameModeSO GameModeSO;
    }

    public struct SceneLoading : IEvent
    {
        public string SceneName;
        public AsyncOperation AsyncOperation;
        public SceneOperation SceneOperation;
    }

    public struct SceneLoaded : IEvent
    {

    }
}

namespace MyToolz.UI.LoadingScreen
{
    public class LoadingScreenPresenter : Singleton<LoadingScreenPresenter>, IEventListener
    {
        [SerializeField, Required] private UIScreen loadingScreen;
        [SerializeField, Required] private Slider loadingBar;
        [SerializeField, Required] private TMP_Text gameModeNameDisplay;
        [SerializeField, Required] private TMP_Text gameModeDescriptionDisplay;
        [SerializeField, Required] private TMP_Text mapNameDisplay;
        [SerializeField, Required] private Image minimapDisplay;
        [SerializeField, Required] private Image backgroundImage;

        [SerializeField] private Sprite[] backgroundScreens;

        private EventBinding<StartedSceneLoading> onSceneStartedLoadingBinding;
        private EventBinding<SceneLoading> onSceneLoadingBinding;
        private EventBinding<SceneLoaded> onSceneLoadedBinding;

        private void Start()
        {
            RegisterEvents();
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            UnregisterEvents();
        }

        private void StartLoading(StartedSceneLoading loadingScene)
        {
            string scene = UIUtilities.ExtractSceneName(loadingScene.SceneName);
            if (SceneManager.GetActiveScene().name == scene) return;
            backgroundImage.sprite = backgroundScreens[Random.Range(0, backgroundScreens.Length)];
            var gameModeSO = loadingScene.GameModeSO;
            DebugUtility.Log(this, "Started looading scene!");
            if (gameModeSO != null)
            {
                gameModeNameDisplay.text = gameModeSO.Title.ToUpper();
                gameModeDescriptionDisplay.text = gameModeSO.Description;
                mapNameDisplay.text = scene.ToUpper();
                //TODO: [DD] add minimap display
                //minimapDisplay.sprite = 
            }
            else
            {
                DebugUtility.LogError(this, "Couldn't load game mode!");
            }
            loadingBar.value = 0f;
            loadingScreen.Open();
        }

        private async void Loading(SceneLoading loadingScene)
        {
            var assop = loadingScene.AsyncOperation;
            do
            {
                await Task.Delay(100);
                DebugUtility.Log(this, "Looading...");
                float progress = Mathf.Clamp01(assop.progress / 0.9f);
                loadingBar.value = progress;
            } while (assop.progress < 0.9f);
        }

        private void StopLoading(SceneLoaded sceneLoaded)
        {
            DebugUtility.Log(this, "Ended loading scene!");
            loadingScreen.Close();
        }

        public void RegisterEvents()
        {
            onSceneStartedLoadingBinding = new EventBinding<StartedSceneLoading>(StartLoading);
            EventBus<StartedSceneLoading>.Register(onSceneStartedLoadingBinding);
            onSceneLoadingBinding = new EventBinding<SceneLoading>(Loading);
            EventBus<SceneLoading>.Register(onSceneLoadingBinding);
            onSceneLoadedBinding = new EventBinding<SceneLoaded>(StopLoading);
            EventBus<SceneLoaded>.Register(onSceneLoadedBinding);
        }

        public void UnregisterEvents()
        {
            EventBus<StartedSceneLoading>.Deregister(onSceneStartedLoadingBinding);
            EventBus<SceneLoading>.Deregister(onSceneLoadingBinding);
            EventBus<SceneLoaded>.Deregister(onSceneLoadedBinding);
        }
    }
}