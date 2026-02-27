using MyToolz.DesignPatterns.MVP.View;
using MyToolz.EditorToolz;
using MyToolz.UI.Management;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace MyToolz.UI.LoadingScreen
{
    [Serializable]
    public class SceneLoaderView : IReadOnlyView<ISceneLoaderModel>
    {
        [SerializeField, Required] private UIScreen loadingScreen;
        [SerializeField, Required] private Slider loadingBar;

        public void Initialize(ISceneLoaderModel model)
        {
            loadingBar.value = 0f;
        }

        public void Show()
        {
            loadingScreen?.Open();
        }

        public void Hide()
        {
            loadingScreen?.Close();
        }

        public void UpdateView(ISceneLoaderModel model)
        {
            loadingBar.value = model.CurrentProgress;
        }

        public void Destroy(ISceneLoaderModel model)
        {
        }
    }
}
