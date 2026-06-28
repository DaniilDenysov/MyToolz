using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;
using MyToolz.Events;

namespace NoSaints.SFX
{
    [RequireComponent(typeof(AudioListener))]
    public class PriorityAudioListener : MonoBehaviour, IEventListener
    {
        private struct PriorityListener
        {
            public uint priority;
            public AudioListener AudioListener;
        }

        [SerializeField, Range(0, 100)] private uint priority;
        private readonly static List<PriorityListener> priorityListeners = new();
        private static Action<AudioListener> onListenerUpdated;
        private AudioListener audioListenerCached;
        private AudioListener audioListener
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

        private void SortListeners()
        {
            if (priorityListeners == null)
            {
                return;
            }
            priorityListeners.OrderByDescending(l => l.priority);
        }

        private void AddSelf()
        {
            if (audioListener != null)
            {
                priorityListeners.Add(new PriorityListener()
                {
                    AudioListener = audioListener,
                    priority = priority
                });
            }
            SortListeners();
            onListenerUpdated?.Invoke(priorityListeners.FirstOrDefault().AudioListener);
        }

        private void RemoveSelf()
        {
            if (audioListener != null)
            {
                priorityListeners.RemoveAll(l => l.AudioListener == audioListener);
            }
            SortListeners();
            onListenerUpdated?.Invoke(priorityListeners.FirstOrDefault().AudioListener);
        }

        private void OnListenerChanged(AudioListener audioListener)
        {
            if (audioListener == null)
            {
                return;
            }
            audioListener.enabled = this.audioListener == audioListener;
        }

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
            onListenerUpdated += OnListenerChanged;
            AddSelf();
        }

        public void UnregisterEvents()
        {
            RemoveSelf();
            onListenerUpdated -= OnListenerChanged;
        }
    }
}
