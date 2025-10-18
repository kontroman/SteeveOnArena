using Devotion.SDK.Async;
using Devotion.SDK.Interfaces;
using MineArena.Levels;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace MineArena.Controllers
{
    public class LevelController : MonoBehaviour
    {
        private LevelConfig _currentConfig;
        private Arena _currentArena;

        public IPromise InitLevel(LevelConfig config)
        {
            var promise = new Promise();
            _currentConfig = config;
            promise.Resolve();
            return promise;
        }

        public IPromise GenerateLevel()
        {
            var promise = new Promise();
            try
            {
                _currentArena = Instantiate(_currentConfig.LevelPrefab, Vector3.zero, _currentConfig.LevelPrefabRotation).GetComponent<Arena>();

                FindObjectOfType<Player>().transform.position = _currentArena.PlayerSpawnPosition.position;

                promise.Resolve();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Generating level error: {ex}");

                promise.Reject(ex);
            }

            promise.Resolve();
            return promise;
        }

        public IPromise GenerateOres()
        {
            var promise = new Promise();

            try
            {
                if (_currentArena == null || _currentConfig == null)
                {
                    promise.Resolve();
                    return promise;
                }

                var spawnPoints = _currentArena.OreSpawnPoints;
                var resourceConfigs = _currentConfig.ResourceSpawnConfigs;

                if (spawnPoints == null || spawnPoints.Count == 0 || resourceConfigs == null || resourceConfigs.Count == 0)
                {
                    promise.Resolve();
                    return promise;
                }

                foreach (var spawnPoint in spawnPoints)
                {
                    if (spawnPoint == null)
                    {
                        continue;
                    }

                    var resourceConfig = SelectResourceByChance(resourceConfigs);
                    if (resourceConfig == null)
                    {
                        continue;
                    }

                    var resourcePrefab = resourceConfig.Resource;
                    if (resourcePrefab == null)
                    {
                        Debug.LogWarning("Resource prefab is not assigned in ResourceSpawnConfig.");
                        continue;
                    }

                    Instantiate(resourcePrefab, spawnPoint.position, spawnPoint.rotation);
                }

                promise.Resolve();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Generating ores error: {ex}");
                promise.Reject(ex);
            }

            return promise;
        }

        public IPromise GenerateChests()
        {
            var promise = new Promise();

            promise.Resolve();
            return promise;
        }

        private ResourceSpawnConfig SelectResourceByChance(IReadOnlyList<ResourceSpawnConfig> resourceConfigs)
        {
            if (resourceConfigs == null || resourceConfigs.Count == 0)
            {
                return null;
            }

            float randomValue = UnityEngine.Random.value;
            float cumulativeChance = 0f;

            for (int i = 0; i < resourceConfigs.Count; i++)
            {
                var config = resourceConfigs[i];
                if (config == null || config.SpawnChance <= 0f)
                {
                    continue;
                }

                cumulativeChance += config.SpawnChance;
                if (randomValue <= cumulativeChance)
                {
                    return config;
                }
            }

            return null;
        }
    }
}
