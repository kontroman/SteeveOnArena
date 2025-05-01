using MineArena.Managers;
using System.Collections.Generic;
using UnityEngine;

namespace MineArena.UI
{
    public class InventoryUI : MonoBehaviour
    {
        [SerializeField] private Transform _inventoryGrid;
        [SerializeField] private List<InventoryCellUI> _inventoryCells = new List<InventoryCellUI>();

        private void OnEnable()
        {
            InventoryManager.Instance.InventoryUpdated += UpdateUI;
            UpdateUI();
        }

        private void OnDisable()
        {
            InventoryManager.Instance.InventoryUpdated -= UpdateUI;
        }

        private void UpdateUI()
        {
            ClearCells();

            var items = InventoryManager.Instance.Items;

            for (int i = 0; i < items.Count; i++)
            {
                if (i < _inventoryCells.Count)
                {
                    var cellUI = _inventoryCells[i];
                    cellUI.Setup(items[i]);
                }
            }
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
    }
}
