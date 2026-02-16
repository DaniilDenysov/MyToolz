using MyToolz.DesignPatterns.EventBus;
using MyToolz.ScriptableObjects.Audio;
using UnityEngine;

namespace MyToolz.Audio.Events
{
    public struct PlayAudioClipSO : IEvent
    {
        public AudioClipSO AudioClipSO;
    }

    public struct PlayAudioClipSOAtPosition : IEvent
    {
        public AudioClipSO AudioClipSO;
        public Vector3 Position;
    }
}