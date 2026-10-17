using System.Reflection;
using MyToolz.DesignPatterns.StateMachine.SimplePriorityBased;
using NUnit.Framework;
using UnityEngine;

namespace MyToolz.Tests.EditMode
{
    /// <summary>Recording <see cref="IPriorityState"/> double for state-machine tests.</summary>
    internal sealed class FakePriorityState : IPriorityState
    {
        private readonly uint _priority;
        public FakePriorityState(uint priority) => _priority = priority;

        public uint Priority => _priority;
        public int InitCount { get; private set; }
        public int EnterCount { get; private set; }
        public int ExitCount { get; private set; }
        public bool IsActive { get; private set; }

        public void Initialize() => InitCount++;
        public void OnEnter() { EnterCount++; IsActive = true; }
        public void OnExit() { ExitCount++; IsActive = false; }
    }

    public class PriorityStrategyTests
    {
        [Test]
        public void HigherPriority_IsStrictlyGreater()
        {
            var strategy = new HigherPriorityStrategy();

            Assert.IsTrue(strategy.HasHigherPriority(new FakePriorityState(5), new FakePriorityState(3)));
            Assert.IsFalse(strategy.HasHigherPriority(new FakePriorityState(3), new FakePriorityState(3)),
                "equal priorities are not strictly higher");
            Assert.IsFalse(strategy.HasHigherPriority(new FakePriorityState(2), new FakePriorityState(3)));
        }

        [Test]
        public void HigherEqualPriority_IncludesTies()
        {
            var strategy = new HigherEqualPriorityStrategy();

            Assert.IsTrue(strategy.HasHigherPriority(new FakePriorityState(5), new FakePriorityState(3)));
            Assert.IsTrue(strategy.HasHigherPriority(new FakePriorityState(3), new FakePriorityState(3)),
                "equal priorities count as higher-or-equal");
            Assert.IsFalse(strategy.HasHigherPriority(new FakePriorityState(2), new FakePriorityState(3)));
        }

        [Test]
        public void PriorityStrategies_TreatNullAsZero()
        {
            var strategy = new HigherEqualPriorityStrategy();

            // null -> 0, so null vs null is 0 >= 0 = true; a real state always beats null.
            Assert.IsTrue(strategy.HasHigherPriority(null, null));
            Assert.IsTrue(strategy.HasHigherPriority(new FakePriorityState(1), null));
            Assert.IsFalse(new HigherPriorityStrategy().HasHigherPriority(null, new FakePriorityState(1)));
        }

        [Test]
        public void IgnorePriority_ReturnsChooseFirstFlag_RegardlessOfPriorities()
        {
            var strategy = new IgnorePriorityStrategy();

            // Defaults to false: never treats 'a' as higher, whatever the priorities.
            Assert.IsFalse(strategy.HasHigherPriority(new FakePriorityState(100), new FakePriorityState(0)));

            typeof(IgnorePriorityStrategy)
                .GetField("chooseFirst", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(strategy, true);

            Assert.IsTrue(strategy.HasHigherPriority(new FakePriorityState(0), new FakePriorityState(100)));
        }
    }

    public class SimplePriorityStateMachineTests : SilentLogTest
    {
        private GameObject _go;
        private SimplePriorityStateMachine _sm;

        [SetUp]
        public void CreateMachine()
        {
            _go = new GameObject("SM");
            _sm = _go.AddComponent<SimplePriorityStateMachine>();
        }

        [TearDown]
        public void DestroyMachine()
        {
            if (_go != null) Object.DestroyImmediate(_go);
        }

        [Test]
        public void ChangeState_EntersState_AndBecomesCurrent()
        {
            var state = new FakePriorityState(1);

            _sm.ChangeState(state);

            Assert.IsTrue(state.IsActive);
            Assert.AreEqual(1, state.EnterCount);
            Assert.IsTrue(_sm.IsExecuting(state));
            Assert.IsTrue(_sm.TryGetCurrentState(out var current));
            Assert.AreSame(state, current);
        }

        [Test]
        public void ChangeState_ExitsPreviousState()
        {
            var a = new FakePriorityState(1);
            var b = new FakePriorityState(2);

            _sm.ChangeState(a);
            _sm.ChangeState(b);

            Assert.AreEqual(1, a.ExitCount);
            Assert.IsFalse(a.IsActive);
            Assert.IsTrue(b.IsActive);
            Assert.IsTrue(_sm.IsExecuting(b));
        }

        [Test]
        public void ChangeState_ToCurrentState_IsIgnored()
        {
            var state = new FakePriorityState(1);

            _sm.ChangeState(state);
            _sm.ChangeState(state);

            Assert.AreEqual(1, state.EnterCount, "re-entering the executing state must be a no-op");
            Assert.AreEqual(0, state.ExitCount);
        }

        [Test]
        public void ChangeState_Null_IsIgnored()
        {
            var state = new FakePriorityState(1);
            _sm.ChangeState(state);

            Assert.DoesNotThrow(() => _sm.ChangeState(null));
            Assert.IsTrue(_sm.IsExecuting(state), "a null transition leaves the current state untouched");
        }

        [Test]
        public void TryGetCurrentState_IsFalse_BeforeAnyState()
        {
            Assert.IsFalse(_sm.TryGetCurrentState(out var current));
            Assert.IsNull(current);
        }

        [Test]
        public void Stop_ExitsTheCurrentState()
        {
            var state = new FakePriorityState(1);
            _sm.ChangeState(state);

            _sm.Stop();

            Assert.AreEqual(1, state.ExitCount);
            Assert.IsFalse(_sm.TryGetCurrentState(out _));
        }

        [Test]
        public void Stop_WithoutState_DoesNothing()
        {
            Assert.DoesNotThrow(() => _sm.Stop());
        }
    }
}
