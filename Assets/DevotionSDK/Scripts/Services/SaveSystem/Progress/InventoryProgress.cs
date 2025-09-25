using System;
using UnityEngine;

namespace Devotion.SDK.Services.SaveSystem.Progress
{
    [Serializable]
    public class InventoryProgress : BaseProgress
    {
        [SerializeField] private int debugOnlyResourcesInInventory;

        public int DebugOnlyResourcesInInventory { get { return debugOnlyResourcesInInventory; } }

        public InventoryProgress()
        {

        }

        public void AddResource()
        {
            debugOnlyResourcesInInventory++;

            Save();
        }
    }
}