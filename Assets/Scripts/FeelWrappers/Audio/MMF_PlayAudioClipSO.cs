#if FEEL_PRESENT
using MoreMountains.Feedbacks;
using MyToolz.Audio.Events;
using MyToolz.DesignPatterns.EventBus;
using MyToolz.ScriptableObjects.Audio;
using MyToolz.Utilities.Debug;
using UnityEngine;

namespace MyToolz.FeelWrappers.Audio
{
    [AddComponentMenu("")]
    [FeedbackPath("Audio/Play AudioClipSO")]
    public class MMF_PlayAudioClipSO : MMF_Feedback
    {
        [MMFInspectorGroup("Sound", true, 14, true)]
        public AudioClipSO audioClipSO;
        public bool playAtPosition;

        protected override void CustomPlayFeedback(Vector3 position, float feedbacksIntensity = 1f)
        {
            if (!Active || audioClipSO == null) return;

            if (playAtPosition)
            {
                EventBus<PlayAudioClipSOAtPosition>.Raise(new PlayAudioClipSOAtPosition
                {
                    AudioClipSO = audioClipSO,
                    Position = position
                });
                DebugUtility.Log(this, "MMF_PlayAudioClipSO: PlayAtPosition");
            }
            else
            {
                EventBus<PlayAudioClipSO>.Raise(new PlayAudioClipSO
                {
                    AudioClipSO = audioClipSO
                });
                DebugUtility.Log("MMF_PlayAudioClipSO: Play");
            }
        }
    }
}
#endif
