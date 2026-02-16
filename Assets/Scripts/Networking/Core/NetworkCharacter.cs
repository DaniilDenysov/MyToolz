using UnityEngine;
using Mirror;

namespace MyToolz.Networking.Core
{
    public class NetworkCharacter : NetworkBehaviour
    {
        [SerializeField] private NetworkPlayer owner;
        [SyncVar(hook = nameof(OnConnectionGuidChanged))] private uint ownerInstanceId;

        private void Start()
        {
            if (isOwned)
            {
                CmdSetConnectionGuid();
            }
        }

        [Command(requiresAuthority = false)]
        public void CmdSetConnectionGuid(NetworkConnectionToClient conn = null)
        {
            ownerInstanceId = conn.identity.netId;
        }

        private void OnConnectionGuidChanged(uint oldConn, uint newConn)
        {
            if (!NetworkClient.spawned.TryGetValue(newConn, out var owner)) return;
            if (!owner.TryGetComponent(out this.owner)) return;
            this.owner.SetCharacter(this);
        }
    }
}