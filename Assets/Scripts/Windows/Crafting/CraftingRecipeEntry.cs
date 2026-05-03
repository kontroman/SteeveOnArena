using System.Collections.Generic;
using MineArena.Buildings;
using MineArena.Items;
using UnityEngine;

namespace MineArena.Windows.Crafting
{
    public sealed class CraftingCategory
    {
        private readonly List<CraftingRecipeEntry> _recipes = new List<CraftingRecipeEntry>();

        public CraftingCategory(string id, string displayName)
        {
            Id = id;
            DisplayName = displayName;
        }

        public string Id { get; }
        public string DisplayName { get; }
        public IReadOnlyList<CraftingRecipeEntry> Recipes => _recipes;

        internal void AddRecipe(CraftingRecipeEntry recipe)
        {
            if (recipe != null && !_recipes.Contains(recipe))
            {
                _recipes.Add(recipe);
            }
        }
    }

    public sealed class CraftingRecipeEntry
    {
        public CraftingRecipeEntry(
            string id,
            ItemConfig item,
            CraftingCategory category,
            BuildingConfig sourceBuilding,
            int requiredBuildingLevel)
        {
            Id = id;
            Item = item;
            Category = category;
            SourceBuilding = sourceBuilding;
            RequiredBuildingLevel = requiredBuildingLevel;
        }

        public string Id { get; }
        public ItemConfig Item { get; }
        public CraftingCategory Category { get; }
        public BuildingConfig SourceBuilding { get; }
        public int RequiredBuildingLevel { get; }

        public string DisplayName => ResolveItemName(Item);
        public Sprite Icon => Item != null ? Item.Icon : null;
        public IReadOnlyList<ResourceRequired> Costs => Item != null ? Item.CraftCosts : null;
        public bool HasBuildingRequirement => SourceBuilding != null && RequiredBuildingLevel > 0;

        private static string ResolveItemName(ItemConfig item)
        {
            if (item == null)
                return string.Empty;

            if (!string.IsNullOrWhiteSpace(item.Name))
                return item.Name;

            return item.name;
        }
    }

    public enum CraftingResultStatus
    {
        Success,
        NoRecipe,
        Locked,
        NotEnoughResources,
        InventoryUnavailable,
        Failed
    }

    public readonly struct CraftingResult
    {
        public CraftingResult(CraftingResultStatus status, string message)
        {
            Status = status;
            Message = message;
        }

        public CraftingResultStatus Status { get; }
        public string Message { get; }
        public bool Success => Status == CraftingResultStatus.Success;

        public static CraftingResult Ok()
        {
            return new CraftingResult(CraftingResultStatus.Success, string.Empty);
        }

        public static CraftingResult Fail(CraftingResultStatus status, string message)
        {
            return new CraftingResult(status, message);
        }
    }
}
