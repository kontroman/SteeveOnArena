using System;
using System.Reflection;
using UnityEngine;

namespace MineArena.VFX
{
    public class VfxInstance : MonoBehaviour
    {
        private static readonly Type VisualEffectType = Type.GetType("UnityEngine.VFX.VisualEffect, Unity.VisualEffectGraph.Runtime");
        private static readonly MethodInfo VisualEffectPlayMethod = VisualEffectType?.GetMethod("Play", Type.EmptyTypes);
        private static readonly MethodInfo VisualEffectStopMethod = VisualEffectType?.GetMethod("Stop", Type.EmptyTypes);
        private static readonly MethodInfo VisualEffectReinitMethod = VisualEffectType?.GetMethod("Reinit", Type.EmptyTypes);

        private ParticleSystem[] _particleSystems;
        private ParticleSystemRenderer[] _particleRenderers;
        private Component[] _visualEffects;
        private MaterialPropertyBlock _propertyBlock;
        private VFXManager _manager;
        private VfxEntry _entry;
        private Transform _owner;
        private Vector3 _followLocalOffset;
        private bool _followOwner;
        private float _releaseAt;
        private bool _fallbackLifetimeExpired;
        private bool _isStopping;

        public bool IsActive { get; private set; }
        public Transform Owner => _owner;
        public VfxEntry Entry => _entry;

        public void CacheComponents()
        {
            _particleSystems = GetComponentsInChildren<ParticleSystem>(true);
            _particleRenderers = GetComponentsInChildren<ParticleSystemRenderer>(true);

            if (VisualEffectType == null)
            {
                _visualEffects = Array.Empty<Component>();
                return;
            }

            _visualEffects = GetComponentsInChildren(VisualEffectType, true);
        }

        public void Play(VFXManager manager, VfxEntry entry, Transform owner, bool followOwner, Vector3 followLocalOffset, Color? colorOverride)
        {
            _manager = manager;
            _entry = entry;
            _owner = owner;
            _followOwner = followOwner;
            _followLocalOffset = followLocalOffset;
            _isStopping = false;
            IsActive = true;
            _fallbackLifetimeExpired = entry.FallbackLifetime <= 0f;
            _releaseAt = entry.FallbackLifetime > 0f ? Time.time + entry.FallbackLifetime : Time.time;

            if (_particleSystems == null)
                CacheComponents();

            if (colorOverride.HasValue)
                ApplyParticleColor(colorOverride.Value);

            RestartParticleSystems();
            RestartVisualEffects();
        }

        public void Stop()
        {
            if (!IsActive || _isStopping)
                return;

            _isStopping = true;
            StopParticleSystems();
            StopVisualEffects();

            if (_entry == null || !_entry.AutoRelease)
                _manager.Release(this);
        }

        private void Update()
        {
            if (!IsActive || _entry == null)
                return;

            if (_followOwner)
            {
                if (_owner == null)
                {
                    _manager.Release(this);
                    return;
                }

                transform.SetPositionAndRotation(_owner.TransformPoint(_followLocalOffset), _owner.rotation * _entry.RotationOffset);
            }

            if (!_entry.AutoRelease)
                return;

            if (!_fallbackLifetimeExpired && Time.time >= _releaseAt)
                _fallbackLifetimeExpired = true;

            if (CanAutoRelease())
                _manager.Release(this);
        }

        public void MarkReleased()
        {
            IsActive = false;
            _manager = null;
            _entry = null;
            _owner = null;
            _followOwner = false;
            _isStopping = false;
        }

        private void RestartParticleSystems()
        {
            if (_particleSystems == null)
                return;

            foreach (var particleSystem in _particleSystems)
            {
                if (particleSystem == null)
                    continue;

                particleSystem.Clear(true);
                particleSystem.Play(true);
            }
        }

        private void ApplyParticleColor(Color color)
        {
            color = NormalizeVfxTint(color);

            if (_particleSystems == null)
                return;

            foreach (var particleSystem in _particleSystems)
            {
                if (particleSystem == null)
                    continue;

                var main = particleSystem.main;
                main.startColor = color;
            }

            ApplyRendererColor(color);
        }

        private static Color NormalizeVfxTint(Color color)
        {
            float max = Mathf.Max(color.r, color.g, color.b);

            if (max > 0.001f && max < 0.85f)
            {
                float multiplier = 0.85f / max;
                color.r = Mathf.Clamp01(color.r * multiplier);
                color.g = Mathf.Clamp01(color.g * multiplier);
                color.b = Mathf.Clamp01(color.b * multiplier);
            }

            color.a = 1f;
            return color;
        }

        private void ApplyRendererColor(Color color)
        {
            if (_particleRenderers == null)
                return;

            _propertyBlock ??= new MaterialPropertyBlock();

            foreach (var particleRenderer in _particleRenderers)
            {
                if (particleRenderer == null)
                    continue;

                particleRenderer.GetPropertyBlock(_propertyBlock);
                _propertyBlock.SetColor("_BaseColor", color);
                _propertyBlock.SetColor("_Color", color);
                particleRenderer.SetPropertyBlock(_propertyBlock);
            }
        }

        private void StopParticleSystems()
        {
            if (_particleSystems == null)
                return;

            foreach (var particleSystem in _particleSystems)
            {
                if (particleSystem != null)
                    particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }

        private void RestartVisualEffects()
        {
            if (_visualEffects == null)
                return;

            foreach (var visualEffect in _visualEffects)
            {
                if (visualEffect == null)
                    continue;

                VisualEffectReinitMethod?.Invoke(visualEffect, null);
                VisualEffectPlayMethod?.Invoke(visualEffect, null);
            }
        }

        private void StopVisualEffects()
        {
            if (_visualEffects == null)
                return;

            foreach (var visualEffect in _visualEffects)
            {
                if (visualEffect != null)
                    VisualEffectStopMethod?.Invoke(visualEffect, null);
            }
        }

        private bool CanAutoRelease()
        {
            bool hasParticleSystem = _particleSystems != null && _particleSystems.Length > 0;

            if (hasParticleSystem)
            {
                foreach (var particleSystem in _particleSystems)
                {
                    if (particleSystem != null && particleSystem.IsAlive(true))
                        return false;
                }

                return true;
            }

            return _fallbackLifetimeExpired;
        }
    }
}
