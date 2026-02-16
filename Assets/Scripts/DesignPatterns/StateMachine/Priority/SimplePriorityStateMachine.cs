using MyToolz.Utilities.Debug;
using System.Linq;
using UnityEngine;

namespace MyToolz.DesignPatterns.StateMachine.SimplePriorityBased
{
    public interface IPriorityState : IState
    {
        public uint Priority { get; }
        public void Initialize();
    }

    [System.Serializable]
    public abstract class PriorityStrategy
    {
        public abstract bool HasHigherPriority(IPriorityState a, IPriorityState b);
    }

    [System.Serializable]
    public class HigherEqualPriorityStrategy : PriorityStrategy
    {
        public override bool HasHigherPriority(IPriorityState a, IPriorityState b)
        {
            uint ia = a?.Priority ?? 0;
            uint ib = b?.Priority ?? 0;
            return ia >= ib;
        }
    }

    [System.Serializable]
    public class HigherPriorityStrategy : PriorityStrategy
    {
        public override bool HasHigherPriority(IPriorityState a, IPriorityState b)
        {
            uint ia = a?.Priority ?? 0;
            uint ib = b?.Priority ?? 0;
            return ia > ib;
        }
    }

    [System.Serializable]
    public class IgnorePriorityStrategy : PriorityStrategy
    {
        [SerializeField] private bool chooseFirst;
        public override bool HasHigherPriority(IPriorityState a, IPriorityState b)
        {
            return chooseFirst;
        }
    }

    public class SimplePriorityStateMachine : MonoBehaviour, IStateMachine<IPriorityState>
    {
        [SerializeReference] protected PriorityStrategy priorityStrategy = new HigherPriorityStrategy();
        protected IPriorityState [] behaviourStates;
        protected IPriorityState current;


        protected virtual void Awake()
        {
            behaviourStates = GetComponentsInChildren<IPriorityState>(true);
        }

        protected virtual void Start()
        {
            if (behaviourStates.Length == 0) return;
            behaviourStates = behaviourStates.OrderByDescending(a => a.Priority).ToArray();
            ChangeState(behaviourStates[0]);
        }

        protected virtual bool HasHigherPriority(IPriorityState a, IPriorityState b)
        {
            return priorityStrategy.HasHigherPriority(a, b);
        }

        public virtual void ChangeState(IPriorityState state)
        {
            if (state == null) return;
            if (IsExecuting(state)) return;
            //if (HasHigherPriority(current, state)) return;
            current?.OnExit();
            current = state;
            current.OnEnter();
            DebugUtility.Log(this, $"State switched to {state.GetType()}");
        }

        public bool IsExecuting(IPriorityState state)
        {
            return state == current;
        }

        public bool TryGetCurrentState(out IPriorityState state)
        {
            state = current;
            return state != null;
        }
    }
}
