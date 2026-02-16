using MyToolz.DesignPatterns.EventBus;
using MyToolz.Events;
using MyToolz.Projectiles;
using MyToolz.ScriptableObjects.Player.Platformer.Combat;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MyToolz.ScriptableObjects.Player.Platformer.Combat
{
    [CreateAssetMenu(fileName = "RangedPlayerAttackSO", menuName = "MyToolz/Player/AttackStates/RangedPlayerAttackSO")]
    public class RangedPlayerAttackSO : PlayerAttackSO
    {
        [FoldoutGroup("Ranged"), SerializeField] protected Projectile projectile;
        [FoldoutGroup("Ranged"), SerializeField] protected Vector3 startDirectionVector = Vector3.forward;
        [FoldoutGroup("Ranged"), SerializeField, Min(0.1f)] protected float projectileSpeed = 14f;
        [FoldoutGroup("Ranged"), SerializeField, Min(0.1f)] protected float projectileLifeTime = 6f;

        public Projectile Projectile => projectile;

        public override bool ProcessAttack(out RaycastHit2D hit)
        {
            playerMovementModel.SetExternalVelocity((playerMovementModel.ExternalVelocity + Direction) * pullForce);
            view.ShowAttack(false);
            hit = default;
            if (projectile != null)
            {
                EventBus<PoolRequest<Projectile>>.Raise(new PoolRequest<Projectile>()
                {
                    Prefab = projectile,
                    Position = shootPoint.position,
                    Rotation = Quaternion.LookRotation(startDirectionVector, Direction),
                    Callback = (obj) =>
                    {
                        obj.Initialize(new ProjectileInit
                        {
                            Origin = shootPoint.position,
                            Direction = Direction,
                            DamageType = damageType
                        });
                    }
                });
            }
            return false;
        }
    }
}
