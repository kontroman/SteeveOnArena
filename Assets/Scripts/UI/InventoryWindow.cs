using MineArena.Managers;
using System.Collections.Generic;
using Devotion.SDK.Controllers;
using UnityEngine;
using Devotion.SDK.Base;
using Devotion.SDK.UI;
using MineArena.Items;
using MineArena.PlayerSystem;
using MineArena.Controllers;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MineArena.UI
{
    public class InventoryWindow : BaseWindow
    {
        [SerializeField] private Transform _inventoryGrid;
        [SerializeField] private List<InventoryCellUI> _inventoryCells = new List<InventoryCellUI>();
        [SerializeField] private int _trashCellIndex = 34;
        [Header("Equipment slots")]
        [SerializeField] private Image _equipHelmet;
        [SerializeField] private Image _equipChest;
        [SerializeField] private Image _equipLeggins;
        [SerializeField] private Image _equipBoots;
        [SerializeField] private RectTransform _playerPreview;

        private InventoryManager _inventoryManager;
        private PlayerEquipment _playerEquipment;
        private readonly Dictionary<ArmorSlot, ArmorEquipmentSlotUI> _equipmentSlots = new();
        private PlayerPreviewRenderer _playerPreviewRenderer;
        private bool _subscribed;
        private bool _equipmentSubscribed;

        private void Awake()
        {
            InitializeEquipmentSlots();
            InitializePlayerPreview();
        }

        private void OnEnable()
        {
            InitializeEquipmentSlots();
            InitializePlayerPreview();
            Subscribe();
            SubscribeEquipment();
            UpdateUI();
            UpdateEquipmentSlots();
        }

        private void OnDisable()
        {
            if (_subscribed)
            {
                if (_inventoryManager != null)
                    _inventoryManager.InventoryUpdated -= UpdateUI;

                PlayingWindow.QuickSlotsChanged -= UpdateUI;
                _subscribed = false;
            }

            if (_equipmentSubscribed && _playerEquipment != null)
            {
                _playerEquipment.ArmorChanged -= HandleArmorChanged;
                _equipmentSubscribed = false;
            }
        }

        private void UpdateUI()
        {
            ClearCells();

            Subscribe();
            UpdateEquipmentSlots();

            var items = GetVisibleInventoryItems();

            int cellIndex = 0;
            for (int i = 0; i < items.Count && cellIndex < _inventoryCells.Count; i++)
            {
                while (IsTrashCellIndex(cellIndex))
                    cellIndex++;

                if (cellIndex >= _inventoryCells.Count)
                    break;

                if (items[i] != null)
                {
                    var cellUI = _inventoryCells[cellIndex];
                    cellUI.Setup(items[i]);
                }

                cellIndex++;
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

        private void SubscribeEquipment()
        {
            var equipment = Player.Instance != null ? Player.Instance.GetComponentFromList<PlayerEquipment>() : null;
            if (_playerEquipment == equipment && _equipmentSubscribed)
                return;

            if (_equipmentSubscribed && _playerEquipment != null)
                _playerEquipment.ArmorChanged -= HandleArmorChanged;

            _playerEquipment = equipment;
            _equipmentSubscribed = false;

            if (_playerEquipment == null)
                return;

            _playerEquipment.ArmorChanged += HandleArmorChanged;
            _equipmentSubscribed = true;
        }

        private void HandleArmorChanged(ArmorSlot slot, ArmorConfig armor)
        {
            UpdateEquipmentSlots();
        }

        public bool TryEquipArmorItem(ArmorSlot slot, Item item)
        {
            Subscribe();
            SubscribeEquipment();

            if (_inventoryManager == null || _playerEquipment == null || item == null)
                return false;

            var armorConfig = ResolveArmorConfig(item);
            if (armorConfig == null || armorConfig.Slot != slot)
                return false;

            _playerEquipment.EquipArmor(armorConfig);
            UpdateEquipmentSlots();
            UpdateUI();
            return true;
        }

        public bool TryUnequipArmor(ArmorSlot slot)
        {
            Subscribe();
            SubscribeEquipment();

            if (_inventoryManager == null || _playerEquipment == null)
                return false;

            if (_playerEquipment.UnequipArmor(slot) == null)
                return false;

            UpdateEquipmentSlots();
            UpdateUI();
            return true;
        }

        public bool IsInventoryDropArea(PointerEventData eventData, ArmorEquipmentSlotUI sourceSlot)
        {
            _ = sourceSlot;

            if (eventData == null || EventSystem.current == null || _inventoryGrid == null)
                return false;

            var raycastResults = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, raycastResults);

            foreach (var result in raycastResults)
            {
                if (result.gameObject == null)
                    continue;

                var equipmentSlot = result.gameObject.GetComponentInParent<ArmorEquipmentSlotUI>();
                if (equipmentSlot != null)
                    return false;

                var resultTransform = result.gameObject.transform;
                if (resultTransform == _inventoryGrid || resultTransform.IsChildOf(_inventoryGrid))
                    return true;
            }

            return false;
        }

        private List<Item> GetVisibleInventoryItems()
        {
            var visibleItems = new List<Item>();
            if (_inventoryManager == null)
                return visibleItems;

            var hiddenCounts = GetHiddenItemCounts();

            foreach (var item in _inventoryManager.Items)
            {
                if (item == null)
                {
                    visibleItems.Add(null);
                    continue;
                }

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
                    else
                        visibleItems.Add(null);

                    hiddenCounts[item.Name] = Mathf.Max(0, hiddenCount - stackableItem.CurrentStack);
                    continue;
                }

                visibleItems.Add(null);
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

        private Dictionary<string, int> GetHiddenItemCounts()
        {
            var counts = GetQuickSlotItemCounts();
            AddEquippedArmorCounts(counts);
            return counts;
        }

        private void AddEquippedArmorCounts(Dictionary<string, int> counts)
        {
            if (counts == null || _playerEquipment == null)
                return;

            AddEquippedArmorCount(counts, ArmorSlot.Helmet);
            AddEquippedArmorCount(counts, ArmorSlot.Chest);
            AddEquippedArmorCount(counts, ArmorSlot.Leggings);
            AddEquippedArmorCount(counts, ArmorSlot.Boots);
        }

        private void AddEquippedArmorCount(Dictionary<string, int> counts, ArmorSlot slot)
        {
            var armor = _playerEquipment.GetEquippedArmor(slot);
            if (armor == null || string.IsNullOrWhiteSpace(armor.Name))
                return;

            counts.TryGetValue(armor.Name, out var count);
            counts[armor.Name] = count + 1;
        }

        private void ClearCells()
        {
            for (int i = 0; i < _inventoryCells.Count; i++)
            {
                if (IsTrashCellIndex(i))
                    _inventoryCells[i].ClearItemPreserveIcon();
                else
                    _inventoryCells[i].Clear();
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

                if (IsTrashCellIndex(toCellIndex))
                {
                    RemoveInventoryItem(itemToMove);
                    return;
                }

                _inventoryManager?.MoveItemToSlot(itemToMove, toCellIndex);
            }
        }

        private void RemoveInventoryItem(Item item)
        {
            if (_inventoryManager == null || item == null)
                return;

            var amount = item is StackableItem stackableItem ? stackableItem.CurrentStack : 1;
            _inventoryManager.RemoveItem(item, amount);
        }

        private bool IsTrashCellIndex(int cellIndex)
        {
            return cellIndex == _trashCellIndex;
        }

        public override void CloseWindow()
        {
            GameRoot.UIManager.CloseWindow<InventoryWindow>();
        }

        private void InitializeEquipmentSlots()
        {
            _equipHelmet = ResolveSlotImage(_equipHelmet, "EquipHelmet");
            _equipChest = ResolveSlotImage(_equipChest, "EquipChest");
            _equipLeggins = ResolveSlotImage(_equipLeggins, "EquipLeggins");
            _equipBoots = ResolveSlotImage(_equipBoots, "EquipBoots");

            InitializeEquipmentSlot(ArmorSlot.Helmet, _equipHelmet);
            InitializeEquipmentSlot(ArmorSlot.Chest, _equipChest);
            InitializeEquipmentSlot(ArmorSlot.Leggings, _equipLeggins);
            InitializeEquipmentSlot(ArmorSlot.Boots, _equipBoots);
        }

        private void InitializeEquipmentSlot(ArmorSlot slot, Image image)
        {
            if (image == null)
                return;

            var slotUI = image.GetComponent<ArmorEquipmentSlotUI>();
            if (slotUI == null)
                slotUI = image.gameObject.AddComponent<ArmorEquipmentSlotUI>();

            slotUI.Initialize(this, slot, image);
            _equipmentSlots[slot] = slotUI;
        }

        private void UpdateEquipmentSlots()
        {
            SubscribeEquipment();

            foreach (var entry in _equipmentSlots)
            {
                var armor = _playerEquipment != null ? _playerEquipment.GetEquippedArmor(entry.Key) : null;
                entry.Value.SetArmor(armor);
            }
        }

        private void InitializePlayerPreview()
        {
            if (_playerPreview == null)
            {
                var preview = FindChildByName(transform, "PlayerPreview");
                _playerPreview = preview as RectTransform;
            }

            if (_playerPreview == null)
                return;

            _playerPreviewRenderer = _playerPreview.GetComponent<PlayerPreviewRenderer>();
            if (_playerPreviewRenderer == null)
                _playerPreviewRenderer = _playerPreview.gameObject.AddComponent<PlayerPreviewRenderer>();

            _playerPreviewRenderer.Initialize();
        }

        private ArmorConfig ResolveArmorConfig(Item item)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.Name))
                return null;

            return GameRoot.GameConfig?.ItemDatabase?.GetItemConfig(item.Name) as ArmorConfig;
        }

        private Image ResolveSlotImage(Image current, string childName)
        {
            if (current != null)
                return current;

            var child = FindChildByName(transform, childName);
            return child != null ? child.GetComponent<Image>() : null;
        }

        private static Transform FindChildByName(Transform root, string childName)
        {
            if (root == null)
                return null;

            foreach (Transform child in root)
            {
                if (child.name == childName)
                    return child;

                var nested = FindChildByName(child, childName);
                if (nested != null)
                    return nested;
            }

            return null;
        }
    }
}
