using System.Collections.Generic;

namespace MyToolz.DesignPatterns.Command
{
    public interface ICommandPipeline<T> where T : ICommand
    {
        IReadOnlyList<T> CommandsOrdered { get; }
        IReadOnlyList<T> ExecutingCommands { get; }
        int QueueSize { get; }
        int CallStackSize { get; }
        void Enqueue(T command);
        void Update();

        /// <summary>Releases the slot held by an executing command.</summary>
        bool Complete(T command);

        void Clear();
    }
}
