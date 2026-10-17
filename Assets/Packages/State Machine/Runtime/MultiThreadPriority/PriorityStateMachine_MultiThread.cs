using Cysharp.Threading.Tasks;
using MyToolz.Utilities.Debug;
using System;
using System.Runtime.CompilerServices;
using System.Threading;
using UnityEngine;
using Zenject;

namespace MyToolz.DesignPatterns.StateMachine.MultiThreadPriorityBased
{
    public enum StateEvaluationMode
    {
        /// <summary>Conditions are evaluated on the main thread. Safe for any condition code.</summary>
        MainThread,
        /// <summary>
        /// <see cref="PriorityState.CaptureSnapshot"/> runs on the main thread, then
        /// <see cref="PriorityState.IsConditionFulfilled"/> runs on a worker thread and may only read
        /// the captured snapshot (no Unity API, no state mutated by the main thread).
        /// </summary>
        Background
    }

    [Serializable]
    public abstract class PriorityState : IState
    {
        public uint Priority => priority;
        public bool Interuptable => interuptable;

        [SerializeField, Range(0, 100)] protected uint priority = 100;
        [SerializeField] protected bool interuptable;

        public virtual void Initialize() { }
        public virtual void OnUpdate() { }

        /// <summary>
        /// Main thread. Copy whatever <see cref="IsConditionFulfilled"/> needs (positions, flags, …) into
        /// fields the condition reads. Called before every background evaluation; not needed in
        /// <see cref="StateEvaluationMode.MainThread"/> mode.
        /// </summary>
        public virtual void CaptureSnapshot() { }

        /// <summary>
        /// In <see cref="StateEvaluationMode.Background"/> mode this runs on a worker thread and must
        /// only read data captured by <see cref="CaptureSnapshot"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public abstract bool IsConditionFulfilled();

        public virtual void OnEnter() { }
        public virtual void OnExit() { }
    }

    /// <summary>
    /// Priority state machine that evaluates state conditions at a fixed rate instead of every frame,
    /// optionally off the main thread. The chosen state is always applied on the main thread.
    /// </summary>
    public abstract class PriorityStateMachine<T> : MonoBehaviour, IStateMachine<T> where T : PriorityState
    {
        [Header("States")]
        [SerializeReference] protected T[] behaviourStates;

        [Header("Evaluation")]
        [Tooltip("How many times per second the next-state search runs.")]
        [SerializeField, Min(0.5f)] private float evaluationRateHz = 10f;
        [Tooltip("MainThread is safe for any condition. Background requires conditions that only read data captured in CaptureSnapshot().")]
        [SerializeField] private StateEvaluationMode evaluationMode = StateEvaluationMode.MainThread;

        public T Current => current;
        public StateEvaluationMode EvaluationMode => evaluationMode;

        protected int statesCount;
        protected DiContainer container;
        protected T current;

        private int nextCandidateIndex = -1;
        private float nextEvaluationAt;
        private bool evaluating;
        private bool started;
        private CancellationTokenSource lifetime;

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
                if (behaviourStates[i] == null) continue;
                container?.Inject(behaviourStates[i]);
                behaviourStates[i].Initialize();
            }

            int initialIndex = SelectNextStateIndex();
            if (initialIndex >= 0)
                ChangeState(behaviourStates[initialIndex]);

            started = true;
        }

        protected virtual void OnEnable()
        {
            lifetime = new CancellationTokenSource();
        }

        protected virtual void OnDisable()
        {
            lifetime?.Cancel();
            lifetime?.Dispose();
            lifetime = null;
            evaluating = false;
            nextCandidateIndex = -1;
        }

        protected virtual void OnDestroy()
        {
            if (current != null)
            {
                current.OnExit();
                current = null;
            }
        }

        protected virtual void Update()
        {
            if (!started) return;

            current?.OnUpdate();

            if (!evaluating && Time.unscaledTime >= nextEvaluationAt)
            {
                nextEvaluationAt = Time.unscaledTime + 1f / Mathf.Max(0.5f, evaluationRateHz);
                Evaluate();
            }

            SelectNext();
        }

        private void Evaluate()
        {
            if (evaluationMode == StateEvaluationMode.MainThread)
            {
                nextCandidateIndex = SelectNextStateIndex();
                return;
            }

            for (int i = 0; i < statesCount; i++)
            {
                behaviourStates[i]?.CaptureSnapshot();
            }

            EvaluateInBackground(lifetime != null ? lifetime.Token : CancellationToken.None).Forget();
        }

        private async UniTaskVoid EvaluateInBackground(CancellationToken token)
        {
            evaluating = true;
            try
            {
                // Returns to the main thread before the result is published.
                int candidate = await UniTask.RunOnThreadPool(SelectNextStateIndex, true, token);
                if (!token.IsCancellationRequested)
                {
                    nextCandidateIndex = candidate;
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception e)
            {
                DebugUtility.LogError(this, $"Background state evaluation failed: {e}");
            }
            finally
            {
                evaluating = false;
            }
        }

        private void SelectNext()
        {
            int candidateIndex = nextCandidateIndex;

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
            Array.Sort(behaviourStates, (a, b) => (b?.Priority ?? 0).CompareTo(a?.Priority ?? 0));
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
    }
}
