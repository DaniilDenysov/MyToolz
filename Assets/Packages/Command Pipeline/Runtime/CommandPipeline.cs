using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MyToolz.DesignPatterns.Command
{
    /// <summary>
    /// Runs at most <see cref="CallStackSize"/> commands at once and queues up to <see cref="QueueSize"/>
    /// more (the oldest queued command is dropped when the queue is full).
    ///
    /// Completion contract: a plain <see cref="ICommand"/> finishes when Execute returns. An
    /// <see cref="ICompletableCommand"/> keeps its slot until IsFinished is true or
    /// <see cref="Complete"/> is called. A command whose Execute throws releases its slot. A slot is
    /// reserved before Execute runs, so a command that enqueues more work cannot exceed the limit.
    /// </summary>
    [Serializable]
    public class CommandPipeline<T> : ICommandPipeline<T> where T : ICommand
    {
        [SerializeField, Min(1)] private int callStackSize = 1;
        [SerializeField, Min(1)] private int queueSize = 8;

        private readonly Queue<T> pendingCommands = new();
        private readonly List<T> executingCommands = new();
        private bool pumping;

        public IReadOnlyList<T> CommandsOrdered => pendingCommands.ToList();
        public IReadOnlyList<T> ExecutingCommands => executingCommands;
        public int QueueSize => queueSize;
        public int CallStackSize => callStackSize;

        public CommandPipeline() { }

        public CommandPipeline(int callStackSize, int queueSize)
        {
            this.callStackSize = Mathf.Max(1, callStackSize);
            this.queueSize = Mathf.Max(1, queueSize);
        }

        public virtual void Enqueue(T command)
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));

            if (pendingCommands.Count >= queueSize)
                pendingCommands.Dequeue();

            pendingCommands.Enqueue(command);
            Pump();
        }

        /// <summary>Releases finished commands and starts queued ones. Call once per frame for completable commands.</summary>
        public virtual void Update()
        {
            for (int i = executingCommands.Count - 1; i >= 0; i--)
            {
                if (IsFinished(executingCommands[i]))
                {
                    executingCommands.RemoveAt(i);
                }
            }

            Pump();
        }

        public bool Complete(T command)
        {
            if (command == null || !executingCommands.Remove(command))
                return false;

            Pump();
            return true;
        }

        public virtual void Clear()
        {
            pendingCommands.Clear();

            var executing = executingCommands.ToArray();
            executingCommands.Clear();
            foreach (T command in executing)
            {
                if (command is ICancellableCommand cancellable)
                {
                    try
                    {
                        cancellable.Cancel();
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                    }
                }
            }
        }

        protected void RemoveFinishedCommand(T command)
        {
            executingCommands.Remove(command);
        }

        protected IReadOnlyList<T> GetExecutingCommandsInternal()
        {
            return executingCommands;
        }

        private void Pump()
        {
            // A command that enqueues from Execute re-enters here; the outer loop picks the new work up.
            if (pumping)
                return;

            pumping = true;
            try
            {
                while (executingCommands.Count < callStackSize && pendingCommands.Count > 0)
                {
                    T command = pendingCommands.Dequeue();
                    executingCommands.Add(command);

                    try
                    {
                        command.Execute();
                    }
                    catch (Exception e)
                    {
                        executingCommands.Remove(command);
                        Debug.LogException(e);
                        continue;
                    }

                    if (IsFinished(command))
                    {
                        executingCommands.Remove(command);
                    }
                }
            }
            finally
            {
                pumping = false;
            }
        }

        private static bool IsFinished(T command) =>
            !(command is ICompletableCommand completable) || completable.IsFinished;
    }
}
