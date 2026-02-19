using MyToolz.EditorToolz;
using UnityEngine;

namespace MyToolz.ScriptableObjects.Player.Platformer.Movement
{
    [CreateAssetMenu(fileName = "RunWalkMovementStateSO", menuName = "ProjectClyde/Player/MovementStates/RunWalkMovementStateSO")]
    public class RunWalkMovementState : HorizontalMovementSO
    {
        [FoldoutGroup("Walk"), SerializeField, Range(1f, 1000f), Tooltip("Maximum horizontal speed while walking")] private float maxWalkSpeed = 80f;
        [FoldoutGroup("Run"), SerializeField, Tooltip("Maximum horizontal speed while running")] private float maxRunSpeed = 120f;

        public override void CalculateMovement(float acceleration, float decelleration, Vector2 moveInput)
        {
            float targetSpeed = moveInput.x; // * upgradePresenter.Get<float>(maxWalkSpeed, nameof(maxWalkSpeed)); //(runIsHeld ? upgradePresenter.Get<float>(maxRunSpeed, nameof(maxRunSpeed)) : upgradePresenter.Get<float>(_maxWalkSpeed, nameof(_maxWalkSpeed)));
            float lerp = (Mathf.Abs(moveInput.x) > 0.01f ? acceleration : decelleration) * Time.fixedDeltaTime;
            Velocity = new Vector2(Mathf.Lerp(Velocity.x, targetSpeed, lerp), 0f);
        }
    }
}

