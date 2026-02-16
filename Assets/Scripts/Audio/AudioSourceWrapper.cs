using Sirenix.OdinInspector;
using UnityEngine;
using MyToolz.ScriptableObjects.Audio;
using MyToolz.DesignPatterns.EventBus;
using MyToolz.Core;
using MyToolz.Audio.Events;

namespace MyToolz.Audio
{
    public class AudioSourceWrapper : MonoBehaviourPlus
    {
        [SerializeField, Required, Tooltip("Audio asset to play via the audio wrapper.")] private AudioClipSO audioClipSO;
        [SerializeField, Tooltip("If enabled, play from a world position.")] private bool playAtPosition;

        public AudioClipSO AudioClipSO => audioClipSO;

        [Button]
        public void Play()
        {
            if (audioClipSO == null) return;

            EventBus<PlayAudioClipSO>.Raise(new PlayAudioClipSO { AudioClipSO = audioClipSO });
            Log("EventAudioSourceWrapper: Play");
        }

        [Button]
        public void PlayOnCurrentPosition()
        {
            if (audioClipSO == null) return;

            EventBus<PlayAudioClipSOAtPosition>.Raise(new PlayAudioClipSOAtPosition { AudioClipSO = audioClipSO, Position = transform.position });
            Log("EventAudioSourceWrapper: PlayOnCurrentPosition");
        }
    }
}
