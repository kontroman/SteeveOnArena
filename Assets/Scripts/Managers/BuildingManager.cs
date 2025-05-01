using MineArena.Buildings;
using Devotion.SDK.Managers;
using UnityEngine;

namespace MineArena.Managers
{
    public class BuildingManager : BaseManager
    {
        public bool TryBuilding(BuildingConfig config)
        {
            Instantiate(config.GetCurrentLevel().ModelPrefab, config.BuildingPlace.transform.position, config.BuildingRotation);

            config.BuildingPlace.gameObject.GetComponent<Collider>().enabled = false;

            return true;
        }
    }
}