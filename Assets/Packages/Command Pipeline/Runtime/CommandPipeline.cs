using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MyToolz.DesignPatterns.Command
{
    [Serializable]
    public class CommandPipeline<T> : ICommandPipeline<T> where T : ICommand
    {
        [SerializeField, Min(1)] private int callStackSize = 1;
        [SerializeField, Min(1)] private int queueSize = 8;

        private readonly Queue<T> pendingCommands = new();
        private readonly List<T> executingCommands = new();

        public IReadOnlyList<T> CommandsOrdered => pendingCommands.ToList();
        public IReadOnlyList<T> ExecutingCommands => executingCommands;
        public int QueueSize => queueSize;
        public int CallStackSize => callStackSize;

        public virtual void Enqueue(T command)
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));

            if (pendingCommands.Count >= queueSize)
                pendingCommands.Dequeue();

            pendingCommands.Enqueue(command);
        }

        public virtual void Update()
        {
            if (executingCommands.Count >= callStackSize) return;
            if (pendingCommands.Count == 0) return;

            var command = pendingCommands.Dequeue();
            command.Execute();
            executingCommands.Add(command);
        }

        public virtual void Clear()
        {
            pendingCommands.Clear();
            executingCommands.Clear();
        }

        protected void RemoveFinishedCommand(T command)
        {
            executingCommands.Remove(command);
        }

        protected IReadOnlyList<T> GetExecutingCommandsInternal()
        {
            return executingCommands;
        }
    }
}
