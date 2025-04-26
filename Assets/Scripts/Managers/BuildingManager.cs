using Devotion.Buildings;
using Devotion.SDK.Managers;
using UnityEngine;

namespace Devotion.Buildings
{
    public class BuildingManager : BaseManager
    {
        public bool TryBuilding(BuildingConfig config)
        {
            Instantiate(config.GetCurrentLevel().ModelPrefab, config.BuildingPlace.transform.position, Quaternion.identity);

            config.BuildingPlace.gameObject.GetComponent<Collider>().enabled = false;

            return true;
        }
    }
}