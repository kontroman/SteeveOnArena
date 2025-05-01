using MineArena.Buildings;
using Devotion.SDK.Managers;
using UnityEngine;

namespace MineArena.Managers
{
    public class BuildingManager : BaseManager
    {
        public bool TryBuilding(BuildingConfig config, Transform buildingPlace)
        {
            Instantiate(config.GetCurrentLevel().ModelPrefab, buildingPlace.position, config.BuildingRotation);

            buildingPlace.gameObject.GetComponent<Collider>().enabled = false;

            return true;
        }
    }
}