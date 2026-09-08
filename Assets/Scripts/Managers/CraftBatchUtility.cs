using System;
using System.Collections.Generic;
using MineArena.Buildings;
using MineArena.Items;

namespace MineArena.Managers
{
    public static class CraftBatchUtility
    {
        public static int MaxBatches(IReadOnlyList<ResourceRequired> costs, IEnumerable<Item> inventory, int outputPerBatch)
        {
            if (costs == null || costs.Count == 0 || inventory == null || outputPerBatch <= 0) return 0;
            var required = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
            var available = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
            int max = int.MaxValue / outputPerBatch;
            foreach (var cost in costs)
            {
                if (cost.Resource == null || cost.Amount <= 0 || string.IsNullOrWhiteSpace(cost.ResourceCategory)) return 0;
                required.TryGetValue(cost.ResourceCategory, out long total);
                required[cost.ResourceCategory] = total + cost.Amount;
                max = Math.Min(max, int.MaxValue / cost.Amount);
            }
            foreach (var item in inventory)
                if (item is StackableItem stack && stack.CurrentStack > 0)
                {
                    available.TryGetValue(stack.ResourceCategory, out long total);
                    available[stack.ResourceCategory] = total + stack.CurrentStack;
                }
            foreach (var pair in required)
            {
                available.TryGetValue(pair.Key, out long total);
                max = (int)Math.Min(max, total / pair.Value);
            }
            return max;
        }

        public static List<ResourceRequired> Scale(IReadOnlyList<ResourceRequired> costs, int batches)
        {
            if (batches <= 0) throw new ArgumentOutOfRangeException(nameof(batches));
            var result = new List<ResourceRequired>(costs.Count);
            foreach (var cost in costs) result.Add(new ResourceRequired(cost.Resource, checked(cost.Amount * batches)));
            return result;
        }
    }
}
