using Cysharp.Threading.Tasks;
using MyToolz.Utilities.Debug;
using System;
using System.Runtime.CompilerServices;
using System.Threading;
using UnityEngine;
using Zenject;

namespace MyToolz.DesignPatterns.StateMachine.MultiThreadPriorityBased
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
        public abstract bool IsConditionFulfilled();

        public virtual void OnEnter() { }
        public virtual void OnExit() { }
    }

    public abstract class PriorityStateMachine<T> : MonoBehaviour, IStateMachine<T> where T : PriorityState
    {
        [Header("States")]
        [SerializeReference] protected T[] behaviourStates;

        [Header("Multithreaded evaluation")]
        [Tooltip("How many times per second the next-state search runs off the main thread.")]
        [SerializeField, Min(0.5f)] private float evaluationRateHz = 10f;

        public T Current => current;

        protected int statesCount;
        protected DiContainer container;
        protected T current;

        private volatile int nextCandidateIndex = -1;
        private int evalIntervalMs;
        private CancellationTokenSource cts;

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
            if (behaviourStates == null || statesCount == 0) return;

            SortStatesByPriority();

            for (int i = 0; i < statesCount; i++)
            {
                container.Inject(behaviourStates[i]);
                behaviourStates[i].Initialize();
            }

            int initialIndex = SelectNextStateIndex();
            if (initialIndex >= 0)
                ChangeState(behaviourStates[initialIndex]);

            evalIntervalMs = Mathf.Max(1, Mathf.RoundToInt(1000f / evaluationRateHz));
            StartWorker();
        }

        protected virtual void Update()
        {
            current?.OnUpdate();
            SelectNext();
        }

        private void SelectNext()
        {
            int candidateIndex = Volatile.Read(ref nextCandidateIndex);

            if (candidateIndex < 0 || candidateIndex >= statesCount)
                return;

            T candidate = behaviourStates[candidateIndex];

            if (candidate == null || candidate == current)
                return;

            if (current == null)
            {
                ChangeState(candidate);
                return;
            }

            if (!current.IsConditionFulfilled())
            {
                ChangeState(candidate);
                return;
            }

            bool candidateHasHigherPriority = candidateIndex < IndexOf(current);
            if (candidateHasHigherPriority || current.Interuptable)
            {
                ChangeState(candidate);
            }
        }

        private void StartWorker()
        {
            cts = new CancellationTokenSource();
            RunEvaluationLoop(cts.Token).Forget();
        }

        private void StopWorker()
        {
            cts?.Cancel();
            cts?.Dispose();
            cts = null;
            nextCandidateIndex = -1;
        }

        private async UniTaskVoid RunEvaluationLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                await UniTask.SwitchToThreadPool();

                int candidate = SelectNextStateIndexThreadSafe();
                Volatile.Write(ref nextCandidateIndex, candidate);

                await UniTask.Delay(evalIntervalMs, cancellationToken: token);
            }
        }

        private int SelectNextStateIndexThreadSafe()
        {
            for (int i = 0; i < statesCount; i++)
            {
                var s = behaviourStates[i];
                if (s == null) continue;
                if (s.IsConditionFulfilled()) return i;
            }
            return -1;
        }

        private int SelectNextStateIndex()
        {
            for (int i = 0; i < statesCount; i++)
            {
                var s = behaviourStates[i];
                if (s == null) continue;
                if (s.IsConditionFulfilled()) return i;
            }
            return -1;
        }

        private int IndexOf(T state)
        {
            for (int i = 0; i < statesCount; i++)
            {
                if (behaviourStates[i] == state) return i;
            }
            return -1;
        }

        private void SortStatesByPriority()
        {
            Array.Sort(behaviourStates, (a, b) => b.Priority.CompareTo(a.Priority));
        }

        public virtual void ChangeState(T state)
        {
            if (state == null)
            {
                DebugUtility.LogError(this, $"Unable to switch to null state!");
                return;
            }
            current?.OnExit();
            current = state;
            current.OnEnter();
            DebugUtility.Log(this, $"State switched to {state.GetType().Name}");
        }

        public virtual bool TryGetCurrentState(out T state)
        {
            state = current;
            return current != default;
        }

        protected virtual void OnDisable() => StopWorker();
        protected virtual void OnDestroy() => StopWorker();
    }
}
