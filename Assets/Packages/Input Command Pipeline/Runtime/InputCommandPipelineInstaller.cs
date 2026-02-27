using MyToolz.DesignPatterns.Command;
using UnityEngine;
using UnityEngine.InputSystem;
using Zenject;

namespace MyToolz.InputManagement.Commands.Pipeline
{
    public class InputCommandPipelineInstaller : MonoInstaller
    {
        [SerializeField] private InputCommandPipeline commandPipeline = new();
        [SerializeField] private InputActionAsset inputActions;

        public override void InstallBindings()
        {
            Container.Bind<ICommandPipeline<IInputCommand>>()
                .FromInstance(commandPipeline)
                .AsSingle();

            Container.Bind<InputCommandPipeline>()
                .FromInstance(commandPipeline)
                .AsSingle();

            commandPipeline.Initialize(inputActions);

            Container.Bind<InputActionAsset>()
                .FromInstance(inputActions)
                .AsSingle()
                .IfNotBound();
        }
    }
}
