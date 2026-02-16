using MyToolz.AI.Platformer.Interfaces;
using MyToolz.Core;
using MyToolz.DesignPatterns.EventBus;
using MyToolz.Events;
using MyToolz.HealthSystem.Interfaces;
using MyToolz.Player.Platformer.Interfaces;
using MyToolz.Projectiles;
using MyToolz.ScriptableObjects.AI.Platformer;
using Sirenix.OdinInspector;
using UnityEngine;
using Zenject;

namespace MyToolz.AI.Platformer.Interfaces
{
    public interface IEnemyCombatPresenter
    {
        void AttackRanged();
        void Attack();
        void AttackRayMelee();
        void FireProjectile(Vector2 dir);
        void AttackBoxMelee(Transform attackPoint, Vector2 boxSize);
        void AttackCircleMelee(Transform attackPoint, float radius);
    }
}

namespace MyToolz.AI.Platformer.Presenters
{
    public class EnemyCombatPresenter : MonoBehaviourPlus, IEnemyCombatPresenter
    {
        [SerializeField, Required] protected Transform attackPoint;
        protected EnemyCombatSO enemyCombatSO => enemyModel?.EnemyCombatSO;
        protected Vector2 direction => movementModel.Direction;

        protected IReadOnlyEnemyMovementModel movementModel;
        protected IEnemyModel enemyModel;
        protected IHealthModel healthModel;

        [Inject]
        protected void Construct(IHealthModel healthModel,IEnemyModel enemyModel,IReadOnlyEnemyMovementModel movementModel)
        {
            this.healthModel = healthModel;
            this.enemyModel = enemyModel;
            this.movementModel = movementModel;
        }

        public void Attack()
        {
            switch (enemyCombatSO.Attack)
            {
                case AttackType.Melee:
                    switch (enemyCombatSO.CastType)
                    {
                        case AttackCastType.Ray:
                            AttackRayMelee();
                            break;
                        case AttackCastType.Circle:
                            AttackCircleMelee(attackPoint, enemyCombatSO.CircleRadius);
                            break;
                        case AttackCastType.Box:
                            AttackBoxMelee(attackPoint, enemyCombatSO.BoxSize);
                            break;
                    }
                    break;
                case AttackType.Ranged:
                    AttackRanged();
                    break;
            }
        }

        public virtual void AttackBoxMelee(Transform attackPoint,Vector2 boxSize)
        {
            var dist = enemyCombatSO.GetAttackRange();
            var mask = enemyCombatSO.LayerMask;
            if (direction.sqrMagnitude > 0f) direction.Normalize();
            var hits = BoxCastAttack(attackPoint, direction, boxSize, dist, mask);
            for (int i = 0; i < hits.Length; i++) ProcessHit(hits[i]);
        }

        public virtual void AttackCircleMelee(Transform attackPoint, float radius)
        {
            var dist = enemyCombatSO.GetAttackRange();
            var mask = enemyCombatSO.LayerMask;
            if (direction.sqrMagnitude > 0f) direction.Normalize();
            var hits = CircleCastAttack(attackPoint, direction, radius, dist, mask);
            for (int i = 0; i < hits.Length; i++) ProcessHit(hits[i]);
        }

        public virtual void AttackRayMelee()
        {
            var dist = enemyCombatSO.GetAttackRange();
            var mask = enemyCombatSO.LayerMask;
            if (direction.sqrMagnitude > 0f) direction.Normalize();
            var hits = RayCastAttack(direction, dist, mask);
            for (int i = 0; i < hits.Length; i++) ProcessHit(hits[i]);
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.TryGetComponent(out IDamagable damagable))
                damagable.DoDamage(enemyCombatSO.DamageType);

            if (!enemyCombatSO.EnableKnockOff)
                return;

            if (!enemyCombatSO.EnableKnockOff) return;

            if (collision.TryGetComponent(out IKnockOffable knockOffable))
            {
                var dir = (collision.transform.position - transform.position);
                var force = enemyCombatSO.CloseCombatKnockOffForce;
                knockOffable.KnockOff(dir.normalized * force);
            }
        }


        public void AttackRanged()
        {
            switch (enemyCombatSO.RangedAttackDirection)
            {
                case RangedAttackDirection.Directional:
                {
                        Vector3 dir = Vector3.zero;
                        if (enemyModel == null || enemyModel.Player == null)
                        {
                            dir = direction;
                        }
                        else
                        {
                            dir = (enemyModel.Player.position - transform.position).normalized;
                        }
                        FireProjectile(dir);
                        break;
                }
                case RangedAttackDirection.Horizontal:
                {
                        var d = direction;
                        var sx = d.x != 0f ? Mathf.Sign(d.x) : Mathf.Sign((attackPoint ? attackPoint.right.x : transform.right.x));
                        FireProjectile(new Vector2(sx, 0f));
                        break;
                }
                case RangedAttackDirection.Vertical:
                {
                        var d = direction;
                        var sy = d.y != 0f ? Mathf.Sign(d.y) : Mathf.Sign((attackPoint ? attackPoint.up.y : transform.up.y));
                        FireProjectile(new Vector2(0f, sy));
                        break;
                }
            }
        }

        public void FireProjectile(Vector2 dir)
        {
            if (dir.sqrMagnitude <= 0f) dir = Vector2.right;
            dir.Normalize();

            var pos = attackPoint ? attackPoint.position : transform.position;
            var rot = Quaternion.FromToRotation(Vector3.right, new Vector3(dir.x, dir.y, 0f));

            EventBus<PoolRequest<Projectile>>.Raise(new PoolRequest<Projectile>()
            {
                Prefab = enemyCombatSO.Projectile,
                Rotation = rot,
                Position = pos,
                Callback = p =>
                {
                    p.transform.position = pos;
                    p.transform.rotation = rot;
                    p.Initialize(new ProjectileInit()
                    {
                        Origin = pos,
                        Direction = dir,
                        DamageType = enemyCombatSO.DamageType,
                    });                  
                    Log($"Enemy fired projectile at {dir} from {pos}");
                }
            });
        }

        protected virtual void ProcessHit(RaycastHit2D hit)
        {
            if (!hit.collider) return;

            if (hit.collider.TryGetComponent(out IDamagable damagable)) damagable.DoDamage(enemyCombatSO.DamageType);

            if (!enemyCombatSO.EnableKnockOff) return;
            if (hit.collider.TryGetComponent(out IKnockOffable knockOffable))
            {
                var dir = -hit.normal;
                var force = enemyCombatSO.GetKnockOffForce();
                knockOffable.KnockOff(dir.normalized * force);
            }
        }

        protected RaycastHit2D[] RayCastAttack(Vector2 dir, float distance, LayerMask mask)
        {
            var origin = attackPoint ? (Vector2)attackPoint.position : (Vector2)transform.position;
            var hits = Physics2D.RaycastAll(origin, dir, distance, mask);
            return hits;
        }

        protected RaycastHit2D[] CircleCastAttack(Transform attackPoint, Vector2 dir, float radius, float distance, LayerMask mask)
        {
            var origin = attackPoint ? (Vector2)attackPoint.position : (Vector2)transform.position;
            var hits = Physics2D.CircleCastAll(origin, radius, dir, distance, mask);
            return hits;
        }

        protected RaycastHit2D [] BoxCastAttack(Transform attackPoint, Vector2 dir, Vector2 size, float distance, LayerMask mask)
        {
            var origin = attackPoint ? (Vector2)attackPoint.position : (Vector2)transform.position;
            var angle = attackPoint ? attackPoint.eulerAngles.z : 0f;
            var hits = Physics2D.BoxCastAll(origin, size, angle, dir, distance, mask);
            return hits;

        }

        protected RaycastHit2D[] BoxCastAttack(Vector2 attackPoint, Vector2 dir, Vector2 size, float distance, LayerMask mask)
        {
            var hits = Physics2D.BoxCastAll(attackPoint, size, 0f, dir, distance, mask);
            return hits;
        }

#if UNITY_EDITOR
        protected void OnDrawGizmos()
        {
            if (enemyCombatSO == null) return;
            var origin = attackPoint ? attackPoint.position : transform.position;
            var dir = attackPoint ? attackPoint.right : transform.right;
            dir.z = 0f;
            if (dir.sqrMagnitude > 0f) dir.Normalize();
            var distance = enemyCombatSO.RandomizeRange ? enemyCombatSO.RandomAttackRange.y : enemyCombatSO.AttackRange;
            Gizmos.color = Color.red;
            if (enemyCombatSO.CastType == AttackCastType.Ray)
            {
                Gizmos.DrawLine(origin, origin + dir * distance);
                Gizmos.DrawWireSphere(origin + dir * distance, 0.05f);
            }
            else if (enemyCombatSO.CastType == AttackCastType.Circle)
            {
                var r = enemyCombatSO.CircleRadius;
                var end = origin + dir * distance;
                var perp = Vector3.Cross(dir, Vector3.forward).normalized * r;
                Gizmos.DrawWireSphere(origin, r);
                Gizmos.DrawWireSphere(end, r);
                Gizmos.DrawLine(origin + perp, end + perp);
                Gizmos.DrawLine(origin - perp, end - perp);
            }
            else if (enemyCombatSO.CastType == AttackCastType.Box)
            {
                var size = new Vector3(enemyCombatSO.BoxSize.x, enemyCombatSO.BoxSize.y, 0.01f);
                var end = origin + dir * distance;
                var angle = attackPoint ? attackPoint.eulerAngles.z : 0f;
                var rot = Quaternion.Euler(0f, 0f, angle);
                var mStart = Matrix4x4.TRS(origin, rot, Vector3.one);
                var mEnd = Matrix4x4.TRS(end, rot, Vector3.one);
                var prev = Gizmos.matrix;
                Gizmos.matrix = mStart;
                Gizmos.DrawWireCube(Vector3.zero, size);
                Gizmos.matrix = mEnd;
                Gizmos.DrawWireCube(Vector3.zero, size);
                Gizmos.matrix = prev;
                var hx = size.x * 0.5f;
                var hy = size.y * 0.5f;
                var c1s = origin + rot * new Vector3(-hx, -hy, 0f);
                var c2s = origin + rot * new Vector3(-hx, hy, 0f);
                var c3s = origin + rot * new Vector3(hx, hy, 0f);
                var c4s = origin + rot * new Vector3(hx, -hy, 0f);
                var c1e = end + rot * new Vector3(-hx, -hy, 0f);
                var c2e = end + rot * new Vector3(-hx, hy, 0f);
                var c3e = end + rot * new Vector3(hx, hy, 0f);
                var c4e = end + rot * new Vector3(hx, -hy, 0f);
                Gizmos.DrawLine(c1s, c1e);
                Gizmos.DrawLine(c2s, c2e);
                Gizmos.DrawLine(c3s, c3e);
                Gizmos.DrawLine(c4s, c4e);
            }
        }
#endif
    }
}
