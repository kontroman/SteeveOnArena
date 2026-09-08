using MineArena.Messages;
using MineArena.Levels;
using MineArena.ObjectPools;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MineArena.AI
{
    public class WaveSpawner : MonoBehaviour
    {
        [SerializeField] private List<SpawnPoint> _spawnPoints = new List<SpawnPoint>();
        [SerializeField] private List<EncounterWaveConfig> _waves = new List<EncounterWaveConfig>();
        [SerializeField] private bool _loopWaves;
        [SerializeField] private float _startDelay = 1f;
        [SerializeField] private float _nextWaveDelay = 5f;
        [SerializeField] private float _retryDelay = 0.2f;
        private bool _configured;
        private bool _tutorialEncounter;

        public int TotalMobCount
        {
            get
            {
                if (_loopWaves)
                {
                    Debug.LogWarning($"{nameof(WaveSpawner)} has loop waves enabled. Level progress uses one pass of configured waves.");
                }

                int total = 0;
                foreach (var wave in _waves)
                {
                    if (wave != null)
                        total += Mathf.Max(0, wave.MobCount);
                }

                return total;
            }
        }

        public void Configure(IReadOnlyList<EncounterWaveConfig> waves)
        {
            _configured = true;
            _tutorialEncounter = MineArena.Managers.TutorialService.Expedition;
            _waves = waves != null ? new List<EncounterWaveConfig>(waves) : new List<EncounterWaveConfig>();
            _loopWaves = false;
            _startDelay = 4f;
            _nextWaveDelay = 12f;
            if (_tutorialEncounter)
                _waves = new List<EncounterWaveConfig> { new EncounterWaveConfig { MobCount = 1, MobTypes = new List<MobTypes> { MobTypes.Zombie } } };
        }

        void Start()
        {
            StartCoroutine(SpawnWaves());
        }

        private IEnumerator SpawnWaves()
        {
            // Scene preview arenas must never start their serialized waves independently.
            while (!_configured) yield return null;
            while (MineArena.Managers.TutorialService.Expedition && MineArena.Managers.TutorialService.Progress.Step == MineArena.Managers.TutorialStep.Mine)
                yield return null;
            if (_waves.Count == 0)
                yield break;

            do
            {
                if (_startDelay > 0f)
                    yield return new WaitForSeconds(_startDelay);

                for (int waveIndex = 0; waveIndex < _waves.Count; waveIndex++)
                {
                    yield return StartCoroutine(SpawnWaveCoroutine(_waves[waveIndex]));

                    if (waveIndex < _waves.Count - 1 && _nextWaveDelay > 0f)
                        yield return new WaitForSeconds(_nextWaveDelay);
                }
            } while (_loopWaves);
        }

        private IEnumerator SpawnWaveCoroutine(EncounterWaveConfig wave)
        {
            if (wave == null || wave.MobCount <= 0 || _spawnPoints.Count == 0)
                yield break;

            int spawnedCount = 0;

            while (spawnedCount < wave.MobCount)
            {
                if (_tutorialEncounter && (!MineArena.Managers.TutorialService.Expedition ||
                    MineArena.Managers.TutorialService.Progress.Step != MineArena.Managers.TutorialStep.Kill)) yield break;
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

        private bool TrySpawnSingle(EncounterWaveConfig wave, ref int spawnedCount)
        {
            if (_spawnPoints.Count == 0)
                return false;

            int startIndex = Random.Range(0, _spawnPoints.Count);
            if (MineArena.Managers.TutorialService.Expedition && MineArena.Controllers.Player.Instance != null)
            {
                float nearest = float.MaxValue;
                for (int i = 0; i < _spawnPoints.Count; i++)
                {
                    if (_spawnPoints[i] == null) continue;
                    float distance = (_spawnPoints[i].transform.position - MineArena.Controllers.Player.Instance.transform.position).sqrMagnitude;
                    if (distance < nearest) { nearest = distance; startIndex = i; }
                }
            }

            for (int checkedCount = 0; checkedCount < _spawnPoints.Count; checkedCount++)
            {
                SpawnPoint spawnPoint = _spawnPoints[(startIndex + checkedCount) % _spawnPoints.Count];
                if (spawnPoint == null) continue;

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

        private MobTypes GetRandomMobType(EncounterWaveConfig wave)
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
            System.Type mobClassType = ResolveMobClassType(mobType);
            if (mobClassType == null)
            {
                Debug.LogError($"Pool for mob type {mobType} not found.");
                return null;
            }

            return ObjectPoolsManager.Instance.Get(mobClassType);
        }

        private System.Type ResolveMobClassType(MobTypes mobType)
        {
            string fullTypeName = $"{typeof(Mob).Namespace}.{mobType}";
            System.Type mobClassType = typeof(Mob).Assembly.GetType(fullTypeName);

            if (mobClassType == null || !typeof(Mob).IsAssignableFrom(mobClassType))
                return null;

            return mobClassType;
        }
    }
}
