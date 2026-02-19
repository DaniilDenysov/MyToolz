using Cysharp.Threading.Tasks;
using MyToolz.Clock.Interfaces;
using System.Threading;
using System;
using UnityEngine;
using MyToolz.Utilities.Debug;

namespace MyToolz.Clock.Presenter
{
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
        private float DeltaTime => Time.deltaTime;

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

            cts = new CancellationTokenSource();
            RunLoopAsync(cts.Token).Forget();
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
            if (model == null) DebugUtility.LogError(this, "Presenter not initialized. Call Initialize(model, view).");
            Bind();

            if (model.StartTime < 0f) model.StartTime = 0f;

            model.IsRunning = true;
            model.IsPaused = false;

            model.CurrentTime = (model.Mode == ClockMode.Countdown) ? model.StartTime : 0f;

            UpdateView();
        }

        public void Stop()
        {
            if (model == null || !model.IsRunning || model.IsPaused) return;

            model.IsRunning = false;
            model.IsPaused = false;
            UpdateView();
            Stopped?.Invoke();

            Unbind();
        }

        public void Pause()
        {
            if (model == null || model.IsPaused) return;
            model.IsPaused = true;
            Paused?.Invoke();
        }

        public void Resume()
        {
            if (model == null || !model.IsPaused) return;
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

                    Tick(DeltaTime);
                    UpdateView();
                }
            }
            catch (OperationCanceledException) { }
        }

        private void Tick(float dt)
        {
            if (model == null || !model.IsRunning || model.IsPaused) return;
            if (dt < 0f) dt = 0f;

            if (model.Mode == ClockMode.Countdown)
            {
                if (model.CurrentTime <= 0f) return;

                model.CurrentTime -= dt;

                if (model.CurrentTime <= 0f)
                {
                    model.CurrentTime = 0f;
                    model.IsRunning = false;
                    model.IsPaused = false;

                    Elapsed?.Invoke();
                }
            }
            else
            {
                model.CurrentTime += dt;
            }
        }

        private void UpdateView()
        {
            if (!bound) return;
            view?.UpdateView(model.CurrentTime);
        }
    }
}