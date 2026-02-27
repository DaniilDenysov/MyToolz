using System.Threading;
using Cysharp.Threading.Tasks;
using MyToolz.DesignPatterns.EventBus;
using MyToolz.DesignPatterns.ObjectPool;
using MyToolz.Events;
using MyToolz.Extensions;
using UnityEngine;

namespace MyToolz.Audio
{
    [RequireComponent(typeof(AudioSource))]
    public class AudioSourceWrapper : MonoBehaviour, IPoolable
    {
        private AudioSource audioSource;
        private CancellationTokenSource cts;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
        }

        public void Play(AudioClipSO audioClipSO)
        {
            var audio = audioClipSO.GetClipAndConfig();
            if (audio.clip == null) return;

            if (audio.config != null)
                audioSource.Configure(audio.config);

            audioSource.clip = audio.clip;
            audioSource.Play();

            if (!audioSource.loop)
                WaitAndRelease().Forget();
        }

        private async UniTaskVoid WaitAndRelease()
        {
            CancelPending();
            cts = new CancellationTokenSource();

            var cancelled = await UniTask.WaitWhile(
                () => audioSource.isPlaying,
                PlayerLoopTiming.Update,
                cts.Token
            ).SuppressCancellationThrow();

            if (cancelled) return;

            EventBus<ReleaseRequest<AudioSourceWrapper>>.Raise(new ReleaseRequest<AudioSourceWrapper>
            {
                PoolObject = this
            });
        }

        private void CancelPending()
        {
            cts?.Cancel();
            cts?.Dispose();
            cts = null;
        }

        public void OnSpawned() { }

        public void OnDespawned()
        {
            CancelPending();
            audioSource.Stop();
            audioSource.clip = null;
        }

        private void OnDestroy()
        {
            CancelPending();
        }
    }
}