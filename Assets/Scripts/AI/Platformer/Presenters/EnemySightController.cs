using MyToolz.EditorToolz;
using MyToolz.HealthSystem.Interfaces;
using MyToolz.ScriptableObjects.AI.Platformer;
using MyToolz.Utilities.Debug;
using UnityEngine;
using UnityEngine.Events;
using Zenject;

namespace MyToolz.AI.Platformer.Presenters
{
    public interface IReadOnlyEnemyModel
    {
        public Transform UnitContext { get; }
        public Transform Context { get; }
        public float LastAttacked { get; }
        public EnemyCombatSO EnemyCombatSO { get; }
        public EnemyMovementSO EnemyMovementSO { get; }
        public Transform Player { get; }
        public Vector3 LastKnownPlayerPosition { get; }
    }

    public interface IEnemyModel : IReadOnlyEnemyModel
    {
        public void SetLastAttack(float time);
        public void SetPlayer(Transform player);
        public void SetLastKnownPlayerPosition(Vector2 lastKnownPlayerPosition);
        public void SetCombatSO(EnemyCombatSO enemyCombatSO);
        public void SetMovementSO(EnemyMovementSO enemyMovementSO);
    }

    [System.Serializable]
    public class EnemyModel :  IEnemyModel
    {
        public Transform Context => context;
        [SerializeField, Required] private Transform context;

        public Transform UnitContext => unitContext;
        [SerializeField, Required] private Transform unitContext;

        public Transform Player => player;
        private Transform player;
        public Vector3 LastKnownPlayerPosition => lastKnownPlayerPosition;
        public Vector3 lastKnownPlayerPosition;

        public EnemyCombatSO EnemyCombatSO => enemyCombatSO;
        private EnemyCombatSO enemyCombatSO;

        public EnemyMovementSO EnemyMovementSO => enemyMovementSO;
        private EnemyMovementSO enemyMovementSO;

        public float LastAttacked => lastAttacked;
        private float lastAttacked;

        public void SetLastKnownPlayerPosition(Vector2 lastKnownPlayerPosition)
        {
            this.lastKnownPlayerPosition = lastKnownPlayerPosition;
        }

        public void SetPlayer(Transform player)
        {
            this.player = player;
        }

        public void Construct(EnemyCombatSO enemyCombatSO, EnemyMovementSO enemyMovementSO)
        {
            if (enemyMovementSO == null || enemyCombatSO == null)
            {
                DebugUtility.LogError(this, "SO's are null!");
                return;
            }
            this.enemyMovementSO = enemyMovementSO;
            this.enemyCombatSO = enemyCombatSO;
        }

        public void SetCombatSO(EnemyCombatSO enemyCombatSO)
        {
            if (enemyCombatSO == null) return;
            this.enemyCombatSO = enemyCombatSO;
        }

        public void SetMovementSO(EnemyMovementSO enemyMovementSO)
        {
            if (enemyMovementSO == null) return;
            this.enemyMovementSO = enemyMovementSO;
        }

        public void SetLastAttack(float time)
        {
            lastAttacked = time;
        }
    }
}

namespace MyToolz.AI.Platformer.Presenters
{
    public class EnemySightController : MonoBehaviour
    {
        [FoldoutGroup("Sight"), SerializeField] private Transform sightOrigin;
        [FoldoutGroup("Sight"), SerializeField] private Transform proximitySightOrigin;
        [FoldoutGroup("Sight"), SerializeField] private UnityEvent onPlayerSpotted;
        [FoldoutGroup("Sight"), SerializeField] private UnityEvent onPlayerUnspotted;
        private Vector3 lastKnownPlayerPosition
        {
            get => model.LastKnownPlayerPosition;
            set => model.SetLastKnownPlayerPosition(value);
        }
        private EnemyCombatSO enemyCombatSO => model.EnemyCombatSO; 
        private Vector3 lookDirection => movementModel.Direction;
        private Transform player
        {
            set
            {
                model.SetPlayer(value);
            }
            get
            {
                return model.Player;
            }
        }
        private IEnemyModel model;
        private IReadOnlyEnemyMovementModel movementModel;

        [Inject]
        private void Construct(IEnemyModel model, IReadOnlyEnemyMovementModel movementModel)
        {
            this.model = model;
            this.movementModel = movementModel;
        }

        private Transform TryUpdateSight()
        {
            Transform player = null;
            if (enemyCombatSO == null) return player;
            var origin = sightOrigin != null ? sightOrigin.position : transform.position;
            if (enemyCombatSO.SightCastType == SightCastType.Circle)
            {
                TryCircleSight(origin, enemyCombatSO.ViewDistance, out player);
            }
            else if (enemyCombatSO.SightCastType == SightCastType.Ray)
            {
                TryConeRaySight(origin, out player);
            }
            else
            {
                TryBoxSight(origin + enemyCombatSO.SightBoxOffset, enemyCombatSO.SightBoxSize, out player);
            }
            if (player == null) player = UpdateProximity();
            return player;
        }

        private Transform UpdateProximity()
        {
            Transform player = null;
            Transform origin = proximitySightOrigin == null ? transform : proximitySightOrigin;
            if (enemyCombatSO.ProximityCheckCastType == ProximityCastType.Circle) TryCircleSight(origin.position, enemyCombatSO.ProximityRadius, out player);
            else TryBoxSight(origin.position + enemyCombatSO.ProximityBoxOffset, enemyCombatSO.ProximityBoxSize, out player);
            return player;
        }

        private bool TryCircleSight(Vector3 origin, float radius, out Transform player) 
        {
            return TryCheck(Physics2D.OverlapCircleAll(origin, radius, enemyCombatSO.ViewLayerMask), origin, out player);
        }

        private bool TryCheck(Collider2D[] hits, Vector3 origin, out Transform player)
        {
            player = null;
            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i] == null) continue;
                if (!IsTargetCollider(hits[i])) continue;
                player = hits[i].transform;
                if (!enemyCombatSO.CheckObstracted) return true;
                var dir = (Vector2)(player.position - origin);
                var hit = Physics2D.Raycast(origin, dir.normalized, dir.magnitude, enemyCombatSO.ViewLayerMask);
                if (hit.collider != null && IsTargetCollider(hit.collider))
                {
                    player = hit.collider.transform;
                    return true;
                }
            }
            return false;
        }

        private bool TryBoxSight(Vector3 origin, Vector2 boxSize, out Transform player)
        {
            return TryCheck(
                Physics2D.OverlapBoxAll(origin, boxSize, 0f, enemyCombatSO.ViewLayerMask),
                origin,
                out player
            );
        }


        private bool TryConeRaySight(Vector3 origin, out Transform player)
        {
            player = null;
            var baseDir = lookDirection;
            baseDir.z = 0f;
            if (baseDir.sqrMagnitude > 0f) baseDir.Normalize();
            var half = enemyCombatSO.ConeViewSize * 0.5f;
            var steps = Mathf.Max(1u, enemyCombatSO.Precision);
            for (uint i = 0; i <= steps; i++)
            {
                var t = steps == 0 ? 0f : (float)i / steps;
                var angle = Mathf.Lerp(-half, half, t);
                var dir = Quaternion.Euler(0f, 0f, angle) * baseDir;
                var hit = Physics2D.Raycast(origin, dir, enemyCombatSO.ViewDistance, enemyCombatSO.ViewLayerMask);
                if (hit.collider == null) continue;
                if (!IsTargetCollider(hit.collider)) continue;
                if (!enemyCombatSO.CheckObstracted)
                {
                    player = hit.collider.transform;
                    return true;
                }
                if (IsTargetCollider(hit.collider))
                {
                    player = hit.collider.transform;
                    return true;
                }
            }
            return false;
        }

        private bool IsTargetCollider(Collider2D c)
        {
            return c.gameObject.TryGetComponent(out IDamagable damagable);
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            var so = model != null ? enemyCombatSO : null;
            if (so == null) return;
            var origin = sightOrigin != null ? sightOrigin.position : transform.position;
            var proximityOrigin = proximitySightOrigin != null ? proximitySightOrigin.position : transform.position;
            var dir = lookDirection;
            if (dir.sqrMagnitude > 0f) dir.Normalize();
            var prev = Gizmos.color;
            Gizmos.color = Color.green;
            if (so.ProximityCheckCastType == ProximityCastType.Circle)
            {
                GizmosDrawSphere(proximityOrigin, so.ViewDistance);
            }
            else
            {
                GizmosDrawBox(proximityOrigin + enemyCombatSO.ProximityBoxOffset, so.ProximityBoxSize);
            }
            Gizmos.color = Color.yellow;
            if (so.SightCastType == SightCastType.Circle)
            {
                GizmosDrawSphere(origin, so.ViewDistance);
            }
            else if (so.SightCastType == SightCastType.Ray)
            {
                GizmosDrawViewCone(origin,dir, so.Precision, so.ConeViewSize, so.ViewDistance);
            }
            else if (so.SightCastType == SightCastType.Box)
            {
                GizmosDrawBox(origin + enemyCombatSO.SightBoxOffset, so.SightBoxSize);
            }
            Gizmos.color = prev;
        }

        private void GizmosDrawSphere(Vector2 origin, float radius)
        {
            Gizmos.DrawWireSphere(origin, radius);
        }

        private void GizmosDrawBox(Vector3 origin, Vector2 size)
        {
            Vector3 halfSize = new Vector3(size.x * 0.5f, size.y * 0.5f, 0f);

            Vector3 topLeft = origin + new Vector3(-halfSize.x, halfSize.y, 0f);
            Vector3 topRight = origin + new Vector3(halfSize.x, halfSize.y, 0f);
            Vector3 bottomLeft = origin + new Vector3(-halfSize.x, -halfSize.y, 0f);
            Vector3 bottomRight = origin + new Vector3(halfSize.x, -halfSize.y, 0f);

            Gizmos.DrawLine(topLeft, topRight);
            Gizmos.DrawLine(topRight, bottomRight);
            Gizmos.DrawLine(bottomRight, bottomLeft);
            Gizmos.DrawLine(bottomLeft, topLeft);
        }


        private void GizmosDrawViewCone(Vector3 origin, Vector3 dir, uint precison,float coneViewSize, float viewDistance)
        {
            var half = coneViewSize * 0.5f;
            var steps = Mathf.Max(1, (int)precison);
            var a0 = -half;
            var stepAngle = steps > 0 ? coneViewSize / steps : coneViewSize;
            var vLeft = Quaternion.Euler(0f, 0f, -half) * dir;
            var vRight = Quaternion.Euler(0f, 0f, half) * dir;
            Gizmos.DrawLine(origin, origin + vLeft * viewDistance);
            Gizmos.DrawLine(origin, origin + vRight * viewDistance);
            Vector3 p1 = origin + (Quaternion.Euler(0f, 0f, a0) * dir) * viewDistance;
            for (int i = 1; i <= steps; i++)
            {
                var ang = a0 + stepAngle * i;
                Vector3 p2 = origin + (Quaternion.Euler(0f, 0f, ang) * dir) * viewDistance;
                Gizmos.DrawLine(p1, p2);
                p1 = p2;
            }
        }

#endif


        private void FixedUpdate()
        {
            if (player) lastKnownPlayerPosition = player.position;
            player = TryUpdateSight();
            if (player)
            {
                onPlayerSpotted?.Invoke();
            }
            else
            {
                onPlayerUnspotted?.Invoke();
            }
        }
    }
}