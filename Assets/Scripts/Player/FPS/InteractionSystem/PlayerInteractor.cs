using Mirror;
using MyToolz.HealthSystem.Interfaces;
using MyToolz.Networking.Extensions;
using System;
using UnityEngine;
using Zenject;

namespace MyToolz.Player.FPS.InteractionSystem.Model
{
    public class PlayerInteractor : NetworkBehaviour
    {
        [SerializeField] private Camera playerCamera;
        [SerializeField] private float interactRange = 3f;
        [SerializeField] private LayerMask interactableLayer;
        [SerializeField, Range(1f, 5f)] private float offset = 1f;

        [SyncVar] private InteractionConnection interactionConnection;

        protected IHealthModel hPModel;

        [Inject]
        private void Construct(IHealthModel hPModel)
        {
            this.hPModel = hPModel;
            hPModel.HealthChanged += OnHpChanged;
        }

        private void OnHpChanged((float currentHealth, float min, float max) tuple, float arg2)
        {
            if (!NetworkServer.active) return;
            if (tuple.currentHealth <= 0)
            {
                interactionConnection?.Close();
            }
        }

        private void Update()
        {
            if (!isOwned) return;
            CheckForInteractable();
        }

        private void CheckForInteractable()
        {
            Vector3 origin = playerCamera.transform.position + playerCamera.transform.forward * offset;
            Ray ray = new Ray(origin, playerCamera.transform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, interactRange, interactableLayer))
            {
                if (hit.collider.TryGetComponent(out NetworkInteractable interactable))
                {
                    CmdOpenConnection(interactable.netId);
                    return;
                }
            }
            CmdCloseCurrentConnection();
        }

        [Command(requiresAuthority = false)]
        private void CmdCloseCurrentConnection()
        {
            interactionConnection?.Close();
        }

        [Command(requiresAuthority = false)]
        private void CmdOpenConnection(uint netId)
        {
            var interactable = netId.ToNetworkInstance<NetworkInteractable>();
            if (interactable == null) return;
            if (!InteractionConnection.TryOpen(interactable, this, out var interaction)) return;
            interactionConnection = interaction;
            interactable.OnInteractStart(interaction);
        }   


        private void OnDestroy()
        {
            if (hPModel != null) hPModel.HealthChanged -= OnHpChanged;
            if (!NetworkServer.active) return;
            interactionConnection?.Close();
        }
    }
}