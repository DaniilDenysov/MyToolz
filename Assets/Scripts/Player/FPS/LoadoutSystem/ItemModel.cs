using MyToolz.Player.FPS.CombatSystem.Presenter;
using MyToolz.Player.FPS.Inventory;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;

namespace MyToolz.Player.FPS.CombatSystem.Model
{
    [System.Serializable]
    public struct StateHandlerWrapper
    {
        [SerializeReference] public CombatSystemStateHandler StateHandler;
    }

    public abstract class ItemModel<T> : MonoBehaviour where T : ItemSO
    {
        [SerializeField, Required] protected T itemSO;
        [SerializeField] protected List<StateHandlerWrapper> stateHandlerWrappers;
        public List<CombatSystemStateHandler> StateHandlers
        {
            get
            {
                List<CombatSystemStateHandler> stateHandlers = new List<CombatSystemStateHandler>();
                foreach (var stateWrapper in stateHandlerWrappers)
                {
                    stateHandlers.Add(stateWrapper.StateHandler);
                }
                return stateHandlers;
            }
        }
        private bool isAccessible = true;
        public bool IsAccessible
        {
            get
            {
                return isAccessible;
            }

            set
            {
                OnAccessibleChanged(value);
               isAccessible = value;
            }
        }
        public T GetItemSO() => itemSO;

        public virtual void OnAccessibleChanged(bool state)
        {

        }
    }
}
