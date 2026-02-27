using MyToolz.DesignPatterns.EventBus;
using MyToolz.DesignPatterns.ObjectPool;
using MyToolz.Audio.Events;
using MyToolz.Events;
using UnityEngine;
using MyToolz.EditorToolz;

namespace MyToolz.Audio
{
    public class AudioSourceObjectPool : DefaultObjectPoolInstaller<AudioSourceWrapper>
    {
        [SerializeField, Required] private AudioSourceWrapper defaultPrefab;

        private EventBinding<PlayAudioClipSO> playBinding;

        protected override void RegisterEventHandlers()
        {
            base.RegisterEventHandlers();

            playBinding = new EventBinding<PlayAudioClipSO>(OnPlayAudioClipSO);
            EventBus<PlayAudioClipSO>.Register(playBinding);
        }

        protected override void DeregisterEventHandlers()
        {
            base.DeregisterEventHandlers();

            EventBus<PlayAudioClipSO>.Deregister(playBinding);
        }

        private void OnPlayAudioClipSO(PlayAudioClipSO e)
        {
            EventBus<PoolRequest<AudioSourceWrapper>>.Raise(new PoolRequest<AudioSourceWrapper>
            {
                Prefab = defaultPrefab,
                Position = e.Position,
                Rotation = Quaternion.identity,
                Parent = null,
                Callback = wrapper => wrapper.Play(e.AudioClipSO)
            });
        }
    }
}
