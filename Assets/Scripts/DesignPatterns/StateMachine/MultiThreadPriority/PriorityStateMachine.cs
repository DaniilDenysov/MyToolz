using MyToolz.Core;
using MyToolz.DesignPatterns.StateMachine.SimplePriorityBased;
using MyToolz.Utilities.Debug;
using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using UnityEngine;
using Zenject;

namespace MyToolz.DesignPatterns.StateMachine.MultiThread
{
    public class MultiThreadPriorityStateMachine<T> where T : IPriorityState
    {
        private T[] behaviourStates;

        private float evaluationRateHz = 10f;

        private int statesCount;

        private Thread evalThread;
        private CancellationTokenSource cts;

        private volatile int nextCandidateIndex = -1;

        private int evalIntervalMs;

        public void Initialize(T[] states, float evaluationRateHz)
        {
            behaviourStates = states;
            this.evaluationRateHz = Mathf.Max(0.5f, evaluationRateHz);

            statesCount = behaviourStates != null ? behaviourStates.Length : 0;
            evalIntervalMs = Mathf.Max(1, (int)Mathf.Round(1000f / this.evaluationRateHz));

            nextCandidateIndex = -1;
        }

        public void StartWorker()
        {
            if (cts != null) return;
            if (behaviourStates == null || behaviourStates.Length == 0) return;

            cts = new CancellationTokenSource();
            evalThread = new Thread(() => EvaluationLoop(cts.Token))
            {
                IsBackground = true,
                Name = $"{GetType().Name}-EvalThread"
            };
            evalThread.Start();
        }

        public void StopWorker()
        {
            if (cts == null) return;

            try
            {
                cts.Cancel();

                if (evalThread != null && evalThread.IsAlive)
                {
                    if (!evalThread.Join(200))
                        evalThread.Interrupt();
                }
            }
            catch (ThreadInterruptedException) { }
            catch (ObjectDisposedException) { }
            finally
            {
                cts.Dispose();
                cts = null;
                evalThread = null;
                nextCandidateIndex = -1;
            }
        }

        /// <summary>
        /// Returns:
        ///  -1  => do not change state (just update current)
        ///  >=0 => apply that index now
        /// </summary>
        public int GetCandidateToApplyIndex(int currentIndex, bool isCurrentInterruptable)
        {
            int candidate = Volatile.Read(ref nextCandidateIndex);

            if (candidate < 0 || candidate >= statesCount)
                return -1;

            if (currentIndex < 0 || currentIndex >= statesCount)
                return candidate;

            if (candidate == currentIndex)
                return -1;

            bool candidateHasHigherOrEqualPriority = candidate < currentIndex;


            if (candidateHasHigherOrEqualPriority)
                return candidate;

            if (isCurrentInterruptable)
                return candidate;

            return -1;
        }

        private void EvaluationLoop(CancellationToken token)
        {
            var spinWait = new SpinWait();
            var sw = System.Diagnostics.Stopwatch.StartNew();
            long nextTick = 0;

            while (!token.IsCancellationRequested)
            {
                int candidate = SelectNextStateIndexThreadSafe();
                Volatile.Write(ref nextCandidateIndex, candidate);

                nextTick += evalIntervalMs;

                while (sw.ElapsedMilliseconds < nextTick && !token.IsCancellationRequested)
                {
                    Thread.Sleep(1);
                    spinWait.SpinOnce();
                }
            }
        }

        private int SelectNextStateIndexThreadSafe()
        {
            for (int i = 0; i < statesCount; i++)
            {
                var s = behaviourStates[i];
                if (s == null) continue;

                if (s.IsConditionFullfilled())
                    return i;
            }
            return -1;
        }
    }

    public interface IPriorityState
    {
        uint Priority { get; }
        bool Interuptable { get; }
        bool IsConditionFullfilled();
    }
}

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
        public abstract bool IsConditionFullfilled();

        public virtual void OnEnter() { }
        public virtual void OnExit() { }
    }

    public abstract class PriorityStateMachine<T> : MonoBehaviourPlus, IStateMachine<T> where T : PriorityState
    {
        [Header("States")]
        [SerializeReference] protected T[] behaviourStates;

        [Header("Multithreaded evaluation")]
        [Tooltip("How many times per second the next-state search runs on the worker thread.")]
        [SerializeField, Min(0.5f)] private float evaluationRateHz = 10f;

        protected int statesCount;
        protected DiContainer container;
        protected T current;

        private Thread evalThread;
        private CancellationTokenSource cts;
        private volatile T nextCandidate;
        private int evalIntervalMs;

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
            behaviourStates = behaviourStates?.OrderByDescending(a => a.Priority).ToArray();
            for (int i = 0; i < statesCount; i++)
            {
                container.Inject(behaviourStates[i]);
                behaviourStates[i].Initialize();
            }
            var initial = SelectNextState();
            if (initial != null) ChangeState(initial);

            evalIntervalMs = Mathf.Max(1, (int)Mathf.Round(1000f / evaluationRateHz));
            cts = new CancellationTokenSource();
            evalThread = new Thread(() => EvaluationLoop(cts.Token))
            {
                IsBackground = true,
                Name = $"{GetType().Name}-EvalThread"
            };
            evalThread.Start();
        }

        protected virtual void Update()
        {
            var next = Volatile.Read(ref nextCandidate);

            current?.OnUpdate();

            if (next == null) return;

            if (current == null)
            {
                ChangeState(next);
                return;
            }

            if (next == current) return;

            bool isCurrentFinished = IsCurrentFinished();
            bool isNotConditionFullfilled = !current.IsConditionFullfilled();
            bool hasNext = HasHigherPriority(next, current) || isCurrentFinished;

            if (isCurrentFinished || isCurrentFinished || hasNext)
            {
                ChangeState(next);
            }
        }

        private void EvaluationLoop(CancellationToken token)
        {
            var spinWait = new SpinWait();
            var sw = System.Diagnostics.Stopwatch.StartNew();
            long nextTick = 0;

            while (!token.IsCancellationRequested)
            {
                var candidate = SelectNextStateThreadSafe();
                System.Threading.Volatile.Write(ref nextCandidate, candidate);

                nextTick += evalIntervalMs;
                while (sw.ElapsedMilliseconds < nextTick && !token.IsCancellationRequested)
                {
                    Thread.Sleep(1);
                    spinWait.SpinOnce();
                }
            }
        }

        private T SelectNextStateThreadSafe()
        {
            for (int i = 0; i < statesCount; i++)
            {
                var s = behaviourStates[i];
                if (s == null) continue;

                if (s.IsConditionFullfilled()) return s;
            }
            return null;
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
            return current?.Interuptable ?? false;
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
            DebugUtility.Log(this, $"Enemy state switched to {state.GetType()}");
        }

        protected virtual void OnDisable() => StopWorker();
        protected virtual void OnDestroy() => StopWorker();

        private void StopWorker()
        {
            if (cts == null) return;
            try
            {
                cts.Cancel();
                if (evalThread != null && evalThread.IsAlive)
                {
                    if (!evalThread.Join(200))
                        evalThread.Interrupt();
                }
            }
            catch (ThreadInterruptedException) { }
            catch (ObjectDisposedException) { }
            finally
            {
                cts.Dispose();
                cts = null;
                evalThread = null;
            }
        }

        public virtual bool TryGetCurrentState(out T state)
        {
            state = current;
            return current != default;
        }
    }
}
