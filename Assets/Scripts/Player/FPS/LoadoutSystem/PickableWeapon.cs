using Mirror;
using MyToolz.DesignPatterns.EventBus;
using MyToolz.Events;
using MyToolz.Player.FPS.CombatSystem.Events;
using MyToolz.Player.FPS.InteractionSystem.Model;
using MyToolz.Strategies;
using UnityEngine;

namespace MyToolz.Player.FPS.CombatSystem.Model
{
    public class PickableWeapon : NetworkInteractable
    {

        [SerializeReference, SubclassSelector] private ButtonStrategy pickupButtonStrategy;
        [SerializeField] private int currentBullets;
        [SerializeField, SyncVar] private string weaponGuid;
        [SerializeField, Range(0f, 60f)] private float disposeTime = 15f;
        private float timer;

        private void Start()
        {
            if (!NetworkServer.active) return;
            ResetTimer();
        }

        public void Construct(int currentBullets, string weaponGuid)
        {
            this.weaponGuid = weaponGuid;
            this.currentBullets = currentBullets;
            ResetTimer();
        }


        public override void OnInteractStart(InteractionConnection interactor)
        {
            base.OnInteractStart(interactor);
            if (!interactable) return;
            pickupButtonStrategy?.OnEnable();
            pickupButtonStrategy.OnFired += PickUpWeapon;
        }


        private void Update()
        {
            if (!NetworkServer.active) return;
            if (timer + disposeTime > Time.time) return;
            CmdRelease();
        }

        private void ResetTimer()
        {
            timer = Time.time;
        }

        public override void OnInteractEnd(InteractionConnection interactor, bool successfully = false)
        {
            base.OnInteractEnd(interactor);
            pickupButtonStrategy?.OnDisable();
            pickupButtonStrategy.OnFired -= PickUpWeapon;
        }

        private void PickUpWeapon()
        {
            OnInteractEnd(null);
            EventBus<OnWeaponPickedUp>.Raise(new OnWeaponPickedUp()
            {
                WeaponGuid = weaponGuid,
                Position = transform.position,
                CurrentBullets = currentBullets
            });
            CmdRelease();
        }

        [Command(requiresAuthority = false)]
        public void CmdRelease()
        {
            EventBus<ReleaseRequest<PickableWeapon>>.Raise(new ReleaseRequest<PickableWeapon>() { PoolObject = this });
        }
    }
}
