using Devotion.SDK.Async;
using Devotion.SDK.Interfaces;
using MineArena.Levels;
using System;
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

            promise.Resolve();
            return promise;
        }

        public IPromise GenerateChests()
        {
            var promise = new Promise();

            promise.Resolve();
            return promise;
        }
    }
}