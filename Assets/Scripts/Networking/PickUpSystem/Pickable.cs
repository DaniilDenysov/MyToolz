using LBG;
using Mirror;
using MyToolz.DesignPatterns.EventBus;
using MyToolz.Events;
using MyToolz.Extensions;
using MyToolz.HealthSystem.Interfaces;
using MyToolz.Networking.Events;
using MyToolz.Networking.GameModes.Events;
using MyToolz.Networking.PickUpSystem;
using MyToolz.Player.FPS.DisposableObjects;
using MyToolz.Player.FPS.InteractionSystem.Model;
using MyToolz.ScriptableObjects.Audio;
using MyToolz.Strategies;
using MyToolz.Utilities.Debug;
using System;
using UnityEngine;

namespace MyToolz.Networking.Events
{
    public struct OnPlayerPickUp : IEvent
    {
        public Pickable Pickable;
    }
}

namespace MyToolz.Networking.PickUpSystem
{
    #region Strategies
    public abstract class PickableStrategy
    {
        protected GameObject context;
        protected NetworkRigidbodyReliable rigidbody;

        public virtual void Construct(GameObject context)
        {
            this.context = context;
            rigidbody = context.GetComponent<NetworkRigidbodyReliable>();
        }

    }


    [System.Serializable]
    public abstract class ReleaseStrategy : PickableStrategy
    {
        public virtual void Release(Vector3 direction)
        {

        }
    }

    [System.Serializable]
    public class ThrowReleaseStrategy : ReleaseStrategy
    {
        [SerializeField, Range(0f, 100f)] private float throwForce = 5f;
        public override void Release(Vector3 direction)
        {
            //rigidbody.AddForce(direction * throwForce, ForceMode.Impulse);
        }
    }

    [System.Serializable]
    public class DefaultReleaseStrategy : ReleaseStrategy
    {
       
    }

    [System.Serializable]
    public abstract class CollisionStrategy : PickableStrategy
    {
        public virtual void ProcessCollision(Collision collision)
        {

        }
    }


    [System.Serializable]
    public class ThrowCollisionStrategy : CollisionStrategy
    {
        [SerializeField, Range(0f,100f)] private float crashThreshold = 5f;
        [SerializeField] private DisposableParticleSystem disposableParticleSystem;

        public override void ProcessCollision(Collision collision)
        {
            //if (rigidbody.velocity.magnitude > crashThreshold)
            //{
            //    //TODO: [DD] add stunning handling
            //    if (context.TryGetComponent(out Pickable pickable))
            //    {
            //        pickable.PlayVFX(disposableParticleSystem, context.transform.position);
            //    }
            //    NetworkServer.Destroy(context.gameObject);
            //}
        }
    }

    [System.Serializable]
    public class DropCollisionStrategy : CollisionStrategy
    {

    }
    #endregion

    [System.Serializable]
    public abstract class PickableState
    {
        protected Pickable context;

        public virtual void Construct(Pickable context)
        {
            this.context = context;
        }
        public virtual void OnPickupButtonPressed() { }
        public virtual void OnDropButtonPressed() { }
        public virtual void OnInteractStart() { }
        public virtual void OnInteractEnd() { }
        public virtual void OnEnter() { }
        public virtual void OnUpdate() { }
        public virtual void OnExit() { }

    }

    [System.Serializable]
    public class PickedUpState : PickableState
    {
        protected Collider collider;
        protected Rigidbody rigidbody;

        public override void Construct(Pickable context)
        {
            base.Construct(context);
            collider = context.GetComponent<Collider>();
            rigidbody = context.GetComponent<Rigidbody>();
        }

        public override void OnEnter()
        {
            if (NetworkServer.active)
            {
                context.CmdToggleInteractable(false);
            }

            rigidbody.constraints = RigidbodyConstraints.FreezeAll;
            collider.enabled = false;
            var drop = context.DropButton;
            if (drop != null)
            {
                drop.OnEnable();
                drop.OnFired += OnDrop;
            }
        }

        public override void OnUpdate()
        {
            if (!NetworkServer.active) return;
            if (!context.SnapPoint) return;
            context.transform.position = context.SnapPoint.position;
            context.transform.rotation = context.SnapPoint.rotation;
        }

        public override void OnExit()
        {
            rigidbody.constraints = RigidbodyConstraints.None;
            collider.enabled = true;
            var drop = context.DropButton;
            if (drop != null)
            {
                drop.OnDisable();
                drop.OnFired -= OnDrop;
            }
        }

        private void OnDrop()
        {
            EventBus<OnPlayerPickUp>.Raise(new OnPlayerPickUp { Pickable = context });
            context.CmdChangeState(typeof(IdleState).ToString());
        }

    }

    [System.Serializable]
    public class IdleState : PickableState
    {
        public override void OnEnter()
        {
            if (NetworkServer.active)
            {
                context.SetSnapPoint(null);
                context.CmdToggleInteractable(true);
            }
        }

        public override void OnExit()
        {
            OnInteractEnd();
        }

        public override void OnInteractStart()
        {
            var pickup = context.PickupButton;
            if (pickup != null)
            {
                pickup.OnEnable();
                pickup.OnFired += OnPickup;
            }
        }

        public override void OnInteractEnd()
        {
            var pickup = context.PickupButton;
            if (pickup != null)
            {
                pickup.OnDisable();
                pickup.OnFired -= OnPickup;
            }
        }

        private void OnPickup()
        {
            EventBus<OnPlayerPickUp>.Raise(new OnPlayerPickUp { Pickable = context });
            context.CmdChangeState(typeof(PickedUpState).ToString());
        }

    }

    [System.Serializable]
    public class LockedState : PickableState
    {
        public override void OnEnter()
        {
            if (!NetworkServer.active) return;
            context.SetSnapPoint(null);
            context.CmdToggleInteractable(false);
        }
    }

    [RequireComponent(typeof(Rigidbody), typeof(Collider))]
    public class Pickable : NetworkInteractable, IKillable
    {
        [SerializeReference, SubclassSelector] private ButtonStrategy pickupButton;
        [SerializeReference, SubclassSelector] private ButtonStrategy dropButton;

        [SerializeReference, SubclassSelector] private CollisionStrategy collisionStrategy;
        [SerializeReference, SubclassSelector] private ReleaseStrategy releaseStrategy;

        [SerializeField] private AudioClipSO collisionSound;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private DisposableParticleSystem landingVFXPrefab;


        public ButtonStrategy PickupButton => pickupButton;
        public ButtonStrategy DropButton => dropButton;
        public AudioSource AudioSource => audioSource;
        public AudioClipSO CollisionSound => collisionSound;
        public DisposableParticleSystem LandingVFXPrefab => landingVFXPrefab;
        public ReleaseStrategy ReleaseStrategy => releaseStrategy;

        public Transform SnapPoint;

        private float lastImpact = float.MinValue;

        [SyncVar(hook = nameof(OnPickableStateChanged))] private string syncedState;

        private PickableState currentState;


        private void Awake()
        {
            collisionStrategy?.Construct(gameObject);
            releaseStrategy?.Construct(gameObject);
        }

        private void Start()
        {
            if (!NetworkServer.active) return;
            CmdChangeState(typeof(IdleState).ToString());
        }

        private void OnPickableStateChanged(string oldState, string newState)
        {
            var type = Type.GetType(newState);
            if (!typeof(PickableState).IsAssignableFrom(type)) return;
            PickableState newStateInstance = Activator.CreateInstance(type) as PickableState;
            if (newStateInstance == null) return;
            ChangeState(newStateInstance);
        }

        protected override void OnInteractorChanged(InteractionConnection oldValue, InteractionConnection newValue)
        {
            base.OnInteractorChanged(oldValue, newValue);

            bool wasLocal = oldValue != null && oldValue.Interactor != null && oldValue.Interactor.isOwned;
            bool isLocal = newValue != null && newValue.Interactor != null && newValue.Interactor.isOwned;

            if (!wasLocal && isLocal)
            {
                DebugUtility.Log(this, $"[Pickable] Local interaction started with {name}");
                currentState?.OnInteractStart();
            }
            else if (wasLocal && !isLocal)
            {
                DebugUtility.Log(this, $"[Pickable] Local interaction ended with {name}");
                currentState?.OnInteractEnd();
            }
        }


        [Command(requiresAuthority = false)]
        public void CmdChangeState(string newState)
        {
            if (!typeof(PickableState).IsAssignableFrom(Type.GetType(newState))) return;
            syncedState = newState;
        }
        
        public void ChangeState(PickableState newState)
        {
            if (newState == null) return;
            if (newState == currentState) return;
            currentState?.OnExit();
            newState?.Construct(this);
            currentState = newState;
            currentState?.OnEnter();
        }

        [Server]
        public override void OnInteractStart(InteractionConnection interactor)
        {
            if (!interactable) return;
            base.OnInteractStart(interactor);
            currentState?.OnInteractStart();
        }

        [Server]
        public override void OnInteractEnd(InteractionConnection interactor, bool successfully = false)
        {
            if (!interactable) return;
            base.OnInteractEnd(interactor);
            currentState?.OnInteractEnd();
        }


        [Server]
        public void SetSnapPoint(Transform snapPoint)
        {
            SnapPoint = snapPoint;
        }

        private void Update()
        {
            currentState?.OnUpdate();
        }


        #region Commands
        [Server]
        public void ServerSetPositionAndRotation(Vector3 pos, Quaternion quaternion)
        {
            transform.position = pos;
            transform.rotation = quaternion;
        }
        #endregion


        [Command(requiresAuthority = false)]
        public void CmdForceDrop()
        {
            CmdChangeState(typeof(IdleState).ToString());
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (NetworkServer.active) collisionStrategy?.ProcessCollision(collision);
            OnImpact(collision.GetContact(0).point);
        }

        public void PlayVFX(DisposableParticleSystem particleSystem,Vector3 point)
        {
            if (particleSystem == null) return;

            EventBus<PoolRequest<DisposableParticleSystem>>.Raise(new PoolRequest<DisposableParticleSystem>()
            {
                Prefab = particleSystem,
                Position = point
            });
        }

        private void OnImpact(Vector3 point)
        {
            //PlayVFX(landingVFXPrefab, point);
            if (collisionSound == null) return;
            if (lastImpact + collisionSound.MinimalInterval > Time.time) return;
            audioSource.Play(collisionSound);
            lastImpact = Time.time;
        }

        public void Kill()
        {
            if (!NetworkServer.active) return;
            EventBus<OnCrateDelivered>.Raise(new OnCrateDelivered());
        }
    }
}
