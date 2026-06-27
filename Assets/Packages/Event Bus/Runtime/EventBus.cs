using MyToolz.Utilities.Debug;
using System;
using System.Collections.Generic;

namespace MyToolz.DesignPatterns.EventBus
{
    public static class EventBus<T> where T : IEvent
    {
        private static readonly HashSet<IEventBinding<T>> bindings = new();
        private static readonly Queue<Action> pendingRequests = new();
        private static bool isResolving;

        public static void Register(EventBinding<T> binding)
        {
            pendingRequests.Enqueue(() =>
            {
                EventBusUtil.AddEventBus(typeof(EventBus<T>));
                bindings.Add(binding);
            });
            TryResolve();
        }

        public static void Deregister(EventBinding<T> binding)
        {
            pendingRequests.Enqueue(() =>
            {
                bindings.Remove(binding);
            });
            TryResolve();
        }

        public static void Raise(T @event)
        {
            pendingRequests.Enqueue(() =>
            {
                foreach (var binding in bindings)
                {
                    binding.OnEvent.Invoke(@event);
                    binding.OnEventNoArgs.Invoke();
                }
            });
            TryResolve();
        }

        private static void TryResolve()
        {
            if (isResolving) return;

            isResolving = true;

            while (pendingRequests.Count > 0)
            {
                pendingRequests.Dequeue().Invoke();
            }

            isResolving = false;
        }
    }
}