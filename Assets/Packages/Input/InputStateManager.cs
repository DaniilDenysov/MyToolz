using MyToolz.DesignPatterns.StateMachine;
using MyToolz.Utilities.Debug;

namespace MyToolz.Input
{
    public class InputStateManager : IStateMachine<IPlayerInputState>
    {
        private IPlayerInputState currentState;
        public IPlayerInputState CurrentState => currentState;

        public void ChangeState(IPlayerInputState playerInputState)
        {
            DebugUtility.LogError(this, $"Exiting {currentState}...");
            currentState?.OnExit();
            currentState = playerInputState;
            DebugUtility.LogError(this, $"Entering {currentState}...");
            currentState?.OnEnter();
        }
    }


    public interface IPlayerInputState : IState
    {

    }
}