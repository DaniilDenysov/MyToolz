using Cysharp.Threading.Tasks;
using MyToolz.Clock.Interfaces;
using System.Threading;
using System;
using UnityEngine;
using MyToolz.Utilities.Debug;

namespace MyToolz.Clock.Presenter
{
    /// <summary>
    /// Transitions: Stopped --Start--> Running --Pause--> Paused --Resume--> Running.
    /// Stop works from Running and Paused. A countdown that reaches zero raises Elapsed exactly once
    /// and ends in Stopped (a zero-length countdown elapses immediately on Start). Start while
    /// running or paused restarts the clock.
    /// </summary>
    public class ClockPresenter : IClockPresenter, IDisposable
    {
        public event Action Resumed;
        public event Action Paused;
        public event Action Stopped;
        public event Action Elapsed;
        public event Action Interrupted;

        private IClockModel model;
        private IClockView view;
        private CancellationTokenSource cts;
        private bool bound;

        /// <summary>Advance with unscaled time, so the clock keeps running while Time.timeScale is 0.</summary>
        public bool UseUnscaledTime { get; set; }

        /// <summary>When false no update loop is started and the owner drives the clock with <see cref="Advance"/>.</summary>
        public bool AutoTick { get; set; } = true;

        private float DeltaTime => UseUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

        public void Initialize(IClockModel model, IClockView view = null)
        {
            if (model == null)
            {
                DebugUtility.LogError(this, "Model is missing!");
                return;
            }
            this.model = model;
            this.view = view;
        }

        private void Bind()
        {
            if (bound) return;
            bound = true;

            view?.Initialize(model.CurrentTime);
            view?.Show();
            UpdateView();

            if (AutoTick)
            {
                cts = new CancellationTokenSource();
                RunLoopAsync(cts.Token).Forget();
            }
        }

        private void Unbind()
        {
            if (!bound) return;
            bound = false;

            if (model.IsRunning) Interrupted?.Invoke();

            cts?.Cancel();
            cts?.Dispose();
            cts = null;

            view?.Hide();
            view?.Destroy(model.CurrentTime);
        }

        private UniTask NextAsync(CancellationToken ct) => UniTask.Yield(PlayerLoopTiming.Update, ct);

        public void Start()
        {
            if (model == null)
            {
                DebugUtility.LogError(this, "Presenter not initialized. Call Initialize(model, view).");
                return;
            }

            if (float.IsNaN(model.StartTime) || model.StartTime < 0f) model.StartTime = 0f;

            model.IsRunning = true;
            model.IsPaused = false;
            model.CurrentTime = (model.Mode == ClockMode.Countdown) ? model.StartTime : 0f;

            Bind();
            UpdateView();

            if (model.Mode == ClockMode.Countdown && model.CurrentTime <= 0f)
            {
                Complete();
            }
        }

        public void Stop()
        {
            if (model == null || !model.IsRunning) return;

            model.IsRunning = false;
            model.IsPaused = false;
            UpdateView();
            Stopped?.Invoke();

            Unbind();
        }

        public void Pause()
        {
            if (model == null || !model.IsRunning || model.IsPaused) return;
            model.IsPaused = true;
            Paused?.Invoke();
        }

        public void Resume()
        {
            if (model == null || !model.IsRunning || !model.IsPaused) return;
            model.IsPaused = false;
            Resumed?.Invoke();
        }

        public void Dispose() => Unbind();

        private async UniTaskVoid RunLoopAsync(CancellationToken ct)
        {
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    await NextAsync(ct);

                    Advance(DeltaTime);
                    UpdateView();
                }
            }
            catch (OperationCanceledException) { }
        }

        /// <summary>Advances a running, unpaused clock by <paramref name="dt"/> seconds.</summary>
        public void Advance(float dt)
        {
            if (model == null || !model.IsRunning || model.IsPaused) return;
            if (float.IsNaN(dt) || dt < 0f) dt = 0f;

            if (model.Mode == ClockMode.Countdown)
            {
                model.CurrentTime -= dt;

                if (model.CurrentTime <= 0f)
                {
                    Complete();
                }
            }
            else
            {
                model.CurrentTime += dt;
            }
        }

        private void Complete()
        {
            model.CurrentTime = 0f;
            model.IsRunning = false;
            model.IsPaused = false;
            UpdateView();

            Elapsed?.Invoke();

            // Nothing left to tick: stop the update loop and release the view.
            Unbind();
        }

        private void UpdateView()
        {
            if (!bound) return;
            view?.UpdateView(model.CurrentTime);
        }
    }
}
