using MineArena.Buildings;
using Devotion.SDK.Managers;
using UnityEngine;
using Devotion.SDK.Controllers;

namespace MineArena.Managers
{
    public class BuildingManager : BaseManager
    {
        public override void InitManager()
        {
            var savedBuildings = GameRoot.PlayerProgress.BuildingProgress.SavedBuildings;

            foreach (var kvp in savedBuildings)
            {
                var buildingConfig = GameRoot.GameConfig.BuildingsDatabase.GetBuildingConfig(kvp.Key);

                if (buildingConfig == null) continue;

                BuildWithoutSaving(buildingConfig, kvp.Value.transform);
            }
        }

        private void BuildWithoutSaving(BuildingConfig config, Transform buildingPlace)
        {
            BuildAtLocation(config, buildingPlace);
            DisableBuildingPlaceCollider(buildingPlace);
        }

        public bool TryBuild(BuildingConfig config, Transform buildingPlace)
        {
            if (config == null || buildingPlace == null)
            {
                Debug.LogError("BuildingManager: Invalid config or building place");
                return false;
            }

            BuildAtLocation(config, buildingPlace);
            DisableBuildingPlaceCollider(buildingPlace);
            SaveBuildingData(config, buildingPlace);

            return true;
        }

        private void BuildAtLocation(BuildingConfig config, Transform buildingPlace)
        {
            var prefab = config.GetCurrentLevel().ModelPrefab;

            if (prefab == null)
            {
                Debug.LogError($"BuildingManager: Prefab is null for building {config.BuildingName}");
                return;
            }

            Instantiate(prefab, buildingPlace.position, config.BuildingRotation);
        }

        private void DisableBuildingPlaceCollider(Transform buildingPlace)
        {
            var collider = buildingPlace.GetComponent<Collider>();

            if (collider != null)
                collider.enabled = false;

            buildingPlace.GetComponent<BuildingZone>().DestroySign();
        }

        private void SaveBuildingData(BuildingConfig config, Transform buildingPlace)
        {
            var saveData = new Structs.BuildingSaveData
            (
                config.GetCurrentLevel().Level,
                buildingPlace
            );

            GameRoot.PlayerProgress.BuildingProgress.CacheBuilding
            (
                config.BuildingName,
                saveData
            );
        }
    }
}