using MyToolz.DesignPatterns.EventBus;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MyToolz.Events
{
    public struct LoadScene : IEvent
    {
        public string SceneName;
        public LoadSceneMode LoadSceneMode;
    }

    public struct SceneLoading : IEvent
    {
        public string SceneName;
        public AsyncOperation AsyncOperation;
    }

    public struct SceneLoaded : IEvent
    {
        public string SceneName;
    }
}
