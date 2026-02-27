using MyToolz.DesignPatterns.EventBus;
using UnityEngine;

namespace MyToolz.Audio.Events
{
    public struct PlayAudioClipSO : IEvent
    {
        public AudioClipSO AudioClipSO;
        public Vector3 Position;
    }
}
