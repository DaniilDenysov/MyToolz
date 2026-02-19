using MyToolz.EditorToolz;
using MyToolz.Player.Platformer.Interfaces;
using MyToolz.Player.Platformer.Movement.Interfaces;
using UnityEngine;
using UnityEngine.UI;

namespace MyToolz.Player.Platformer.Movement.Interfaces
{
    public interface IPlayerHorizontalMovementView
    {
        public void Show(float velocity);
    }

    public interface IPlayerVerticalMovementView
    {
        public void OnJump();
        public void OnLanding(float landingForce);
    }
}

namespace MyToolz.Player.Platformer.Movement.View
{
    public class PlayerMovementView : MonoBehaviour, IPlayerVerticalMovementView, IPlayerHorizontalMovementView, IPlayerDashView
    {
        [SerializeField] private Image dashCD;
        //[SerializeField] private WorldVFX dashVFX;
        //[SerializeField] private MMF_Player player;
        [SerializeField] private Transform legs;
        //[SerializeField] private WorldVFX landingVFX;
        //[SerializeField] private WorldVFX jumpVFX;
        [SerializeField, Required] private Animator animator;
        [SerializeField] private int layerIndex = 1;

        public void OnJump()
        {
            //EventBus<PoolRequest<WorldVFX>>.Raise(new PoolRequest<WorldVFX> { Prefab = jumpVFX, Position = legs.transform.position });
        }

        public void OnLanding(float landingForce)
        {
            //landingFeedback.GenerateImpulseWithForce(Mathf.Abs(landingForce));
            //EventBus<PoolRequest<WorldVFX>>.Raise(new PoolRequest<WorldVFX> { Prefab = landingVFX, Position = legs.transform.position });
        }

        public void Dashing(Vector2 direction)
        {

        }

        public void DashStarted(Vector2 direction)
        {
            var dirNorm = direction.normalized;
            //EventBus<PoolRequest<WorldVFX>>.Raise(new PoolRequest<WorldVFX>
            //{
            //    Prefab = dashVFX,
            //    Position = legs.transform.position + new Vector3(-dirNorm.x, -dirNorm.y, 0f),
            //    Callback = (obj) => {
            //        obj.transform.rotation = Quaternion.Euler(0f, dirNorm.magnitude >= 0 ? 0f : 180f, 0f);
            //    }
            //});
            //player.PlayFeedbacks();
        }

        public void EndDashing(Vector2 direction)
        {

        }

        public void Show(float velocity)
        {
            //animator.SetLayerWeight(layerIndex,Mathf.Clamp01(velocity));

        }

        public void DashCoolDown(float dashed, float duration)
        {
           if (dashCD)
           {
                dashCD.fillAmount = 1 - Mathf.Clamp01((Time.time - dashed) / duration);
           }
        }
    }
}
