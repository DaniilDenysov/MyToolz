#if FEEL_PRESENT
using MyToolz.Audio.Events;
using MyToolz.Core;
using MyToolz.DesignPatterns.EventBus;
using Sirenix.OdinInspector;
using UnityEngine;
using MoreMountains.Tools;
using MyToolz.ScriptableObjects.Audio;

namespace MyToolz.FeelWrappers.Audio
{
    public class FeelAudioManager : MonoBehaviourPlus
    {
        private EventBinding<PlayAudioClipSO> playBinding;
        private EventBinding<PlayAudioClipSOAtPosition> playPositionalBinding;

        private float _lastPlayedTime;

        private void OnEnable()
        {
            playBinding = new EventBinding<PlayAudioClipSO>(OnPlayAudioClipSO);
            EventBus<PlayAudioClipSO>.Register(playBinding);

            playPositionalBinding = new EventBinding<PlayAudioClipSOAtPosition>(OnPlayAudioClipSOAtPosition);
            EventBus<PlayAudioClipSOAtPosition>.Register(playPositionalBinding);
        }

        private void OnDisable()
        {
            EventBus<PlayAudioClipSO>.Deregister(playBinding);
            EventBus<PlayAudioClipSOAtPosition>.Deregister(playPositionalBinding);
        }

        private void PlayInternal(AudioClipSO audioClipSO, Vector3? worldPosition)
        {
            if (audioClipSO == null) return;

            var tuple = audioClipSO.GetClipAndConfig();
            if (tuple.clip == null || tuple.config == null) return;

            //TODO: ADD BETTER TRACKING OR ENSURE HANDLING THROUGH THE FEEL
            //if (_lastPlayedTime + audioClipSO.MinimalInterval > Time.time) return;
            //_lastPlayedTime = Time.time;

            var cfg = tuple.config;
            var location = cfg.IsGlobal ? Vector3.zero : worldPosition ?? Vector3.zero;

            MMSoundManagerSoundPlayEvent.Trigger(
                tuple.clip,
                mmSoundManagerTrack: cfg.Track,
                location: location,
                loop: cfg.Loop,
                volume: cfg.Volume,
                ID: audioClipSO.ID,
                fade: cfg.Fade,
                fadeInitialVolume: cfg.FadeInitialVolume,
                fadeDuration: cfg.FadeDuration,
                fadeTween: cfg.FadeTween,
                persistent: cfg.Persistent,
                recycleAudioSource: null,
                audioGroup: cfg.AudioMixer,
                pitch: cfg.GetEffectivePitch(),
                panStereo: cfg.PanStereo,
                spatialBlend: cfg.SpatialBlend,
                soloSingleTrack: cfg.SoloSingleTrack,
                soloAllTracks: cfg.SoloAllTracks,
                autoUnSoloOnEnd: cfg.AutoUnSoloOnEnd,
                bypassEffects: cfg.BypassEffects,
                bypassListenerEffects: false,
                bypassReverbZones: cfg.BypassReverbZones,
                priority: cfg.Priority,
                reverbZoneMix: cfg.ReverbZoneMix,
                dopplerLevel: cfg.DopplerLevel,
                spread: cfg.Spread,
                rolloffMode: cfg.RolloffMode,
                minDistance: cfg.MinDistance,
                maxDistance: cfg.MaxDistance,
                audioResourceToPlay: null
            );

        }

        private void OnPlayAudioClipSO(PlayAudioClipSO args)
        {
            PlayInternal(args.AudioClipSO, null);
        }

        private void OnPlayAudioClipSOAtPosition(PlayAudioClipSOAtPosition args)
        {
            PlayInternal(args.AudioClipSO, args.Position);
        }
    }
}
#endif