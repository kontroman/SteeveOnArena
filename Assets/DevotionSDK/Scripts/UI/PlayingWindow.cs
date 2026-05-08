using Windows;
using System;
using System.Collections.Generic;
using Devotion.SDK.Base;
using Devotion.SDK.Controllers;
using MineArena.Controllers;
using MineArena.Items;
using MineArena.Managers;
using MineArena.PlayerSystem;
using MineArena.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Devotion.SDK.UI
{
    public class PlayingWindow : BaseWindow
    {
        private const int SlotCount = 5;
        private const string ResourceIconPrefabPath = "Prefabs/Windows/ResourceIcon";

        public static event Action QuickSlotsChanged;

        [Header("Inventory panel")]
        [SerializeField] private Transform _inventoryPanel;
        [SerializeField] private List<PlayingInventorySlotUI> _inventorySlots = new();
        [SerializeField] private Sprite _activeSlotSprite;
        [SerializeField] private Sprite _inactiveSlotSprite;
        [SerializeField] private Sprite _fallbackItemSprite;

        private InventoryManager _inventoryManager;
        private ResourceIcon _resourceIconPrefab;
        private bool _initialized;

        private void Awake()
        {
            InitializeInventoryPanel();
        }

        private void OnEnable()
        {
            InitializeInventoryPanel();
            SubscribeInventory();
            RefreshInventorySlots();
            ApplySelectedSlotItemToPlayer();
        }

        private void OnDisable()
        {
            if (_inventoryManager != null)
            {
                _inventoryManager.InventoryUpdated -= HandleInventoryUpdated;
                _inventoryManager = null;
            }
        }

        private void Update()
        {
            for (int i = 0; i < SlotCount; i++)
            {
                if (Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha1 + i)) ||
                    Input.GetKeyDown((KeyCode)((int)KeyCode.Keypad1 + i)))
                {
                    SelectInventorySlot(i);
                }
            }
        }

        public void OnAchievmentButtonClick() => GameRoot.UIManager.ShowWindow<WindowAchievements>();

        public void OnWheelButtonClick() => GameRoot.UIManager.ShowWindow<FortuneWheelWindow>();

        public void SelectInventorySlot(int index)
        {
            if (!IsValidSlotIndex(index))
                return;

            var progress = GameRoot.PlayerProgress?.InventoryProgress;
            if (progress == null)
                return;

            progress.SetSelectedQuickSlotIndex(index);
            RefreshInventorySlots();
            ApplySelectedSlotItemToPlayer();
        }

        public bool TrySetInventorySlotItem(int index, Item item)
        {
            if (!IsValidSlotIndex(index) || item == null || string.IsNullOrWhiteSpace(item.Name))
                return false;

            var progress = GameRoot.PlayerProgress?.InventoryProgress;
            if (progress == null)
                return false;

            progress.SetQuickSlotItemId(index, item.Name);
            SelectInventorySlot(index);
            QuickSlotsChanged?.Invoke();
            return true;
        }

        public bool TryReturnInventorySlotItem(int index)
        {
            if (!IsValidSlotIndex(index))
                return false;

            var progress = GameRoot.PlayerProgress?.InventoryProgress;
            if (progress == null || string.IsNullOrWhiteSpace(progress.GetQuickSlotItemId(index)))
                return false;

            progress.SetQuickSlotItemId(index, string.Empty);
            RefreshInventorySlots();
            ApplySelectedSlotItemToPlayer();
            QuickSlotsChanged?.Invoke();
            return true;
        }

        private void InitializeInventoryPanel()
        {
            if (_initialized)
                return;

            if (_inventoryPanel == null)
            {
                var panel = transform.Find("InventoryPanel");
                _inventoryPanel = panel != null ? panel : FindChildByName(transform, "InventoryPanel");
            }

            if (_inventoryPanel == null)
                return;

            _inventorySlots.Clear();

            for (int i = 0; i < _inventoryPanel.childCount && _inventorySlots.Count < SlotCount; i++)
            {
                var child = _inventoryPanel.GetChild(i);
                var background = child.GetComponent<Image>();
                if (background == null)
                    continue;

                var flatIcon = ResolveFlatIcon(child, background);
                var resourceIcon = ResolveResourceIcon(child);

                var slot = child.GetComponent<PlayingInventorySlotUI>();
                if (slot == null)
                    slot = child.gameObject.AddComponent<PlayingInventorySlotUI>();

                slot.Initialize(this, _inventorySlots.Count, background, flatIcon, resourceIcon);
                _inventorySlots.Add(slot);
            }

            ResolveSlotSprites();
            _initialized = _inventorySlots.Count > 0;
        }

        private void SubscribeInventory()
        {
            var manager = GameRoot.GetManager<InventoryManager>();
            if (_inventoryManager == manager)
                return;

            if (_inventoryManager != null)
                _inventoryManager.InventoryUpdated -= HandleInventoryUpdated;

            _inventoryManager = manager;

            if (_inventoryManager != null)
                _inventoryManager.InventoryUpdated += HandleInventoryUpdated;
        }

        private void HandleInventoryUpdated()
        {
            RefreshInventorySlots();
            ApplySelectedSlotItemToPlayer();
        }

        private void RefreshInventorySlots()
        {
            InitializeInventoryPanel();

            var progress = GameRoot.PlayerProgress?.InventoryProgress;
            if (progress == null)
                return;

            int selectedIndex = progress.SelectedQuickSlotIndex;

            for (int i = 0; i < _inventorySlots.Count; i++)
            {
                var slot = _inventorySlots[i];
                slot.SetSelected(i == selectedIndex, _activeSlotSprite, _inactiveSlotSprite);

                var item = ResolveInventoryItem(progress.GetQuickSlotItemId(i));
                slot.SetItem(item, _fallbackItemSprite);
            }
        }

        private void ApplySelectedSlotItemToPlayer()
        {
            var progress = GameRoot.PlayerProgress?.InventoryProgress;
            if (progress == null)
                return;

            var item = ResolveInventoryItem(progress.GetQuickSlotItemId(progress.SelectedQuickSlotIndex));
            var equipment = Player.Instance != null ? Player.Instance.GetComponentFromList<PlayerEquipment>() : null;
            if (equipment == null)
                return;

            var handItem = ResolveHandItemType(item);

            if (handItem != HandItemType.None)
            {
                equipment.SetActiveHandItem(handItem);
                Debug.Log($"[PlayingWindow] Quick slot selected: {item.Name}, hand item: {handItem}.");
            }

            if (item == null)
                Debug.Log("[PlayingWindow] Quick slot selected: empty. Hand item unchanged.");
            else if (handItem == HandItemType.None)
                Debug.Log($"[PlayingWindow] Quick slot selected: {item.Name}. Hand item unchanged.");
        }

        private Item ResolveInventoryItem(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId))
                return null;

            SubscribeInventory();

            if (_inventoryManager != null)
            {
                foreach (var item in _inventoryManager.Items)
                {
                    if (item != null && string.Equals(item.Name, itemId, StringComparison.OrdinalIgnoreCase))
                        return item;
                }
            }

            var database = GameRoot.GameConfig != null ? GameRoot.GameConfig.ItemDatabase : null;
            var config = database != null ? database.GetItemConfig(itemId) : null;

            if (config is StackableItemConfig stackableConfig)
                return new StackableItem(stackableConfig, 1);

            if (config != null)
                return new Item(config.Name, config.Prefab, config.Icon);

            return new Item(itemId, null, null);
        }

        private static HandItemType ResolveHandItemType(Item item)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.Name))
                return HandItemType.None;

            if (Contains(item.Name, "Bow"))
                return HandItemType.Bow;

            if (Contains(item.Name, "Sword"))
                return HandItemType.Sword;

            if (Contains(item.Name, "Pickaxe"))
                return HandItemType.Pickaxe;

            return HandItemType.None;
        }

        private void ResolveSlotSprites()
        {
            if (_inventorySlots.Count == 0)
                return;

            if (_activeSlotSprite == null)
                _activeSlotSprite = _inventorySlots[0].Background != null ? _inventorySlots[0].Background.sprite : null;

            if (_inactiveSlotSprite != null)
                return;

            foreach (var slot in _inventorySlots)
            {
                var sprite = slot.Background != null ? slot.Background.sprite : null;
                if (sprite != null && sprite != _activeSlotSprite)
                {
                    _inactiveSlotSprite = sprite;
                    return;
                }
            }
        }

        private ResourceIcon ResolveResourceIcon(Transform slot)
        {
            var resourceIcon = slot.GetComponentInChildren<ResourceIcon>(true);
            if (resourceIcon != null)
                return resourceIcon;

            if (_resourceIconPrefab == null)
                _resourceIconPrefab = Resources.Load<ResourceIcon>(ResourceIconPrefabPath);

            if (_resourceIconPrefab == null)
            {
                Debug.LogWarning($"[PlayingWindow] ResourceIcon prefab not found at Resources/{ResourceIconPrefabPath}.");
                return null;
            }

            resourceIcon = Instantiate(_resourceIconPrefab, slot);
            resourceIcon.name = "ResourceIcon";

            if (resourceIcon.transform is RectTransform rectTransform)
            {
                rectTransform.anchorMin = Vector2.zero;
                rectTransform.anchorMax = Vector2.one;
                rectTransform.offsetMin = Vector2.zero;
                rectTransform.offsetMax = Vector2.zero;
                rectTransform.localScale = Vector3.one;
            }

            resourceIcon.gameObject.SetActive(false);
            return resourceIcon;
        }

        private static Image ResolveFlatIcon(Transform slot, Image background)
        {
            var iconTransform = slot.Find("FlatIcon");
            var flatIcon = iconTransform != null ? iconTransform.GetComponent<Image>() : null;
            if (flatIcon != null)
            {
                PrepareFlatIcon(flatIcon);
                return flatIcon;
            }

            for (int i = 0; i < slot.childCount; i++)
            {
                var child = slot.GetChild(i);
                if (child.GetComponent<ResourceIcon>() != null)
                    continue;

                var image = child.GetComponent<Image>();
                if (image != null && image != background)
                {
                    image.name = "FlatIcon";
                    PrepareFlatIcon(image);
                    return image;
                }
            }

            var iconObject = new GameObject("FlatIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            iconObject.transform.SetParent(slot, false);

            var rectTransform = iconObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = new Vector2(10f, 10f);
            rectTransform.offsetMax = new Vector2(-10f, -10f);

            flatIcon = iconObject.GetComponent<Image>();
            PrepareFlatIcon(flatIcon);
            return flatIcon;
        }

        private static void PrepareFlatIcon(Image icon)
        {
            if (icon == null)
                return;

            icon.raycastTarget = false;
            icon.preserveAspect = true;
            icon.color = Color.white;
            icon.enabled = false;
            icon.gameObject.SetActive(false);
        }

        private static bool IsValidSlotIndex(int index)
        {
            return index >= 0 && index < SlotCount;
        }

        private static bool Contains(string value, string part)
        {
            return value.IndexOf(part, StringComparison.OrdinalIgnoreCase) >= 0;
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
