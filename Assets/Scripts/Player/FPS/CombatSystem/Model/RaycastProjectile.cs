using DG.Tweening;
using MyToolz.DesignPatterns.EventBus;
using MyToolz.Events;
using MyToolz.Extensions;
using MyToolz.ScriptableObjects.Audio;
using System;
using UnityEngine;

namespace MyToolz.Player.FPS.CombatSystem.Model
{
    [RequireComponent(typeof(LineRenderer))]
    public class RaycastProjectile : MonoBehaviour
    {
        [SerializeField, Range(0, 100f)] private float _fadeSpeed = 1f;
        private LineRenderer _renderer;
        [SerializeField] private TrailRenderer trailRenderer;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClipSO audioClipSO;

        private Tween movementTween;

        private void Awake()
        {
            _renderer = GetComponent<LineRenderer>();
        }

        public void Fire(float speed, Vector3 position, Vector3 hit)
        {
            audioSource.Stop();
            audioSource.clip = null;

            transform.position = position;

            float distance = Vector3.Distance(position, hit);
            float travelTime = distance / speed;
            float delay = travelTime * 0.25f;

            audioSource.Play(audioClipSO, delay);
            trailRenderer.Clear();

            MoveProjectileByTime(position, hit, travelTime);
        }

        private void MoveProjectileByTime(Vector3 start, Vector3 destination, float originalTime)
        {
            if (movementTween != null && movementTween.IsActive())
            {
                movementTween.Kill();
            }

            float distance = Vector3.Distance(start, destination);

            float minTime = 0.05f;
            float maxTime = 0.3f;
            float t = Mathf.InverseLerp(0f, 100f, distance);
            float totalTime = Mathf.Lerp(minTime, maxTime, t);

            movementTween = transform.DOMove(destination, totalTime)
                .SetEase(Ease.InOutQuad)
                .OnComplete(() =>
                {
                    audioSource.Stop();
                    audioSource.clip = null;
                    EventBus<ReleaseRequest<RaycastProjectile>>.Raise(new ReleaseRequest<RaycastProjectile>()
                    {
                        PoolObject = this,
                    });
                });
        }
    }
}
