using System;
using System.Collections.Generic;
using System.Linq;
using Devotion.SDK.Base;
using Devotion.SDK.Controllers;
using Devotion.SDK.Services.Localization;
using MineArena.Buildings;
using MineArena.Items;
using MineArena.Managers;
using UnityEngine;
using UnityEngine.UI;

namespace MineArena.Windows.Crafting
{
    public class CraftingWindow : BaseWindow
    {
        [SerializeField] private ScrollRect _scrollRect;
        [SerializeField] private RectTransform _sectionsContainer;
        [SerializeField] private Transform _tabsContainer;
        [SerializeField] private CraftingSection _sectionPrefab;
        [SerializeField] private CraftingTabButton _tabPrefab;
        [SerializeField] private CraftingItemView _itemPrefab;
        [SerializeField] private CraftingDetailsPanel _detailsPanel;

        private readonly List<SectionEntry> _sections = new();
        private readonly List<TabEntry> _tabs = new();
        private SelectionState _selection;

        private InventoryManager _inventoryManager;
        private BuildingManager _buildingManager;

        private void OnEnable()
        {
            _inventoryManager = GameRoot.GetManager<InventoryManager>();
            _buildingManager = GameRoot.GetManager<BuildingManager>();

            if (_inventoryManager != null)
            {
                _inventoryManager.InventoryUpdated += HandleInventoryUpdated;
            }

            if (_detailsPanel != null)
            {
                _detailsPanel.Hide();
            }
        }

        private void OnDisable()
        {
            if (_inventoryManager != null)
            {
                _inventoryManager.InventoryUpdated -= HandleInventoryUpdated;
            }
        }

        public void Initialize(BuildingConfig initialBuilding)
        {
            ClearView();

            if (_scrollRect != null && _sectionsContainer != null)
            {
                _scrollRect.content = _sectionsContainer;
            }

            var database = GameRoot.GameConfig?.BuildingsDatabase;
            if (database == null)
            {
                Debug.LogError("[CraftingWindow] Buildings database is not configured.");
                return;
            }

            var allBuildings = database.AllBuildings;
            if (allBuildings == null || allBuildings.Count == 0)
                return;

            foreach (var building in allBuildings)
            {
                if (building == null)
                    continue;

                var section = CreateSection(building);
                if (section != null)
                {
                    _sections.Add(section);
                }
            }

            var tabToSelect = _tabs.FirstOrDefault(t => t.Building == initialBuilding) ?? _tabs.FirstOrDefault();
            if (tabToSelect != null)
            {
                SelectTab(tabToSelect);
                ScrollToSection(tabToSelect.Section.SectionRect);

                var firstUnlocked = tabToSelect.SectionEntry.Items.FirstOrDefault(i => !i.IsLocked);
                if (firstUnlocked != null)
                {
                    SelectItem(firstUnlocked);
                }
                else
                {
                    _detailsPanel?.Hide();
                }
            }
        }

        public override void CloseWindow()
        {
            base.CloseWindow();
            GameRoot.UIManager.CloseWindow<CraftingWindow>();
        }

        private SectionEntry CreateSection(BuildingConfig building)
        {
            if (_sectionPrefab == null || _sectionsContainer == null)
                return null;

            var sectionInstance = Instantiate(_sectionPrefab, _sectionsContainer);
            sectionInstance.SetTitle(GetDisplayName(building.BuildingName));

            var currentLevel = _buildingManager != null ? _buildingManager.GetBuildingLevel(building) : 0;

            var entry = new SectionEntry
            {
                Building = building,
                Section = sectionInstance,
                BuildingLevel = currentLevel
            };

            BuildItems(entry);
            CreateTab(entry);

            return entry;
        }

        private void BuildItems(SectionEntry section)
        {
            if (_itemPrefab == null)
                return;

            var building = section.Building;
            var levels = building.Levels;
            if (levels == null || levels.Count == 0)
                return;

            var itemLevels = new Dictionary<ItemConfig, int>();

            foreach (var levelConfig in levels)
            {
                if (levelConfig == null)
                    continue;

                foreach (var item in levelConfig.Unlocks)
                {
                    if (item == null)
                        continue;

                    if (!itemLevels.TryGetValue(item, out var storedLevel) || levelConfig.Level < storedLevel)
                    {
                        itemLevels[item] = levelConfig.Level;
                    }
                }
            }

            foreach (var pair in itemLevels
                         .OrderBy(p => p.Value)
                         .ThenBy(p => p.Key.Name, StringComparer.OrdinalIgnoreCase))
            {
                var view = Instantiate(_itemPrefab, section.Section.ItemsRoot);
                var displayName = GetDisplayName(pair.Key.Name);
                view.Setup(pair.Key.Icon, displayName, () => HandleItemClicked(section, pair.Key));

                var itemEntry = new ItemEntry
                {
                    Building = building,
                    Item = pair.Key,
                    RequiredLevel = pair.Value,
                    View = view
                };

                itemEntry.IsLocked = section.BuildingLevel < itemEntry.RequiredLevel;
                view.SetLocked(itemEntry.IsLocked);

                section.Items.Add(itemEntry);
            }
        }

        private void CreateTab(SectionEntry section)
        {
            if (_tabPrefab == null || _tabsContainer == null)
                return;

            var tabInstance = Instantiate(_tabPrefab, _tabsContainer);
            tabInstance.Setup(GetDisplayName(section.Building.BuildingName), () =>
            {
                var tab = _tabs.FirstOrDefault(t => t.SectionEntry == section);
                if (tab != null)
                {
                    SelectTab(tab);
                    ScrollToSection(tab.Section.SectionRect);
                }
            });

            var tabEntry = new TabEntry
            {
                Building = section.Building,
                Tab = tabInstance,
                Section = section.Section,
                SectionEntry = section
            };

            _tabs.Add(tabEntry);
        }

        private void SelectTab(TabEntry tab)
        {
            foreach (var tabEntry in _tabs)
            {
                tabEntry.Tab.SetSelected(tabEntry == tab);
            }

            RefreshSectionState(tab.SectionEntry);
        }

        private void HandleItemClicked(SectionEntry section, ItemConfig item)
        {
            var entry = section.Items.FirstOrDefault(i => i.Item == item);
            if (entry == null || entry.IsLocked)
                return;

            SelectItem(entry);
        }

        private void SelectItem(ItemEntry entry)
        {
            _selection = new SelectionState
            {
                Section = _sections.FirstOrDefault(s => s.Items.Contains(entry)),
                Item = entry
            };

            RefreshSectionState(_selection.Section);

            foreach (var section in _sections)
            {
                foreach (var item in section.Items)
                {
                    item.View.SetSelected(item == entry);
                }
            }

            UpdateDetails();
        }

        private void UpdateDetails()
        {
            if (_selection?.Item == null || _detailsPanel == null)
            {
                _detailsPanel?.Hide();
                return;
            }

            var item = _selection.Item.Item;
            var section = _selection.Section ?? _sections.FirstOrDefault(s => s.Items.Contains(_selection.Item));

            RefreshSectionState(section);

            var buildingLevel = section?.BuildingLevel ?? 0;
            var unlocked = buildingLevel >= _selection.Item.RequiredLevel;

            var costs = item.CraftCosts;
            var canAfford = unlocked && (_inventoryManager == null || _inventoryManager.CanAfford(costs));

            _detailsPanel.Show(
                item.Icon,
                GetDisplayName(item.Name),
                ResolveDescription(item),
                costs,
                canAfford,
                () => CraftSelectedItem()
            );
        }

        private void CraftSelectedItem()
        {
            if (_selection?.Item == null || _inventoryManager == null)
                return;

            var itemConfig = _selection.Item.Item;
            var section = _selection.Section ?? _sections.FirstOrDefault(s => s.Items.Contains(_selection.Item));

            if (section == null || section.BuildingLevel < _selection.Item.RequiredLevel)
                return;

            var costs = itemConfig.CraftCosts;
            if (costs != null && costs.Count > 0)
            {
                if (!_inventoryManager.TryConsumeResources(costs))
                {
                    UpdateDetails();
                    return;
                }
            }

            var craftedItem = CreateRuntimeItem(itemConfig);
            var amount = craftedItem is StackableItem stackable ? Mathf.Max(1, stackable.CurrentStack) : 1;
            _inventoryManager.AddItem(craftedItem, amount);

            UpdateDetails();
        }

        private void HandleInventoryUpdated()
        {
            RefreshAllSections();
            UpdateDetails();
        }

        private void ScrollToSection(RectTransform section)
        {
            if (_scrollRect == null || section == null)
                return;

            var content = _scrollRect.content;
            if (content == null)
                return;

            var contentHeight = content.rect.height;
            var viewportHeight = _scrollRect.viewport != null ? _scrollRect.viewport.rect.height : ((RectTransform)_scrollRect.transform).rect.height;

            if (contentHeight <= viewportHeight)
            {
                _scrollRect.verticalNormalizedPosition = 1f;
                return;
            }

            var target = Mathf.Abs(section.anchoredPosition.y);
            var normalized = 1f - Mathf.Clamp01(target / Mathf.Max(1f, contentHeight - viewportHeight));
            _scrollRect.verticalNormalizedPosition = normalized;
        }

        private void ClearView()
        {
            foreach (var tab in _tabs)
            {
                if (tab.Tab != null)
                {
                    Destroy(tab.Tab.gameObject);
                }
            }

            foreach (var section in _sections)
            {
                if (section.Section != null)
                {
                    Destroy(section.Section.gameObject);
                }
            }

            _tabs.Clear();
            _sections.Clear();
            _selection = null;

            _detailsPanel?.Hide();
        }

        private void RefreshAllSections()
        {
            foreach (var section in _sections)
            {
                RefreshSectionState(section);
            }
        }

        private void RefreshSectionState(SectionEntry section)
        {
            if (section == null)
                return;

            if (_buildingManager != null)
            {
                section.BuildingLevel = _buildingManager.GetBuildingLevel(section.Building);
            }

            foreach (var item in section.Items)
            {
                var locked = section.BuildingLevel < item.RequiredLevel;
                if (item.IsLocked != locked)
                {
                    item.IsLocked = locked;
                    item.View.SetLocked(locked);
                }
            }
        }

        private static Item CreateRuntimeItem(ItemConfig config)
        {
            if (config == null)
                return null;

            if (config.Stackable && config is StackableItemConfig stackableConfig)
            {
                return new StackableItem(stackableConfig, 1);
            }

            if (config is EquipmentItemConfig equipmentConfig)
            {
                return new EquipmentItem(equipmentConfig);
            }

            return new Item(config.Name, config.Prefab, config.Icon);
        }

        private static string ResolveDescription(ItemConfig item)
        {
            if (item == null)
                return string.Empty;

            if (!string.IsNullOrWhiteSpace(item.DescriptionLocalizationKey))
            {
                return LocalizationService.GetLocalizedText(item.DescriptionLocalizationKey);
            }

            if (!string.IsNullOrWhiteSpace(item.Description))
                return item.Description;

            return item.Name;
        }

        private static string GetDisplayName(string keyOrName)
        {
            if (string.IsNullOrWhiteSpace(keyOrName))
                return string.Empty;

            return keyOrName;
        }

        private class SectionEntry
        {
            public BuildingConfig Building;
            public CraftingSection Section;
            public int BuildingLevel;
            public List<ItemEntry> Items { get; } = new();
        }

        private class ItemEntry
        {
            public BuildingConfig Building;
            public ItemConfig Item;
            public int RequiredLevel;
            public CraftingItemView View;
            public bool IsLocked;
        }

        private class TabEntry
        {
            public BuildingConfig Building;
            public CraftingTabButton Tab;
            public CraftingSection Section;
            public SectionEntry SectionEntry;
        }

        private class SelectionState
        {
            public SectionEntry Section;
            public ItemEntry Item;
        }
    }
}
