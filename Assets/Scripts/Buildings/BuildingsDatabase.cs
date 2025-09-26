using System;
using System.Collections.Generic;
using UnityEngine;

namespace MineArena.Buildings
{
    [CreateAssetMenu(fileName = "BuildingsDatabase", menuName = "MineArena/Buildings Database")]
    public class BuildingsDatabase : ScriptableObject
    {
        [SerializeField] private List<BuildingConfig> allItems;

        public BuildingConfig GetBuildingConfig(string buildingName)
        {
            try
            {
                return allItems.Find(x => x.BuildingName.Equals(buildingName));
            }
            catch (NullReferenceException)
            {
                return null;
            }
        }
    }
}