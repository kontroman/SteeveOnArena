using System;
using UnityEngine;
using static Devotion.SDK.Helpers.ContainersHelper;

namespace Devotion.SDK.Services.SaveSystem.Progress
{
    [Serializable]
    public class InventoryProgress : BaseProgress
    {
        [SerializeField] private SerializableDictionary<string, int> savedResources = new();

        public SerializableDictionary<string, int> SavedResources { get { return savedResources; } }

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
    }
}
