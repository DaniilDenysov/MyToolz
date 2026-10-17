using MyToolz.DesignPatterns.Command;

namespace MyToolz.InputManagement.Commands
{
    public interface IInputCommand : ICompletableCommand
    {
        void Update();
    }
}
