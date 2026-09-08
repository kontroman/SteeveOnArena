using System.Collections.Generic;
using Devotion.SDK.Base;
using Devotion.SDK.Controllers;
using Devotion.SDK.Helpers;
using MineArena.Items;
using MineArena.Managers;
using MineArena.UI;
using TMPro;
using UnityEngine;

namespace MineArena.Windows
{
    // Presentation only: the storage owner supplies its items; this view never moves or saves them.
    public class StorageWindow : BaseWindow
    {
        [SerializeField] private Transform _inventoryContent;
        [SerializeField] private Transform _storageContent;
        [SerializeField] private InventoryCellUI _cellTemplate;
        [SerializeField] private TMP_Text _inventoryCount;
        [SerializeField] private TMP_Text _storageCount;
        [SerializeField] private GameObject _storageEmpty;
        [SerializeField, Min(1)] private int _minimumSlots = 30;

        private readonly List<Item> _storedItems = new();
        private readonly List<InventoryCellUI> _inventoryCells = new();
        private readonly List<InventoryCellUI> _storageCells = new();
        private InventoryManager _inventory;

        private void OnEnable()
        {
            if (!Application.isPlaying || GameRoot.Instance == null) return;
            _inventory = GameRoot.GetManager<InventoryManager>();
            if (_inventory != null) _inventory.InventoryUpdated += RefreshInventory;
            RefreshInventory();
            RefreshStorage();
        }

        private void OnDisable()
        {
            if (_inventory != null) _inventory.InventoryUpdated -= RefreshInventory;
            _inventory = null;
        }

        public void SetStorageContents(IEnumerable<Item> items)
        {
            // Copy first so callers may safely pass a view over the previous contents.
            var snapshot = items == null ? new List<Item>() : new List<Item>(items);
            _storedItems.Clear();
            _storedItems.AddRange(snapshot);
            RefreshStorage();
        }

        private void RefreshInventory() => ShowItems(_inventory != null ? _inventory.Items : null,
            _inventoryContent, _inventoryCells, _inventoryCount);

        private void RefreshStorage()
        {
            int count = ShowItems(_storedItems, _storageContent, _storageCells, _storageCount);
            if (_storageEmpty != null) _storageEmpty.SetActive(count == 0);
        }

        private int ShowItems(IEnumerable<Item> items, Transform content, List<InventoryCellUI> cells, TMP_Text counter)
        {
            if (content == null || _cellTemplate == null) return 0;
            if (cells.Count == 0) cells.AddRange(content.GetComponentsInChildren<InventoryCellUI>(true));
            var visible = new List<Item>();
            if (items != null)
                foreach (var item in items)
                    if (item != null && (!(item is StackableItem stack) || stack.CurrentStack > 0)) visible.Add(item);
            int slots = Mathf.Max(_minimumSlots, Mathf.CeilToInt(visible.Count / 6f) * 6);
            while (cells.Count < slots) cells.Add(Instantiate(_cellTemplate, content));
            for (int i = 0; i < cells.Count; i++)
            {
                cells[i].gameObject.SetActive(i < slots);
                if (i < visible.Count) cells[i].Setup(visible[i]);
                else cells[i].Clear();
            }
            if (counter != null) counter.text = "Занято ячеек: " + visible.Count;
            return visible.Count;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape)) CloseWindow();
        }

        public override void CloseWindow() => this.CloseWindow<StorageWindow>();
    }
}
