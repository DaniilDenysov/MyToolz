using UnityEngine;
using System.Collections.Generic;
using MyToolz.Events;

namespace MyToolz.Audio
{
    /// <summary>
    /// Keeps exactly one managed <see cref="AudioListener"/> enabled: the one on the enabled component
    /// with the highest priority (the earliest registered wins ties).
    /// </summary>
    [RequireComponent(typeof(AudioListener))]
    public class PriorityAudioListener : MonoBehaviour, IEventListener
    {
        [SerializeField, Range(0, 100)] private uint priority;

        private static readonly List<PriorityAudioListener> registered = new();
        private AudioListener audioListenerCached;
        private bool isRegistered;

        public uint Priority
        {
            get => priority;
            set
            {
                priority = value;
                if (isRegistered) Refresh();
            }
        }

        private AudioListener AudioListener
        {
            get
            {
                if (audioListenerCached == null)
                {
                    audioListenerCached = GetComponent<AudioListener>();
                }
                return audioListenerCached;
            }
        }

        /// <summary>The listener component currently in charge, or null when none is registered.</summary>
        public static PriorityAudioListener Active { get; private set; }

        private void OnEnable()
        {
            RegisterEvents();
        }

        private void OnDisable()
        {
            UnregisterEvents();
        }

        private void OnDestroy()
        {
            UnregisterEvents();
        }

        public void RegisterEvents()
        {
            if (isRegistered)
            {
                return;
            }

            registered.Add(this);
            isRegistered = true;
            Refresh();
        }

        public void UnregisterEvents()
        {
            if (!isRegistered)
            {
                return;
            }

            registered.Remove(this);
            isRegistered = false;

            // A disabled component must not leave a second listener enabled behind it.
            if (registered.Count > 0 && AudioListener != null)
            {
                AudioListener.enabled = false;
            }

            Refresh();
        }

        private static void Refresh()
        {
            registered.RemoveAll(l => l == null);

            PriorityAudioListener best = null;
            foreach (PriorityAudioListener candidate in registered)
            {
                if (best == null || candidate.priority > best.priority)
                {
                    best = candidate;
                }
            }

            Active = best;

            // Each component toggles its own listener, so the choice never depends on callback order.
            foreach (PriorityAudioListener candidate in registered)
            {
                AudioListener listener = candidate.AudioListener;
                if (listener != null)
                {
                    listener.enabled = candidate == best;
                }
            }
        }
    }
}
