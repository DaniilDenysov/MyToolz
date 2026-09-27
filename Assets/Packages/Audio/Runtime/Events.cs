using MyToolz.DesignPatterns.EventBus;
using UnityEngine;

namespace MyToolz.Audio.Events
{
    public struct PlayAudioClipSO : IEvent
    {
        public AudioClipSO AudioClipSO;
        public Vector3 Position;
        public float IntervalOverload;

        /// <summary>
        /// Pitch to play at, replacing whatever the clip's config would have chosen.
        /// Zero or less means "leave it to the config".
        /// </summary>
        public float Pitch;
    }
}
