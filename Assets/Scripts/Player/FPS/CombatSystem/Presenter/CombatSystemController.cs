using System;
using Mirror;
using UnityEngine;
using System.Collections.Generic;
using Zenject;
using MyToolz.DesignPatterns.EventBus;
using MyToolz.Player.FPS.CombatSystem.Presenter;
using MyToolz.Utilities.Debug;
using MyToolz.Events;
using MyToolz.Networking.Core;
using MyToolz.Networking.Events;
using MyToolz.Player.FPS.CombatSystem.Model;
using MyToolz.Player.FPS.LoadoutSystem.Model;
using MyToolz.InputManagement.Commands;

namespace MyToolz.Networking.Events
{
    public struct LethalEquipmentThrowEvent : IEvent
    {

    }
}

namespace MyToolz.Player.FPS.CombatSystem
{
    public class CombatSystemController : NetworkBehaviour, IEventListener
    {
        [SerializeField] private WeaponModel model;

        private NetworkCharacter networkCharacter;
        private WeaponLoadoutModel weaponLoadoutModel;

        private WeaponAnimationsHandler weaponAnimationsHandler;

        /// <summary>
        /// Always attacking state!
        /// </summary>
        private CombatSystemStateHandler defaultState;
        private Stack<CombatSystemStateHandler> stateHandlerStack = new Stack<CombatSystemStateHandler>();
        private Dictionary<Type, CombatSystemStateHandler> stateHandlers = new Dictionary<Type, CombatSystemStateHandler>();
        private EventBinding<LethalEquipmentThrowEvent> LethalEquipmentThrowBinding;

        public WeaponModel WeaponModel
        {
            get
            {
               if (model == null)
               {
                 DebugUtility.LogWarning("Weapon model is null!");
               }
               return model;
            }
        }
        public bool IsAiming
        {
            get
            {
                return WeaponModel.IsAiming();
            }
        }

        public float CurrentSpeedReduction
        {
            get
            {
                return WeaponSO.SpeedReduction;
            }
        }

        public WeaponSO WeaponSO
        {
            get
            {
                if (WeaponModel == null)
                {
                    DebugUtility.LogError(this, "Model is null!");
                    return null;
                }
                var item = WeaponModel.GetItemSO();
                if (item == null)
                {
                    DebugUtility.LogError(this, "Item is null!");
                    return null;
                }
                return item;
            }
        }

        public WeaponLoadoutModel WeaponLoadoutModel
        {
            get
            {
                if (weaponLoadoutModel == null)
                {
                    DebugUtility.LogError(this, "Loadout model is null!");
                    return null;
                }
                return weaponLoadoutModel;
            }
        }
        
        public Action onAim;
        public Action onShoot;

        private DiContainer container;
        private InputCommandSO shootingInput;
        private InputCommandSO reloadingInput;
        private InputCommandSO aimingInput;
        private InputCommandSO bashInput;

        #region Client

        private void Start()
        {
            if (!isOwned) return;
            weaponLoadoutModel.InitializeLoadout();
        }

        public void RegisterEvents()
        {
            shootingInput.OnPerformed += OnShootEnter;
            aimingInput.OnPerformed += OnShootEnter;
            reloadingInput.OnPerformed += OnReloaded;
            bashInput.OnPerformed += OnBash;
            LethalEquipmentThrowBinding = new EventBinding<LethalEquipmentThrowEvent>(OnLethalEquipmentThrow);
            EventBus<LethalEquipmentThrowEvent>.Register(LethalEquipmentThrowBinding);
        }

        public void UnregisterEvents()
        {
            shootingInput.OnPerformed -= OnShootEnter;
            aimingInput.OnPerformed -= OnShootEnter;
            reloadingInput.OnPerformed -= OnReloaded;
            bashInput.OnPerformed -= OnBash;
            EventBus<LethalEquipmentThrowEvent>.Deregister(LethalEquipmentThrowBinding);
        }

        public override void OnStartClient()
        {
            if (!isOwned) return;
            RegisterEvents();
        }

        public override void OnStopClient()
        {
            if (!isOwned) return;
            UnregisterEvents();
        }

        [Inject]
        public void Construct(
            DiContainer container,
            WeaponLoadoutModel weaponLoadoutModel,
            NetworkCharacter networkCharacter,
            WeaponAnimationsHandler weaponAnimationsHandler)
        {
            this.container = container;
            this.weaponAnimationsHandler = weaponAnimationsHandler;
            this.networkCharacter = networkCharacter;
            this.weaponLoadoutModel = weaponLoadoutModel;
        }


        private void Update()
        {
            if (!isOwned) return;

            if (!stateHandlerStack.TryPeek(out var state))
            {
                DebugUtility.LogWarning("No state on stack! Forcing default.");
                EnterState(defaultState.GetType());
                return;
            }

            state.Update();
        }

        private void OnDestroy()
        {
            OnStopClient();
        }

        #endregion

        #region Input handling
        private void OnBash()
        {
           EnterState(typeof(WeaponBashingStateHandler));
        }

        private void OnReloaded()
        {
            DebugUtility.Log(this, "Trying to reload!");
            if (WeaponModel.CanReload()) EnterState(typeof(WeaponReloadingStateHandler));
        }
        private void OnShootEnter()
        {
            DebugUtility.Log(this, "Shooting");
            EnterState(defaultState.GetType());
        }

        #endregion  

        public void OnNewWeaponEquiped(WeaponModel weapon)
        {
            if (weapon == null)
            {
                DebugUtility.LogError(this, "Unable to replace weapon with null!");
                return;
            }
            model = weapon;
            InitializeStateHandlers(weapon.StateHandlers);
            weaponAnimationsHandler.OnEquip(weapon);
            //UpdateFov();
        }
        
        public void OnLethalEquipmentThrow(LethalEquipmentThrowEvent @event)
        {
            EnterState(typeof(LethalEquipmentStateHandler));
        }

        #region State transition handling

        public void InitializeStateHandlers(List<CombatSystemStateHandler> handlerStates)
        {
            stateHandlers.Clear();
            if (handlerStates.Count == 0) return;
            defaultState = handlerStates[0];
            container.Inject(defaultState);
            DebugUtility.Log(this, $"default state:{defaultState.GetType()}");
            handlerStates.Remove(defaultState);
            stateHandlers.TryAdd(defaultState.GetType(), defaultState);
            foreach (var state in handlerStates)
            {
                if (stateHandlers.TryAdd(state.GetType(), state))
                {
                    container.Inject(state);
                    DebugUtility.Log(this, $"initialized state:{state.GetType()}");
                }
            }
            ResetStateStack();
        }

        private void ExitCurrentState()
        {
            if (stateHandlerStack.TryPop(out var state))
            {
                state.Exit();
            }
        }

        public void ReplaceCurrentState(Type newHandlerStateType)
        {
            if (!typeof(CombatSystemStateHandler).IsAssignableFrom(newHandlerStateType)) return;
            if (!stateHandlers.TryGetValue(newHandlerStateType, out var newHandlerState)) return;
            if (newHandlerState == null)
            {
                DebugUtility.LogError(this, "Unable to replace with null state!");
                return;
            }
            if (stateHandlerStack.Count > 1)
            {
                stateHandlerStack.Pop().Exit();
                stateHandlerStack.Push(newHandlerState);
                newHandlerState.Enter();
            }
            else EnterState(newHandlerStateType);
        }

        public void EnterState(Type handlerStateType)
        {
            if (!typeof(CombatSystemStateHandler).IsAssignableFrom(handlerStateType)) return;
            if (!stateHandlers.TryGetValue(handlerStateType, out var handlerState)) return;
            if (handlerState == null)
            {
                DebugUtility.LogError(this, "Unable to enter null state!");
                return;
            }
            if (stateHandlerStack.TryPeek(out var state))
            {
                if (state.GetType().Equals(handlerState.GetType()))
                {
                    return;
                }
                state.Exit();
            }
            DebugUtility.Log(this, $"Entering {handlerState.GetType()}");
            stateHandlerStack.Push(handlerState);
            handlerState.Enter();
        }

        public void ExitState(Type handlerStateType)
        {
            if (!typeof(CombatSystemStateHandler).IsAssignableFrom(handlerStateType)) return;
            if (!stateHandlers.TryGetValue(handlerStateType, out var handlerState)) return;
            if (handlerState == null)
            {
                DebugUtility.LogError(this, "Unable to enter null state!");
                return;
            }
            if (stateHandlerStack.TryPeek(out CombatSystemStateHandler handlerStateComp))
            {
                if (handlerState != handlerStateComp) return;
            } 
            else
            {
                return;
            }
            DebugUtility.Log(this, $"Exiting {handlerState.GetType()}");
            handlerState.Exit();
            stateHandlerStack.Pop();
            if (stateHandlerStack.TryPeek(out var newState))
            {
                newState.Enter();
            }
            else
            {
                return;
            }
        }

        public void ResetStateStack()
        {
            ExitCurrentState();
            stateHandlerStack = new Stack<CombatSystemStateHandler>();
            EnterState(defaultState.GetType());
        }

        #endregion
    }
}
    