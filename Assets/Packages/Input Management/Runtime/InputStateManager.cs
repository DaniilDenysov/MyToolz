using MyToolz.Utilities.Debug;
using System;

namespace MyToolz.InputManagement
{
    public interface IInputStateManager
    {
        IPlayerInputState CurrentState { get; }
        event Action<IPlayerInputState, IPlayerInputState> OnStateChanged;
        void ChangeState(IPlayerInputState newState);
    }

    public class InputStateManager : IInputStateManager
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

        /// <summary>Exits the current state (disabling its actions) and leaves no state active.</summary>
        public void Clear()
        {
            if (currentState == null) return;

            var previousState = currentState;
            currentState = null;
            previousState.OnExit();
            OnStateChanged?.Invoke(previousState, null);
        }
    }
}
