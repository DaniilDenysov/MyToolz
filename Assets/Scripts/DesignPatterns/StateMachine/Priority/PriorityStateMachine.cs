using MyToolz.EditorToolz;
using MyToolz.Utilities.Debug;
using System;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using Zenject;

namespace MyToolz.DesignPatterns.StateMachine.PriorityBased
{
    [Serializable]
    public abstract class PriorityState : IState
    {
        public uint Priority => priority;
        public bool Interuptable => interuptable;
        [SerializeField, Range(0, 100)] protected uint priority = 100;
        [SerializeField] protected bool interuptable;
        public virtual void Initialize() { }
        public virtual void OnUpdate() { }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public abstract bool IsConditionFullfilled();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public abstract bool IsCurrentFinished();

        public virtual void OnEnter()
        {

        }

        public virtual void OnExit()
        {

        }
    }

    public abstract class PriorityStateMachine<T> : MonoBehaviour, IStateMachine<T> where T : PriorityState
    {
        public T Current => current;
        [FoldoutGroup("Config"), SerializeReference] protected T[] behaviourStates;
        [FoldoutGroup("Config"), SerializeField] protected bool useFixedUpdate;
        [FoldoutGroup("Runtime"), SerializeField, ReadOnly] protected string currentState;
        protected int statesCount;
        protected DiContainer container;
        protected T current;


        [Inject]
        private void Construct(DiContainer container)
        {
            this.container = container;
        }

        protected virtual void Awake()
        {
            statesCount = behaviourStates != null ? behaviourStates.Length : 0;
        }

        protected virtual void Start()
        {
            behaviourStates = behaviourStates.OrderByDescending(a => a.Priority).ToArray();
            for (int i = 0; i < statesCount; i++)
            {
                container.Inject(behaviourStates[i]);
                behaviourStates[i].Initialize();
            }

            var initial = SelectNextState();
            if (initial != null) ChangeState(initial);
        }


        protected virtual void DoUpdate()
        {
            current?.OnUpdate();

            var next = SelectNextState();
            if (next == null) return;

            if (current == null)
            {
                ChangeState(next);
                return;
            }

            if (next == current) return;

            if (IsCurrentFinished())
            {
                ChangeState(next);
                return;
            }

            if (!current.IsConditionFullfilled())
            {
                ChangeState(next);
                return;
            }

            if (HasHigherPriority(next, current) || IsCurrentFinished())
            {
                ChangeState(next);
                return;
            }
        }

        protected void Update()
        {
            if (useFixedUpdate) return;
            DoUpdate();
        }

        protected void FixedUpdate()
        {
            if (!useFixedUpdate) return;
            DoUpdate();
        }

        protected virtual T SelectNextState()
        {
            for (int i = 0; i < statesCount; i++)
            {
                var s = behaviourStates[i];
                if (s == null) continue;
                if (s.IsConditionFullfilled()) return s;
            }
            return null;
        }

        protected virtual bool IsCurrentFinished()
        {
            return (current?.Interuptable ?? false) || (current?.IsCurrentFinished() ?? false);
        }

        protected virtual bool HasHigherPriority(T a, T b)
        {
            uint ia = a.Priority;
            uint ib = b.Priority;
            return ia > ib;
        }

        public virtual void ChangeState(T state)
        {
            if (state == null) return;
            current?.OnExit();
            current = state;
            current.OnEnter();
            currentState = current?.GetType().Name ?? "Error, null state!";
            DebugUtility.Log(this, $"Enemy state switched to {state.GetType()}");
        }

        public virtual bool TryGetCurrentState(out T state)
        {
            state = current;
            return current != default;
        }
    }
}