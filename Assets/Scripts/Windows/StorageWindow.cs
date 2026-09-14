using System.Collections.Generic;
using Devotion.SDK.Base;
using Devotion.SDK.Controllers;
using Devotion.SDK.Helpers;
using Devotion.SDK.Services.SaveSystem.Progress;
using MineArena.Buildings;
using MineArena.Items;
using MineArena.Managers;
using MineArena.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MineArena.Windows
{
    public class StorageWindow : BaseWindow
    {
        [SerializeField] private Transform _inventoryContent;
        [SerializeField] private Transform _storageContent;
        [SerializeField] private InventoryCellUI _cellTemplate;
        [SerializeField] private TMP_Text _inventoryCount;
        [SerializeField] private TMP_Text _storageCount;
        [SerializeField] private GameObject _storageEmpty;
        [SerializeField, Min(1)] private int _minimumSlots = 30;
        [SerializeField] private BuildingConfig _storageBuilding;

        private readonly List<Item> _storedItems = new();
        private readonly List<InventoryCellUI> _inventoryCells = new();
        private readonly List<InventoryCellUI> _storageCells = new();
        private readonly Dictionary<InventoryCellUI, StoredInventoryItem> _records = new();
        private InventoryManager _inventory;
        private BuildingConfig _building;
        private Transform _buildingPlace;
        private Button _upgrade;
        private TMP_Text _hint;
        private MineArena.PlayerSystem.PlayerEquipment _equipment;
        private int _shownCapacity = -1;
        private bool _refreshPending;
        private InventoryProgress Progress => GameRoot.PlayerProgress?.InventoryProgress;
        public int Capacity
        {
            get
            {
                var config = _building != null ? _building : _storageBuilding;
                int level = _building != null && GameRoot.Instance != null
                    ? GameRoot.GetManager<BuildingManager>()?.GetBuildingLevel(_building) ?? 0 : 1;
                return config?.GetLevelByNumber(level)?.StorageSlots ?? 0;
            }
        }
        private int MaximumCapacity
        {
            get
            {
                int maximum = 0;
                var config = _building != null ? _building : _storageBuilding;
                if (config != null)
                    foreach (var level in config.Levels) maximum = Mathf.Max(maximum, level.StorageSlots);
                return maximum;
            }
        }

        public static bool IsStorage(BuildingConfig config) => config != null && config.name == "StorageBuilding";
        public static void Open(BuildingConfig config, Transform place)
        {
            if (!IsStorage(config) || (GameRoot.GetManager<BuildingManager>()?.GetBuildingLevel(config) ?? 0) <= 0) return;
            var window = GameRoot.UIManager.OpenWindow<StorageWindow>() as StorageWindow;
            if (window == null) return;
            window._building = config;
            window._buildingPlace = place;
            window.RefreshUpgrade();
            window.RefreshAll();
        }

        private void OnEnable()
        {
            if (!Application.isPlaying || GameRoot.Instance == null) return;
            _inventory = GameRoot.GetManager<InventoryManager>();
            if (_inventory != null) _inventory.InventoryUpdated += RefreshAll;
            Devotion.SDK.UI.PlayingWindow.QuickSlotsChanged += RefreshAll;
            _equipment = MineArena.Controllers.Player.Instance?.GetComponent<MineArena.PlayerSystem.PlayerEquipment>();
            if (_equipment != null) _equipment.ArmorChanged += OnArmorChanged;
            ConfigureControls();
            RefreshAll();
        }

        private void OnDisable()
        {
            if (_inventory != null) _inventory.InventoryUpdated -= RefreshAll;
            Devotion.SDK.UI.PlayingWindow.QuickSlotsChanged -= RefreshAll;
            if (_equipment != null) _equipment.ArmorChanged -= OnArmorChanged;
            _equipment = null;
            _inventory = null;
            _building = null;
            _buildingPlace = null;
        }

        // Equipment raises its event before persisting the selected armor ID.
        private void OnArmorChanged(ArmorSlot slot, ArmorConfig armor) => _refreshPending = true;
        public void ShowLockedHint()
        {
            if (_hint != null) _hint.text = "Улучшите склад, чтобы открыть новые ячейки";
        }

        private void ConfigureControls()
        {
            foreach (var label in GetComponentsInChildren<TMP_Text>(true))
            {
                if (label.name == "Description")
                {
                    label.text = "Клик — перенести стак • ПКМ — одну вещь • Можно перетаскивать";
                    label.fontSize = 19;
                    label.enableAutoSizing = true; label.fontSizeMin = 13; label.fontSizeMax = 19;
                    label.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 1020);
                }
                if (label.name == "InventoryHint")
                {
                    _hint = label;
                    label.enableAutoSizing = true; label.fontSizeMin = 13; label.fontSizeMax = 20;
                    label.text = "Вещи на складе не расходуются на крафт";
                }
            }
            if (_upgrade != null || _storageEmpty == null) return;
            var go = new GameObject("UpgradeStorage", typeof(RectTransform), typeof(Image), typeof(Button));
            go.layer = gameObject.layer;
            go.transform.SetParent(_storageEmpty.transform.parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(1, 1);
            rect.anchoredPosition = new Vector2(-36, -110);
            rect.sizeDelta = new Vector2(220, 40);
            go.GetComponent<Image>().color = new Color(.22f, .40f, .36f);
            _upgrade = go.GetComponent<Button>();
            var labelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelObject.layer = go.layer;
            labelObject.transform.SetParent(go.transform, false);
            var labelRect = (RectTransform)labelObject.transform;
            labelRect.anchorMin = Vector2.zero; labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;
            var text = labelObject.GetComponent<TextMeshProUGUI>();
            text.font = _inventoryCount.font;
            text.fontSize = 20; text.alignment = TextAlignmentOptions.Center;
            text.enableAutoSizing = true; text.fontSizeMin = 13; text.fontSizeMax = 20;
            text.color = Color.white; text.raycastTarget = false; text.text = "Улучшить склад";
            _upgrade.onClick.AddListener(() =>
            {
                var config = _building;
                var place = _buildingPlace;
                CloseWindow();
                var window = GameRoot.UIManager.OpenWindow<BuildingWindow>() as BuildingWindow;
                window?.InitializeBuilding(config, place);
            });
            RefreshUpgrade();
        }

        private void RefreshUpgrade()
        {
            if (_upgrade == null) return;
            var manager = GameRoot.GetManager<BuildingManager>();
            _upgrade.gameObject.SetActive(_building != null && manager != null &&
                _building.TryGetNextLevel(manager.GetBuildingLevel(_building), out _));
        }

        // Used by the editor preview without touching player progress.
        public void SetStorageContents(IEnumerable<Item> items)
        {
            var snapshot = items == null ? new List<Item>() : new List<Item>(items);
            _storedItems.Clear();
            _storedItems.AddRange(snapshot);
            ShowItems(_storedItems, _storageContent, _storageCells, _storageCount);
        }

        private void RefreshAll()
        {
            _shownCapacity = Capacity;
            ShowItems(_inventory != null ? _inventory.Items : null, _inventoryContent, _inventoryCells, _inventoryCount);
            foreach (var cell in _inventoryCells)
                if (cell.HasItem && !(cell.Item is StackableItem) && Progress != null &&
                    Progress.SavedResources.TryGetValue(cell.Item.Name, out int count)) cell.ShowStoredTransferCount(count);
            _storedItems.Clear();
            var records = new List<StoredInventoryItem>();
            if (Progress != null)
                foreach (var entry in Progress.StoredItems)
                {
                    if (entry == null || entry.Amount <= 0) continue;
                    var config = GameRoot.GameConfig.ItemDatabase.GetItemConfig(entry.ItemId);
                    // Preserve unknown entries in the save for future content versions.
                    if (config == null) continue;
                    _storedItems.Add(InventoryManager.CreateItemFromConfig(config, entry.Amount));
                    records.Add(entry);
                }
            ShowItems(_storedItems, _storageContent, _storageCells, _storageCount);
            _records.Clear();
            for (int i = 0; i < records.Count; i++)
            {
                _records[_storageCells[i]] = records[i];
                ItemDurabilityBar.BindStored(_storageCells[i].gameObject,
                    GameRoot.GameConfig.ItemDatabase.GetItemConfig(records[i].ItemId) as ArmorConfig, records[i].Durability);
            }
            if (_storageEmpty != null) _storageEmpty.SetActive(records.Count == 0);
            if (_storageCount != null) _storageCount.text = "Занято ячеек: " + (Progress?.UsedStorageSlots ?? records.Count) + " / " + Capacity;
        }

        public bool CanStartTransfer(InventoryCellUI cell)
        {
            if (cell == null || !cell.HasItem) return false;
            if (_records.ContainsKey(cell)) return true;
            if (Progress?.IsStorageTransferProtected(cell.Item.Name) == true)
            {
                if (_hint != null) _hint.text = "Сначала снимите предмет или уберите его с панели";
                return false;
            }
            return true;
        }

        public bool Transfer(InventoryCellUI cell, bool single = false)
        {
            if (!isActiveAndEnabled || cell == null || !cell.HasItem || _inventory == null || _building == null ||
                (GameRoot.GetManager<BuildingManager>()?.GetBuildingLevel(_building) ?? 0) <= 0) return false;
            if (!CanStartTransfer(cell)) return false;
            var config = GameRoot.GameConfig.ItemDatabase.GetItemConfig(cell.Item.Name);
            bool success;
            if (_records.TryGetValue(cell, out var entry))
                success = Progress.TryWithdraw(entry, config, !single && config is StackableItemConfig ? entry.Amount : 1);
            else if (_inventoryCells.Contains(cell) && ContainsLiveItem(cell.Item))
                success = Progress.TryDeposit(config, !single && cell.Item is StackableItem stack ? stack.CurrentStack : 1, Capacity);
            else return false;
            if (!success)
            {
                if (_hint != null) _hint.text = config?.Name == "WoodenPickaxe"
                    ? "Базовая кирка всегда остаётся с собой" : Progress.UsedStorageSlots >= Capacity
                        ? "Склад заполнен. Освободите ячейку или улучшите склад" : "Не удалось перенести предмет";
                return false;
            }
            if (_hint != null) _hint.text = "Вещи на складе не расходуются на крафт";
            _inventory.InitManager();
            MineArena.Controllers.Player.Instance?.GetComponent<MineArena.PlayerSystem.PlayerEquipment>()
                ?.OnMessage(new Devotion.SDK.Messages.Player.PlayerProgressLoaded());
            return true;
        }

        private bool ContainsLiveItem(Item item)
        {
            foreach (var owned in _inventory.Items) if (ReferenceEquals(owned, item)) return true;
            return false;
        }

        public void TryDrop(InventoryCellUI source, Item draggedItem, List<RaycastResult> hits)
        {
            if (source == null || !ReferenceEquals(source.Item, draggedItem)) return;
            bool fromStorage = _records.ContainsKey(source);
            foreach (var hit in hits)
            {
                var target = hit.gameObject.transform;
                var locked = target.GetComponentInParent<StorageLockedSlotGraphic>();
                if (locked != null && locked.isActiveAndEnabled) { ShowLockedHint(); return; }
                var targetCell = target.GetComponentInParent<InventoryCellUI>();
                if (!fromStorage && targetCell != null && targetCell.transform.IsChildOf(_storageContent) &&
                    !targetCell.HasItem && _storageCells.IndexOf(targetCell) >= Capacity) { ShowLockedHint(); return; }
                if (target.IsChildOf((fromStorage ? _inventoryContent : _storageContent).parent))
                {
                    Transfer(source);
                    return;
                }
                if (target.IsChildOf((fromStorage ? _storageContent : _inventoryContent).parent)) return;
            }
        }

        private void ShowItems(IEnumerable<Item> items, Transform content, List<InventoryCellUI> cells, TMP_Text counter)
        {
            if (content == null || _cellTemplate == null) return;
            if (cells.Count == 0) cells.AddRange(content.GetComponentsInChildren<InventoryCellUI>(true));
            var visible = new List<Item>();
            if (items != null)
                foreach (var item in items)
                    if (item != null && (!(item is StackableItem stack) || stack.CurrentStack > 0) &&
                        (content != _inventoryContent || (item.Name != "WoodenPickaxe" && Progress?.IsStorageTransferProtected(item.Name) != true))) visible.Add(item);
            bool storage = content == _storageContent;
            int slots = storage ? Mathf.Max(MaximumCapacity, visible.Count)
                : Mathf.Max(_minimumSlots, Mathf.CeilToInt(visible.Count / 6f) * 6);
            while (cells.Count < slots) cells.Add(Instantiate(_cellTemplate, content));
            for (int i = 0; i < cells.Count; i++)
            {
                cells[i].gameObject.SetActive(i < slots);
                if (i < visible.Count) cells[i].Setup(visible[i]);
                else cells[i].Clear();
                // Old saves above the new limit remain visible and withdrawable.
                StorageLockedSlotGraphic.SetLocked(cells[i], storage && i >= Capacity && i >= visible.Count);
                if (cells[i].GetComponent<StorageCellInteraction>() == null) cells[i].gameObject.AddComponent<StorageCellInteraction>();
                if (cells[i].GetComponent<InventoryCellDragHandler>() == null) cells[i].gameObject.AddComponent<InventoryCellDragHandler>();
            }
            if (counter != null) counter.text = "Занято ячеек: " + visible.Count;
            if (storage && counter != null) counter.text += " / " + Capacity;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape)) CloseWindow();
            if (_inventory != null && (_refreshPending || _shownCapacity != Capacity))
            {
                _refreshPending = false;
                RefreshAll(); RefreshUpgrade();
            }
        }
        public override void CloseWindow() => this.CloseWindow<StorageWindow>();
    }
}
