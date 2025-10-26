using System.Collections.Generic;
using Devotion.SDK.Controllers;
using Devotion.SDK.Managers;
using MineArena.Buildings;
using MineArena.Structs;
using UnityEngine;

namespace MineArena.Managers
{
    public class BuildingManager : BaseManager
    {
        private readonly Dictionary<Transform, GameObject> _activeBuildings = new();
        private readonly Dictionary<Transform, int> _buildingLevels = new();

        public int GetBuildingLevel(BuildingConfig config)
        {
            if (config == null)
                return 0;

            if (GameRoot.PlayerProgress != null &&
                GameRoot.PlayerProgress.BuildingProgress != null &&
                GameRoot.PlayerProgress.BuildingProgress.SavedBuildings != null &&
                GameRoot.PlayerProgress.BuildingProgress.SavedBuildings.TryGetValue(config.BuildingName, out var savedData) &&
                savedData != null)
            {
                return savedData.Level;
            }

            var currentLevelConfig = config.GetCurrentLevel();
            if (currentLevelConfig != null)
                return currentLevelConfig.Level;

            var levels = config.Levels;
            if (levels != null && levels.Count > 0 && levels[0] != null)
                return levels[0].Level;

            return 0;
        }

        public override void InitManager()
        {
            _activeBuildings.Clear();
            _buildingLevels.Clear();

            var savedBuildings = GameRoot.PlayerProgress.BuildingProgress.SavedBuildings;

            foreach (var kvp in savedBuildings)
            {
                var buildingConfig = GameRoot.GameConfig.BuildingsDatabase.GetBuildingConfig(kvp.Key);

                if (buildingConfig == null)
                    continue;

                BuildWithoutSaving(buildingConfig, kvp.Value);
            }
        }

        private void BuildWithoutSaving(BuildingConfig config, BuildingSaveData saveData)
        {
            if (config == null || saveData == null || saveData.transform == null)
                return;

            var levelConfig = config.GetLevelByNumber(saveData.Level) ?? config.GetCurrentLevel();

            BuildAtLocation(config, saveData.transform, levelConfig);
            DisableBuildingPlaceCollider(saveData.transform);
        }

        public bool TryBuild(BuildingConfig config, Transform buildingPlace)
        {
            var levelConfig = config.GetCurrentLevel();

            var inventory = GameRoot.GetManager<InventoryManager>();

            if (!inventory.TryConsumeResources(levelConfig.RequiredResources))
            {
                Debug.Log($"BuildingManager: Not enough resources to build {config.BuildingName}");
                return false;
            }

            var instance = BuildAtLocation(config, buildingPlace, levelConfig);

            if (instance == null)
                return false;

            DisableBuildingPlaceCollider(buildingPlace);
            SaveBuildingData(config.BuildingName, levelConfig.Level, buildingPlace);

            return true;
        }

        public bool TryUpgrade(BuildingConfig config, Transform buildingPlace)
        {
            var currentLevel = GetTrackedLevel(config.BuildingName, buildingPlace);

            if (!config.TryGetNextLevel(currentLevel, out var nextLevelConfig) || nextLevelConfig == null)
            {
                Debug.Log($"BuildingManager: {config.BuildingName} already at max level");
                return false;
            }

            var inventory = GameRoot.GetManager<InventoryManager>();

            var instance = BuildAtLocation(config, buildingPlace, nextLevelConfig);

            if (instance == null)
                return false;

            SaveBuildingData(config.BuildingName, nextLevelConfig.Level, buildingPlace);
            return true;
        }

        private GameObject BuildAtLocation(BuildingConfig config, Transform buildingPlace, BuildingLevelConfig levelConfig)
        {
            if (_activeBuildings.TryGetValue(buildingPlace, out var existingBuilding) && existingBuilding != null)
            {
                Destroy(existingBuilding);
            }

            var prefab = levelConfig.ModelPrefab;

            var instance = Instantiate(prefab, buildingPlace.position, config.BuildingRotation);
            _activeBuildings[buildingPlace] = instance;
            _buildingLevels[buildingPlace] = levelConfig.Level;

            return instance;
        }

        private void DisableBuildingPlaceCollider(Transform buildingPlace)
        {
            if (buildingPlace == null)
                return;

            var collider = buildingPlace.GetComponent<Collider>();

            if (collider != null)
                collider.enabled = false;

            var zone = buildingPlace.GetComponent<BuildingZone>();
            zone?.DestroySign();
        }

        private void SaveBuildingData(string buildingName, int level, Transform buildingPlace)
        {
            var saveData = new BuildingSaveData(level, buildingPlace);

            GameRoot.PlayerProgress.BuildingProgress.CacheBuilding(buildingName, saveData);
        }

        private int GetTrackedLevel(string buildingName, Transform buildingPlace)
        {
            if (buildingPlace != null && _buildingLevels.TryGetValue(buildingPlace, out var levelFromPlace))
                return levelFromPlace;

            if (GameRoot.PlayerProgress.BuildingProgress.SavedBuildings.TryGetValue(buildingName, out var saveData))
                return saveData.Level;

            return 0;
        }
    }
}
