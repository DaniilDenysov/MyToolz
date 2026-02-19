using Mirror;
using MyToolz.Networking.Extensions;
using MyToolz.Player.FPS.InteractionSystem.Interfaces;
using MyToolz.Player.FPS.InteractionSystem.Model;
using MyToolz.Utilities.Debug;
using System;
using System.Collections.Generic;

namespace MyToolz.Player.FPS.InteractionSystem.Interfaces
{
    public interface IInteractable
    {
        public void OnInteractStart(InteractionConnection interactor);
        public void OnInteractEnd(InteractionConnection interactor, bool successfully = false);
    }
}

namespace MyToolz.Player.FPS.InteractionSystem.Model
{
    [System.Serializable]
    public class InteractionConnection
    {
        private static HashSet<uint> interactions = new();
        public event Action OnConnectionClosed;
        private bool closed;
        public NetworkInteractable Interactable
        {
            get
            {
                return InteractableNetId.ToNetworkInstance<NetworkInteractable>();
            }
        }
        public PlayerInteractor Interactor
        {
            get
            {
                return InteractorNetId.ToNetworkInstance<PlayerInteractor>();
            }
        }
        public uint InteractableNetId;
        public uint InteractorNetId;

        public InteractionConnection()
        {

        }
        

        private InteractionConnection(NetworkInteractable interactable, PlayerInteractor interactor)
        {
            DebugUtility.Log(this, $"Interaction connection is opened for {interactable.name} and {interactor.name}");
            InteractableNetId = interactable.netId;
            InteractorNetId = interactor.netId;
            interactable.OnInteractStart(this);
        }

        public static InteractionConnection Open(NetworkInteractable interactable, PlayerInteractor interactor)
        {
            if (!interactable || !interactor)
            {
                return null;
            }
            if (!interactable.IsInteractable)
            {
                return null;
            }
            if (interactions.Contains(interactable.netIdentity.netId) || interactions.Contains(interactor.netIdentity.netId))
            {
                return null;
            }
            interactions.Add(interactable.netIdentity.netId);
            interactions.Add(interactor.netIdentity.netId);
            return new InteractionConnection(interactable, interactor);
        }

        public static bool TryOpen(NetworkInteractable interactable, PlayerInteractor interactor, out InteractionConnection connection)
        {
            connection = Open(interactable, interactor);
            return connection != null;
        }

        public void Close()
        {
            if (closed) return;
            closed = true;

            Interactable?.OnInteractEnd(this);

            if (Interactable.netIdentity != null) interactions.Remove(InteractableNetId);
            if (Interactor.netIdentity != null) interactions.Remove(InteractorNetId);  
            OnConnectionClosed?.Invoke();
            DebugUtility.Log(this, $"Interaction connection is closed for {Interactable.name} and {Interactor.name}");
        }
    }

    public class NetworkInteractable : NetworkBehaviour, IInteractable
    {
        [SyncVar] protected bool interactable = true;
        [SyncVar(hook = nameof(OnInteractorChanged))] protected InteractionConnection interactionConnection;
        public bool IsInteractable => interactable;
        protected NetworkIdentity interactor => interactionConnection?.Interactor?.netIdentity;
        //[SerializeField] protected Outline outline;

        [Server]
        public virtual void OnInteractStart(InteractionConnection newConnection)
        {
            if (newConnection == null)
            {
                DebugUtility.LogWarning(this, "[Interactions] Unable to interact with objects concurrently!");
                return;
            }

            if (interactionConnection != null && interactionConnection != newConnection)
            {
                interactionConnection.Close();
            }

            interactionConnection = newConnection;
        }

        [Server]
        public virtual void OnInteractEnd(InteractionConnection closingConnection, bool successfully = false)
        {
            if (closingConnection == null) return;

            if (interactionConnection == closingConnection)
            {
                interactionConnection = null;
            }
        }

        private void OnDestroy()
        {
            if (!NetworkServer.active) return;
            interactionConnection?.Close();
        }

        protected virtual void OnInteractorChanged(InteractionConnection oldValue, InteractionConnection newValue)
        {
            DebugUtility.Log(this, $"[Interactable] state changed from {oldValue} to {newValue}");
            bool enableOutline = interactable && newValue != null && newValue.Interactor != null && newValue.Interactor.isOwned;
            //outline.enabled = enableOutline;
        }

        [Command(requiresAuthority = false)]
        public void CmdToggleInteractable(bool interactable)
        {
            this.interactable = interactable;
        }
    }
}
