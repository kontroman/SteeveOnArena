using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

namespace MineArena.Managers
{
    public sealed class MobSoundBus : MonoBehaviour
    {
        private sealed class Voice
        {
            public AudioSource Source;
            public int Owner, Priority;
            public float Started;
        }
        private readonly Voice[] _voices = new Voice[6];
        private readonly Dictionary<string, AudioClip> _clips = new Dictionary<string, AudioClip>();
        private readonly Dictionary<string, float> _lastPlayed = new Dictionary<string, float>();
        private AudioListener _listener;
        private float _nextOrdinary;

        public void Initialize(AudioMixerGroup group)
        {
            foreach (var clip in Resources.LoadAll<AudioClip>("MobFeedback")) _clips[clip.name] = clip;
            for (int i = 0; i < _voices.Length; i++)
            {
                var child = new GameObject("Mob voice " + i);
                child.transform.SetParent(transform);
                var source = child.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 1;
                source.rolloffMode = AudioRolloffMode.Linear;
                source.minDistance = 2;
                source.maxDistance = 22;
                source.dopplerLevel = 0;
                source.outputAudioMixerGroup = group;
                _voices[i] = new Voice { Source = source };
            }
        }

        public void Play(int owner, Vector3 position, string key, int priority, float duration = 0)
        {
            if (!_clips.TryGetValue(key, out var clip) || Time.timeScale == 0) return;
            if (_listener == null || !_listener.isActiveAndEnabled) _listener = FindObjectOfType<AudioListener>();
            if (_listener == null || Vector3.Distance(position, _listener.transform.position) > 22) return;
            // A death/explosion always ends the owner's previous voice, even when globally throttled.
            if (priority >= 3) StopOwner(owner, true);
            if (_lastPlayed.TryGetValue(key, out float last) && Time.time - last < (priority == 4 ? 0.12f : 0.16f)) return;
            if (priority < 2 && Time.time < _nextOrdinary) return;
            Voice selected = null;
            int ordinary = 0;
            foreach (var voice in _voices)
            {
                if (!voice.Source.isPlaying) { if (selected == null) selected = voice; continue; }
                if (voice.Priority < 2) ordinary++;
                if (voice.Owner != owner) continue;
                if (voice.Priority >= priority || (priority < 2 && Time.time - voice.Started < 0.1f)) return;
                selected = voice;
                break;
            }
            if (priority < 2 && ordinary >= 3) return;
            if (selected == null)
                foreach (var voice in _voices)
                    if (voice.Priority < priority && (selected == null || voice.Priority < selected.Priority)) selected = voice;
            if (selected == null) return;
            selected.Source.Stop();
            selected.Owner = owner;
            selected.Priority = priority;
            selected.Started = Time.time;
            selected.Source.transform.position = position;
            selected.Source.clip = clip;
            selected.Source.pitch = duration > 0 ? clip.length / Mathf.Max(0.05f, duration) : Random.Range(0.94f, 1.06f);
            selected.Source.volume = priority == 4 ? 1f : priority == 2 ? 0.67f : 0.46f;
            selected.Source.Play();
            _lastPlayed[key] = Time.time;
            if (priority < 2) _nextOrdinary = Time.time + 0.07f;
        }

        public void StopOwner(int owner, bool includeTerminal)
        {
            foreach (var voice in _voices)
                if (voice != null && voice.Owner == owner && (includeTerminal || voice.Priority < 3)) voice.Source.Stop();
        }
        public void StopFuse(int owner)
        {
            foreach (var voice in _voices)
                if (voice != null && voice.Owner == owner && voice.Priority == 2) voice.Source.Stop();
        }
        private void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mode == LoadSceneMode.Additive) return;
            StopAll();
            _lastPlayed.Clear();
            _nextOrdinary = 0;
            _listener = null;
        }
        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            StopAll();
        }
        private void StopAll()
        {
            foreach (var voice in _voices) if (voice != null) voice.Source.Stop();
        }
    }
}
