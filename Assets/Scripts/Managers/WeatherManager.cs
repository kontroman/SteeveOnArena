using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class WeatherPreset
{
    public string presetName;
    public bool enableRain;

    [Header("Sun Settings")]
    public Vector3 sunRotation;
    public float sunIntensity = 1f;
    public Color sunColor = Color.white;

    [Header("Sky & Fog")]
    public Material skyboxMaterial;
    public Color fogColor = Color.gray;
    public float fogDensity = 0.01f;
    public bool fogEnabled = true;

    [Header("Ambient Light")]
    public Color ambientLight = new Color(0.2f, 0.2f, 0.2f);
    public float ambientIntensity = 1.0f;

    [Header("Rain Settings")]
    public float rainIntensity = 0.5f;
}

namespace MineArena.Managers
{
    public class WeatherManager : MonoBehaviour
    {
        public static WeatherManager Instance;

        [Header("References")]
        [SerializeField] private Light directionalLight;
        [SerializeField] private ParticleSystem rainParticles;

        [Header("Presets")]
        [SerializeField] private List<WeatherPreset> presets = new List<WeatherPreset>();
        [SerializeField] private int defaultPresetIndex = 0;

        [Header("Auto Cycle")]
        [SerializeField] private bool autoCycleEnabled = false;
        [SerializeField] private float autoCycleInterval = 60f;

        private int currentPresetIndex = 0;
        private Coroutine autoCycleCoroutine;
        private WeatherPreset lastAppliedPreset; // ƒл€ отслеживани€ изменений

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        void Start()
        {
            currentPresetIndex = defaultPresetIndex;
            ApplyPreset(presets[currentPresetIndex]);
            lastAppliedPreset = CreateDeepCopy(presets[currentPresetIndex]);

            if (autoCycleEnabled) ToggleAutoCycle(true);
        }

        void Update()
        {
            // ≈сли текущий пресет был изменен в инспекторе, примен€ем изменени€
            if (currentPresetIndex >= 0 && currentPresetIndex < presets.Count)
            {
                if (!PresetEquals(lastAppliedPreset, presets[currentPresetIndex]))
                {
                    ApplyPreset(presets[currentPresetIndex]);
                    lastAppliedPreset = CreateDeepCopy(presets[currentPresetIndex]);
                }
            }
        }

        // —оздаем глубокую копию пресета дл€ сравнени€
        private WeatherPreset CreateDeepCopy(WeatherPreset original)
        {
            return new WeatherPreset()
            {
                presetName = original.presetName,
                enableRain = original.enableRain,
                sunRotation = original.sunRotation,
                sunIntensity = original.sunIntensity,
                sunColor = original.sunColor,
                skyboxMaterial = original.skyboxMaterial,
                fogColor = original.fogColor,
                fogDensity = original.fogDensity,
                fogEnabled = original.fogEnabled,
                ambientLight = original.ambientLight,
                ambientIntensity = original.ambientIntensity,
                rainIntensity = original.rainIntensity
            };
        }

        // —равниваем два пресета
        private bool PresetEquals(WeatherPreset a, WeatherPreset b)
        {
            if (a == null || b == null) return false;

            return a.presetName == b.presetName &&
                   a.enableRain == b.enableRain &&
                   a.sunRotation == b.sunRotation &&
                   Mathf.Approximately(a.sunIntensity, b.sunIntensity) &&
                   a.sunColor == b.sunColor &&
                   a.skyboxMaterial == b.skyboxMaterial &&
                   a.fogColor == b.fogColor &&
                   Mathf.Approximately(a.fogDensity, b.fogDensity) &&
                   a.fogEnabled == b.fogEnabled &&
                   a.ambientLight == b.ambientLight &&
                   Mathf.Approximately(a.ambientIntensity, b.ambientIntensity) &&
                   Mathf.Approximately(a.rainIntensity, b.rainIntensity);
        }

        public void ApplyPreset(WeatherPreset preset)
        {
            if (directionalLight != null)
            {
                directionalLight.transform.rotation = Quaternion.Euler(preset.sunRotation);
                directionalLight.intensity = preset.sunIntensity;
                directionalLight.color = preset.sunColor;
            }

            RenderSettings.skybox = preset.skyboxMaterial;
            DynamicGI.UpdateEnvironment();

            RenderSettings.fog = preset.fogEnabled;
            RenderSettings.fogColor = preset.fogColor;
            RenderSettings.fogDensity = preset.fogDensity;

            RenderSettings.ambientLight = preset.ambientLight;
            RenderSettings.ambientIntensity = preset.ambientIntensity;

            if (rainParticles != null)
            {
                if (preset.enableRain && !rainParticles.isPlaying)
                    rainParticles.Play();
                else if (!preset.enableRain && rainParticles.isPlaying)
                    rainParticles.Stop();

                var emission = rainParticles.emission;
                emission.rateOverTime = preset.rainIntensity * 1000f;
            }
        }

        public void SwitchToPreset(string presetName)
        {
            WeatherPreset preset = presets.Find(p => p.presetName == presetName);
            if (preset != null)
            {
                currentPresetIndex = presets.IndexOf(preset);
                ApplyPreset(preset);
                lastAppliedPreset = CreateDeepCopy(preset);
            }
        }

        public void ToggleAutoCycle(bool enable)
        {
            autoCycleEnabled = enable;
            if (enable) autoCycleCoroutine = StartCoroutine(AutoCycleRoutine());
            else if (autoCycleCoroutine != null) StopCoroutine(autoCycleCoroutine);
        }

        private IEnumerator AutoCycleRoutine()
        {
            while (autoCycleEnabled)
            {
                yield return new WaitForSeconds(autoCycleInterval);
                currentPresetIndex = (currentPresetIndex + 1) % presets.Count;
                ApplyPreset(presets[currentPresetIndex]);
                lastAppliedPreset = CreateDeepCopy(presets[currentPresetIndex]);
            }
        }
    }
}