using System.Collections.Generic;
using MyToolz.DesignPatterns.EventBus;
using NUnit.Framework;

namespace MyToolz.Tests.EditMode
{
    public class EventBusTests
    {
        private struct SampleEvent : IEvent
        {
            public int Value;
        }

        private struct OtherEvent : IEvent
        {
            public int Value;
        }

        [TearDown]
        public void ClearBuses()
        {
            // Buses are static; wipe every registered bus so state never leaks between tests.
            EventBusUtil.ClearAllBuses();
        }

        [Test]
        public void Raise_InvokesArgListener_WithPayload()
        {
            int received = -1;
            var binding = new EventBinding<SampleEvent>(e => received = e.Value);
            EventBus<SampleEvent>.Register(binding);

            EventBus<SampleEvent>.Raise(new SampleEvent { Value = 42 });

            Assert.AreEqual(42, received);
            EventBus<SampleEvent>.Deregister(binding);
        }

        [Test]
        public void Raise_InvokesNoArgListener()
        {
            int calls = 0;
            var binding = new EventBinding<SampleEvent>(() => calls++);
            EventBus<SampleEvent>.Register(binding);

            EventBus<SampleEvent>.Raise(new SampleEvent { Value = 1 });

            Assert.AreEqual(1, calls);
            EventBus<SampleEvent>.Deregister(binding);
        }

        [Test]
        public void Raise_InvokesBothArgAndNoArgCallbacksOnSameBinding()
        {
            int argCalls = 0;
            int noArgCalls = 0;
            var binding = new EventBinding<SampleEvent>(_ => argCalls++);
            binding.Add(() => noArgCalls++);
            EventBus<SampleEvent>.Register(binding);

            EventBus<SampleEvent>.Raise(new SampleEvent());

            Assert.AreEqual(1, argCalls, "arg callback should fire");
            Assert.AreEqual(1, noArgCalls, "no-arg callback should fire");
            EventBus<SampleEvent>.Deregister(binding);
        }

        [Test]
        public void Raise_InvokesEveryRegisteredListener()
        {
            int a = 0, b = 0;
            var bindingA = new EventBinding<SampleEvent>(_ => a++);
            var bindingB = new EventBinding<SampleEvent>(_ => b++);
            EventBus<SampleEvent>.Register(bindingA);
            EventBus<SampleEvent>.Register(bindingB);

            EventBus<SampleEvent>.Raise(new SampleEvent());

            Assert.AreEqual(1, a);
            Assert.AreEqual(1, b);
            EventBus<SampleEvent>.Deregister(bindingA);
            EventBus<SampleEvent>.Deregister(bindingB);
        }

        [Test]
        public void Deregister_StopsDelivery()
        {
            int calls = 0;
            var binding = new EventBinding<SampleEvent>(_ => calls++);
            EventBus<SampleEvent>.Register(binding);
            EventBus<SampleEvent>.Deregister(binding);

            EventBus<SampleEvent>.Raise(new SampleEvent());

            Assert.AreEqual(0, calls);
        }

        [Test]
        public void Register_SameBindingTwice_DeliversOnce()
        {
            int calls = 0;
            var binding = new EventBinding<SampleEvent>(_ => calls++);
            EventBus<SampleEvent>.Register(binding);
            EventBus<SampleEvent>.Register(binding);

            EventBus<SampleEvent>.Raise(new SampleEvent());

            Assert.AreEqual(1, calls, "bindings are stored in a set, so a duplicate register must not double-fire");
            EventBus<SampleEvent>.Deregister(binding);
        }

        [Test]
        public void Raise_WithNoListeners_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => EventBus<SampleEvent>.Raise(new SampleEvent { Value = 7 }));
        }

        [Test]
        public void Buses_AreIsolatedPerEventType()
        {
            int sample = 0, other = 0;
            var sampleBinding = new EventBinding<SampleEvent>(_ => sample++);
            var otherBinding = new EventBinding<OtherEvent>(_ => other++);
            EventBus<SampleEvent>.Register(sampleBinding);
            EventBus<OtherEvent>.Register(otherBinding);

            EventBus<SampleEvent>.Raise(new SampleEvent());

            Assert.AreEqual(1, sample);
            Assert.AreEqual(0, other, "raising one event type must not touch another type's bus");
            EventBus<SampleEvent>.Deregister(sampleBinding);
            EventBus<OtherEvent>.Deregister(otherBinding);
        }

        [Test]
        public void Raise_ReentrantSameType_IsDeferredUntilCurrentDispatchCompletes()
        {
            // The bus queues operations per event type behind an `isResolving` guard: a raise
            // of the SAME type triggered from inside a handler must run only after the in-flight
            // dispatch drains, never nested inside it. (Re-entrancy is per closed generic, so a
            // raise of a *different* type from a handler is not deferred — that is a separate bus.)
            var order = new List<string>();
            bool reentered = false;

            var binding = new EventBinding<SampleEvent>(_ =>
            {
                order.Add("start");
                if (!reentered)
                {
                    reentered = true;
                    EventBus<SampleEvent>.Raise(new SampleEvent());
                }
                order.Add("end");
            });
            EventBus<SampleEvent>.Register(binding);

            EventBus<SampleEvent>.Raise(new SampleEvent());

            // Synchronous re-entrancy would interleave as start,start,end,end.
            // Deferred dispatch completes the first handler before running the queued raise:
            CollectionAssert.AreEqual(new[] { "start", "end", "start", "end" }, order);
            EventBus<SampleEvent>.Deregister(binding);
        }

        [Test]
        public void Deregister_DuringRaise_TakesEffectOnNextRaise_WithoutThrowing()
        {
            int keepCalls = 0;
            int dropCalls = 0;

            EventBinding<SampleEvent> drop = null;
            drop = new EventBinding<SampleEvent>(_ => dropCalls++);
            var keep = new EventBinding<SampleEvent>(_ =>
            {
                keepCalls++;
                // Deregistering mid-dispatch must not corrupt the in-progress iteration.
                EventBus<SampleEvent>.Deregister(drop);
            });

            EventBus<SampleEvent>.Register(keep);
            EventBus<SampleEvent>.Register(drop);

            Assert.DoesNotThrow(() => EventBus<SampleEvent>.Raise(new SampleEvent()));
            Assert.AreEqual(1, dropCalls, "first raise still delivers to the binding removed during dispatch");

            EventBus<SampleEvent>.Raise(new SampleEvent());
            Assert.AreEqual(2, keepCalls);
            Assert.AreEqual(1, dropCalls, "second raise must skip the deregistered binding");

            EventBus<SampleEvent>.Deregister(keep);
        }

        [Test]
        public void ClearAllBuses_RemovesAllBindings()
        {
            int calls = 0;
            var binding = new EventBinding<SampleEvent>(_ => calls++);
            EventBus<SampleEvent>.Register(binding);

            EventBusUtil.ClearAllBuses();

            EventBus<SampleEvent>.Raise(new SampleEvent());
            Assert.AreEqual(0, calls, "ClearAllBuses (used on exiting play mode) must drop existing bindings");
        }

        [Test]
        public void EventBinding_Remove_DetachesOneArgCallback_KeepingOthers()
        {
            int a = 0, b = 0;
            void HandlerA(SampleEvent _) => a++;
            void HandlerB(SampleEvent _) => b++;

            var binding = new EventBinding<SampleEvent>(HandlerA);
            binding.Add(HandlerB);
            EventBus<SampleEvent>.Register(binding);

            EventBus<SampleEvent>.Raise(new SampleEvent());
            Assert.AreEqual(1, a);
            Assert.AreEqual(1, b);

            binding.Remove(HandlerA);
            EventBus<SampleEvent>.Raise(new SampleEvent());
            Assert.AreEqual(1, a, "removed callback must not fire again");
            Assert.AreEqual(2, b, "the remaining callback keeps firing");

            EventBus<SampleEvent>.Deregister(binding);
        }

        [Test]
        public void EventBinding_Remove_DetachesNoArgCallback()
        {
            int calls = 0;
            void Handler() => calls++;

            // Construct via the arg ctor so the no-arg delegate keeps its safe sentinel;
            // removing the added no-arg callback then leaves a non-null delegate to invoke.
            var binding = new EventBinding<SampleEvent>(_ => { });
            binding.Add(Handler);
            EventBus<SampleEvent>.Register(binding);

            EventBus<SampleEvent>.Raise(new SampleEvent());
            Assert.AreEqual(1, calls);

            binding.Remove(Handler);
            EventBus<SampleEvent>.Raise(new SampleEvent());
            Assert.AreEqual(1, calls, "removed no-arg callback must not fire again");

            EventBus<SampleEvent>.Deregister(binding);
        }
    }
}
