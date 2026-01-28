using System.Collections.Generic;

namespace MyToolz.DesignPatterns.EventBus
{
    public static class EventBus<T> where T : IEvent
    {
        private static readonly HashSet<IEventBinding<T>> bindings = new();

        public static void Register(EventBinding<T> binding)
        {
            EventBusUtil.AddEventBus(typeof(EventBus<T>));
            bindings.Add(binding);
        }

        public static void Deregister(EventBinding<T> binding) => bindings.Remove(binding);

        public static void Raise(T @event)
        {
            var snapshot = new HashSet<IEventBinding<T>>(bindings);

            foreach (var binding in snapshot)
            {
                if (bindings.Contains(binding))
                {
                    binding.OnEvent.Invoke(@event);
                    binding.OnEventNoArgs.Invoke();
                }
            }
        }
    }
}