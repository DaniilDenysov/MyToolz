using UnityEngine;
using MyToolz.ScriptableObjects.Audio;
using MyToolz.DesignPatterns.EventBus;
using MyToolz.Audio.Events;
using MyToolz.EditorToolz;
using MyToolz.Utilities.Debug;

namespace MyToolz.Audio
{
    public class AudioSourceWrapper : MonoBehaviour
    {
        [SerializeField, Required, Tooltip("Audio asset to play via the audio wrapper.")] private AudioClipSO audioClipSO;
        [SerializeField, Tooltip("If enabled, play from a world position.")] private bool playAtPosition;

        public AudioClipSO AudioClipSO => audioClipSO;

        [Button]
        public void Play()
        {
            if (audioClipSO == null) return;

            EventBus<PlayAudioClipSO>.Raise(new PlayAudioClipSO { AudioClipSO = audioClipSO });
            DebugUtility.Log(this, "EventAudioSourceWrapper: Play");
        }

        [Button]
        public void PlayOnCurrentPosition()
        {
            if (audioClipSO == null) return;

            EventBus<PlayAudioClipSOAtPosition>.Raise(new PlayAudioClipSOAtPosition { AudioClipSO = audioClipSO, Position = transform.position });
            DebugUtility.Log(this, "EventAudioSourceWrapper: PlayOnCurrentPosition");
        }
    }
}
