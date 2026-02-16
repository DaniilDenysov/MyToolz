using MyToolz.Clock.Interfaces;
using MyToolz.DesignPatterns.Prototype;
using System;

namespace MyToolz.Clock.Model
{
    [Serializable]
    public class ClockModel : IClockModel, IPrototype<ClockModel>
    {
        public float CurrentTime { get; set; }
        public bool IsRunning { get; set; }
        public bool IsPaused { get; set; }
        public ClockMode Mode { get; set; }
        public float StartTime { get; set; }

        public ClockModel Get()
        {
            var model = new ClockModel();
            model.Mode = Mode;
            model.StartTime = StartTime;
            model.CurrentTime = CurrentTime;
            model.IsPaused = IsPaused;
            model.IsRunning = IsRunning;
            return model;
        }
    }
}