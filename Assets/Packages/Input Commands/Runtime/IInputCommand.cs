using MyToolz.DesignPatterns.Command;

namespace MyToolz.InputManagement.Commands
{
    public interface IInputCommand : ICommand
    {
        void Update();
        bool IsFinished { get; }
    }
}
