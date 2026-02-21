using MyToolz.DesignPatterns.EventBus;
using MyToolz.Events;
using MyToolz.InventorySystem.Settings;
using MyToolz.UI.Management;
using MyToolz.Utilities.Debug;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace MyToolz.InventorySystem.Views
{
    public interface IInventoryView<T> where T : ScriptableObject
    {
        void Open();
        void Close();
    }

    public abstract class InventoryView<ItemSO> : MonoBehaviour, IInventoryView<ItemSO>, IEventListener where ItemSO : ScriptableObject
    {
        [SerializeField] protected UIScreenBase window;
        [SerializeField] protected RectTransform container;
        [SerializeField] protected InventoryHudView<ItemSO> hudView;

        protected InventoryCell<ItemSO> cellPrefab;
        protected InventoryItemView<ItemSO> itemViewPrefab;
        protected int inventorySize;

        private readonly List<InventoryCell<ItemSO>> cells = new List<InventoryCell<ItemSO>>();
        private readonly Dictionary<ItemSO, InventoryCell<ItemSO>> itemCellMap = new Dictionary<ItemSO, InventoryCell<ItemSO>>();

        private EventBinding<InventoryInitializeEvent<ItemSO>> initBinding;
        private EventBinding<InventoryItemUpdatedEvent<ItemSO>> itemUpdatedBinding;
        private EventBinding<InventoryCellDropEvent<ItemSO>> cellDropBinding;

        [Inject]
        private void Construct(InventorySettingsSO<ItemSO> settings)
        {
            if (settings != null)
            {
                if (settings.ItemViewPrefab == null)
                {
                    DebugUtility.LogError(this, "InventoryItemView is null!");
                    return;
                }
                itemViewPrefab = settings.ItemViewPrefab;
                if (settings.CellPrefab == null)
                {
                    DebugUtility.LogError(this, "InventoryCellView is null!");
                    return;
                }
                cellPrefab = settings.CellPrefab;
                inventorySize = settings.InventorySize;

            }
        }

        public void Open()  => window?.Open();
        public void Close() => window?.Close();

        public void RegisterEvents()
        {
            initBinding = new EventBinding<InventoryInitializeEvent<ItemSO>>(OnInitialize);
            itemUpdatedBinding = new EventBinding<InventoryItemUpdatedEvent<ItemSO>>(OnItemUpdated);
            cellDropBinding = new EventBinding<InventoryCellDropEvent<ItemSO>>(HandleCellDrop);

            EventBus<InventoryInitializeEvent<ItemSO>>.Register(initBinding);
            EventBus<InventoryItemUpdatedEvent<ItemSO>>.Register(itemUpdatedBinding);
            EventBus<InventoryCellDropEvent<ItemSO>>.Register(cellDropBinding);

            hudView?.RegisterEvents();
        }

        public void UnregisterEvents()
        {
            EventBus<InventoryInitializeEvent<ItemSO>>.Deregister(initBinding);
            EventBus<InventoryItemUpdatedEvent<ItemSO>>.Deregister(itemUpdatedBinding);
            EventBus<InventoryCellDropEvent<ItemSO>>.Deregister(cellDropBinding);

            hudView?.UnregisterEvents();
        }

        protected virtual void OnEnable()
        {
            RegisterEvents();
        }

        protected virtual void OnDisable()
        {
            UnregisterEvents();
        }

        protected virtual void OnDestroy()
        {
            UnregisterEvents();
        }

        private void OnInitialize(InventoryInitializeEvent<ItemSO> e)
        {
            StartCoroutine(InitializeRoutine(e.Items, e.CellPositions));
        }

        private IEnumerator InitializeRoutine(IReadOnlyDictionary<ItemSO, uint> items, IReadOnlyDictionary<ItemSO, int> cellPositions)
        {
            SpawnEmptyCells();

            yield return null;

            foreach (var kvp in items)
            {
                int targetIndex = -1;
                if (cellPositions != null && cellPositions.TryGetValue(kvp.Key, out int idx))
                    targetIndex = idx;
                PlaceItemInCell(kvp.Key, kvp.Value, targetIndex);
            }

            hudView?.Initialize(items);
        }

        private void SpawnEmptyCells()
        {
            for (int i = 0; i < inventorySize; i++)
            {
                EventBus<PoolRequest<InventoryCell<ItemSO>>>.Raise(
                    new PoolRequest<InventoryCell<ItemSO>>
                    {
                        Prefab  = cellPrefab,
                        Parent  = container,
                        Callback = cell =>
                        {
                            if (cell.TryGetComponent(out RectTransform rt))
                                rt.localScale = Vector3.one;

                            cell.ClearCell();
                            cells.Add(cell);
                        }
                    });
            }
        }

        private void OnItemUpdated(InventoryItemUpdatedEvent<ItemSO> e)
        {
            PlaceItemInCell(e.Item, e.Amount, -1);
            hudView?.UpdateItem(e.Item, e.Amount);
        }

        private void PlaceItemInCell(ItemSO itemSO, uint amount, int targetCellIndex)
        {
            if (itemViewPrefab == null || itemSO == null) return;
            if (amount > 0)
            {

                if (itemCellMap.TryGetValue(itemSO, out var existingCell))
                {
                    EventBus<PoolRequest<InventoryItemView<ItemSO>>>.Raise(new PoolRequest<InventoryItemView<ItemSO>>()
                    {
                        Prefab = itemViewPrefab,
                        Parent = existingCell.transform,
                        Callback = itm =>
                        {
                            existingCell.Initialize(itm, itemSO, amount);
                        }
                    });
                }
                else
                {
                    InventoryCell<ItemSO> emptyCell = null;
                    if (targetCellIndex >= 0 && targetCellIndex < cells.Count)
                    {
                        var candidate = cells.Find(c => c.transform.GetSiblingIndex() == targetCellIndex);
                        if (candidate != null && candidate.IsEmpty)
                            emptyCell = candidate;
                    }
                    if (emptyCell == null)
                        emptyCell = cells.Find(c => c.IsEmpty);
                    if (emptyCell != null)
                    {
                        if (emptyCell.ItemView != null)
                            emptyCell.ItemView.OnValueChanged += OnCellValueChanged;

                        EventBus<PoolRequest<InventoryItemView<ItemSO>>>.Raise(new PoolRequest<InventoryItemView<ItemSO>>()
                        {
                            Prefab = itemViewPrefab,
                            Parent = emptyCell.transform,
                            Callback = itm =>
                            {
                                emptyCell.Initialize(itm, itemSO, amount);
                            }                        
                        });

                        itemCellMap[itemSO] = emptyCell;
                    }
                    else
                    {
                        DebugUtility.Log(this, "No empty itm available for item.");
                    }
                }
            }
            else
            {
                if (itemCellMap.TryGetValue(itemSO, out var cell))
                {
                    if (cell.ItemView != null)
                        cell.ItemView.OnValueChanged -= OnCellValueChanged;

                    cell.ClearCell();
                    itemCellMap.Remove(itemSO);
                }
            }
            DebugUtility.Log(this, "Placed item in a itm");
        }

        public int GetCellIndex(ItemSO item)
        {
            if (itemCellMap.TryGetValue(item, out var cell))
                return cell.transform.GetSiblingIndex();
            return -1;
        }

        private void HandleCellDrop(InventoryCellDropEvent<ItemSO> e)
        {
            if (e.TargetCell == null || !e.TargetCell.transform.IsChildOf(container)) return;
            if (e.SourceCell == null || !e.SourceCell.transform.IsChildOf(container)) return;
            if (e.SourceCell == e.TargetCell) return;

            var sourceItem = e.DroppedItem;
            var sourceAmount = e.DroppedAmount;
            var targetItem = e.TargetItem;
            var targetAmount = e.TargetAmount;

            if (sourceItem != null)
                itemCellMap.Remove(sourceItem);
            if (targetItem != null)
                itemCellMap.Remove(targetItem);

            e.SourceCell.ClearCell();
            e.TargetCell.ClearCell();

            if (targetItem != null)
            {
                EventBus<PoolRequest<InventoryItemView<ItemSO>>>.Raise(new PoolRequest<InventoryItemView<ItemSO>>()
                {
                    Prefab = itemViewPrefab,
                    Parent = e.SourceCell.transform,
                    Callback = itm =>
                    {
                        e.SourceCell.Initialize(itm, targetItem, targetAmount);
                    }
                });
                itemCellMap[targetItem] = e.SourceCell;
            }

            if (sourceItem != null)
            {
                EventBus<PoolRequest<InventoryItemView<ItemSO>>>.Raise(new PoolRequest<InventoryItemView<ItemSO>>()
                {
                    Prefab = itemViewPrefab,
                    Parent = e.TargetCell.transform,
                    Callback = itm =>
                    {
                        e.TargetCell.Initialize(itm, sourceItem, sourceAmount);
                    }
                });
                itemCellMap[sourceItem] = e.TargetCell;
            }

            InventoryDragState<ItemSO>.DropHandled = true;
        }

        private void OnCellValueChanged(ItemSO item, uint amount)
        {
            DebugUtility.Log(this, "Cell value changed — notifying Presenter via EventBus.");
            EventBus<InventoryItemAmountChangedEvent<ItemSO>>.Raise(new InventoryItemAmountChangedEvent<ItemSO>
            {
                Item   = item,
                Amount = amount
            });
        }
    }
}



