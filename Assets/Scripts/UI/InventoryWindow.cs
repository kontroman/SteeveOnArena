using MineArena.Managers;
using System.Collections.Generic;
using Devotion.SDK.Controllers;
using UnityEngine;
using Devotion.SDK.Base;
using Devotion.SDK.UI;
using MineArena.Items;

namespace MineArena.UI
{
    public class InventoryWindow : BaseWindow
    {
        [SerializeField] private Transform _inventoryGrid;
        [SerializeField] private List<InventoryCellUI> _inventoryCells = new List<InventoryCellUI>();

        private InventoryManager _inventoryManager;
        private bool _subscribed;

        private void OnEnable()
        {
            Subscribe();
            UpdateUI();
        }

        private void OnDisable()
        {
            if (!_subscribed)
                return;

            if (_inventoryManager != null)
                _inventoryManager.InventoryUpdated -= UpdateUI;

            PlayingWindow.QuickSlotsChanged -= UpdateUI;
            _subscribed = false;
        }

        private void UpdateUI()
        {
            ClearCells();

            Subscribe();

            var items = GetVisibleInventoryItems();

            for (int i = 0; i < items.Count; i++)
            {
                if (i < _inventoryCells.Count)
                {
                    var cellUI = _inventoryCells[i];
                    cellUI.Setup(items[i]);
                }
            }
        }

        private void Subscribe()
        {
            if (_subscribed)
                return;

            _inventoryManager = GameRoot.GetManager<InventoryManager>();
            if (_inventoryManager != null)
                _inventoryManager.InventoryUpdated += UpdateUI;

            PlayingWindow.QuickSlotsChanged += UpdateUI;
            _subscribed = true;
        }

        private List<Item> GetVisibleInventoryItems()
        {
            var visibleItems = new List<Item>();
            if (_inventoryManager == null)
                return visibleItems;

            var hiddenCounts = GetQuickSlotItemCounts();

            foreach (var item in _inventoryManager.Items)
            {
                if (item == null)
                    continue;

                var hiddenCount = hiddenCounts.TryGetValue(item.Name, out var count) ? count : 0;
                if (hiddenCount <= 0)
                {
                    visibleItems.Add(item);
                    continue;
                }

                if (item is StackableItem stackableItem)
                {
                    var visibleAmount = stackableItem.CurrentStack - hiddenCount;
                    if (visibleAmount > 0)
                        visibleItems.Add(new StackableItem(stackableItem.Config, visibleAmount));

                    hiddenCounts[item.Name] = Mathf.Max(0, hiddenCount - stackableItem.CurrentStack);
                    continue;
                }

                hiddenCounts[item.Name] = hiddenCount - 1;
            }

            return visibleItems;
        }

        private static Dictionary<string, int> GetQuickSlotItemCounts()
        {
            var counts = new Dictionary<string, int>();
            var quickSlots = GameRoot.PlayerProgress?.InventoryProgress?.QuickSlotItemIds;
            if (quickSlots == null)
                return counts;

            foreach (var itemId in quickSlots)
            {
                if (string.IsNullOrWhiteSpace(itemId))
                    continue;

                counts.TryGetValue(itemId, out var count);
                counts[itemId] = count + 1;
            }

            return counts;
        }

        private void ClearCells()
        {
            foreach (var cell in _inventoryCells)
            {
                cell.Clear();
            }
        }

        public void MoveItem(int fromCellIndex, int toCellIndex)
        {
            if (fromCellIndex < 0 || fromCellIndex >= _inventoryCells.Count ||
                toCellIndex < 0 || toCellIndex >= _inventoryCells.Count)
            {
                Debug.LogWarning("Invalid cell indices for moving item.");
                return;
            }

            var fromCell = _inventoryCells[fromCellIndex];
            var toCell = _inventoryCells[toCellIndex];

            if (fromCell.HasItem)
            {
                var itemToMove = fromCell.Item;
                if (toCell.HasItem)
                {
                    var tempItem = toCell.Item;
                    toCell.Setup(itemToMove);
                    fromCell.Setup(tempItem);
                }
                else
                {
                    toCell.Setup(itemToMove);
                    fromCell.Clear();
                }
            }
        }

        public override void CloseWindow()
        {
            GameRoot.UIManager.CloseWindow<InventoryWindow>();
        }
    }
}
