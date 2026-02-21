using MyToolz.DesignPatterns.EventBus;
using System;
using UnityEngine;

namespace MyToolz.DesignPatterns.EventBus
{
    public interface IEvent { }
}

namespace MyToolz.Events
{
    public interface IEventListener
    {
        public void RegisterEvents();
        public void UnregisterEvents();
    }
}