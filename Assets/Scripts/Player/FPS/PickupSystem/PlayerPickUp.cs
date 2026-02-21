using Mirror;
using MyToolz.DesignPatterns.EventBus;
using MyToolz.Events;
using MyToolz.Networking.Events;
using MyToolz.Networking.PickUpSystem;
using MyToolz.Player.FPS.LoadoutSystem.Model;
using UnityEngine;

namespace MyToolz.Player.FPS.PickUpSystem
{
    public class PlayerPickUp : NetworkBehaviour, IEventListener
    {
        [SerializeField] private Camera playerCamera;
        [SerializeField] private Transform pickUpPoint;
        [SerializeField] private WeaponLoadoutModel loadoutModel;
        public Transform PickUpPoint => pickUpPoint;
        [SyncVar, SerializeField] private uint pickupId = uint.MaxValue;
        public Pickable CurrentPickable
        {
            get
            {
                if (pickupId == uint.MaxValue) return null;
                if (!NetworkServer.spawned.TryGetValue(pickupId, out var result)) return null;
                return result.GetComponent<Pickable>();
            }
        }

        private EventBinding<OnPlayerPickUp> onPlayerPickUpEventBinding;

        private void Start()
        {
            RegisterEvents();
        }

        private void OnDisable()
        {
            UnregisterEvents();
        }

        public bool HasPickable() => pickupId != uint.MaxValue;

        private void OnPickUp(OnPlayerPickUp onPlayerPickUp)
        {
            if (CurrentPickable)
            {
                CmdReleaseCurrentPickableNotify();
            }
            else if (onPlayerPickUp.Pickable)
            {
                loadoutModel.SelectWeaponByIndex(1);
                SetPickUpGuid(onPlayerPickUp.Pickable.netId);
            }
        }

        [Command(requiresAuthority = false)]
        public void CmdReleaseCurrentPickableNotify()
        {
            CurrentPickable.CmdForceDrop();
            CurrentPickable.ReleaseStrategy?.Release(playerCamera.transform.forward);
            CmdReleaseCurrentPickable();
        }

        [Command(requiresAuthority = false)]
        public void CmdReleaseCurrentPickable()
        {
            SetPickUpGuid(uint.MaxValue);
            TargetRpcReleaseCurrentPickable();
        }

        [TargetRpc]
        public void TargetRpcReleaseCurrentPickable()
        {
            //InputStateManager.Instance.ChangeState(typeof(DefaultState));
            loadoutModel.SelectHiddenWeapon();
        }

        [Command(requiresAuthority = false)]
        private void SetPickUpGuid(uint guid)
        {
            pickupId = guid;
            if (CurrentPickable)
            {
                CurrentPickable.SetSnapPoint(pickUpPoint);
            }
        }

        public override void OnStopClient()
        {
            if (NetworkServer.active && CurrentPickable != null)
            {
                CurrentPickable.CmdForceDrop();
            }
        }

        public void RegisterEvents()
        {
            if (isOwned)
            {
                onPlayerPickUpEventBinding = new EventBinding<OnPlayerPickUp>(OnPickUp);
                EventBus<OnPlayerPickUp>.Register(onPlayerPickUpEventBinding);
            }
        }

        public void UnregisterEvents()
        {
            EventBus<OnPlayerPickUp>.Deregister(onPlayerPickUpEventBinding);
            if (isOwned && NetworkClient.active)
            {
                if (CurrentPickable)
                {
                    CurrentPickable.CmdForceDrop();
                    SetPickUpGuid(uint.MaxValue);
                }
            }
        }
    }
}
