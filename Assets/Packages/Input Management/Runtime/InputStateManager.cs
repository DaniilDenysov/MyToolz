using MyToolz.DesignPatterns.StateMachine;
using MyToolz.EditorToolz;
using MyToolz.Utilities.Debug;
using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MyToolz.InputManagement
{
    [Serializable]
    public class InputStateManager : IStateMachine<IPlayerInputState>
    {
        public IPlayerInputState CurrentState => currentState;

        public event Action<IPlayerInputState, IPlayerInputState> OnStateChanged;

        private IPlayerInputState currentState;
        [SerializeField, Required] private InputActionAsset asset;

        public void ChangeState(IPlayerInputState newState)
        {
            if (newState == null)
            {
                DebugUtility.LogError(this, $"{nameof(newState)} is null!");
                return;
            }

            if (ReferenceEquals(currentState, newState)) return;

            var previousState = currentState;
            currentState?.Initialize(asset);
            currentState?.OnExit();
            currentState = newState;
            currentState.OnEnter();
            OnStateChanged?.Invoke(previousState, currentState);
        }
    }
}
