using System;
using System.Reflection;
using System.Collections.Generic;
using UnityEditor;
using System.Linq;
using MyToolz.Utilities.Debug;

namespace MyToolz.DesignPatterns.EventBus
{
    /// <summary>
    /// Contains methods and properties related to event buses and event types in the Unity application.
    /// </summary>
    public static class EventBusUtil
    {
        private static HashSet<Type> eventBusTypes = new HashSet<Type>();
        public static IReadOnlyList<Type> EventBusTypes
        {
            get => eventBusTypes.ToList();
        }

#if UNITY_EDITOR
        public static PlayModeStateChange PlayModeState { get; set; }

        /// <summary>
        /// Initializes the Unity Editor related components of the EventBusUtil.
        /// The [InitializeOnLoadMethod] attribute causes this method to be called every time a script
        /// is loaded or when the game enters Play Mode in the Editor. This is useful to initialize
        /// fields or states of the class that are necessary during the editing state that also apply
        /// when the game enters Play Mode.
        /// The method sets up a subscriber to the playModeStateChanged event to allow
        /// actions to be performed when the Editor's play mode changes.
        /// </summary>    
        [InitializeOnLoadMethod]
        public static void InitializeEditor()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            PlayModeState = state;
            if (state == PlayModeStateChange.ExitingPlayMode)
            {
                ClearAllBuses();
            }
        }
#endif

        public static void AddEventBus(Type type)
        {
            if (type == default) return;
            eventBusTypes.Add(type);
        }

        /// <summary>
        /// Clears (removes all listeners from) all event buses in the application.
        /// </summary>
        public static void ClearAllBuses()
        {
            DebugUtility.Log("Clearing all buses...", AutoTag.NamespaceSegments);
            for (int i = 0; i < EventBusTypes.Count; i++)
            {
                var busType = EventBusTypes[i];
                var clearMethod = busType.GetMethod("Clear", BindingFlags.Static | BindingFlags.NonPublic);
                clearMethod?.Invoke(null, null);
            }
        }
    }
}