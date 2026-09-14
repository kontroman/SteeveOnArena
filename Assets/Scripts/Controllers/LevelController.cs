using Devotion.SDK.Async;
using Devotion.SDK.Base;
using Devotion.SDK.Controllers;
using Devotion.SDK.Interfaces;
using MineArena.AI;
using MineArena.Basics;
using MineArena.Levels;
using MineArena.Managers;
using MineArena.Buildings;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using MineArena.Items;
using MineArena.PlayerSystem;
using MineArena.Windows;
using Devotion.SDK.UI;

namespace MineArena.Controllers
{
    public class LevelController : MonoBehaviour
    {
        public static LevelController Current { get; private set; }

        [SerializeField] private MonoBehaviour _rewardedAdsProviderBehaviour;

        private LevelConfig _currentConfig;
        private Arena _currentArena;
        private WaveSpawner _waveSpawner;
        private LevelProgressWindow _progressWindow;
        private GameObject _spawnedPortal;
        private int _totalMobs;
        private int _killedMobs;
        private bool _portalSpawned;
        private bool _levelCompleteOpened;
        private bool _rewardsApplied;
        private bool _portalPaused;
        private float _timeScaleBeforePortal;
        private LevelCompleteWindow _completeWindow;
        private readonly Dictionary<ItemConfig, int> _collectedResources = new Dictionary<ItemConfig, int>();

        public IReadOnlyDictionary<ItemConfig, int> CollectedResources { get { return _collectedResources; } }

        private void Awake()
        {
            // The scene contains an editor preview arena. The selected level supplies the runtime arena.
            foreach (var root in gameObject.scene.GetRootGameObjects())
            foreach (var arena in root.GetComponentsInChildren<Arena>(true))
            {
                arena.gameObject.SetActive(false);
                Destroy(arena.gameObject);
            }
            if (Current != null && Current != this)
            {
                Debug.LogWarning("Multiple LevelController instances detected. Overwriting current instance reference.");
            }

            Current = this;
            LevelConfig.ChangedInInspector += HandleLevelConfigChangedInInspector;
        }

        private void OnDestroy()
        {
            RestorePortalTime();
            if (Current == this)
            {
                Current = null;
            }

            if (_waveSpawner != null)
            {
                _waveSpawner.EnemyKilled -= HandleMobDied;
                _waveSpawner.CycleStarted -= HandleCycleStarted;
            }
            LevelConfig.ChangedInInspector -= HandleLevelConfigChangedInInspector;
        }

        private void HandleLevelConfigChangedInInspector(LevelConfig config)
        {
            if (config == _currentConfig)
                ApplyCameraSettings();
        }

        public IPromise InitLevel(LevelConfig config)
        {
            var promise = new Promise();
            _currentConfig = config;
            TutorialService.BeginLevel();
            _waveSpawner = null;
            _progressWindow = null;
            _spawnedPortal = null;
            _totalMobs = 0;
            _killedMobs = 0;
            _portalSpawned = false;
            _levelCompleteOpened = false;
            _rewardsApplied = false;
            ResetCollectedResources();
            promise.Resolve();
            return promise;
        }

        public IPromise GenerateLevel()
        {
            var promise = new Promise();
            try
            {
                _currentArena = Instantiate(_currentConfig.LevelPrefab, _currentConfig.LevelPrefabPosition, _currentConfig.LevelPrefabRotation).GetComponent<Arena>();

                Player player = FindObjectOfType<Player>();
                if (player != null && _currentArena != null && _currentArena.PlayerSpawnPosition != null)
                    player.transform.position = _currentArena.PlayerSpawnPosition.position;
                else
                    Debug.LogWarning($"{nameof(LevelController)}: player or player spawn point is not assigned.");

                ApplyCameraSettings();
                InitializeLevelProgress(player);

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

        private void ApplyCameraSettings()
        {
            if (_currentConfig == null)
                return;

            var cameraZoomController = FindObjectOfType<CameraZoomController>();
            if (cameraZoomController == null)
            {
                Debug.LogWarning($"{nameof(LevelController)}: {nameof(CameraZoomController)} was not found. Camera follow offset from level config was not applied.");
                return;
            }

            if (_currentConfig.OverrideCameraZoomLimits)
            {
                cameraZoomController.SetDistanceLimits(
                    _currentConfig.CameraMinDistance,
                    _currentConfig.CameraMaxDistance);
            }

            if (_currentConfig.OverrideCameraFollowOffset)
                cameraZoomController.SetFollowOffset(_currentConfig.CameraFollowOffset);
        }

        public IPromise GenerateOres()
        {
            var promise = new Promise();

            try
            {
                if (_currentArena == null || _currentConfig == null || _currentArena.UseAuthoredResources)
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

                var tutorialResource = TutorialService.Expedition ? resourceConfigs.FirstOrDefault(r => r?.Resource != null && r.Resource.GetComponent<InteractableObject>()?.IsMineable == true) : null;
                IEnumerable<Transform> selectedPoints = spawnPoints;
                if (TutorialService.Expedition && Player.Instance != null)
                    selectedPoints = spawnPoints.Where(p => p != null).OrderBy(p => (p.position - Player.Instance.transform.position).sqrMagnitude).Take(3);
                foreach (var spawnPoint in selectedPoints)
                {
                    if (spawnPoint == null)
                    {
                        continue;
                    }

                    var resourceConfig = tutorialResource ?? SelectResourceByChance(resourceConfigs);
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
            TutorialService.CollectedResource(resource);
        }

        public void ResetCollectedResources()
        {
            _collectedResources.Clear();
        }

        private void InitializeLevelProgress(Player player)
        {
            OpenPlayingWindowForGameplay();

            _waveSpawner = _currentArena != null ? _currentArena.GetComponentInChildren<WaveSpawner>(true) : FindObjectOfType<WaveSpawner>();
            if (_waveSpawner == null)
                Debug.LogWarning($"{nameof(LevelController)}: {nameof(WaveSpawner)} was not found. Level progress will use 0 total mobs.");

            if (_waveSpawner != null)
            {
                _waveSpawner.Configure(_currentConfig.EncounterWaves, _currentConfig.ExperiencePerClear);
                _waveSpawner.EnemyKilled += HandleMobDied;
                _waveSpawner.CycleStarted += HandleCycleStarted;
            }
            _totalMobs = _waveSpawner != null ? _waveSpawner.TotalMobCount : 0;
            _killedMobs = 0;

            _progressWindow = OpenOrCreateWindow<LevelProgressWindow>();
            if (_progressWindow != null)
            {
                _progressWindow.SetProgress(_killedMobs, _totalMobs);
                _progressWindow.SetPortalTarget(null, player != null ? player.transform : null);
            }
        }

        private void OpenPlayingWindowForGameplay()
        {
            try
            {
                GameRoot.UIManager?.ShowWindow<PlayingWindow>();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"{nameof(LevelController)}: failed to open {nameof(PlayingWindow)}. {ex.Message}");
            }
        }

        private void Update()
        {
            if (_waveSpawner == null) return;
            if (PlayerMovement.IsPlayerDead) _waveSpawner.StopSpawning();
            if (_progressWindow != null)
                _progressWindow.SetNextWaveCountdown(_waveSpawner.NextCycleSeconds);
        }

        private void HandleCycleStarted()
        {
            _killedMobs = 0;
            if (_progressWindow != null)
            {
                _progressWindow.SetNextWaveCountdown(-1f);
                _progressWindow.SetProgress(0, _totalMobs);
            }
        }

        private void HandleMobDied(MobHealth mobHealth)
        {
            if (_levelCompleteOpened)
                return;

            _killedMobs = Mathf.Min(_killedMobs + 1, _totalMobs);
            TutorialService.EnemyKilled();

            if (_progressWindow != null)
                _progressWindow.SetProgress(_killedMobs, _totalMobs);

            TrySpawnPortal();
        }

        private void TrySpawnPortal()
        {
            if (TutorialService.Expedition && TutorialService.Progress.Step != TutorialStep.Exit) return;
            if (_portalSpawned || _currentConfig == null)
                return;

            if (_totalMobs <= 0)
            {
                Debug.LogWarning($"{nameof(LevelController)}: total mob count is 0, portal will not be spawned by kill progress.");
                return;
            }

            float progress = (float)_killedMobs / _totalMobs;
            if (progress < _currentConfig.RequiredKillPercentToOpenPortal)
                return;

            if (_currentConfig.PortalPrefab == null)
            {
                Debug.LogWarning($"{nameof(LevelController)}: portal prefab is not assigned in {nameof(LevelConfig)}.");
                return;
            }

            Transform spawnPoint = _currentArena != null ? _currentArena.PortalSpawnPoint : null;
            if (spawnPoint == null)
            {
                Debug.LogWarning($"{nameof(LevelController)}: portal spawn point is not assigned on arena.");
                return;
            }

            _spawnedPortal = Instantiate(_currentConfig.PortalPrefab, spawnPoint.position, spawnPoint.rotation);
            _portalSpawned = true;
            PlayPortalConstructionEffect(_spawnedPortal);

            LevelPortal portal = _spawnedPortal.GetComponentInChildren<LevelPortal>();
            if (portal == null)
                portal = AddLevelPortalToTrigger(_spawnedPortal);

            portal.Entered += HandlePortalEntered;

            Player player = Player.Instance != null ? Player.Instance : FindObjectOfType<Player>();
            if (_progressWindow != null)
                _progressWindow.SetPortalTarget(_spawnedPortal.transform, player != null ? player.transform : null);
        }

        private static void PlayPortalConstructionEffect(GameObject portalObject)
        {
            if (portalObject == null)
                return;

            var effect = portalObject.GetComponentInChildren<BuildingConstructionEffect>(true);
            if (effect != null)
                effect.PlayRuntime();
        }

        public bool TryEnterSpawnedPortal(Transform portalTrigger, Collider playerCollider)
        {
            if (_spawnedPortal == null || portalTrigger == null || !portalTrigger.IsChildOf(_spawnedPortal.transform))
                return false;

            var portal = _spawnedPortal.GetComponentInChildren<LevelPortal>();
            if (portal != null && playerCollider != null) portal.TryEnter(playerCollider);
            return true;
        }

        public void ExitSpawnedPortal(Transform portalTrigger, Collider playerCollider)
        {
            if (_spawnedPortal != null && portalTrigger.IsChildOf(_spawnedPortal.transform))
                _spawnedPortal.GetComponentInChildren<LevelPortal>()?.Exit(playerCollider);
        }

        private static LevelPortal AddLevelPortalToTrigger(GameObject portalObject)
        {
            Collider[] colliders = portalObject.GetComponentsInChildren<Collider>(true);
            foreach (var collider in colliders)
            {
                if (collider != null && collider.isTrigger)
                    return collider.gameObject.AddComponent<LevelPortal>();
            }

            return portalObject.AddComponent<LevelPortal>();
        }

        private void HandlePortalEntered()
        {
            if (PlayerMovement.IsPlayerDead) return;
            if (TutorialService.Expedition && TutorialService.Progress.Step != TutorialStep.Exit) return;
            if (_levelCompleteOpened)
                return;

            _levelCompleteOpened = true;
            _timeScaleBeforePortal = Time.timeScale;
            _portalPaused = true;
            Time.timeScale = 0f;
            DisablePlayerControl();

            _completeWindow = OpenOrCreateWindow<LevelCompleteWindow>();
            if (_completeWindow == null)
            {
                Debug.LogWarning($"{nameof(LevelController)}: {nameof(LevelCompleteWindow)} could not be opened.");
                ResumeLevel();
                return;
            }

            _completeWindow.Setup(BuildDisplayedRewardTotals(), ContinueLevel, DoubleRewards, !TutorialService.Active && HasRewardedAdsProvider(), ResumeLevel);
        }

        private void RestorePortalTime()
        {
            if (!_portalPaused) return;
            _portalPaused = false;
            Time.timeScale = _timeScaleBeforePortal;
        }

        private void ResumeLevel()
        {
            GameRoot.UIManager?.CloseWindow<LevelCompleteWindow>();
            if (_completeWindow != null) _completeWindow.gameObject.SetActive(false);
            _levelCompleteOpened = false;
            RestorePortalTime();
            Player player = Player.Instance != null ? Player.Instance : FindObjectOfType<Player>();
            if (player == null || PlayerMovement.IsPlayerDead) return;
            player.GetComponentFromList<PlayerMovement>()?.SetMovement(true);
            player.GetComponentFromList<PlayerAttack>()?.SetComponentEnable(true);
        }

        private void ContinueLevel()
        {
            ApplyRewardsOnce(1);
            if (MineArena.Platform.YandexPlatform.IsWebPlatform && !TutorialService.Active)
                MineArena.Platform.YandexPlatform.Instance.ShowInterstitial(ReturnToLobby);
            else ReturnToLobby();
        }

        private void DoubleRewards()
        {
            var provider = _rewardedAdsProviderBehaviour as ILevelRewardedAdsProvider;
            if (provider == null && MineArena.Platform.YandexPlatform.IsWebPlatform)
                provider = MineArena.Platform.YandexPlatform.Instance;
            if (provider == null)
            {
                Debug.LogWarning($"{nameof(LevelController)}: rewarded ads provider is not assigned. Implement {nameof(ILevelRewardedAdsProvider)} on a component and assign it here.");
                ApplyRewardsOnce(1);
                ReturnToLobby();
                return;
            }

            provider.ShowLevelDoubleRewardsAd(success =>
            {
                ApplyRewardsOnce(success ? 2 : 1);
                ReturnToLobby();
            });
        }

        private void ApplyRewardsOnce(int multiplier)
        {
            if (_rewardsApplied)
                return;

            _rewardsApplied = true;
            var levels = GameRoot.GameConfig != null ? GameRoot.GameConfig.Levels : null;
            if (levels != null && _currentConfig != null)
            {
                int completedIndex = levels.IndexOf(_currentConfig);
                if (completedIndex >= 0 && completedIndex + 1 < levels.Count)
                    GameRoot.PlayerProgress?.LevelsProgress?.UnlockNextLevel(completedIndex);
            }

            var inventoryManager = GameRoot.GetManager<InventoryManager>();
            if (inventoryManager == null)
            {
                Debug.LogWarning($"{nameof(LevelController)}: {nameof(InventoryManager)} was not found. Level rewards were not applied.");
                return;
            }

            int safeMultiplier = Mathf.Max(1, multiplier);

            foreach (var reward in CompletionRewards())
                inventoryManager.AddItemById(reward.Key.Name, reward.Value * safeMultiplier);
            if (TutorialService.Expedition)
            {
                TutorialService.Progress.SmithSuppliesGranted = true;
                TutorialService.SetStep(TutorialStep.BuildSmith);
            }

            if (safeMultiplier > 1)
            {
                foreach (var collectedResource in _collectedResources)
                {
                    if (collectedResource.Key == null || collectedResource.Value <= 0)
                        continue;

                    inventoryManager.AddItemById(collectedResource.Key.Name, collectedResource.Value * (safeMultiplier - 1));
                }
            }
        }

        private Dictionary<ItemConfig, int> BuildDisplayedRewardTotals()
        {
            var rewards = new Dictionary<ItemConfig, int>();

            foreach (var resource in _collectedResources)
                AddReward(rewards, resource.Key, resource.Value);

            foreach (var reward in CompletionRewards()) AddReward(rewards, reward.Key, reward.Value);

            return rewards;
        }

        private Dictionary<ItemConfig, int> CompletionRewards()
        {
            var rewards = new Dictionary<ItemConfig, int>();
            if (_currentConfig?.RewardResources != null)
                foreach (var reward in _currentConfig.RewardResources)
                    if (reward != null) AddReward(rewards, reward.Item, GetCompletionRewardAmount(reward.Amount));
            if (TutorialService.Expedition && GameRoot.GameConfig?.Levels.IndexOf(_currentConfig) == 0)
                TutorialService.EnsureFirstBuildingReward(rewards);
            return rewards;
        }

        private int GetCompletionRewardAmount(int amount)
        {
            var buildings = GameRoot.GetManager<BuildingManager>();
            int bonus = buildings != null ? buildings.ExpeditionRewardBonusPercent : 0;
            return amount + Mathf.FloorToInt(amount * bonus / 100f);
        }

        private static void AddReward(Dictionary<ItemConfig, int> rewards, ItemConfig item, int amount)
        {
            if (item == null || amount <= 0)
                return;

            if (rewards.TryGetValue(item, out var currentAmount))
                rewards[item] = currentAmount + amount;
            else
                rewards[item] = amount;
        }

        private bool HasRewardedAdsProvider()
        {
            return _rewardedAdsProviderBehaviour is ILevelRewardedAdsProvider || MineArena.Platform.YandexPlatform.IsWebPlatform;
        }

        private void DisablePlayerControl()
        {
            Player player = Player.Instance != null ? Player.Instance : FindObjectOfType<Player>();
            if (player == null)
                return;

            player.GetComponentFromList<PlayerMovement>()?.SetMovement(false);
            player.GetComponentFromList<PlayerAttack>()?.SetComponentEnable(false);
        }

        private void ReturnToLobby()
        {
            _waveSpawner?.StopSpawning();
            RestorePortalTime();
            GameRoot.UIManager.CloseAllWindows();
            GameRoot.GetManager<UnitySceneLoader>()?.LoadSceneAsync(Constants.SceneNames.PlayerBaseScene);
        }

        public bool CanAbandon => !_levelCompleteOpened && !PlayerMovement.IsPlayerDead;

        public void AbandonLevel()
        {
            if (!CanAbandon) return;
            _levelCompleteOpened = true;
            _waveSpawner?.StopSpawning();
            DisablePlayerControl();
            DiscardCollectedResources();
            ReturnToLobby();
        }

        private void DiscardCollectedResources()
        {
            var inventory = GameRoot.PlayerProgress?.InventoryProgress;
            if (inventory != null)
            {
                foreach (var resource in _collectedResources)
                    if (resource.Key != null && resource.Value > 0 &&
                        inventory.SavedResources.TryGetValue(resource.Key.Name, out int owned))
                        inventory.TrySpendExact(resource.Key.Name, Mathf.Min(owned, resource.Value));
                GameRoot.GetManager<InventoryManager>()?.InitManager();
            }
            ResetCollectedResources();
        }

        private T OpenOrCreateWindow<T>() where T : BaseWindow
        {
            T window = null;

            try
            {
                var uiManager = GameRoot.UIManager;
                window = uiManager != null ? uiManager.OpenWindow<T>() as T : null;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"{nameof(LevelController)}: failed to open {typeof(T).Name} through UIManager. Runtime fallback will be used. {ex.Message}");
            }

            if (window != null)
            {
                window.transform.SetAsLastSibling();
                return window;
            }

            if (!AllowsRuntimeWindowFallback<T>())
            {
                Debug.LogWarning($"{nameof(LevelController)}: {typeof(T).Name} prefab was not opened by UIManager. Add the configured prefab to UIManager windows list.");
                return null;
            }

            Canvas canvas = FindMainCanvas();
            if (canvas == null)
            {
                Debug.LogWarning($"{nameof(LevelController)}: canvas was not found for runtime window {typeof(T).Name}.");
                return null;
            }

            var windowObject = new GameObject(typeof(T).Name, typeof(RectTransform));
            windowObject.transform.SetParent(canvas.transform, false);
            window = windowObject.AddComponent<T>();
            windowObject.SetActive(true);
            window.transform.SetAsLastSibling();
            return window;
        }

        private static bool AllowsRuntimeWindowFallback<T>() where T : BaseWindow
        {
            return typeof(T) != typeof(LevelProgressWindow);
        }

        private static Canvas FindMainCanvas()
        {
            GameObject mainCanvasObject = GameObject.FindGameObjectWithTag(Constants.GameTags.MainCanvas);
            if (mainCanvasObject != null && mainCanvasObject.TryGetComponent(out Canvas mainCanvas))
                return mainCanvas;

            return FindObjectOfType<Canvas>();
        }

        private ResourceSpawnConfig SelectResourceByChance(IReadOnlyList<ResourceSpawnConfig> resourceConfigs)
        {
            if (resourceConfigs == null || resourceConfigs.Count == 0)
            {
                return null;
            }

            float totalWeight = 0f;
            foreach (var config in resourceConfigs)
                if (config != null && config.Resource != null && config.SpawnChance > 0f)
                    totalWeight += config.SpawnChance;
            if (totalWeight <= 0f) return null;
            float randomValue = UnityEngine.Random.value * totalWeight;
            float cumulativeChance = 0f;

            for (int i = 0; i < resourceConfigs.Count; i++)
            {
                var config = resourceConfigs[i];
                if (config == null || config.Resource == null || config.SpawnChance <= 0f)
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
