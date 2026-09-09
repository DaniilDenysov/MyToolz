using MyToolz.InputManagement;
using NUnit.Framework;

namespace MyToolz.Tests.EditMode
{
    /// <summary>Recording <see cref="IPlayerInputState"/> double.</summary>
    internal sealed class FakePlayerInputState : IPlayerInputState
    {
        public int EnterCount { get; private set; }
        public int ExitCount { get; private set; }
        public bool IsActive { get; private set; }

        public void OnEnter() { EnterCount++; IsActive = true; }
        public void OnExit() { ExitCount++; IsActive = false; }
    }

    public class InputStateManagerTests : SilentLogTest
    {
        private InputStateManager _manager;

        [SetUp]
        public void CreateManager() => _manager = new InputStateManager();

        [Test]
        public void CurrentState_IsNull_Initially()
        {
            Assert.IsNull(_manager.CurrentState);
        }

        [Test]
        public void ChangeState_EntersState_AndBecomesCurrent()
        {
            var state = new FakePlayerInputState();

            _manager.ChangeState(state);

            Assert.AreSame(state, _manager.CurrentState);
            Assert.IsTrue(state.IsActive);
            Assert.AreEqual(1, state.EnterCount);
        }

        [Test]
        public void ChangeState_ExitsPreviousState_ThenEntersNew()
        {
            var a = new FakePlayerInputState();
            var b = new FakePlayerInputState();

            _manager.ChangeState(a);
            _manager.ChangeState(b);

            Assert.AreEqual(1, a.ExitCount);
            Assert.IsFalse(a.IsActive);
            Assert.IsTrue(b.IsActive);
            Assert.AreSame(b, _manager.CurrentState);
        }

        [Test]
        public void ChangeState_ToSameState_IsIgnored()
        {
            var state = new FakePlayerInputState();

            _manager.ChangeState(state);
            _manager.ChangeState(state);

            Assert.AreEqual(1, state.EnterCount, "changing to the current state must be a no-op");
            Assert.AreEqual(0, state.ExitCount);
        }

        [Test]
        public void ChangeState_Null_IsIgnored_AndDoesNotThrow()
        {
            var state = new FakePlayerInputState();
            _manager.ChangeState(state);

            Assert.DoesNotThrow(() => _manager.ChangeState(null));
            Assert.AreSame(state, _manager.CurrentState, "a null transition leaves the current state untouched");
        }

        [Test]
        public void OnStateChanged_FiresWithPreviousAndNextState()
        {
            IPlayerInputState reportedPrevious = null;
            IPlayerInputState reportedNext = null;
            int raised = 0;

            _manager.OnStateChanged += (prev, next) =>
            {
                reportedPrevious = prev;
                reportedNext = next;
                raised++;
            };

            var a = new FakePlayerInputState();
            var b = new FakePlayerInputState();

            _manager.ChangeState(a);
            Assert.AreEqual(1, raised);
            Assert.IsNull(reportedPrevious, "first transition has no previous state");
            Assert.AreSame(a, reportedNext);

            _manager.ChangeState(b);
            Assert.AreEqual(2, raised);
            Assert.AreSame(a, reportedPrevious);
            Assert.AreSame(b, reportedNext);
        }
    }
}
