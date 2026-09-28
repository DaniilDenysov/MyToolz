using MyToolz.DesignPatterns.EventBus;
using UnityEngine;

namespace MyToolz.Audio.Events
{
    public struct PlayAudioClipSO : IEvent
    {
        public AudioClipSO AudioClipSO;
        public Vector3 Position;

        /// <summary>
        /// Minimum seconds between two plays of this clip, replacing the clip's MinimalInterval.
        /// Zero or less means "use the clip's MinimalInterval".
        /// </summary>
        public float IntervalOverload;

        /// <summary>
        /// Pitch to play at, replacing whatever the clip's config would have chosen.
        /// Zero or less means "leave it to the config".
        /// </summary>
        public float Pitch;
    }
}
