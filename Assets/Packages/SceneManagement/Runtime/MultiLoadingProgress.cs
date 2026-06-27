using Cysharp.Threading.Tasks;
using MyToolz.DesignPatterns.EventBus;
using MyToolz.Events;
using MyToolz.Interfaces;
using MyToolz.Utilities.Debug;
using System.Collections.Generic;
using System;
using UnityEngine;

namespace MyToolz.SceneManagement
{
    public class MultiLoadingProgress : IProgressReporter<float>, IDisposable
    {
        public event Action<float> Progressed;

        private readonly List<LoadingProgress> childProgresses;
        private readonly Action<float>[] childHandlers;
        private readonly float[] childValues;

        public MultiLoadingProgress(List<LoadingProgress> loadingProgresses)
        {
            childProgresses = loadingProgresses;
            childValues = new float[loadingProgresses.Count];
            childHandlers = new Action<float>[loadingProgresses.Count];

            for (int i = 0; i < loadingProgresses.Count; i++)
            {
                int index = i;
                childHandlers[i] = value => OnChildProgressed(index, value);
                loadingProgresses[i].Progressed += childHandlers[i];
            }
        }

        public void Report(float value)
        {
            Progressed?.Invoke(Mathf.Clamp01(value));
        }

        public void Dispose()
        {
            for (int i = 0; i < childProgresses.Count; i++)
                childProgresses[i].Progressed -= childHandlers[i];
        }

        private void OnChildProgressed(int index, float value)
        {
            childValues[index] = Mathf.Clamp01(value);

            float total = 0f;
            for (int i = 0; i < childValues.Length; i++)
                total += childValues[i];

            Report(total / childValues.Length);
        }
    }
}
