using MyToolz.DesignPatterns.EventBus;
using MyToolz.Events;
using MyToolz.Interfaces;
using System;
using UnityEngine;

namespace MyToolz.UI.LoadingScreen
{
    [Serializable]
    public class SceneLoaderModel : ISceneLoaderModel
    {
        public event Action OnLoadingStarted;
        public event Action OnLoadingFinished;
        public event Action OnProgressChanged;

        public float CurrentProgress { get; private set; }
        public bool IsLoading { get; private set; }

        private EventBinding<LoadingScreenShow> onShowBinding;
        private EventBinding<LoadingScreenHide> onHideBinding;

        private IProgressReporter<float> currentProgress;

        public void RegisterEvents()
        {
            onShowBinding = new EventBinding<LoadingScreenShow>(OnLoadingScreenShow);
            EventBus<LoadingScreenShow>.Register(onShowBinding);

            onHideBinding = new EventBinding<LoadingScreenHide>(OnLoadingScreenHide);
            EventBus<LoadingScreenHide>.Register(onHideBinding);
        }

        public void UnregisterEvents()
        {
            EventBus<LoadingScreenShow>.Deregister(onShowBinding);
            EventBus<LoadingScreenHide>.Deregister(onHideBinding);

            UnsubscribeProgress();
        }

        private void OnLoadingScreenShow(LoadingScreenShow e)
        {
            UnsubscribeProgress();

            IsLoading = true;
            SetProgress(0f);
            OnLoadingStarted?.Invoke();

            if (e.Progress != null)
            {
                currentProgress = e.Progress;
                currentProgress.Progressed += OnProgressReport;
            }
        }

        private void OnLoadingScreenHide(LoadingScreenHide e)
        {
            UnsubscribeProgress();

            SetProgress(1f);
            IsLoading = false;
            OnLoadingFinished?.Invoke();
        }

        private void OnProgressReport(float value)
        {
            SetProgress(value);
        }

        private void SetProgress(float progress)
        {
            CurrentProgress = Mathf.Clamp01(progress);
            OnProgressChanged?.Invoke();
        }

        private void UnsubscribeProgress()
        {
            if (currentProgress != null)
            {
                currentProgress.Progressed -= OnProgressReport;
                currentProgress = null;
            }
        }
    }
}
