using MyToolz.Utilities.Debug;
using System;

namespace MyToolz.InputManagement
{
    public class InputStateManager
    {
        private IPlayerInputState currentState;

        public IPlayerInputState CurrentState => currentState;

        public event Action<IPlayerInputState, IPlayerInputState> OnStateChanged;

        public void ChangeState(IPlayerInputState newState)
        {
            if (newState == null)
            {
                DebugUtility.LogError(this, $"{nameof(newState)} is null!");
                return;
            }

            if (ReferenceEquals(currentState, newState)) return;

            var previousState = currentState;
            currentState?.OnExit();
            currentState = newState;
            currentState.OnEnter();
            OnStateChanged?.Invoke(previousState, currentState);
        }
    }
}
