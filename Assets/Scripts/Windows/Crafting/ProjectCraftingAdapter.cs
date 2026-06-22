using System;
using System.Collections.Generic;
using System.Linq;
using Devotion.SDK.Controllers;
using Devotion.SDK.Services.Localization;
using MineArena.Buildings;
using MineArena.Items;
using MineArena.Managers;
using UnityEngine;

namespace MineArena.Windows.Crafting
{
    public sealed class ProjectCraftingAdapter
    {
        private const string FallbackCategoryId = "items";
        private const string FallbackCategoryName = "Items";

        private InventoryManager _inventoryManager;
        private BuildingManager _buildingManager;

        public event Action InventoryChanged;

        public void Connect()
        {
            Disconnect();

            if (GameRoot.Instance == null)
                return;

            _inventoryManager = GameRoot.GetManager<InventoryManager>();
            _buildingManager = GameRoot.GetManager<BuildingManager>();

            if (_inventoryManager != null)
            {
                _inventoryManager.InventoryUpdated += HandleInventoryUpdated;
            }
        }

        public void Disconnect()
        {
            if (_inventoryManager != null)
            {
                _inventoryManager.InventoryUpdated -= HandleInventoryUpdated;
            }
        }

        public IReadOnlyList<CraftingCategory> BuildCatalog()
        {
            var categories = new List<CraftingCategory>();
            var categorizedItems = new HashSet<ItemConfig>();
            var gameConfig = GameRoot.Instance != null ? GameRoot.GameConfig : null;
            var buildingsDatabase = gameConfig != null ? gameConfig.BuildingsDatabase : null;

            if (buildingsDatabase != null && buildingsDatabase.AllBuildings != null)
            {
                foreach (var building in buildingsDatabase.AllBuildings)
                {
                    var category = BuildBuildingCategory(building, categorizedItems);

                    if (category != null && category.Recipes.Count > 0)
                    {
                        categories.Add(category);
                    }
                }
            }

            AddFallbackItemCategory(categories, categorizedItems);

            return categories;
        }

        public bool IsUnlocked(CraftingRecipeEntry recipe)
        {
            if (recipe == null)
                return false;

            if (!recipe.HasBuildingRequirement)
                return true;

            return GetBuildingLevel(recipe.SourceBuilding) >= recipe.RequiredBuildingLevel;
        }

        public int GetBuildingLevel(BuildingConfig building)
        {
            if (building == null)
                return 0;

            return _buildingManager != null ? _buildingManager.GetBuildingLevel(building) : 0;
        }

        public bool CanCraft(CraftingRecipeEntry recipe)
        {
            if (recipe == null || recipe.Item == null || _inventoryManager == null)
                return false;

            return IsUnlocked(recipe) && _inventoryManager.CanAfford(recipe.Costs);
        }

        public int GetAvailable(ResourceRequired requirement)
        {
            if (_inventoryManager == null || requirement.Resource == null)
                return 0;

            var category = requirement.Resource.ResourceCategory;

            if (string.IsNullOrWhiteSpace(category))
                return 0;

            var total = 0;

            foreach (var stackable in _inventoryManager.Items.OfType<StackableItem>())
            {
                if (string.Equals(stackable.ResourceCategory, category, StringComparison.OrdinalIgnoreCase))
                {
                    total += stackable.CurrentStack;
                }
            }

            return total;
        }

        public CraftingResult TryCraft(CraftingRecipeEntry recipe)
        {
            if (recipe == null || recipe.Item == null)
                return CraftingResult.Fail(CraftingResultStatus.NoRecipe, "Recipe is unavailable.");

            if (_inventoryManager == null)
                return CraftingResult.Fail(CraftingResultStatus.InventoryUnavailable, "Inventory is unavailable.");

            if (!IsUnlocked(recipe))
                return CraftingResult.Fail(CraftingResultStatus.Locked, "Recipe is locked.");

            if (!_inventoryManager.TryConsumeResources(recipe.Costs))
                return CraftingResult.Fail(CraftingResultStatus.NotEnoughResources, "Not enough resources.");

            var craftedItem = CreateRuntimeItem(recipe.Item);

            if (craftedItem == null)
                return CraftingResult.Fail(CraftingResultStatus.Failed, "Item could not be created.");

            var amount = craftedItem is StackableItem stackable ? Mathf.Max(1, stackable.CurrentStack) : 1;
            _inventoryManager.AddItem(craftedItem, amount);

            return CraftingResult.Ok();
        }

        public string GetDescription(CraftingRecipeEntry recipe)
        {
            var item = recipe != null ? recipe.Item : null;

            if (item == null)
                return string.Empty;

            if (!string.IsNullOrWhiteSpace(item.DescriptionLocalizationKey))
                return LocalizationService.GetLocalizedText(item.DescriptionLocalizationKey);

            if (!string.IsNullOrWhiteSpace(item.Description))
                return item.Description;

            return item.Name;
        }

        private CraftingCategory BuildBuildingCategory(BuildingConfig building, HashSet<ItemConfig> categorizedItems)
        {
            if (building == null || building.Levels == null || building.Levels.Count == 0)
                return null;

            var buildingName = ResolveBuildingName(building);
            var category = new CraftingCategory(buildingName, buildingName);
            var minimumLevelsByItem = new Dictionary<ItemConfig, int>();

            foreach (var level in building.Levels)
            {
                if (level == null || level.Unlocks == null)
                    continue;

                foreach (var item in level.Unlocks)
                {
                    if (item == null)
                        continue;

                    if (!minimumLevelsByItem.TryGetValue(item, out var storedLevel) || level.Level < storedLevel)
                    {
                        minimumLevelsByItem[item] = level.Level;
                    }
                }
            }

            foreach (var pair in minimumLevelsByItem
                         .OrderBy(p => p.Value)
                         .ThenBy(p => ResolveItemName(p.Key), StringComparer.OrdinalIgnoreCase))
            {
                var recipe = new CraftingRecipeEntry(
                    $"{buildingName}:{ResolveItemName(pair.Key)}",
                    pair.Key,
                    category,
                    building,
                    pair.Value);

                category.AddRecipe(recipe);
                categorizedItems.Add(pair.Key);
            }

            return category;
        }

        private void AddFallbackItemCategory(List<CraftingCategory> categories, HashSet<ItemConfig> categorizedItems)
        {
            var gameConfig = GameRoot.Instance != null ? GameRoot.GameConfig : null;
            var itemDatabase = gameConfig != null ? gameConfig.ItemDatabase : null;

            if (itemDatabase == null || itemDatabase.AllItems == null)
                return;

            var category = new CraftingCategory(FallbackCategoryId, FallbackCategoryName);

            foreach (var item in itemDatabase.AllItems
                         .Where(i => i != null && !categorizedItems.Contains(i) && HasCraftCost(i))
                         .OrderBy(ResolveItemName, StringComparer.OrdinalIgnoreCase))
            {
                var recipe = new CraftingRecipeEntry(
                    $"{FallbackCategoryId}:{ResolveItemName(item)}",
                    item,
                    category,
                    null,
                    0);

                category.AddRecipe(recipe);
            }

            if (category.Recipes.Count > 0)
            {
                categories.Add(category);
            }
        }

        private static bool HasCraftCost(ItemConfig item)
        {
            return item != null && item.CraftCosts != null && item.CraftCosts.Any(cost => cost.Resource != null && cost.Amount > 0);
        }

        private static Item CreateRuntimeItem(ItemConfig config)
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

            return new Item(ResolveItemName(config), config.Prefab, config.Icon);
        }

        private static string ResolveBuildingName(BuildingConfig building)
        {
            if (building == null)
                return string.Empty;

            if (!string.IsNullOrWhiteSpace(building.BuildingName))
                return building.BuildingName;

            return building.name;
        }

        private static string ResolveItemName(ItemConfig item)
        {
            if (item == null)
                return string.Empty;

            if (!string.IsNullOrWhiteSpace(item.Name))
                return item.Name;

            return item.name;
        }

        private void HandleInventoryUpdated()
        {
            InventoryChanged?.Invoke();
        }
    }
}
