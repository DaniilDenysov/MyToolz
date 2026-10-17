using MyToolz.Clock.Interfaces;
using MyToolz.Clock.Model;
using MyToolz.Clock.Presenter;
using NUnit.Framework;

namespace MyToolz.Tests.EditMode
{
    public class ClockPresenterTests : SilentLogTest
    {
        private ClockModel model;
        private ClockPresenter presenter;
        private int elapsed, stopped, paused, resumed;

        private void Create(ClockMode mode, float startTime)
        {
            model = new ClockModel { Mode = mode, StartTime = startTime };
            presenter = new ClockPresenter { AutoTick = false };
            presenter.Initialize(model);
            presenter.Elapsed += () => elapsed++;
            presenter.Stopped += () => stopped++;
            presenter.Paused += () => paused++;
            presenter.Resumed += () => resumed++;
        }

        [SetUp]
        public void ResetCounters() => elapsed = stopped = paused = resumed = 0;

        [Test]
        public void Countdown_ElapsesExactlyOnce()
        {
            Create(ClockMode.Countdown, 2f);
            presenter.Start();

            presenter.Advance(1.5f);
            presenter.Advance(1.5f);
            presenter.Advance(1.5f);

            Assert.AreEqual(1, elapsed);
            Assert.IsFalse(model.IsRunning);
            Assert.AreEqual(0f, model.CurrentTime);
        }

        [Test]
        public void ZeroLengthCountdown_ElapsesImmediately()
        {
            Create(ClockMode.Countdown, 0f);

            presenter.Start();

            Assert.AreEqual(1, elapsed);
            Assert.IsFalse(model.IsRunning, "a zero countdown must not stay running forever");
        }

        [Test]
        public void PausedClock_CanBeStopped()
        {
            Create(ClockMode.Stopwatch, 0f);
            presenter.Start();
            presenter.Pause();

            presenter.Stop();

            Assert.AreEqual(1, stopped);
            Assert.IsFalse(model.IsRunning);
            Assert.IsFalse(model.IsPaused);
        }

        [Test]
        public void StoppedClock_CannotBePausedOrResumed()
        {
            Create(ClockMode.Stopwatch, 0f);

            presenter.Pause();
            presenter.Resume();

            Assert.AreEqual(0, paused);
            Assert.AreEqual(0, resumed);
            Assert.IsFalse(model.IsPaused);
        }

        [Test]
        public void PausedClock_DoesNotAdvance_UntilResumed()
        {
            Create(ClockMode.Stopwatch, 0f);
            presenter.Start();
            presenter.Advance(1f);
            presenter.Pause();
            presenter.Advance(5f);
            presenter.Resume();
            presenter.Advance(1f);

            Assert.AreEqual(2f, model.CurrentTime, 1e-5);
            Assert.AreEqual(1, paused);
            Assert.AreEqual(1, resumed);
        }

        [Test]
        public void Restart_ResetsTheCountdown()
        {
            Create(ClockMode.Countdown, 3f);
            presenter.Start();
            presenter.Advance(2f);

            presenter.Start();

            Assert.AreEqual(3f, model.CurrentTime, 1e-5);
            Assert.IsTrue(model.IsRunning);
        }
    }
}
