using MineArena.Messages;
using MineArena.ObjectPools;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MineArena.AI
{
    public class WaveSpawner : MonoBehaviour
    {
        [System.Serializable]
        private class WaveConfig
        {
            [Min(1)] public int MobCount = 3;
            [Min(0f)] public float DelayBetweenMobs = 0f;
            public List<MobTypes> MobTypes = new List<MobTypes>();
        }

        [SerializeField] private List<SpawnPoint> _spawnPoints = new List<SpawnPoint>();
        [SerializeField] private List<WaveConfig> _waves = new List<WaveConfig>();
        [SerializeField] private float _startDelay = 1f;
        [SerializeField] private float _nextWaveDelay = 5f;
        [SerializeField] private float _retryDelay = 0.2f;

        void Start()
        {
            StartCoroutine(SpawnWaves());
        }

        private IEnumerator SpawnWaves()
        {
            if (_startDelay > 0f)
                yield return new WaitForSeconds(_startDelay);

            if (_waves.Count == 0)
                yield break;

            for (int waveIndex = 0; waveIndex < _waves.Count; waveIndex++)
            {
                yield return StartCoroutine(SpawnWaveCoroutine(_waves[waveIndex]));

                if (waveIndex < _waves.Count - 1 && _nextWaveDelay > 0f)
                    yield return new WaitForSeconds(_nextWaveDelay);
            }
        }

        private IEnumerator SpawnWaveCoroutine(WaveConfig wave)
        {
            if (wave == null || wave.MobCount <= 0 || _spawnPoints.Count == 0)
                yield break;

            int spawnedCount = 0;

            while (spawnedCount < wave.MobCount)
            {
                bool spawnedAnyThisPass = TrySpawnSingle(wave, ref spawnedCount);

                if (!spawnedAnyThisPass)
                {
                    yield return new WaitForSeconds(_retryDelay);
                }
                else if (spawnedCount < wave.MobCount && wave.DelayBetweenMobs > 0f)
                {
                    yield return new WaitForSeconds(wave.DelayBetweenMobs);
                }
                else
                {
                    yield return null;
                }
            }
        }

        private bool TrySpawnSingle(WaveConfig wave, ref int spawnedCount)
        {
            if (_spawnPoints.Count == 0)
                return false;

            int startIndex = Random.Range(0, _spawnPoints.Count);

            for (int checkedCount = 0; checkedCount < _spawnPoints.Count; checkedCount++)
            {
                SpawnPoint spawnPoint = _spawnPoints[(startIndex + checkedCount) % _spawnPoints.Count];

                if (spawnedCount >= wave.MobCount)
                    break;

                if (!spawnPoint.IsReadyForSpawn())
                    continue;

                GameObject mobObject = GetMobFromPool(GetRandomMobType(wave));
                if (!mobObject)
                    return false;

                if (spawnPoint.TrySpawn(mobObject))
                {
                    spawnedCount++;
                    return true;
                }
                else
                {
                    ObjectPoolsManager.Instance.Release<Mob>(mobObject);
                }
            }

            return false;
        }

        private MobTypes GetRandomMobType(WaveConfig wave)
        {
            if (wave.MobTypes == null || wave.MobTypes.Count == 0)
            {
                System.Array values = System.Enum.GetValues(typeof(MobTypes));
                return (MobTypes)values.GetValue(Random.Range(0, values.Length));
            }

            return wave.MobTypes[Random.Range(0, wave.MobTypes.Count)];
        }

        private GameObject GetMobFromPool(MobTypes mobType)
        {
            switch (mobType)
            {
                case MobTypes.Zombie:
                    return ObjectPoolsManager.Instance.Get<Zombie, Mob>();
                case MobTypes.Skeleton:
                    return ObjectPoolsManager.Instance.Get<Skeleton, Mob>();
                default:
                    Debug.LogError($"Pool for mob type {mobType} not found.");
                    return null;
            }
        }
    }
}
