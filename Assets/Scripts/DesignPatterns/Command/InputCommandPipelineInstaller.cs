using MyToolz.Player.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using Zenject;

namespace MyToolz.DesignPatterns.Command
{
    public interface ICommand
    {
        void Execute();
    }

    public interface ICommandPipeline<T> where T : ICommand
    {
        public List<T> CommandsOrdered { get; }
        public List<T> ExecutingCommands { get; }
        void Enqueue(T command, InputDevice device);
    }

    [System.Serializable]
    public class CommandPipeline<T> : ICommandPipeline<T> where T : ICommand
    {
        [SerializeField, Min(1)] protected int callStackSize = 1;
        [SerializeField, Min(1)] protected int queueSize = 8;
        protected readonly Queue<T> commandsOrdered = new();
        protected readonly List<T> executingCommands = new();

        protected InputDevice inputDevice;
        public List<T> CommandsOrdered => commandsOrdered.ToList();
        public List<T> ExecutingCommands => executingCommands.ToList();

        public virtual void Enqueue(T command, InputDevice device)
        {
            if (commandsOrdered.Count == queueSize) commandsOrdered.Dequeue();
            commandsOrdered.Enqueue(command);
        }

        public virtual void Update()
        {
            if (executingCommands.Count > callStackSize) return;
            if (commandsOrdered.Count == 0) return;
            var command = commandsOrdered.Peek();
            command.Execute();
            executingCommands.Add(command);
        }
    }

    public interface IInputCommand : ICommand
    {
        void Update();
        bool IsFinishedExecution();
    }

    [System.Serializable]
    public class InputCommandPipeline : CommandPipeline<IInputCommand>
    {
        public static event Action<InputDevice> OnInputDeviceChanged;
        [SerializeField] private List<InputCommandSO> register = new();
        private DefaultInputActions inputActions;

        public void Initialize(DefaultInputActions inputActions, DiContainer container)
        {
            this.inputActions = inputActions;
            foreach (var cmd in register)
            {
                container.Inject(cmd);
            }
        }

        private readonly List<InputAction> _subscribed = new();
        private InputDevice lastDevice;


        private void OnActionFired(InputAction.CallbackContext ctx)
        {
            var device = ctx.control?.device;
            if (device == null || device == lastDevice) return;
            lastDevice = device;
            OnInputDeviceChanged?.Invoke(device);
        }

        public virtual void UnregisterBindings() 
        {
            foreach (var cmd in register)
            {
                cmd.Unregister();
            }
            foreach (var a in _subscribed)
            {
                //a.started -= OnActionFired;
                a.performed -= OnActionFired;
                //a.canceled -= OnActionFired;
            }
            _subscribed.Clear();
        }
        public virtual void RegisterBindings()
        {
            foreach (var cmd in register)
            {
                cmd.Register();
            }
            var asset = inputActions?.asset;
            if (asset != null)
            {
                foreach (var map in asset.actionMaps)
                {
                    foreach (var action in map.actions)
                    {
                        //action.started += OnActionFired;
                        action.performed += OnActionFired;
                        //action.canceled -= OnActionFired;
                        _subscribed.Add(action);
                    }
                }
            }
        }

        public override void Update()
        {
            List<IInputCommand> cmds = new List<IInputCommand>(commandsOrdered);
            foreach (var cmd in cmds)
            {
                cmd.Update();
                if (cmd.IsFinishedExecution())
                {
                    executingCommands.Remove(cmd);
                }
            }
            base.Update();
        }
    }

    public class InputCommandPipelineInstaller : MonoInstaller
    {
        [SerializeField] private InputCommandPipeline commandPipeline = new();
        private DiContainer container;
        private DefaultInputActions inputActions;

        [Inject]
        private void Construct(DefaultInputActions inputActions, DiContainer container)
        {
            this.inputActions = inputActions;
            this.container = container;
        }

        private new void Start()
        {
            base.Start();
            commandPipeline.Initialize(inputActions, container);
            commandPipeline.RegisterBindings();
        }

        private void OnDestroy()
        {
            commandPipeline.UnregisterBindings();
        }

        private void Update()
        {
            commandPipeline.Update();
        }

        public override void InstallBindings()
        {
            Container.Bind<ICommandPipeline<IInputCommand>>()
                .FromInstance(commandPipeline)
                .AsSingle();
        }
    }
}
