using MyToolz.DesignPatterns.EventBus;
using MyToolz.Events;
using MyToolz.Extensions;
using MyToolz.ScriptableObjects.Audio;
using UnityEngine;

namespace MyToolz.Player.FPS.DisposableObjects
{
    public class DisposableBulletHole : DisposableObject
    {
        //[SerializeField] private DecalProjector decalProjector;
        [SerializeField] private ParticleSystem particleSystem;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClipSO audioClipSO;
        //[SerializeField] private VisualEffect visualEffect;
        private float initialOpacity;

        public override void Awake()
        {
            //if (decalProjector != null) initialOpacity = decalProjector.fadeFactor;
        }

        public void Play()
        {
            if (particleSystem != null) particleSystem.Play();
            PlaySound(audioClipSO, audioSource);
        }

        public void PlaySound(AudioClipSO audioClipSO, AudioSource audioSource)
        {
            if (audioClipSO == null || audioSource == null) return;
            audioSource.Play(audioClipSO);
        }

        public override bool ShouldBeShrinken()
        {
            //if (decalProjector == null) return false;

            return true; //decalProjector.fadeFactor > threshold;
        }

        public override void ResetObject()
        {
            //if (decalProjector != null) decalProjector.fadeFactor = initialOpacity;
        }

        public override void Shrink()
        {
           // if (decalProjector == null) return;
            //decalProjector.fadeFactor = Mathf.Lerp(decalProjector.fadeFactor, 0f, disposeSpeed * Time.deltaTime);
        }

        public override void OnObjectDispose()
        {
            EventBus<ReleaseRequest<DisposableBulletHole>>.Raise(new ReleaseRequest<DisposableBulletHole>()
            {
                PoolObject = this,
            });
        }
    }
}
