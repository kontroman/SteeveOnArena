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
            if (!TutorialService.AllowCraft(recipe?.Item)) return false;
            if (recipe == null || recipe.Item == null || _inventoryManager == null)
                return false;

            return !recipe.IsProduction && ActiveJob == null && HasCraftCost(recipe.Item) && IsUnlocked(recipe) && GetMaxBatches(recipe) > 0;
        }

        public CraftJob ActiveJob => _inventoryManager != null ? _inventoryManager.GetComponent<CraftProductionService>()?.Job : null;

        public int GetCraftAmount(CraftingRecipeEntry recipe)
        {
            if (recipe?.Item == null) return 0;
            var level = recipe.SourceBuilding != null
                ? recipe.SourceBuilding.GetLevelByNumber(GetBuildingLevel(recipe.SourceBuilding)) : null;
            if (recipe.IsProduction)
                return (level?.Production.FirstOrDefault(p => p.Item == recipe.Item) ?? recipe.SourceBuilding.GetLevelByNumber(recipe.RequiredBuildingLevel).Production.First(p => p.Item == recipe.Item)).Amount;
            return recipe.Item.CraftAmount + (recipe.Item.Stackable && level != null ? level.CraftOutputBonus : 0);
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

        public int GetMaxBatches(CraftingRecipeEntry recipe)
        {
            if (recipe?.Item == null || recipe.IsProduction || !IsUnlocked(recipe) || _inventoryManager == null) return 0;
            return _inventoryManager.GetComponent<CraftProductionService>()?.MaxBatches(recipe.Item, GetCraftAmount(recipe)) ?? 0;
        }

        public CraftingResult TryCraft(CraftingRecipeEntry recipe, int batches = 1)
        {
            if (recipe == null || recipe.Item == null || recipe.IsProduction || !HasCraftCost(recipe.Item))
                return CraftingResult.Fail(CraftingResultStatus.NoRecipe, "Recipe is unavailable.");

            if (_inventoryManager == null)
                return CraftingResult.Fail(CraftingResultStatus.InventoryUnavailable, "Inventory is unavailable.");

            if (!IsUnlocked(recipe))
                return CraftingResult.Fail(CraftingResultStatus.Locked, "Recipe is locked.");

            if (ActiveJob != null)
                return CraftingResult.Fail(CraftingResultStatus.Failed, "Дождитесь завершения текущего крафта.");
            var service = _inventoryManager.GetComponent<CraftProductionService>();
            if (service == null || !service.TryStart(recipe.Item, GetCraftAmount(recipe), batches))
                return CraftingResult.Fail(CraftingResultStatus.NotEnoughResources, "Недостаточно ресурсов.");

            return CraftingResult.Ok();
        }

        public string GetDescription(CraftingRecipeEntry recipe)
        {
            if (recipe != null && recipe.IsProduction)
            {
                var level = recipe.SourceBuilding.GetLevelByNumber(Math.Max(recipe.RequiredBuildingLevel, GetBuildingLevel(recipe.SourceBuilding)));
                return $"Урожай: {GetCraftAmount(recipe)} шт. каждые {level.ProductionSeconds:0} с.\nРастёт на ферме. Сбор автоматический.";
            }
            var item = recipe != null ? recipe.Item : null;

            if (item == null)
                return string.Empty;

            if (!string.IsNullOrWhiteSpace(item.DescriptionLocalizationKey))
                return LocalizationService.GetLocalizedText(item.DescriptionLocalizationKey);

            if (!string.IsNullOrWhiteSpace(item.Description))
                return item.Description;

            if (item is ArmorConfig armor)
                return FormatDescription("Даёт +{0} к броне.", armor.Resist);

            if (item is WeaponItemConfig weapon && weapon.AttackConfig != null)
                return FormatDescription("Урон: {0:0.##}.", weapon.AttackConfig.BaseDamage);

            if (item is PickaxeConfig pickaxe)
                return FormatDescription("Время добычи: {0:0.##} с.", pickaxe.MiningDuration * Mathf.Max(1, pickaxe.MiningLoops));

            return string.Empty;
        }

        private static string FormatDescription(string template, params object[] values)
        {
            if (LocalizationService.TryGetLocalizedText(template, out var localized))
                template = localized;

            return string.Format(System.Globalization.CultureInfo.InvariantCulture, template, values);
        }

        private CraftingCategory BuildBuildingCategory(BuildingConfig building, HashSet<ItemConfig> categorizedItems)
        {
            if (building == null || building.Levels == null || building.Levels.Count == 0)
                return null;

            var buildingName = ResolveBuildingName(building);
            var category = new CraftingCategory(buildingName, buildingName);
            var minimumLevelsByItem = new Dictionary<ItemConfig, int>();

            foreach (var level in building.Levels)
                foreach (var output in level.Production)
                    if (output.Item != null && !category.Recipes.Any(r => r.Item == output.Item))
                    {
                        category.AddRecipe(new CraftingRecipeEntry($"{buildingName}:grow:{output.Item.Name}", output.Item, category, building, level.Level) { IsProduction = true });
                        categorizedItems.Add(output.Item);
                    }

            foreach (var level in building.Levels)
            {
                if (level == null || level.Unlocks == null)
                    continue;

                foreach (var item in level.Unlocks)
                {
                    if (item == null || !HasCraftCost(item))
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

            if (!string.IsNullOrWhiteSpace(item.DisplayName))
                return item.DisplayName;

            return item.name;
        }

        private void HandleInventoryUpdated()
        {
            InventoryChanged?.Invoke();
        }
    }
}
