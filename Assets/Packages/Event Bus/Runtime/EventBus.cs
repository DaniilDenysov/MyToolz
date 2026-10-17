using MyToolz.Utilities.Debug;
using System;
using System.Collections.Generic;

namespace MyToolz.DesignPatterns.EventBus
{
    /// <summary>
    /// Static, type-safe bus for one event type. Registrations, deregistrations and raises are
    /// processed strictly in the order they were requested: anything requested while a dispatch is
    /// running is queued and runs once that dispatch finishes, so handlers never see a collection
    /// change under them and a raise from inside a handler never nests.
    /// <para>
    /// Listeners are called in registration order. A listener that throws is logged and skipped;
    /// the remaining listeners still receive the event and the queue always drains completely.
    /// Raising does not allocate once the bus has warmed up.
    /// </para>
    /// </summary>
    public static class EventBus<T> where T : IEvent
    {
        private enum OperationKind : byte
        {
            Register,
            Deregister,
            Raise
        }

        private readonly struct Operation
        {
            public readonly OperationKind Kind;
            public readonly IEventBinding<T> Binding;
            public readonly T Event;

            public Operation(OperationKind kind, IEventBinding<T> binding, T @event)
            {
                Kind = kind;
                Binding = binding;
                Event = @event;
            }
        }

        private static readonly List<IEventBinding<T>> bindings = new();
        private static readonly HashSet<IEventBinding<T>> registered = new();
        private static readonly Queue<Operation> pending = new();
        private static bool isResolving;
        private static bool trackedByUtil;

        public static void Register(EventBinding<T> binding)
        {
            if (binding == null)
            {
                return;
            }

            Enqueue(new Operation(OperationKind.Register, binding, default));
        }

        public static void Deregister(EventBinding<T> binding)
        {
            if (binding == null)
            {
                return;
            }

            Enqueue(new Operation(OperationKind.Deregister, binding, default));
        }

        public static void Raise(T @event)
        {
            Enqueue(new Operation(OperationKind.Raise, null, @event));
        }

        private static void Enqueue(in Operation operation)
        {
            pending.Enqueue(operation);

            if (isResolving)
            {
                return;
            }

            isResolving = true;

            try
            {
                while (pending.Count > 0)
                {
                    Execute(pending.Dequeue());
                }
            }
            finally
            {
                isResolving = false;
            }
        }

        private static void Execute(in Operation operation)
        {
            switch (operation.Kind)
            {
                case OperationKind.Register:
                    if (!trackedByUtil)
                    {
                        trackedByUtil = true;
                        EventBusUtil.AddEventBus(typeof(EventBus<T>));
                    }

                    if (registered.Add(operation.Binding))
                    {
                        bindings.Add(operation.Binding);
                    }
                    break;

                case OperationKind.Deregister:
                    if (registered.Remove(operation.Binding))
                    {
                        bindings.Remove(operation.Binding);
                    }
                    break;

                case OperationKind.Raise:
                    Dispatch(operation.Event);
                    break;
            }
        }

        private static void Dispatch(T @event)
        {
            // Mutations are deferred while resolving, so the list cannot change during this loop.
            for (int i = 0; i < bindings.Count; i++)
            {
                IEventBinding<T> binding = bindings[i];

                try
                {
                    binding.OnEvent?.Invoke(@event);
                    binding.OnEventNoArgs?.Invoke();
                }
                catch (Exception exception)
                {
                    DebugUtility.LogError($"A listener of {typeof(T).Name} threw; the remaining listeners still receive the event.\n{exception}");
                }
            }
        }

        // Invoked via reflection by EventBusUtil.ClearAllBuses().
        private static void Clear()
        {
            bindings.Clear();
            registered.Clear();
            pending.Clear();
            isResolving = false;
        }
    }
}
