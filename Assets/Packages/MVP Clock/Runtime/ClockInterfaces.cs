using System;
using MyToolz.DesignPatterns.MVP.View;

namespace MyToolz.Clock.Interfaces
{
    public interface IClockView : IReadOnlyView<float> 
    {
        
    }

    public enum ClockMode
    {
        Countdown,
        Stopwatch
    }

    public interface IClockModel
    {
        public float CurrentTime { get; set; }
        public bool IsRunning { get; set; }
        public bool IsPaused { get; set; }
        public ClockMode Mode { get; set; }

        public float StartTime { get; set; }
    }

    public interface IClockPresenter
    {
        public event Action Resumed;
        public event Action Paused;
        public event Action Stopped;
        public event Action Elapsed;
        public event Action Interrupted;
        public void Initialize(IClockModel model, IClockView view = null);
        public void Start();
        public void Stop();
        public void Pause();
        public void Resume();
    }
}
