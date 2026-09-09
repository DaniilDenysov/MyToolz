using MyToolz.UI.Management;
using NUnit.Framework;

namespace MyToolz.Tests.EditMode
{
    public class UIStateManagerTests : SilentLogTest
    {
        private UIStateManager _manager;

        [SetUp]
        public void SetUp() => _manager = new UIStateManager();

        [Test]
        public void CurrentState_IsNull_Initially()
        {
            Assert.IsNull(_manager.CurrentState);
        }

        [Test]
        public void ChangeState_EntersNewState()
        {
            var state = new FakeUIState();

            _manager.ChangeState(state);

            Assert.AreSame(state, _manager.CurrentState);
            Assert.IsTrue(state.IsActive);
            Assert.AreEqual(1, state.EnterCount);
        }

        [Test]
        public void ChangeState_ExitsPreviousState()
        {
            var a = new FakeUIState();
            var b = new FakeUIState();

            _manager.ChangeState(a);
            _manager.ChangeState(b);

            Assert.IsFalse(a.IsActive);
            Assert.AreEqual(1, a.ExitCount);
            Assert.IsTrue(b.IsActive);
            Assert.AreSame(b, _manager.CurrentState);
        }

        [Test]
        public void ChangeState_ToCurrentState_IsIgnored()
        {
            var state = new FakeUIState();

            _manager.ChangeState(state);
            _manager.ChangeState(state);

            Assert.AreEqual(1, state.EnterCount, "re-entering the current state must be a no-op");
        }

        [Test]
        public void ChangeState_Null_IsIgnored()
        {
            var state = new FakeUIState();
            _manager.ChangeState(state);

            Assert.DoesNotThrow(() => _manager.ChangeState(null));
            Assert.AreSame(state, _manager.CurrentState);
        }

        [Test]
        public void ExitState_PopsAndReentersPrevious()
        {
            var a = new FakeUIState();
            var b = new FakeUIState();

            _manager.ChangeState(a);
            _manager.ChangeState(b);
            _manager.ExitState();

            Assert.IsFalse(b.IsActive, "the popped state is exited");
            Assert.AreSame(a, _manager.CurrentState);
            Assert.IsTrue(a.IsActive, "the revealed state is re-entered");
            Assert.AreEqual(2, a.EnterCount);
        }

        [Test]
        public void ClearStack_ExitsActiveStatesAndEmptiesStack()
        {
            var a = new FakeUIState();
            var b = new FakeUIState();

            _manager.ChangeState(a);
            _manager.ChangeState(b);
            _manager.ClearStack();

            Assert.IsNull(_manager.CurrentState);
            Assert.IsFalse(b.IsActive);
            Assert.AreEqual(1, b.ExitCount, "the active top state is exited exactly once");
        }
    }
}
