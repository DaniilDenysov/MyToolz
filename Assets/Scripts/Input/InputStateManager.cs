using MyToolz.Core;
using MyToolz.DesignPatterns.StateMachine;

namespace MyToolz.Input
{
    public class InputStateManager : ObjectPlus, IStateMachine<IPlayerInputState>
    {
        private IPlayerInputState currentState;
        public IPlayerInputState CurrentState => currentState;

        public void ChangeState(IPlayerInputState playerInputState)
        {
            Log($"Exiting {currentState}...");
            currentState?.OnExit();
            currentState = playerInputState;
            Log($"Entering {currentState}...");
            currentState?.OnEnter();
        }
    }


    public interface IPlayerInputState : IState
    {

    }
}