namespace MyToolz.DesignPatterns.Command
{
    /// <summary>
    /// A unit of work run by a <see cref="CommandPipeline{T}"/>. A plain command is finished as soon as
    /// <see cref="Execute"/> returns.
    /// </summary>
    public interface ICommand
    {
        void Execute();
    }

    /// <summary>
    /// A command that keeps running after <see cref="ICommand.Execute"/>. It occupies a pipeline slot
    /// until <see cref="IsFinished"/> becomes true (checked on every pipeline update) or until
    /// <see cref="CommandPipeline{T}.Complete"/> is called for it.
    /// </summary>
    public interface ICompletableCommand : ICommand
    {
        bool IsFinished { get; }
    }

    /// <summary>A long-running command that can be stopped when the pipeline is cleared.</summary>
    public interface ICancellableCommand : ICommand
    {
        void Cancel();
    }
}
