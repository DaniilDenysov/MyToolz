using MyToolz.DesignPatterns.MVP.View;
using MyToolz.EditorToolz;
using MyToolz.UI.Management;
using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MyToolz.UI.LoadingScreen
{
    [Serializable]
    public class SceneLoaderView : IReadOnlyView<ISceneLoaderModel>
    {
        [SerializeField, Required] private Camera loadingCamera;
        [SerializeField, Required] private UIScreen loadingScreen;
        [SerializeField, RequireInterface(typeof(IProgressBar))] private Object loadingBar;

        private IProgressBar ProgressBar => loadingBar as IProgressBar;
        private bool bound;

        public void Initialize(ISceneLoaderModel model)
        {
            BindCameraToScreen();
            SetProgress(0f);
        }

        public void Show()
        {
            loadingScreen?.Open();
        }

        public void Hide()
        {
            if (loadingScreen.IsActive) loadingScreen?.Close();
        }

        public void UpdateView(ISceneLoaderModel model)
        {
            SetProgress(model.CurrentProgress);
        }

        public void Destroy(ISceneLoaderModel model)
        {
            UnbindCameraFromScreen();
        }

        private void BindCameraToScreen()
        {
            if (bound || loadingScreen == null)
            {
                return;
            }

            bound = true;
            loadingScreen.OnEnterEvent.AddListener(EnableCamera);
            loadingScreen.OnExitEvent.AddListener(DisableCamera);
        }

        private void UnbindCameraFromScreen()
        {
            if (!bound || loadingScreen == null)
            {
                return;
            }

            bound = false;
            loadingScreen.OnEnterEvent.RemoveListener(EnableCamera);
            loadingScreen.OnExitEvent.RemoveListener(DisableCamera);
        }

        private void EnableCamera() => SetCameraActive(true);

        private void DisableCamera() => SetCameraActive(false);

        private void SetCameraActive(bool active)
        {
            if (loadingCamera != null)
            {
                loadingCamera.gameObject.SetActive(active);
            }
        }

        private void SetProgress(float value)
        {
            if (ProgressBar != null)
                ProgressBar.Value = value;
        }
    }
}
