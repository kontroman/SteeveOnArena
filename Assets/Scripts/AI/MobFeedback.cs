using Devotion.SDK.Controllers;
using System.Collections.Generic;
using MineArena.Managers;
using UnityEngine;

namespace MineArena.AI
{
    // Added by Mob before applying its preset, including mobs obtained from pools.
    public sealed class MobFeedback : MonoBehaviour
    {
        private static readonly List<GameObject> Explosions = new List<GameObject>();
        private MobHealth _health;
        private MobTypes _type;
        private float _previousHealth;
        private bool _exploded;
        private MobSoundBus Bus => GameRoot.Instance != null
            ? GameRoot.GetManager<AudioManager>()?.MobSounds : null;

        private void OnEnable()
        {
            _health = GetComponent<MobHealth>();
            _previousHealth = _health.CurrentValue;
            _health.OnHealthChanged += OnHealthChanged;
            _exploded = false;
        }

        private void OnDisable()
        {
            if (_health != null) _health.OnHealthChanged -= OnHealthChanged;
            Bus?.StopOwner(GetInstanceID(), false);
        }

        public void Configure(MobTypes type) { _type = type; _exploded = false; }
        private void OnHealthChanged(float current, float maximum)
        {
            if (current > 0 && current < _previousHealth) Play("Hurt", 1);
            _previousHealth = current;
        }

        public void Attack() => Play("Attack", 0);
        public void Fuse(float duration) => Bus?.Play(GetInstanceID(), transform.position, "CreeperFuse", 2, duration);
        public void CancelFuse() => Bus?.StopFuse(GetInstanceID());
        public void Death() { if (!_exploded) Play("Death", 3); }
        public void Explode(float radius)
        {
            _exploded = true;
            Bus?.Play(GetInstanceID(), transform.position, "CreeperExplosion", 4);
            var prefab = Resources.Load<GameObject>("MobFeedback/CreeperExplosion");
            if (prefab == null) return;
            Explosions.RemoveAll(effect => effect == null);
            if (Explosions.Count >= 4)
            {
                Destroy(Explosions[0]);
                Explosions.RemoveAt(0);
            }
            var effect = Instantiate(prefab, transform.position + Vector3.up * 0.5f, Quaternion.identity);
            Explosions.Add(effect);
            effect.transform.localScale = Vector3.one * Mathf.Clamp(radius / 2f, 0.5f, 3f);
            Destroy(effect, 2f);
        }

        private void Play(string action, int priority) =>
            Bus?.Play(GetInstanceID(), transform.position, _type + action, priority);
    }
}
