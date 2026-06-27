using Cysharp.Threading.Tasks;
using MyToolz.DesignPatterns.EventBus;
using MyToolz.Events;
using MyToolz.Interfaces;
using MyToolz.Utilities.Debug;
using System.Collections.Generic;
using System;
using UnityEngine;

namespace MyToolz.SceneManagement
{
    public struct LoadSceneGroup : IEvent
    {
        public SceneGroupSO Group;
        public List<AsyncLoadStep> Steps;
    }
}
