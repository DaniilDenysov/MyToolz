using MyToolz.DesignPatterns.StateMachine;
using UnityEngine.InputSystem;

namespace MyToolz.InputManagement
{
    public interface IPlayerInputState : IState
    {
        public void Initialize(InputActionAsset asset);
    }
}
