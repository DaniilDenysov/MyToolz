using System.Collections.Generic;
using MyToolz.Utilities.Debug;

namespace MyToolz.UI.Management
{
    public interface IUIState
    {
        bool IsActive { get; }
        void OnEnter();
        void OnExit();
    }

    public class UIStateManager
    {
        private readonly Stack<IUIState> stateStack = new Stack<IUIState>();
        public IUIState CurrentState => stateStack.Count > 0 ? stateStack.Peek() : null;

        public void ChangeState(IUIState newState)
        {
            if (newState == null) return;
            if (newState == CurrentState)
                return;

            DebugUtility.Log(this, $"Exiting state: {CurrentState}");
            if (CurrentState != null && CurrentState.IsActive)
                CurrentState.OnExit();

            stateStack.Push(newState);

            DebugUtility.Log(this, $"Entered state: {newState}");
            if (!newState.IsActive)
                newState.OnEnter();
        }

        public void ExitState()
        {
            DebugUtility.Log(this, $"Exiting state: {CurrentState}");
            if (CurrentState != null && CurrentState.IsActive)
                CurrentState.OnExit();

            if (stateStack.Count > 0)
                stateStack.Pop();

            if (CurrentState != null && !CurrentState.IsActive)
                CurrentState.OnEnter();
            DebugUtility.Log(this, $"Entered state: {CurrentState}");
        }

        public void ClearStack()
        {
            while (stateStack.TryPop(out var state))
            {
                if (state.IsActive)
                    state.OnExit();
            }
        }

    }

}
