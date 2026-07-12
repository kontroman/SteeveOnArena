using System.Collections;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
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

        private AudioSource _musicSourceA;
        private AudioSource _musicSourceB;
        private AudioSource _effectSource;       
        private AudioSource _activeMusicSource;
        private Coroutine _musicFadeRoutine;
        private string _currentMusicName;

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
            _audioMixer.SetFloat("MusicVolume", Mathf.Log10(volume) * 20);
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

            float startVolume = fromSource.isPlaying ? fromSource.volume : 1f;

            toSource.clip = targetClip;
            toSource.loop = true;
            toSource.volume = 0f;
            toSource.Play();

            if (_musicFadeDuration <= 0f)
            {
                if (fromSource.isPlaying)
                    fromSource.Stop();

                toSource.volume = 1f;
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

                toSource.volume = Mathf.Lerp(0f, 1f, t);
                yield return null;
            }

            if (fromSource.isPlaying)
                fromSource.Stop();

            fromSource.volume = startVolume;
            toSource.volume = 1f;
            _activeMusicSource = toSource;
            _musicFadeRoutine = null;
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
    }
}
