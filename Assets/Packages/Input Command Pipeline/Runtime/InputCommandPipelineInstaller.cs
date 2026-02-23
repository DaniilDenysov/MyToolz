using MyToolz.DesignPatterns.Command;
using UnityEngine;
using UnityEngine.InputSystem;
using Zenject;

namespace MyToolz.InputManagement.Commands.Pipeline
{
    public class InputCommandPipelineInstaller : MonoInstaller
    {
        [SerializeField] private InputCommandPipeline commandPipeline = new();

        private InputActionAsset inputActions;
        private DiContainer injectedContainer;

        [Inject]
        private void Construct(InputActionAsset inputActions, DiContainer container)
        {
            this.inputActions = inputActions;
            this.injectedContainer = container;
        }

        public override void InstallBindings()
        {
            Container.Bind<ICommandPipeline<IInputCommand>>()
                .FromInstance(commandPipeline)
                .AsSingle();

            Container.Bind<InputCommandPipeline>()
                .FromInstance(commandPipeline)
                .AsSingle();
        }

        private new void Start()
        {
            base.Start();
            commandPipeline.Initialize(inputActions, injectedContainer);
            commandPipeline.RegisterBindings();
        }

        private void Update()
        {
            commandPipeline.Update();
        }

        private void OnDestroy()
        {
            commandPipeline.Dispose();
        }
    }
}
