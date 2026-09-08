using System;
using Devotion.SDK.Controllers;
using MineArena.Buildings;
using MineArena.Items;
using UnityEngine;

namespace MineArena.Managers
{
    [Serializable]
    public class CraftJob
    {
        public string ItemId;
        public int Amount;
        public long StartedUtcTicks;
        public long ReadyUtcTicks;
        public float Progress(long now) => ReadyUtcTicks <= StartedUtcTicks ? 1f : Mathf.Clamp01((float)((double)(now - StartedUtcTicks) / (ReadyUtcTicks - StartedUtcTicks)));
        public float Remaining(long now) => Mathf.Max(0f, (float)TimeSpan.FromTicks(ReadyUtcTicks - now).TotalSeconds);
    }

    // Lives with InventoryManager, so closing a window or changing scene cannot cancel paid work.
    public class CraftProductionService : MonoBehaviour
    {
        private float _nextPoll;
        private bool _starting;
        public CraftJob Job => GameRoot.PlayerProgress?.InventoryProgress?.PendingCraft;

        public int MaxBatches(ItemConfig item, int outputPerBatch)
        {
            var inventory = GetComponent<InventoryManager>();
            if (item == null || inventory == null || outputPerBatch <= 0) return 0;
            int max = CraftBatchUtility.MaxBatches(item.CraftCosts, inventory.Items, outputPerBatch);
            int space = (int.MaxValue - Math.Max(0, inventory.GetItemAmount(item.Name))) / outputPerBatch;
            return Math.Min(max, Math.Min(space, TutorialService.Active || !item.Stackable ? 1 : int.MaxValue));
        }

        public bool TryStart(ItemConfig item, int amount, int batches = 1)
        {
            if (TutorialService.BlocksInput) return false;
            if (!TutorialService.AllowCraft(item)) return false;
            var inventory = GetComponent<InventoryManager>();
            var progress = GameRoot.PlayerProgress?.InventoryProgress;
            if (_starting || item == null || item.CraftCosts == null || item.CraftCosts.Count == 0 || inventory == null || amount <= 0 || progress == null || Job != null ||
                batches <= 0 || batches > MaxBatches(item, amount)) return false;
            var costs = CraftBatchUtility.Scale(item.CraftCosts, batches);
            _starting = true;
            try
            {
                var now = DateTime.UtcNow.Ticks;
                // Resource consumption publishes its save only after the job is present.
                progress.PendingCraft = new CraftJob { ItemId = item.Name, Amount = checked(amount * batches),
                    StartedUtcTicks = now, ReadyUtcTicks = now + TimeSpan.FromSeconds(item.CraftSeconds).Ticks };
                if (!inventory.TryConsumeResources(costs)) { progress.PendingCraft = null; return false; }
                return true;
            }
            finally { _starting = false; }
        }

        private void Update()
        {
            if (GameRoot.Instance == null || Time.unscaledTime < _nextPoll) return;
            _nextPoll = Time.unscaledTime + 0.25f;
            var progress = GameRoot.PlayerProgress?.InventoryProgress;
            var inventory = GetComponent<InventoryManager>();
            if (progress == null || inventory == null) return;
            long now = DateTime.UtcNow.Ticks;
            var job = Job;
            if (job != null && now >= job.ReadyUtcTicks && GameRoot.GameConfig.ItemDatabase.GetItemConfig(job.ItemId) != null)
            {
                progress.PendingCraft = null;
                TutorialService.Crafted(job.ItemId);
                inventory.AddItemById(job.ItemId, job.Amount);
            }
            var buildings = GameRoot.GetManager<BuildingManager>();
            if (buildings == null) return;
            foreach (var config in GameRoot.GameConfig.BuildingsDatabase.AllBuildings)
            {
                var level = config.GetLevelByNumber(buildings.GetBuildingLevel(config));
                if (level?.Production == null || level.Production.Count == 0) continue;
                long interval = TimeSpan.FromSeconds(level.ProductionSeconds).Ticks;
                if (progress.FarmNextProductionUtcTicks <= 0)
                {
                    progress.FarmNextProductionUtcTicks = now + interval;
                    progress.Save();
                }
                else if (now >= progress.FarmNextProductionUtcTicks)
                {
                    // Up to ten stored harvests when returning after an absence.
                    int cycles = (int)Math.Min(10L, 1L + (now - progress.FarmNextProductionUtcTicks) / interval);
                    progress.FarmNextProductionUtcTicks = now + interval;
                    // Commit all outputs together before the inventory refresh/save notification.
                    foreach (var output in level.Production)
                    {
                        if (output?.Item == null || output.Amount <= 0) continue;
                        progress.SavedResources.TryGetValue(output.Item.Name, out int owned);
                        progress.SavedResources[output.Item.Name] = (int)Math.Min(int.MaxValue, (long)owned + (long)output.Amount * cycles);
                    }
                    inventory.InitManager();
                    progress.Save();
                }
            }
        }
    }
}
