using Mirror;
using MyToolz.DesignPatterns.EventBus;
using MyToolz.HealthSystem;
using MyToolz.HealthSystem.Interfaces;
using MyToolz.Player.FPS.CombatSystem.Model;
using MyToolz.ScriptableObjects.Audio;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using UnityEngine;

namespace MyToolz.Player.FPS.CombatSystem.Presenter
{
    public class LethalEquipmentController<T> : LethalEquipmentPresenterAbstract, ILethalEquipment where T : LethalEquipmentSO
    {
        [SerializeField, Required] protected T lethalEquipmentSO;
        public T LethalEquipmentS => lethalEquipmentSO;
        [SerializeField, Optional] private Transform forcePoint; 
        [SerializeField, Required] protected Rigidbody _rigidbody;
        [SerializeField] protected AudioClipSO collision_sfx;
        [SerializeField, Range(0, 10)] protected float sfx_force_trashold = 2;
        protected NetworkConnectionToClient owner;
        public NetworkConnectionToClient Owner => owner;

        protected bool calledDisposure = false;

        public T GetSO() => lethalEquipmentSO;

        protected virtual void Start()
        {
            if (!NetworkServer.active)
            {
                _rigidbody.isKinematic = true;
            } else {
                transform.parent = null;
                if (lethalEquipmentSO.TTLPreset == TTLPreset.OnStart)
                {
                    CallDispoose();
                }
            }
        }

        public void StartThrow()
        {
        }

        [Server]
        public virtual void Construct(NetworkConnectionToClient conn, Vector3 throwDirection)
        {
            if (forcePoint == null) _rigidbody.AddForce(throwDirection * lethalEquipmentSO.ThrowForce,ForceMode.Impulse);
            else _rigidbody.AddForceAtPosition(throwDirection * lethalEquipmentSO.ThrowForce, forcePoint.position,ForceMode.Impulse);
            owner = conn;
        }

        protected virtual void OnCollisionEnter(Collision collision)
        {
            if (!NetworkServer.active) return;
            if (lethalEquipmentSO.TTLPreset == TTLPreset.FirstCollision)
            {
                StartCoroutine(DelayedDisposure(lethalEquipmentSO.TimeToLive));
            }
            if (_rigidbody.velocity.magnitude > sfx_force_trashold)
            {
                if (collision_sfx != null)
                {
                    //EventBus<SoundEvent>.Raise(new SoundEvent
                    //{
                    //    AudioClipGuid = AudioMapper.Instance.GetAudioGuid(collision_sfx),
                    //    position = transform.position,
                    //    Cancell = false
                    //});
                }
            }
            ProcessDamageOnCollision(collision);
            ProcessCollision();

        }

        protected virtual void ProcessDamageOnCollision (Collision collision)
        {
            if (!lethalEquipmentSO.IsDamageOnHitEnabled()) return;
            if (collision.gameObject.TryGetComponent(out IDamagable damageable))
            {
                if (_rigidbody.velocity.magnitude > lethalEquipmentSO.DamageOnHitThreshold)
                {
                    damageable.DoDamage(new PhysicalDamageType (lethalEquipmentSO.DamageOnHit));
                }
            }
        }

        protected virtual void CallDispoose()
        {
            if (!NetworkServer.active) return;
            if (calledDisposure) return;
            calledDisposure = true;
            StartCoroutine(DelayedDisposure(lethalEquipmentSO.TimeToLive));
        }

        protected void DisposeEquipment ()
        {
            if (!NetworkServer.active) return;
            NetworkServer.Destroy(gameObject);
        }

        private IEnumerator DelayedDisposure (float duration)
        {
            yield return new WaitForSeconds(duration);
            DisposeEquipment();
        }

        protected virtual void ProcessCollision ()
        {
            if (lethalEquipmentSO.TouchDownMode == TouchDownMode.NonInertial)
            {
                _rigidbody.velocity = Vector3.zero;
                return;
            }
            if (lethalEquipmentSO.TouchDownMode == TouchDownMode.Sticky)
            {
                _rigidbody.isKinematic = true;
                return;
            }
        }

        protected virtual void OnDestroy()
        {
            StopAllCoroutines();
        }
    }


    public interface ILethalEquipment
    {
        public void Construct(NetworkConnectionToClient conn,Vector3 throwDirection);
    }
}
