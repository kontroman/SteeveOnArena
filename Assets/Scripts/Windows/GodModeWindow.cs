using System.Collections.Generic;
using Devotion.SDK.Base;
using Devotion.SDK.Controllers;
using MineArena.Items;
using MineArena.Managers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MineArena.Windows
{
    public class GodModeWindow : BaseWindow
    {
        [Header("God Mode Toggles")]
        [SerializeField] private Toggle _invulnerabilityToggle;
        [SerializeField] private Toggle _oneHitKillToggle;

        [Header("Inventory Controls")]
        [SerializeField] private TMP_Dropdown _itemDropdown;
        [SerializeField] private Button _addItemButton;
        [SerializeField] private Button _removeItemButton;

        private readonly List<ItemConfig> _availableItems = new();
        private bool _databaseInitialized;
        private bool _suppressToggleCallbacks;
        private bool _invulnerabilityEnabled;
        private bool _oneHitKillEnabled;

        private void Start()
        {
            SubscribeUi();
            SubscribeConfig();

            PopulateDropdown();
            SyncTogglesWithConfig();
            UpdateToggleInteractivity();
        }

        private void OnDisable()
        {
            UnsubscribeUi();
            UnsubscribeConfig();
        }

        private void SubscribeUi()
        {
            if (_invulnerabilityToggle != null)
                _invulnerabilityToggle.onValueChanged.AddListener(OnToggleChanged);

            if (_oneHitKillToggle != null)
                _oneHitKillToggle.onValueChanged.AddListener(OnToggleChanged);

            if (_addItemButton != null)
                _addItemButton.onClick.AddListener(OnAddItemClicked);

            if (_removeItemButton != null)
                _removeItemButton.onClick.AddListener(OnRemoveItemClicked);
        }

        private void UnsubscribeUi()
        {
            if (_invulnerabilityToggle != null)
                _invulnerabilityToggle.onValueChanged.RemoveListener(OnToggleChanged);

            if (_oneHitKillToggle != null)
                _oneHitKillToggle.onValueChanged.RemoveListener(OnToggleChanged);

            if (_addItemButton != null)
                _addItemButton.onClick.RemoveListener(OnAddItemClicked);

            if (_removeItemButton != null)
                _removeItemButton.onClick.RemoveListener(OnRemoveItemClicked);
        }

        private void SubscribeConfig()
        {
            var config = GameRoot.GameConfig;
            if (config == null)
                return;

            config.GodModeChanged += OnGodModeChanged;
            config.InvulnerabilityChanged += OnInvulnerabilityChanged;
            config.OneHitKillChanged += OnOneHitKillChanged;
        }

        private void UnsubscribeConfig()
        {
            var config = GameRoot.GameConfig;
            if (config == null)
                return;

            config.GodModeChanged -= OnGodModeChanged;
            config.InvulnerabilityChanged -= OnInvulnerabilityChanged;
            config.OneHitKillChanged -= OnOneHitKillChanged;
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

            var item = CreateItemFromConfig(config);
            if (item == null)
                return;

            inventory.AddItem(item, 1);
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

            inventory.RemoveItem(existingItem, 1);
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

        private static Item CreateItemFromConfig(ItemConfig config)
        {
            if (config == null)
                return null;

            if (config.Stackable && config is StackableItemConfig stackableConfig)
                return new StackableItem(stackableConfig, 1);

            if (config is EquipmentItemConfig equipmentConfig)
                return new EquipmentItem(equipmentConfig);

            return new Item(config.Name, config.Prefab, config.Icon);
        }
    }
}
