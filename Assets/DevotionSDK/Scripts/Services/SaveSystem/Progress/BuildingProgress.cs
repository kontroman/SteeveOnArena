using MineArena.Structs;
using System;
using UnityEngine;
using static Devotion.SDK.Helpers.ContainersHelper;

namespace Devotion.SDK.Services.SaveSystem.Progress
{
    [Serializable]
    public class BuildingProgress : BaseProgress
    {
        [SerializeField] private SerializableDictionary<string, BuildingSaveData> savedBuilding = new();
        public SerializableDictionary<string, BuildingSaveData> SavedBuildings { get { return savedBuilding; } }

        public BuildingProgress() { }

        public void CacheBuilding(string id, BuildingSaveData currentData)
        {
            if (savedBuilding.TryGetValue(id, out BuildingSaveData data))
                savedBuilding[id] = currentData;
            else
                savedBuilding[id] = currentData;

            Debug.LogError("[TODO]: remove autosave");
            Save();
        }
    }
}