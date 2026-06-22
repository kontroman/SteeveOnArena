using System.Collections.Generic;
using Devotion.SDK.Base;
using Devotion.SDK.Controllers;
using MineArena.Basics;
using MineArena.Controllers;
using MineArena.Items;
using MineArena.Managers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MineArena.Windows
{
    public class GodModeWindow : BaseWindow
    {
        private const int DefaultItemAmount = 1;
        private const int DefaultExperienceAmount = 250;
        private const int DefaultLevelAmount = 1;

        [Header("God Mode Toggles")]
        [SerializeField] private Toggle _invulnerabilityToggle;
        [SerializeField] private Toggle _oneHitKillToggle;

        [Header("Inventory Controls")]
        [SerializeField] private TMP_Dropdown _itemDropdown;
        [SerializeField] private Button _addItemButton;
        [SerializeField] private Button _removeItemButton;
        [SerializeField] private TMP_InputField _itemAmountInput;

        [Header("Progress Controls")]
        [SerializeField] private TMP_InputField _experienceAmountInput;
        [SerializeField] private TMP_InputField _levelAmountInput;
        [SerializeField] private Button _addExperienceButton;
        [SerializeField] private Button _addLevelButton;
        [SerializeField] private Button _clearInventoryButton;
        [SerializeField] private Button _addAllItemsButton;
        [SerializeField] private Button _unlockAllLevelsButton;
        [SerializeField] private Button _addFortuneSpinButton;

        private readonly List<ItemConfig> _availableItems = new();
        private bool _databaseInitialized;
        private bool _extraControlsBuilt;
        private bool _uiSubscribed;
        private bool _configSubscribed;
        private bool _suppressToggleCallbacks;
        private bool _invulnerabilityEnabled;
        private bool _oneHitKillEnabled;
        private Text _playerProgressLabel;
        private static Font _uiFont;

        private void OnEnable()
        {
            BuildExtraControls();
            SubscribeUi();
            SubscribeConfig();

            PopulateDropdown();
            SyncTogglesWithConfig();
            UpdateToggleInteractivity();
            RefreshPlayerProgressLabel();
        }

        private void OnDisable()
        {
            UnsubscribeUi();
            UnsubscribeConfig();
        }

        private void SubscribeUi()
        {
            if (_uiSubscribed)
                return;

            if (_invulnerabilityToggle != null)
                _invulnerabilityToggle.onValueChanged.AddListener(OnToggleChanged);

            if (_oneHitKillToggle != null)
                _oneHitKillToggle.onValueChanged.AddListener(OnToggleChanged);

            if (_addItemButton != null)
                _addItemButton.onClick.AddListener(OnAddItemClicked);

            if (_removeItemButton != null)
                _removeItemButton.onClick.AddListener(OnRemoveItemClicked);

            if (_addExperienceButton != null)
                _addExperienceButton.onClick.AddListener(OnAddExperienceClicked);

            if (_addLevelButton != null)
                _addLevelButton.onClick.AddListener(OnAddLevelClicked);

            if (_clearInventoryButton != null)
                _clearInventoryButton.onClick.AddListener(OnClearInventoryClicked);

            if (_addAllItemsButton != null)
                _addAllItemsButton.onClick.AddListener(OnAddAllItemsClicked);

            if (_unlockAllLevelsButton != null)
                _unlockAllLevelsButton.onClick.AddListener(OnUnlockAllLevelsClicked);

            if (_addFortuneSpinButton != null)
                _addFortuneSpinButton.onClick.AddListener(OnAddFortuneSpinClicked);

            _uiSubscribed = true;
        }

        private void UnsubscribeUi()
        {
            if (!_uiSubscribed)
                return;

            if (_invulnerabilityToggle != null)
                _invulnerabilityToggle.onValueChanged.RemoveListener(OnToggleChanged);

            if (_oneHitKillToggle != null)
                _oneHitKillToggle.onValueChanged.RemoveListener(OnToggleChanged);

            if (_addItemButton != null)
                _addItemButton.onClick.RemoveListener(OnAddItemClicked);

            if (_removeItemButton != null)
                _removeItemButton.onClick.RemoveListener(OnRemoveItemClicked);

            if (_addExperienceButton != null)
                _addExperienceButton.onClick.RemoveListener(OnAddExperienceClicked);

            if (_addLevelButton != null)
                _addLevelButton.onClick.RemoveListener(OnAddLevelClicked);

            if (_clearInventoryButton != null)
                _clearInventoryButton.onClick.RemoveListener(OnClearInventoryClicked);

            if (_addAllItemsButton != null)
                _addAllItemsButton.onClick.RemoveListener(OnAddAllItemsClicked);

            if (_unlockAllLevelsButton != null)
                _unlockAllLevelsButton.onClick.RemoveListener(OnUnlockAllLevelsClicked);

            if (_addFortuneSpinButton != null)
                _addFortuneSpinButton.onClick.RemoveListener(OnAddFortuneSpinClicked);

            _uiSubscribed = false;
        }

        private void SubscribeConfig()
        {
            if (_configSubscribed)
                return;

            var config = GameRoot.GameConfig;
            if (config == null)
                return;

            config.GodModeChanged += OnGodModeChanged;
            config.InvulnerabilityChanged += OnInvulnerabilityChanged;
            config.OneHitKillChanged += OnOneHitKillChanged;

            _configSubscribed = true;
        }

        private void UnsubscribeConfig()
        {
            if (!_configSubscribed)
                return;

            var config = GameRoot.GameConfig;
            if (config == null)
            {
                _configSubscribed = false;
                return;
            }

            config.GodModeChanged -= OnGodModeChanged;
            config.InvulnerabilityChanged -= OnInvulnerabilityChanged;
            config.OneHitKillChanged -= OnOneHitKillChanged;

            _configSubscribed = false;
        }

        private void OnGodModeChanged(bool isEnabled)
        {
            UpdateToggleInteractivity(isEnabled);

            if (!isEnabled)
            {
                SetToggleStates(false, false);
            }
        }

        private void OnInvulnerabilityChanged(bool isEnabled)
        {
            UpdateInvulnerabilityToggle(isEnabled);
        }

        private void OnOneHitKillChanged(bool isEnabled)
        {
            UpdateOneHitKillToggle(isEnabled);
        }

        private void SyncTogglesWithConfig()
        {
            var config = GameRoot.GameConfig;
            if (config == null)
                return;

            SetToggleStates(config.GodModeInvulnerability, config.GodModeOneHitKill);
        }

        private void SetToggleStates(bool invulnerabilityEnabled, bool oneHitKillEnabled)
        {
            UpdateInvulnerabilityToggle(invulnerabilityEnabled);
            UpdateOneHitKillToggle(oneHitKillEnabled);
        }

        private void UpdateInvulnerabilityToggle(bool isOn)
        {
            _invulnerabilityEnabled = isOn;

            if (_invulnerabilityToggle == null)
                return;

            _suppressToggleCallbacks = true;
            _invulnerabilityToggle.isOn = isOn;
            _suppressToggleCallbacks = false;
        }

        private void UpdateOneHitKillToggle(bool isOn)
        {
            _oneHitKillEnabled = isOn;

            if (_oneHitKillToggle == null)
                return;

            _suppressToggleCallbacks = true;
            _oneHitKillToggle.isOn = isOn;
            _suppressToggleCallbacks = false;
        }

        private void UpdateToggleInteractivity()
        {
            var config = GameRoot.GameConfig;
            bool isEnabled = config != null && config.GodMode;
            UpdateToggleInteractivity(isEnabled);
        }

        private void UpdateToggleInteractivity(bool isEnabled)
        {
            if (_invulnerabilityToggle != null)
                _invulnerabilityToggle.interactable = isEnabled;

            if (_oneHitKillToggle != null)
                _oneHitKillToggle.interactable = isEnabled;
        }

        private void OnToggleChanged(bool _)
        {
            if (_suppressToggleCallbacks)
                return;

            var config = GameRoot.GameConfig;
            if (config == null)
                return;

            _invulnerabilityEnabled = _invulnerabilityToggle != null && _invulnerabilityToggle.isOn;
            _oneHitKillEnabled = _oneHitKillToggle != null && _oneHitKillToggle.isOn;

            config.GodModeInvulnerability = _invulnerabilityEnabled;
            config.GodModeOneHitKill = _oneHitKillEnabled;
        }

        private void PopulateDropdown()
        {
            if (_itemDropdown == null)
                return;

            var config = GameRoot.GameConfig;
            if (config == null || config.ItemDatabase == null)
                return;

            if (!_databaseInitialized)
            {
                config.ItemDatabase.Initialize();
                _databaseInitialized = true;
            }

            var items = config.ItemDatabase.AllItems;
            if (items == null || items.Count == 0)
            {
                _itemDropdown.ClearOptions();
                return;
            }

            _availableItems.Clear();
            _itemDropdown.ClearOptions();

            foreach (var item in items)
            {
                if (item == null)
                    continue;

                _availableItems.Add(item);
                _itemDropdown.options.Add(new TMP_Dropdown.OptionData(item.Name));
            }

            if (_itemDropdown.options.Count == 0)
                return;

            _itemDropdown.value = 0;
            _itemDropdown.RefreshShownValue();
        }

        private void OnAddItemClicked()
        {
            var inventory = GameRoot.GetManager<InventoryManager>();
            if (inventory == null)
                return;

            var config = GetSelectedItemConfig();
            if (config == null)
                return;

            AddItemByConfig(inventory, config, ParsePositiveInt(_itemAmountInput, DefaultItemAmount));
        }

        private void OnRemoveItemClicked()
        {
            var inventory = GameRoot.GetManager<InventoryManager>();
            if (inventory == null)
                return;

            var config = GetSelectedItemConfig();
            if (config == null)
                return;

            var existingItem = FindExistingItem(config, inventory);
            if (existingItem == null)
                return;

            int amount = ParsePositiveInt(_itemAmountInput, DefaultItemAmount);
            if (existingItem is StackableItem)
            {
                inventory.RemoveItem(existingItem, amount);
                return;
            }

            for (int i = 0; i < amount; i++)
            {
                existingItem = FindExistingItem(config, inventory);
                if (existingItem == null)
                    break;

                inventory.RemoveItem(existingItem, 1);
            }
        }

        private void OnAddAllItemsClicked()
        {
            var inventory = GameRoot.GetManager<InventoryManager>();
            if (inventory == null)
                return;

            int amount = ParsePositiveInt(_itemAmountInput, DefaultItemAmount);
            foreach (var config in _availableItems)
            {
                AddItemByConfig(inventory, config, amount);
            }
        }

        private void OnClearInventoryClicked()
        {
            var inventory = GameRoot.GetManager<InventoryManager>();
            if (inventory != null)
            {
                inventory.ClearInventory();
                return;
            }

            GameRoot.PlayerProgress?.InventoryProgress?.ClearInventory();
        }

        private void OnAddExperienceClicked()
        {
            AddExperience(ParsePositiveInt(_experienceAmountInput, DefaultExperienceAmount));
            RefreshPlayerProgressLabel();
        }

        private void OnAddLevelClicked()
        {
            int levelAmount = ParsePositiveInt(_levelAmountInput, DefaultLevelAmount);
            var playerExperience = Player.Instance?.Experience;
            if (playerExperience != null)
            {
                playerExperience.AddExperience(playerExperience.ExperiencePerLevel * levelAmount);
                RefreshPlayerProgressLabel();
                return;
            }

            var playerData = GameRoot.PlayerProgress?.PlayerDataProgress;
            if (playerData != null)
            {
                playerData.CacheExperience(playerData.CurrentLevel + levelAmount, playerData.CurrentExperience);
            }

            RefreshPlayerProgressLabel();
        }

        private void OnUnlockAllLevelsClicked()
        {
            var levels = GameRoot.GameConfig?.Levels;
            if (levels == null || levels.Count == 0)
                return;

            GameRoot.PlayerProgress?.LevelsProgress?.UnlockLevel(levels.Count - 1);
        }

        private void OnAddFortuneSpinClicked()
        {
            GameRoot.PlayerProgress?.LuckyWheelProgress?.AddFortuneSpins(1);
        }

        private static void AddExperience(int amount)
        {
            var playerExperience = Player.Instance?.Experience;
            if (playerExperience != null)
            {
                playerExperience.AddExperience(amount);
                return;
            }

            var playerData = GameRoot.PlayerProgress?.PlayerDataProgress;
            if (playerData == null)
                return;

            int experiencePerLevel = Constants.GameSetting.ExperiencePerLevel;
            int nextExperience = playerData.CurrentExperience + amount;
            int levelsToAdd = experiencePerLevel > 0 ? nextExperience / experiencePerLevel : 0;
            int cachedExperience = experiencePerLevel > 0 ? nextExperience % experiencePerLevel : nextExperience;

            playerData.CacheExperience(playerData.CurrentLevel + levelsToAdd, cachedExperience);
        }

        private ItemConfig GetSelectedItemConfig()
        {
            if (_itemDropdown == null || _availableItems.Count == 0)
                return null;

            var index = _itemDropdown.value;

            if (index < 0 || index >= _availableItems.Count)
                return null;

            return _availableItems[index];
        }

        private static Item FindExistingItem(ItemConfig config, InventoryManager inventory)
        {
            foreach (var item in inventory.Items)
            {
                if (item != null && item.Name == config.Name)
                    return item;
            }

            return null;
        }

        private static void AddItemByConfig(InventoryManager inventory, ItemConfig config, int amount)
        {
            if (inventory == null || config == null)
                return;

            amount = Mathf.Max(1, amount);

            if (config.Stackable)
            {
                var stackable = CreateItemFromConfig(config);
                if (stackable != null)
                    inventory.AddItem(stackable, amount);

                return;
            }

            for (int i = 0; i < amount; i++)
            {
                var item = CreateItemFromConfig(config);
                if (item != null)
                    inventory.AddItem(item, 1);
            }
        }

        private static Item CreateItemFromConfig(ItemConfig config)
        {
            if (config == null)
                return null;

            if (config.Stackable && config is StackableItemConfig stackableConfig)
                return new StackableItem(stackableConfig, 1);

            if (config is PickaxeConfig pickaxeConfig)
                return new Pickaxe(pickaxeConfig);

            if (config is ArmorConfig armorConfig)
                return new Armor(armorConfig);

            if (config is EquipmentItemConfig equipmentConfig)
                return new EquipmentItem(equipmentConfig);

            return new Item(config.Name, config.Prefab, config.Icon);
        }

        private void BuildExtraControls()
        {
            if (_extraControlsBuilt)
                return;

            _extraControlsBuilt = true;

            var root = transform as RectTransform;
            if (root == null)
                return;

            var panel = CreateRect("ExtraCheatsPanel", root);
            panel.anchorMin = new Vector2(0.5f, 1f);
            panel.anchorMax = new Vector2(0.5f, 1f);
            panel.pivot = new Vector2(0.5f, 1f);
            panel.anchoredPosition = new Vector2(0f, -312f);
            panel.sizeDelta = new Vector2(420f, 238f);

            var panelImage = panel.gameObject.AddComponent<Image>();
            panelImage.color = new Color(0f, 0f, 0f, 0.55f);
            panelImage.raycastTarget = true;

            CreateLabel(panel, "ExtraTitle", "читы прогресса", 16, FontStyle.Normal, 12f, 8f, 396f, 24f);
            CreateLabel(panel, "ItemAmountLabel", "Кол-во предметов", 13, FontStyle.Normal, 12f, 40f, 130f, 28f);

            _itemAmountInput ??= CreateInput(panel, "ItemAmountInput", DefaultItemAmount.ToString(), 148f, 38f, 64f, 32f);
            _addAllItemsButton ??= CreateButton(panel, "AddAllItemsButton", "все предметы", 224f, 38f, 184f, 32f);

            CreateLabel(panel, "ExperienceLabel", "Опыт", 13, FontStyle.Normal, 12f, 80f, 70f, 28f);
            _experienceAmountInput ??= CreateInput(panel, "ExperienceAmountInput", DefaultExperienceAmount.ToString(), 84f, 78f, 90f, 32f);
            _addExperienceButton ??= CreateButton(panel, "AddExperienceButton", "+XP", 184f, 78f, 84f, 32f);
            _addFortuneSpinButton ??= CreateButton(panel, "AddFortuneSpinButton", "+спин", 280f, 78f, 128f, 32f);

            CreateLabel(panel, "LevelLabel", "Уровни", 13, FontStyle.Normal, 12f, 120f, 70f, 28f);
            _levelAmountInput ??= CreateInput(panel, "LevelAmountInput", DefaultLevelAmount.ToString(), 84f, 118f, 90f, 32f);
            _addLevelButton ??= CreateButton(panel, "AddLevelButton", "+Level", 184f, 118f, 84f, 32f);
            _unlockAllLevelsButton ??= CreateButton(panel, "UnlockAllLevelsButton", "открыть уровни", 280f, 118f, 128f, 32f);

            _clearInventoryButton ??= CreateButton(panel, "ClearInventoryButton", "очистить инвентарь", 12f, 162f, 396f, 34f, new Color32(125, 35, 35, 255));
            _playerProgressLabel = CreateLabel(panel, "PlayerProgressLabel", string.Empty, 12, FontStyle.Normal, 12f, 204f, 396f, 24f);
        }

        private void RefreshPlayerProgressLabel()
        {
            if (_playerProgressLabel == null)
                return;

            var playerExperience = Player.Instance?.Experience;
            if (playerExperience != null)
            {
                _playerProgressLabel.text = $"Level: {playerExperience.CurrentLevel} | XP: {playerExperience.CurrentExperience}/{playerExperience.ExperiencePerLevel}";
                return;
            }

            var playerData = GameRoot.PlayerProgress?.PlayerDataProgress;
            if (playerData != null)
            {
                _playerProgressLabel.text = $"Level: {playerData.CurrentLevel} | XP: {playerData.CurrentExperience}/{Constants.GameSetting.ExperiencePerLevel}";
                return;
            }

            _playerProgressLabel.text = "Player progress is not ready";
        }

        private static TMP_InputField CreateInput(RectTransform parent, string name, string value, float x, float y, float width, float height)
        {
            var rect = CreateRect(name, parent);
            SetTopLeft(rect, x, y, width, height);

            var image = rect.gameObject.AddComponent<Image>();
            image.color = new Color32(245, 245, 245, 255);

            var input = rect.gameObject.AddComponent<TMP_InputField>();
            input.contentType = TMP_InputField.ContentType.IntegerNumber;
            input.targetGraphic = image;

            var text = CreateTmpLabel(rect, "Text", value, 14f, FontStyles.Normal, 8f, 4f, width - 16f, height - 8f);
            text.color = Color.black;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            input.textComponent = text;
            input.text = value;

            return input;
        }

        private static Button CreateButton(RectTransform parent, string name, string label, float x, float y, float width, float height)
        {
            return CreateButton(parent, name, label, x, y, width, height, new Color32(58, 82, 120, 255));
        }

        private static Button CreateButton(RectTransform parent, string name, string label, float x, float y, float width, float height, Color color)
        {
            var rect = CreateRect(name, parent);
            SetTopLeft(rect, x, y, width, height);

            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = true;

            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;

            var text = CreateLabel(rect, "Label", label, 13, FontStyle.Normal, 6f, 2f, width - 12f, height - 4f);
            text.alignment = TextAnchor.MiddleCenter;

            return button;
        }

        private static Text CreateLabel(RectTransform parent, string name, string text, int fontSize, FontStyle style, float x, float y, float width, float height)
        {
            var rect = CreateRect(name, parent);
            SetTopLeft(rect, x, y, width, height);

            var label = rect.gameObject.AddComponent<Text>();
            label.text = text;
            label.font = GetUiFont(fontSize);
            label.fontSize = fontSize;
            label.fontStyle = style;
            label.color = Color.white;
            label.alignment = TextAnchor.MiddleLeft;
            label.raycastTarget = false;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Truncate;

            return label;
        }

        private static Font GetUiFont(int fontSize)
        {
            if (_uiFont != null)
                return _uiFont;

            _uiFont = Font.CreateDynamicFontFromOSFont(new[] { "Arial", "Segoe UI", "Tahoma" }, fontSize);
            if (_uiFont != null)
                return _uiFont;

            _uiFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
            return _uiFont;
        }

        private static TextMeshProUGUI CreateTmpLabel(RectTransform parent, string name, string text, float fontSize, FontStyles style, float x, float y, float width, float height)
        {
            var rect = CreateRect(name, parent);
            SetTopLeft(rect, x, y, width, height);

            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = fontSize;
            label.fontStyle = style;
            label.color = Color.white;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.raycastTarget = false;
            label.enableWordWrapping = false;

            return label;
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            gameObject.layer = parent != null ? parent.gameObject.layer : 5;
            var rect = gameObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }

        private static void SetTopLeft(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
        }

        private static int ParsePositiveInt(TMP_InputField input, int fallback)
        {
            if (input != null && int.TryParse(input.text, out int value))
                return Mathf.Max(1, value);

            return Mathf.Max(1, fallback);
        }
    }
}
