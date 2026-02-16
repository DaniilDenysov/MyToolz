using Mirror;
using MyToolz.Clock.Interfaces;
using MyToolz.Clock.Model;
using MyToolz.Clock.Presenter;
using MyToolz.DesignPatterns.EventBus;
using MyToolz.Networking.Events;
using System;
using UnityEngine;

namespace MyToolz.Networking.Events
{
    public struct ClockEvent : IEvent
    {
        public bool Elapsed;
        public int Minutes;
        public int Seconds;
    }
}

namespace MyToolz.Networking.SynchronizedClock
{
    public class SyncedClock : NetworkBehaviour
    {
        [SerializeField, SyncVar(hook = nameof(OnTimeChanged))] private float currentTime;

        [SerializeField] private InterfaceReference<IClockView> clockViewReference;
        private IClockView clockView => clockViewReference.Value;
        [SerializeField] private ClockModel clockModel = new();
        private IClockPresenter clockPresenter = new ClockPresenter();

        private void Awake()
        {
            clockPresenter.Initialize(clockModel, clockView);
        }

        private void FixedUpdate()
        {
            if (isServer)
            {
                currentTime = clockModel.CurrentTime;
            }
        }

        private void OnTimeChanged(float oldValue, float newValue)
        {
            int minutes = Mathf.FloorToInt(newValue / 60);
            int oldMinutes = Mathf.FloorToInt(oldValue / 60);
            int seconds = Mathf.FloorToInt(newValue % 60);
            if (oldMinutes - minutes > 0 || newValue == 0) EventBus<ClockEvent>.Raise(new ClockEvent() { Minutes = minutes, Seconds = seconds,Elapsed = newValue == 0 });
        }

        #region Public API

        [Server]
        public void StartTimer(float timeEstimation, Action<float> onClockTick = null,Action onStopped = null, Action onElapsed = null)
        {
            clockModel.Mode = ClockMode.Countdown;
            clockPresenter.Start();
            currentTime = clockModel.CurrentTime;
        }

        [Server]
        public void StartStopWatch(Action onStopped = null)
        {
            clockModel.Mode = ClockMode.Stopwatch;
            clockPresenter.Start();
            currentTime = clockModel.CurrentTime;
        }

        [Server]
        public void StopTimer()
        {
            clockPresenter?.Stop();
        }

        [Server]
        public void ResumeTimer()
        {
            clockPresenter?.Resume();
        }

        public bool IsRunning()
        {
            return clockModel.IsRunning;
        }
        #endregion

        #region Callbacks
        private void UpdateTimeCallback(float newTime)
        {
            currentTime = newTime;
        }

        private void TimerStoppedCallback()
        {
            Debug.Log("Timer has stopped.");
        }
        #endregion

    }
}
