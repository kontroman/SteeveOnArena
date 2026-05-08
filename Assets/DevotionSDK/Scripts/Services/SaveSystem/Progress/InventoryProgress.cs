using System;
using System.Collections.Generic;
using UnityEngine;
using static Devotion.SDK.Helpers.ContainersHelper;

namespace Devotion.SDK.Services.SaveSystem.Progress
{
    [Serializable]
    public class InventoryProgress : BaseProgress
    {
        private const int QuickSlotCount = 5;

        [SerializeField] private SerializableDictionary<string, int> savedResources = new();
        [SerializeField] private List<string> quickSlotItemIds = new();
        [SerializeField] private int selectedQuickSlotIndex;

        public SerializableDictionary<string, int> SavedResources
        {
            get
            {
                savedResources ??= new SerializableDictionary<string, int>();
                return savedResources;
            }
        }

        public IReadOnlyList<string> QuickSlotItemIds
        {
            get
            {
                EnsureQuickSlots();
                return quickSlotItemIds;
            }
        }

        public int SelectedQuickSlotIndex
        {
            get
            {
                EnsureQuickSlots();
                return Mathf.Clamp(selectedQuickSlotIndex, 0, QuickSlotCount - 1);
            }
        }

        public InventoryProgress() { }

        public void AddResource(string id, int amount = 1)
        {
            if (savedResources.TryGetValue(id, out int currentAmount))
                savedResources[id] = currentAmount + amount;
            else
                savedResources[id] = amount;

            Debug.LogError("[TODO]: remove autosave");
            Save();
        }

        public void RemoveResource(string id, int amount = 1)
        {
            if (!savedResources.TryGetValue(id, out int currentAmount))
                return;

            int newAmount = Mathf.Max(0, currentAmount - amount);

            if (newAmount <= 0)
                savedResources.Remove(id);
            else
                savedResources[id] = newAmount;

            Debug.LogError("[TODO]: remove autosave");
            Save();
        }

        public void ClearInventory(bool clearQuickSlots = true)
        {
            SavedResources.Clear();

            if (clearQuickSlots)
            {
                EnsureQuickSlots();

                for (int i = 0; i < quickSlotItemIds.Count; i++)
                    quickSlotItemIds[i] = string.Empty;
            }

            Save();
        }

        public string GetQuickSlotItemId(int index)
        {
            EnsureQuickSlots();

            if (index < 0 || index >= quickSlotItemIds.Count)
                return string.Empty;

            return quickSlotItemIds[index];
        }

        public void SetQuickSlotItemId(int index, string itemId)
        {
            EnsureQuickSlots();

            if (index < 0 || index >= quickSlotItemIds.Count)
                return;

            itemId ??= string.Empty;
            if (quickSlotItemIds[index] == itemId)
                return;

            if (!string.IsNullOrWhiteSpace(itemId))
            {
                for (int i = 0; i < quickSlotItemIds.Count; i++)
                {
                    if (i != index && quickSlotItemIds[i] == itemId)
                        quickSlotItemIds[i] = string.Empty;
                }
            }

            quickSlotItemIds[index] = itemId;
            Save();
        }

        public void SetSelectedQuickSlotIndex(int index)
        {
            EnsureQuickSlots();
            index = Mathf.Clamp(index, 0, QuickSlotCount - 1);
            if (selectedQuickSlotIndex == index)
                return;

            selectedQuickSlotIndex = index;
            Save();
        }

        private void EnsureQuickSlots()
        {
            quickSlotItemIds ??= new List<string>(QuickSlotCount);

            while (quickSlotItemIds.Count < QuickSlotCount)
                quickSlotItemIds.Add(string.Empty);

            while (quickSlotItemIds.Count > QuickSlotCount)
                quickSlotItemIds.RemoveAt(quickSlotItemIds.Count - 1);

            selectedQuickSlotIndex = Mathf.Clamp(selectedQuickSlotIndex, 0, QuickSlotCount - 1);
        }
    }
}
