using Devotion.SDK.Async;
using Devotion.SDK.Interfaces;
using MineArena.Levels;
using System;
using System.Collections.Generic;
using UnityEngine;
using MineArena.Items;

namespace MineArena.Controllers
{
    public class LevelController : MonoBehaviour
    {
        public static LevelController Current { get; private set; }

        private LevelConfig _currentConfig;
        private Arena _currentArena;
        private readonly Dictionary<ItemConfig, int> _collectedResources = new Dictionary<ItemConfig, int>();

        public IReadOnlyDictionary<ItemConfig, int> CollectedResources { get { return _collectedResources; } }

        private void Awake()
        {
            if (Current != null && Current != this)
            {
                Debug.LogWarning("Multiple LevelController instances detected. Overwriting current instance reference.");
            }

            Current = this;
        }

        private void OnDestroy()
        {
            if (Current == this)
            {
                Current = null;
            }
        }

        public IPromise InitLevel(LevelConfig config)
        {
            var promise = new Promise();
            _currentConfig = config;
            ResetCollectedResources();
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

        public void RegisterCollectedResource(ItemConfig resource, int amount)
        {
            if (resource == null || amount <= 0)
            {
                return;
            }

            if (_collectedResources.TryGetValue(resource, out var total))
            {
                _collectedResources[resource] = total + amount;
            }
            else
            {
                _collectedResources[resource] = amount;
            }
        }

        public void ResetCollectedResources()
        {
            _collectedResources.Clear();
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
