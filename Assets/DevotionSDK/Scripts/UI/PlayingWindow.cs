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
        private bool? _fullHudVisible;
        private bool _arenaHud;
        private TutorialStep? _hudStep;
        public RectTransform QuickAccessPanel => _inventoryPanel as RectTransform;
        public void RefreshTutorialVisibility()
        {
            var progress = GameRoot.PlayerProgress?.TutorialProgress;
            bool show = !TutorialService.Active;
            bool arena = LevelController.Current != null;
            if (_fullHudVisible == show && _arenaHud == arena && _hudStep == progress?.Step) return;
            _fullHudVisible = show;
            _arenaHud = arena;
            _hudStep = progress?.Step;
            foreach (string name in new[] { "PlayerPanel", "IconNavigation", "GiftNavigation", "CurrencyPouch", "Levels", "AchievementPopup" })
            {
                var group = transform.Find(name);
                bool visible = show;
                if (name == "IconNavigation") visible |= TutorialService.AllowHud(GameUiDestination.Inventory) || TutorialService.AllowHud(GameUiDestination.Crafting);
                if (name == "GiftNavigation") visible = !arena && (show || TutorialService.AllowHud(GameUiDestination.Daily) || TutorialService.AllowHud(GameUiDestination.Playtime) || TutorialService.AllowHud(GameUiDestination.Wheel));
                if (group != null) group.gameObject.SetActive(visible);
            }
            if (_inventoryPanel != null) _inventoryPanel.gameObject.SetActive(true);
        }

        private void Awake()
        {
            var playerPanel = transform.Find("PlayerPanel");
            if (playerPanel != null && playerPanel.GetComponent<PlayerPanelUI>() == null) playerPanel.gameObject.AddComponent<PlayerPanelUI>();
            InitializeInventoryPanel();
            InitializePortrait();
            HudActionNotice.Install(transform);
        }

        private void InitializePortrait()
        {
            var portrait = FindChildByName(transform, "PlayerIcon");
            if (portrait == null || portrait.Find("PixelFace") != null) return;
            var face = new GameObject("PixelFace", typeof(RectTransform), typeof(PixelPortraitGraphic));
            face.transform.SetParent(portrait, false);
            var rect = (RectTransform)face.transform;
            rect.anchorMin = new Vector2(0.1f, 0.1f);
            rect.anchorMax = new Vector2(0.9f, 0.9f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            face.GetComponent<PixelPortraitGraphic>().raycastTarget = false;
        }

        private void OnEnable()
        {
            _fullHudVisible = null;
            RefreshTutorialVisibility();
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
            RefreshTutorialVisibility();
            RefreshPotionButton();
            if (Input.GetKeyDown(KeyCode.R)) PotionEffects.TryDrinkSelected();
            for (int i = 0; i < SlotCount; i++)
            {
                if (Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha1 + i)) ||
                    Input.GetKeyDown((KeyCode)((int)KeyCode.Keypad1 + i)))
                {
                    SelectInventorySlot(i);
                }
            }
        }

        private void LateUpdate()
        {
            // Recover HUD instances stranded by the old inventory reparenting code after a script reload.
            // This runs outside any parent's activation/deactivation callback.
            if (_inventoryPanel != null && _inventoryPanel.parent != transform)
            {
                _inventoryPanel.SetParent(transform, false);
                _inventoryPanel.gameObject.SetActive(true);
            }
        }

        private Button _potionButton;
        private TMPro.TMP_Text _potionLabel;
        private void RefreshPotionButton()
        {
            var potion = PotionEffects.SelectedPotion;
            if (_potionButton == null && potion != null)
            {
                var go = new GameObject("DrinkPotion", typeof(RectTransform), typeof(Image), typeof(Button));
                go.transform.SetParent(transform, false);
                var rect = go.GetComponent<RectTransform>();
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
                rect.pivot = new Vector2(0.5f, 0f);
                rect.anchoredPosition = new Vector2(0, 125f);
                rect.sizeDelta = new Vector2(320f, 44f);
                go.GetComponent<Image>().color = new Color32(94, 116, 62, 245);
                _potionButton = go.GetComponent<Button>();
                _potionButton.onClick.AddListener(() => PotionEffects.TryDrinkSelected());
                var label = new GameObject("Label", typeof(RectTransform), typeof(TMPro.TextMeshProUGUI));
                label.transform.SetParent(go.transform, false);
                _potionLabel = label.GetComponent<TMPro.TMP_Text>();
                var existingFont = GetComponentInChildren<TMPro.TMP_Text>();
                if (existingFont != null) _potionLabel.font = existingFont.font;
                _potionLabel.fontSize = 18;
                _potionLabel.alignment = TMPro.TextAlignmentOptions.Center;
                _potionLabel.raycastTarget = false;
                _potionLabel.rectTransform.anchorMin = Vector2.zero;
                _potionLabel.rectTransform.anchorMax = Vector2.one;
                _potionLabel.rectTransform.offsetMin = _potionLabel.rectTransform.offsetMax = Vector2.zero;
            }
            if (_potionButton == null) return;
            _potionButton.gameObject.SetActive(potion != null);
            if (potion != null)
            {
                _potionLabel.text = potion.DisplayName + " · Выпить [R]";
                _potionButton.interactable = _inventoryManager != null && _inventoryManager.GetItemAmount(potion.Name) > 0;
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
            var itemConfig = ResolveItemConfig(progress.GetQuickSlotItemId(progress.SelectedQuickSlotIndex));
            var equipment = Player.Instance != null ? Player.Instance.GetComponentFromList<PlayerEquipment>() : null;
            if (equipment == null)
                return;

            var handItem = ResolveHandItemType(item);
            ApplyEquipmentConfig(equipment, itemConfig);

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

        private ItemConfig ResolveItemConfig(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId))
                return null;

            var database = GameRoot.GameConfig != null ? GameRoot.GameConfig.ItemDatabase : null;
            return database != null ? database.GetItemConfig(itemId) : null;
        }

        private static void ApplyEquipmentConfig(PlayerEquipment equipment, ItemConfig itemConfig)
        {
            if (equipment == null || itemConfig is not WeaponItemConfig weaponConfig)
                return;

            switch (weaponConfig.Kind)
            {
                case WeaponItemKind.Sword:
                    equipment.EquipSword(weaponConfig.AttackConfig, false);
                    break;
                case WeaponItemKind.Bow:
                    equipment.EquipBow(weaponConfig.AttackConfig, false);
                    break;
            }
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
            if (resourceIcon == null)
            {
                if (_resourceIconPrefab == null)
                    _resourceIconPrefab = Resources.Load<ResourceIcon>(ResourceIconPrefabPath);

                if (_resourceIconPrefab == null)
                {
                    Debug.LogWarning($"[PlayingWindow] ResourceIcon prefab not found at Resources/{ResourceIconPrefabPath}.");
                    return null;
                }

                resourceIcon = Instantiate(_resourceIconPrefab, slot);
                resourceIcon.name = "ResourceIcon";
            }

            // Prefab instances need the same slot-relative sizing as newly created icons.
            if (resourceIcon.transform is RectTransform rectTransform)
            {
                rectTransform.anchorMin = Vector2.zero;
                rectTransform.anchorMax = Vector2.one;
                rectTransform.offsetMin = new Vector2(10f, 10f);
                rectTransform.offsetMax = new Vector2(-10f, -10f);
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
