using MyToolz.DesignPatterns.StateMachine;
using MyToolz.DesignPatterns.StateMachine.SimplePriorityBased;
using MyToolz.EditorToolz;
using UnityEngine;
using Zenject;

namespace MyToolz.InputManagement
{
    public class InputStateManagementInstaller : MonoInstaller
    {
        [SerializeField] private InputStateManager inputStateManager = new();
        [SerializeField, Required] private InputModeSO defaultInputModeSO;

        public override void InstallBindings()
        {
            Container.Bind<IStateMachine<IPriorityState>>().FromInstance((IStateMachine<IPriorityState>)inputStateManager).AsSingle();
            Container.Bind<InputStateManager>().FromInstance(inputStateManager).AsSingle();
            inputStateManager.ChangeState(defaultInputModeSO);
        }
    }
}
