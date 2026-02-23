using System.Collections.Generic;
using UnityEngine.InputSystem;

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
        void Clear();
    }
}
