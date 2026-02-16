using MyToolz.DesignPatterns.EventBus;
using MyToolz.Events;
using NoSaints.UI.Labels;
using Sirenix.OdinInspector;
using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.VFX;

namespace MyToolz.Player.FPS.DisposableObjects
{
    public class DisposableParticleSystem : MonoBehaviour
    {
        public event Action<GameObject> OnCollision;
        [SerializeField] private bool useVisualEffectsGraph = false;
        [SerializeField,  HideIf("@useVisualEffectsGraph")] private ParticleSystem particleSystem;
        [SerializeField,  HideIf("@!useVisualEffectsGraph")] private VisualEffect visualEffects;

        private void OnEnable()
        {
            if (useVisualEffectsGraph)
            {
                visualEffects?.Play();
            }
            else
            {
                //var main = particleSystem.main;
                //main.stopAction = ParticleSystemStopAction.Callback;
                particleSystem.Play();
            }
        }

        public void Stop ()
        {
            if (useVisualEffectsGraph)
            {
                visualEffects?.Stop();
                DisposeWithDelay();
            }
            else
            {
                particleSystem?.Stop();
            }
        }

        private async void DisposeWithDelay ()
        {
            await Task.Delay(5000);
            EventBus<ReleaseRequest<DisposableParticleSystem>>.Raise(new ReleaseRequest<DisposableParticleSystem>()
            {
                PoolObject = this
            });
        }

        //private void OnParticleCollision(GameObject other)
        //{
        //    OnCollision?.Invoke(other);
        //}

        private void OnParticleSystemStopped()
        {
            EventBus<ReleaseRequest<DisposableParticleSystem>>.Raise(new ReleaseRequest<DisposableParticleSystem>()
            {
                PoolObject = this
            });
        }
    }
}
