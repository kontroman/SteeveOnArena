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
        [SerializeField, Min(1)] private int _maxAliveEnemies = 5;
        [SerializeField, Min(0f)] private float _repeatDelay = 45f;
        [SerializeField] private float _retryDelay = 0.2f;
        private bool _configured;
        private bool _tutorialEncounter;
        private int _experienceBudget = -1;
        private int _spawnedThisCycle;
        private readonly HashSet<MobHealth> _aliveEnemies = new HashSet<MobHealth>();
        private float _nextCycleTime = -1f;
        public event System.Action CycleStarted;
        public event System.Action<MobHealth> EnemyKilled;
        public float NextCycleSeconds => _nextCycleTime < 0f ? -1f : Mathf.Max(0f, _nextCycleTime - Time.time);

        private void OnEnable() => MobHealth.MobDied += HandleMobDied;
        private void OnDisable()
        {
            MobHealth.MobDied -= HandleMobDied;
            StopSpawning();
        }

        public void StopSpawning()
        {
            StopAllCoroutines();
            _nextCycleTime = -1f;
        }

        private void HandleMobDied(MobHealth health)
        {
            if (_aliveEnemies.Remove(health)) EnemyKilled?.Invoke(health);
        }

        public int TotalMobCount
        {
            get
            {
                int total = 0;
                foreach (var wave in _waves)
                {
                    if (wave != null)
                        total += Mathf.Max(0, wave.MobCount);
                }

                return total;
            }
        }

        public void Configure(IReadOnlyList<EncounterWaveConfig> waves, int experienceBudget = -1)
        {
            _configured = true;
            _tutorialEncounter = MineArena.Managers.TutorialService.Expedition;
            _waves = waves != null ? new List<EncounterWaveConfig>(waves) : new List<EncounterWaveConfig>();
            _experienceBudget = experienceBudget;
            _spawnedThisCycle = 0;
            _loopWaves = !_tutorialEncounter;
            _startDelay = _tutorialEncounter ? 0f : 4f;
            if (_tutorialEncounter)
            {
                // The single tutorial enemy earns one normal enemy's share, not a whole arena's XP.
                if (_experienceBudget >= 0)
                    _experienceBudget = MineArena.PlayerSystem.PlayerExperience.ArenaMonsterReward(_experienceBudget, TotalMobCount, 0);
                _waves = new List<EncounterWaveConfig> { new EncounterWaveConfig { MobCount = 1, MobTypes = new List<MobTypes> { MobTypes.Zombie } } };
            }
        }

        void Start()
        {
            StartCoroutine(SpawnWaves());
        }

        private IEnumerator SpawnWaves()
        {
            // Scene preview arenas must never start their serialized waves independently.
            while (!_configured) yield return null;
            if (_waves.Count == 0)
                yield break;

            if (TotalMobCount <= 0) yield break;
            if (_startDelay > 0f)
                yield return new WaitForSeconds(_startDelay);
            do
            {
                _nextCycleTime = -1f;
                _spawnedThisCycle = 0;
                CycleStarted?.Invoke();
                for (int waveIndex = 0; waveIndex < _waves.Count; waveIndex++)
                    yield return SpawnWaveCoroutine(_waves[waveIndex]);

                // Wait for the entire encounter to be cleared before starting the countdown.
                while (_aliveEnemies.Count > 0) yield return null;
                if (!_loopWaves) yield break;
                _nextCycleTime = Time.time + Mathf.Max(0f, _repeatDelay);
                while (Time.time < _nextCycleTime) yield return null;
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
                    (MineArena.Managers.TutorialService.Progress.Step != MineArena.Managers.TutorialStep.Mine &&
                     MineArena.Managers.TutorialService.Progress.Step != MineArena.Managers.TutorialStep.Kill))) yield break;
                if (_aliveEnemies.Count >= Mathf.Max(1, _maxAliveEnemies))
                {
                    yield return null;
                    continue;
                }
                bool spawnedAnyThisPass = TrySpawnSingle(wave, ref spawnedCount);

                if (!spawnedAnyThisPass)
                {
                    yield return new WaitForSeconds(_retryDelay);
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
                    mobObject.GetComponent<Mob>()?.SetTutorialDormant(_tutorialEncounter && MineArena.Managers.TutorialService.Progress.Step == MineArena.Managers.TutorialStep.Mine);
                    var health = mobObject.GetComponent<MobHealth>();
                    if (health != null && _experienceBudget >= 0)
                        health.SetExperienceReward(MineArena.PlayerSystem.PlayerExperience.ArenaMonsterReward(_experienceBudget, TotalMobCount, _spawnedThisCycle));
                    _spawnedThisCycle++;
                    _aliveEnemies.Add(health);
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
