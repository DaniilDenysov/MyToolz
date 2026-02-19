using MyToolz.Clock.Interfaces;
using MyToolz.Clock.Model;
using MyToolz.Clock.Presenter;
using MyToolz.Clock.View;
using UnityEngine;

namespace MyToolz.Clock
{
    public class ClockConfiguration : MonoBehaviour
    {
        [SerializeField] private ClockMode clockMode = ClockMode.Stopwatch;
        [SerializeReference, SubclassSelector] private ClockPresenter presenter;
        [SerializeReference, SubclassSelector] private ClockView view;
        [SerializeReference, SubclassSelector] private ClockModel model;

        private void Awake()
        {
            presenter?.Initialize(model, view);
        }

        public void SetCurrentTime(string value)
        {
            model = new();
            model.StartTime = float.Parse(value);
            model.Mode = clockMode;
            StartTimer();
        }

        public void StartTimer()
        {
            presenter?.Stop();
            presenter?.Initialize(model, view);
            presenter?.Start();
        }

        public void Stop()
        {
            presenter?.Stop();
        }

        public void Resume()
        {
            presenter?.Resume();
        }

        public void Pause()
        {
            presenter?.Pause();
        }
    }
}
