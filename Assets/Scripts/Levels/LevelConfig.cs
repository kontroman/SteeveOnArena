using MineArena.Items;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace MineArena.Levels
{
    [CreateAssetMenu(menuName = "Levels/" + nameof(LevelConfig))]
    public class LevelConfig : ScriptableObject
    {
        public static event Action<LevelConfig> ChangedInInspector;

        [SerializeField] private Sprite levelIcon;
        [Header("Level selection")]
        [SerializeField] private string displayName;
        [SerializeField, TextArea(2, 4)] private string description;
        [SerializeField] private LevelDifficulty difficulty;
        [SerializeField] private LevelSettings settings;
        [SerializeField] private List<ItemConfig> availableResources;
        [SerializeField] private List<ResourceSpawnConfig> resourceSpawnConfigs;
        [SerializeField] private List<LevelRewards> rewardResources;
        [SerializeField] private List<EncounterWaveConfig> encounterWaves = new List<EncounterWaveConfig>();
        public IReadOnlyList<EncounterWaveConfig> EncounterWaves => encounterWaves;
        [SerializeField] private GameObject levelPrefab;
        [SerializeField] private Vector3 levelPrefabPosition;
        [SerializeField] private Quaternion levelPrefabRotation;

        [Header("Camera")]
        [SerializeField] private bool overrideCameraFollowOffset;
        [SerializeField] private Vector3 cameraFollowOffset = new Vector3(-8.78f, 11.75f, -7.11f);
        [SerializeField] private bool overrideCameraZoomLimits;
        [SerializeField, Min(0.1f)] private float cameraMinDistance = 8f;
        [SerializeField, Min(0.1f)] private float cameraMaxDistance = 28f;

        [Header("Weather")]
        [SerializeField] private List<WeatherPreset> weatherPresets = new List<WeatherPreset>();
        [SerializeField] private WeatherPreset weatherPreset;

        [SerializeField, Range(0f, 1f)] private float requiredKillPercentToOpenPortal = 0.7f;
        [SerializeField] private GameObject portalPrefab;

        public Sprite LevelIcon {  get { return levelIcon; } }
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
        public string Description => description;
        public LevelDifficulty Difficulty { get { return difficulty; } }
        public LevelSettings Settings { get { return settings; } }
        public IReadOnlyList<ItemConfig> AvailableResources { get { return availableResources; } }
        public IReadOnlyList<ResourceSpawnConfig> ResourceSpawnConfigs { get { return resourceSpawnConfigs; } }
        public List<LevelRewards> RewardResources { get { return rewardResources; } }
        public GameObject LevelPrefab { get { return levelPrefab; } }
        public Vector3 LevelPrefabPosition { get { return levelPrefabPosition; } }
        public Quaternion LevelPrefabRotation { get { return levelPrefabRotation; } }
        public bool OverrideCameraFollowOffset { get { return overrideCameraFollowOffset; } }
        public Vector3 CameraFollowOffset { get { return cameraFollowOffset; } }
        public bool OverrideCameraZoomLimits { get { return overrideCameraZoomLimits; } }
        public float CameraMinDistance { get { return cameraMinDistance; } }
        public float CameraMaxDistance { get { return cameraMaxDistance; } }
        public WeatherPreset WeatherPreset { get { return weatherPreset; } }
        public IReadOnlyList<WeatherPreset> WeatherPresets { get { return weatherPresets; } }
        public float RequiredKillPercentToOpenPortal { get { return requiredKillPercentToOpenPortal; } }
        public GameObject PortalPrefab { get { return portalPrefab; } }

        public WeatherPreset GetRandomWeatherPreset()
        {
            if (weatherPresets != null && weatherPresets.Count > 0)
                return weatherPresets[UnityEngine.Random.Range(0, weatherPresets.Count)];

            return weatherPreset;
        }

        private void OnValidate()
        {
            cameraMinDistance = Mathf.Max(0.1f, cameraMinDistance);
            cameraMaxDistance = Mathf.Max(cameraMinDistance, cameraMaxDistance);

            if (Application.isPlaying)
                ChangedInInspector?.Invoke(this);
        }
    }

    [Serializable]
    public class EncounterWaveConfig
    {
        [Min(1)] public int MobCount = 3;
        [Min(0f)] public float DelayBetweenMobs = 1.5f;
        public List<MineArena.AI.MobTypes> MobTypes = new List<MineArena.AI.MobTypes>();
    }

    [Serializable]
    public class LevelRewards
    {
        public ItemConfig Item;
        public int Amount;
    }

    [Serializable]
    public class ResourceSpawnConfig
    {
        [SerializeField] private GameObject resource;
        [SerializeField, Range(0f, 1f), Tooltip("Relative weight among valid resource prefabs. Weights are normalized when spawning.")] private float spawnChance = 1f;

        public GameObject Resource { get { return resource; } }
        public float SpawnChance { get { return spawnChance; } }
    }
}
