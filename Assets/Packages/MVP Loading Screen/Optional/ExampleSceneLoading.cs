using MyToolz.DesignPatterns.EventBus;
using MyToolz.EditorToolz;
using MyToolz.Events;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MyToolz.UI.LoadingScreen.Demo
{
    public class ExampleSceneLoading : MonoBehaviour
    {
        [SerializeField] private string scene;
        [SerializeField] private LoadSceneMode loadSceneMode = LoadSceneMode.Single;

        [Button]
        public void LoadScene()
        {
            EventBus<LoadScene>.Raise(new LoadScene()
            {
                SceneName = scene,
                LoadSceneMode = loadSceneMode
            });
        }
    }
}

