using MyToolz.DesignPatterns.EventBus;
using MyToolz.Interfaces;
using System;
using UnityEngine;

namespace MyToolz.Interfaces
{
    public interface IProgressReporter<T> : IProgress<T>
    {
        public event Action<T> Progressed;
    }
}

namespace MyToolz.Events
{
    public class LoadingProgress : IProgressReporter<float>
    {
        public event Action<float> Progressed;

        public void Report(float value)
        {
            Progressed?.Invoke(Mathf.Clamp01(value));
        }
    }

    public struct LoadingScreenShow : IEvent
    {
        public IProgressReporter<float> Progress;
    }

    public struct LoadingScreenHide : IEvent { }
}
