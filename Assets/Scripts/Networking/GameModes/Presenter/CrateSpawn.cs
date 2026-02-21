using Mirror;
using MyToolz.DesignPatterns.EventBus;
using MyToolz.Events;
using MyToolz.Networking.GameModes.Events;
using MyToolz.Networking.PickUpSystem;
using UnityEngine;

namespace MyToolz.Networking.GameModes.Events
{
    public struct OnRoundStart : IEvent
    {

    }

    public struct OnCrateDelivered : IEvent
    {
        public bool Success;
    }
}

namespace MyToolz.Networking.GameModes.Presenter
{
    public class CrateSpawn : NetworkBehaviour, IEventListener
    {
        [SerializeField] private float delay = 10f;
        [SerializeField] private Pickable cratePrefab;
        private Pickable currentCrate;
        private EventBinding<OnRoundStart> onRoundStartEventBinding;
        private EventBinding<OnCrateDelivered> crateDeliveredEventBinding;

        private void OnEnable()
        {
            RegisterEvents();
        }

        private void OnDisable()
        {
            UnregisterEvents();
        }


        private void CrateDelivered()
        {
            if (!NetworkServer.active) return;
            OnNewRoundStarted();
        }

        private void OnNewRoundStarted()
        {
            if (!NetworkServer.active) return;
            //deliveryAnimation?.PlayFeedbacks();
        }

        [Server]
        private void DestroyCurrentCrate()
        {
            if (currentCrate == null) return;
            NetworkServer.Destroy(currentCrate.gameObject);
        }

        [Server]
        public void SpawnNewCrate()
        {
            if (!NetworkServer.active) return;
            DestroyCurrentCrate();
            currentCrate = Instantiate(cratePrefab, transform.position, Quaternion.identity);
            NetworkServer.Spawn(currentCrate.gameObject);
        }

        public void RegisterEvents()
        {
            crateDeliveredEventBinding = new EventBinding<OnCrateDelivered>(CrateDelivered);
            EventBus<OnCrateDelivered>.Register(crateDeliveredEventBinding);
            onRoundStartEventBinding = new EventBinding<OnRoundStart>(OnNewRoundStarted);
            EventBus<OnRoundStart>.Register(onRoundStartEventBinding);
        }

        public void UnregisterEvents()
        {
            EventBus<OnCrateDelivered>.Deregister(crateDeliveredEventBinding);
            EventBus<OnRoundStart>.Deregister(onRoundStartEventBinding);
        }
    }
}
