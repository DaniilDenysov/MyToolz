using System.Collections.Generic;
using MyToolz.Utilities.Debug;

namespace MyToolz.UI
{
    public interface IUIState
    {
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
            {
                newState.OnEnter();
                return;
            }
            DebugUtility.Log(this, $"Exiting state: {CurrentState}");
            CurrentState?.OnExit();

            stateStack.Push(newState);

            DebugUtility.Log(this, $"Entered state: {newState}");
            CurrentState?.OnEnter();
        }

        public void ExitState()
        {
            DebugUtility.Log(this, $"Exiting state: {CurrentState}");
            CurrentState?.OnExit();

            if (stateStack.Count > 0) 
                stateStack.Pop();

            CurrentState?.OnEnter();
            DebugUtility.Log(this, $"Entered state: {CurrentState}");
        }

        public void ClearStack()
        {
            while (stateStack.TryPop(out var state))
            {
                state.OnExit();
            }

            if (CurrentState != null)
            {
                CurrentState.OnExit();
            }

        }

    }

}
