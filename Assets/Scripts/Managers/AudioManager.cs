using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using MineArena.Basics;
using Devotion.SDK.Managers;
using MineArena.MusicResourses;

namespace MineArena.Managers
{
    public class AudioManager : BaseManager
    {
        [SerializeField] private AudioMixer _audioMixer;
        [SerializeField] private MusicResourses.MusicResourses _music;
        [SerializeField, Min(0f)] private float _musicFadeDuration = 1.25f;
        [SerializeField, Range(0f, 1f)] private float _musicVolume = 1f;

        private AudioSource _musicSourceA;
        private AudioSource _musicSourceB;
        private AudioSource _effectSource;       
        private AudioSource _activeMusicSource;
        private Coroutine _musicFadeRoutine;
        private string _currentMusicName;
        private readonly List<RaycastResult> _uiRaycastResults = new List<RaycastResult>();

        private void Awake()
        {
            var sources = GetComponents<AudioSource>();
            if (sources.Length == 0)
            {
                _effectSource = gameObject.AddComponent<AudioSource>();
                _musicSourceA = gameObject.AddComponent<AudioSource>();
                _musicSourceB = gameObject.AddComponent<AudioSource>();
            }
            else if (sources.Length == 1)
            {
                _effectSource = sources[0];
                _musicSourceA = gameObject.AddComponent<AudioSource>();
                _musicSourceB = gameObject.AddComponent<AudioSource>();
            }
            else if (sources.Length == 2)
            {
                _effectSource = sources[0];
                _musicSourceA = sources[1];
                _musicSourceB = gameObject.AddComponent<AudioSource>();
            }
            else
            {
                _effectSource = sources[0];
                _musicSourceA = sources[1];
                _musicSourceB = sources[2];
            }

            var outputGroup = _effectSource != null ? _effectSource.outputAudioMixerGroup : null;
            ConfigureEffectSource(_effectSource);
            ConfigureMusicSource(_musicSourceA, outputGroup);
            ConfigureMusicSource(_musicSourceB, outputGroup);
            _activeMusicSource = _musicSourceA;
            ApplyMusicVolumeToSources();
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void Start()
        {
            ApplySceneMusic(SceneManager.GetActiveScene().name);
        }

        private void OnValidate()
        {
            _musicVolume = Mathf.Clamp01(_musicVolume);

            if (Application.isPlaying)
                ApplyMusicVolumeToSources();
        }

        private void Update()
        {
            if (TryHandleUiClick())
                PlayEffect(Constants.AudioNames.UIClick);
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            ApplySceneMusic(scene.name);
        }

        private void ApplySceneMusic(string sceneName)
        {
            if (sceneName == Constants.SceneNames.GameplayScene)
            {
                PlayMusic(Constants.AudioNames.ArenaMusic);
                return;
            }

            PlayMusic(Constants.AudioNames.SpawnMusic);
        }

        public void PlayEffect(string name)
        {
            AudioClip clip = _music.GetEffect(name);
            if (clip == null || _effectSource == null)
                return;

            _effectSource.PlayOneShot(clip, Mathf.Max(0f, _music.GetEffectVolume(name)));
        }

        public void PlayRandomEffect(string[] names)
        {
            if (names == null || names.Length == 0)
                return;

            PlayEffect(names[Random.Range(0, names.Length)]);
        }

        public void PlayMusic(string name)
        {
            AudioClip targetClip = _music.GetMusic(name);
            if (targetClip == null)
                return;

            if (_currentMusicName == name && _activeMusicSource != null && _activeMusicSource.isPlaying)
                return;

            _currentMusicName = name;

            if (_musicFadeRoutine != null)
                StopCoroutine(_musicFadeRoutine);

            _musicFadeRoutine = StartCoroutine(CrossFadeMusic(targetClip));
        }

        public void StopMusic()
        {
            if (_musicFadeRoutine != null)
            {
                StopCoroutine(_musicFadeRoutine);
                _musicFadeRoutine = null;
            }

            _musicSourceA?.Stop();
            _musicSourceB?.Stop();
            _currentMusicName = null;
            _activeMusicSource = null;
        }

        public void SetMusicVolume(float volume)
        {
            _musicVolume = Mathf.Clamp01(volume);
            ApplyMusicVolumeToSources();
        }

        public void SetEffectVolume(float volume)
        {
            _audioMixer.SetFloat("EffectVolume", Mathf.Log10(volume) * 20);
        }

        private IEnumerator CrossFadeMusic(AudioClip targetClip)
        {
            if (_musicSourceA == null || _musicSourceB == null)
                yield break;

            var fromSource = _activeMusicSource != null ? _activeMusicSource : _musicSourceA;
            var toSource = fromSource == _musicSourceA ? _musicSourceB : _musicSourceA;

            if (fromSource == null || toSource == null)
                yield break;

            if (fromSource.clip == targetClip && fromSource.isPlaying)
                yield break;

            float startVolume = fromSource.isPlaying ? fromSource.volume : GetTargetMusicVolume();
            float targetVolume = GetTargetMusicVolume();

            toSource.clip = targetClip;
            toSource.loop = true;
            toSource.volume = 0f;
            toSource.Play();

            if (_musicFadeDuration <= 0f)
            {
                if (fromSource.isPlaying)
                    fromSource.Stop();

                toSource.volume = targetVolume;
                _activeMusicSource = toSource;
                _musicFadeRoutine = null;
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < _musicFadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / _musicFadeDuration);

                if (fromSource.isPlaying)
                    fromSource.volume = Mathf.Lerp(startVolume, 0f, t);

                toSource.volume = Mathf.Lerp(0f, targetVolume, t);
                yield return null;
            }

            if (fromSource.isPlaying)
                fromSource.Stop();

            fromSource.volume = startVolume;
            toSource.volume = targetVolume;
            _activeMusicSource = toSource;
            _musicFadeRoutine = null;
        }

        private void ApplyMusicVolumeToSources()
        {
            float targetVolume = GetTargetMusicVolume();

            if (_musicSourceA != null && _musicSourceA != _activeMusicSource)
                _musicSourceA.volume = targetVolume;

            if (_musicSourceB != null && _musicSourceB != _activeMusicSource)
                _musicSourceB.volume = targetVolume;

            if (_activeMusicSource != null && _activeMusicSource.clip != null)
                _activeMusicSource.volume = targetVolume;
        }

        private float GetTargetMusicVolume()
        {
            float sourceMultiplier = _effectSource != null ? Mathf.Clamp01(_effectSource.volume) : 1f;
            return Mathf.Clamp01(_musicVolume) * sourceMultiplier;
        }

        private static void ConfigureEffectSource(AudioSource source)
        {
            if (source == null)
                return;

            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;
        }

        private static void ConfigureMusicSource(AudioSource source, AudioMixerGroup outputGroup)
        {
            if (source == null)
                return;

            source.playOnAwake = false;
            source.loop = true;
            source.spatialBlend = 0f;
            source.outputAudioMixerGroup = outputGroup;
        }

        private bool TryHandleUiClick()
        {
            if (EventSystem.current == null || !IsPointerPressedThisFrame())
                return false;

            var eventData = new PointerEventData(EventSystem.current)
            {
                position = GetPointerPosition()
            };

            _uiRaycastResults.Clear();
            EventSystem.current.RaycastAll(eventData, _uiRaycastResults);

            for (int i = 0; i < _uiRaycastResults.Count; i++)
            {
                var target = _uiRaycastResults[i].gameObject;
                if (target == null)
                    continue;

                var button = target.GetComponentInParent<Button>(true);
                if (button != null && button.interactable)
                    return true;
            }

            return false;
        }

        private static bool IsPointerPressedThisFrame()
        {
            return Input.GetMouseButtonDown(0);
        }

        private static Vector2 GetPointerPosition()
        {
            return Input.mousePosition;
        }
    }
}
