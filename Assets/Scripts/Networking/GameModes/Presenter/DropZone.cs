using Mirror;
using MyToolz.DesignPatterns.EventBus;
using MyToolz.Networking.Events;
using MyToolz.Networking.GameModes.Events;
using MyToolz.Networking.GameModes.Model;
using MyToolz.Player.FPS.CombatSystem.Model;
using MyToolz.Player.FPS.PickUpSystem;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace MyToolz.Networking.GameModes.Presenter
{
    public class DropZone : NetworkBehaviour
    {
        [SyncVar] private string teamGuid;
        [SerializeField, Required] private LethalEquipmentSO expectedType;
        [SerializeField] private Image icon;
        [SerializeField] private Transform dropPoint;
        [SerializeField] private int packageCost = 25;

        private EventBinding<PlayersStateChanged> playerStateBinding;

        [Server]
        public void DropPackage(PlayerPickUp player,Crate pickable)
        {
            Core.NetworkPlayer networkPlayer = player.connectionToClient.identity.GetComponent<Core.NetworkPlayer>();
            if (networkPlayer == null) return;
            player.CmdReleaseCurrentPickable();
            pickable.CmdForceLock();
            pickable.ServerSetPositionAndRotation(dropPoint.position, Quaternion.identity);
            networkPlayer.AddPoints(packageCost);
        }

        public virtual void OnEnable()
        {
            playerStateBinding = new EventBinding<PlayersStateChanged>(OnPlayersStateChanged);
            EventBus<PlayersStateChanged>.Register(playerStateBinding);
        }
        public virtual void OnDisable()
        {
            EventBus<PlayersStateChanged>.Deregister(playerStateBinding);
        }

        private void OnPlayersStateChanged()
        {
            if (Core.NetworkPlayer.LocalPlayerInstance == null) return;
            icon.color = Core.NetworkPlayer.LocalPlayerInstance.TeamGuid.Equals(teamGuid) ? Color.green : Color.red;
        }

        public string GetTeamGuid()
        {
           return teamGuid;
        }

        [Server]
        public void SetTeamGuid(string guid)
        {
           teamGuid = guid;
        }

        //TODO: [DD] refactor pick up system / separate from lethal equipment / sync or fix in other way delivering of package
        private void OnTriggerEnter(Collider other)
        {
            if (!NetworkServer.active) return;
            if (!other.gameObject.TryGetComponent(out PlayerPickUp playerPickUp)) return;
            if (!playerPickUp.connectionToClient.identity.TryGetComponent(out Core.NetworkPlayer networkPlayer)) return;
            if (!networkPlayer.IsFriendly(teamGuid)) return;
            if (!playerPickUp.HasPickable()) return;
            if (!typeof(Crate).IsAssignableFrom(playerPickUp.CurrentPickable.GetType())) return;
            DropPackage(playerPickUp, (Crate)playerPickUp.CurrentPickable);
            EventBus<OnCrateDelivered>.Raise(new OnCrateDelivered()
            {
                Success = true
            });
        }
    }
}
