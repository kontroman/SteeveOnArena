using MineArena.Commands;
using MineArena.Interfaces;
using MineArena.ObjectPools;
using MineArena.Structs;
using MineArena.Networking;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MineArena
{
    public class Projectile : MonoBehaviour
    {
        [Header("Projectile Settings")]
        [SerializeField] private float speed = 10f;
        [SerializeField] private float destroyDelay = 10f;
        [SerializeField] private float stickDepth = 0.08f;
        [SerializeField] private float stuckLifetime = 2f;
        [SerializeField, Min(0f)] private float enemySpeed = 18f;
        [SerializeField, Min(0f)] private float gravity;
        [SerializeField, Min(0f)] private float splashRadius;
        [SerializeField, Min(0.01f)] private float sweepRadius = 0.08f;

        private Vector3 _velocity;
        private bool _enemyShot;

        private Transform _target;
        private DamageData _damageData;
        private LayerMask _attackableLayers;
        private Transform _owner;
        private bool _hasHit;
        private bool _isMoving;
        private bool _stickOnCollision;
        private string _networkWeaponId;

        public void SetParameters(Transform target, DamageData damageData, Transform owner = null)
        {
            CancelInvoke(nameof(ReturnToPool));
            _damageData = damageData;
            _target = target;
            _enemyShot = true;
            _attackableLayers = default;
            _owner = owner;
            _hasHit = false;
            _isMoving = true;
            _stickOnCollision = splashRadius <= 0f;
            _networkWeaponId = null;
            SetCollidersEnabled(true);

            if (_target != null)
            {
                Vector3 delta = AI.CombatTargeting.AimPoint(_target) - transform.position;
                float launchSpeed = enemySpeed > 0 ? enemySpeed : speed;
                if (gravity > 0)
                {
                    float flightTime = Mathf.Max(0.25f, new Vector2(delta.x, delta.z).magnitude / launchSpeed);
                    _velocity = delta / flightTime + Vector3.up * (0.5f * gravity * flightTime);
                }
                else _velocity = delta.normalized * launchSpeed;
                if (_velocity.sqrMagnitude > 0.001f) transform.rotation = Quaternion.LookRotation(_velocity);
            }

            Invoke(nameof(ReturnToPool), destroyDelay);
        }

        public void SetParameters(
            Vector3 direction,
            DamageData damageData,
            LayerMask attackableLayers,
            Transform owner = null,
            string networkWeaponId = null)
        {
            CancelInvoke(nameof(ReturnToPool));
            _damageData = damageData;
            _target = null;
            _enemyShot = false;
            _attackableLayers = attackableLayers;
            _owner = owner;
            _hasHit = false;
            _isMoving = true;
            _stickOnCollision = true;
            _networkWeaponId = networkWeaponId;
            SetCollidersEnabled(true);

            if (direction.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(direction.normalized);

            _velocity = transform.forward * speed;

            Invoke(nameof(ReturnToPool), destroyDelay);
        }

        private void Update()
        {
            Step(Time.deltaTime);
        }

        private void Step(float deltaTime)
        {
            if (!_isMoving)
                return;
            if (_enemyShot && (_target == null || !_target.gameObject.activeInHierarchy))
            {
                ReturnToPool();
                return;
            }
            // Substeps also follow the potion's arc during a long frame.
            int steps = Mathf.Max(1, Mathf.CeilToInt(deltaTime / 0.02f));
            float dt = deltaTime / steps;
            for (int i = 0; i < steps && _isMoving; i++)
            {
                Vector3 acceleration = _target != null ? Vector3.down * gravity : Vector3.zero;
                Vector3 delta = _velocity * dt + acceleration * (0.5f * dt * dt);
                _velocity += acceleration * dt;
                Vector3 origin = transform.position;
                foreach (var body in Physics.OverlapSphere(origin, sweepRadius, ~0, QueryTriggerInteraction.Collide))
                {
                    OnTriggerEnter(body);
                    if (_hasHit || !_isMoving) return;
                }
                var hits = Physics.SphereCastAll(origin, sweepRadius, delta.normalized, delta.magnitude, ~0, QueryTriggerInteraction.Collide);
                Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
                foreach (var hit in hits)
                {
                    transform.position = origin + delta.normalized * hit.distance;
                    OnTriggerEnter(hit.collider);
                    if (_hasHit || !_isMoving) return;
                }
                transform.position = origin + delta;
                if (_velocity.sqrMagnitude > 0.001f) transform.rotation = Quaternion.LookRotation(_velocity);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!_isMoving || _hasHit || other == null || IsOwner(other.transform) || other.GetComponentInParent<Projectile>() != null)
                return;

            if (_target != null)
            {
                var otherTransform = other.transform;
                bool hitTarget = otherTransform == _target || otherTransform.IsChildOf(_target);
                if (!hitTarget && other.isTrigger) return;
                if (splashRadius > 0f)
                {
                    Splash(other);
                }
                else if (hitTarget) OnHit(other);
                else TryStickToCollider(other);

                return;
            }

            if (_attackableLayers.value != 0)
            {
                if (TryHitNetworkPlayer(other))
                    return;

                if (!other.TryGetComponent<IDamageable>(out var damageable))
                    damageable = other.GetComponentInParent<IDamageable>();

                if (damageable != null && IsDamageableInLayerMask(damageable, other, _attackableLayers))
                {
                    OnHit(damageable, other);
                    return;
                }

                TryStickToCollider(other);

                return;
            }

            if (other.CompareTag("Player"))
            {
                OnHit(other);
                return;
            }

            TryStickToCollider(other);
        }

        private bool TryHitNetworkPlayer(Collider hitCollider)
        {
            if (string.IsNullOrEmpty(_networkWeaponId) || hitCollider == null)
                return false;

            var view = hitCollider.GetComponentInParent<NetworkPlayerView>();
            if (view == null || view.IsLocalPlayer || !view.IsAlive)
                return false;

            var manager = NetworkClientManager.Instance;
            if (manager == null || !manager.IsConnected)
                return false;

            CancelInvoke(nameof(ReturnToPool));
            _hasHit = true;
            manager.SendDamageRequest(
                view.PlayerId,
                Mathf.Max(1, Mathf.RoundToInt(_damageData.Damage)),
                _networkWeaponId,
                hitCollider.ClosestPoint(transform.position));

            if (_stickOnCollision && TryStickToCollider(hitCollider))
                return true;

            ReturnToPool();
            return true;
        }

        private void OnHit(Collider hitCollider)
        {
            OnHit(_damageData.Target, hitCollider);
        }

        private void OnHit(IDamageable target, Collider hitCollider)
        {
            CancelInvoke(nameof(ReturnToPool));
            _hasHit = true;

            if (target != null)
                _damageData = new DamageData(_damageData.Damage, target);

            _damageData.Target?.TakeDamage(_damageData);

            if (_stickOnCollision && TryStickToCollider(hitCollider))
                return;

            ReturnToPool();
        }

        private bool TryStickToCollider(Collider hitCollider)
        {
            if (!_stickOnCollision || hitCollider == null || hitCollider.isTrigger)
                return false;

            CancelInvoke(nameof(ReturnToPool));
            _hasHit = true;
            _isMoving = false;
            transform.position -= transform.forward * stickDepth;
            transform.SetParent(hitCollider.transform, true);
            SetCollidersEnabled(false);
            Invoke(nameof(ReturnToPool), stuckLifetime);
            return true;
        }

        private void ReturnToPool()
        {
            _isMoving = false;
            _hasHit = true;
            CancelInvoke(nameof(ReturnToPool));
            Release();
        }

        protected virtual void Release()
        {
            var projectileType = GetType();

            if (ObjectPoolsManager.Instance != null && ObjectPoolsManager.Instance.HasPool(projectileType))
            {
                ObjectPoolsManager.Instance.Release<Projectile>(gameObject);
                return;
            }

            Destroy(gameObject);
        }

        protected virtual void OnImpact() { }

        private void OnDisable()
        {
            CancelInvoke(nameof(ReturnToPool));
            _target = null;
            _owner = null;
            _attackableLayers = default;
            _damageData = default;
            _hasHit = false;
            _isMoving = false;
            _stickOnCollision = false;
            _networkWeaponId = null;
            _velocity = Vector3.zero;
            transform.SetParent(null, true);
            SetCollidersEnabled(true);
        }

        private bool IsOwner(Transform other)
        {
            return _owner != null && (other == _owner || other.IsChildOf(_owner));
        }

        private void Splash(Collider hitCollider)
        {
            _hasHit = true;
            _isMoving = false;
            bool direct = hitCollider.transform == _target || hitCollider.transform.IsChildOf(_target);
            foreach (var body in _target.GetComponentsInChildren<Collider>())
            {
                if (!body.enabled || !body.gameObject.activeInHierarchy) continue;
                Vector3 point = body.ClosestPoint(transform.position);
                if (!direct && (Vector3.Distance(point, transform.position) > splashRadius ||
                    !AI.CombatTargeting.HasLineOfSight(transform.position, AI.CombatTargeting.AimPoint(_target), transform, _target))) continue;
                _damageData.Target?.TakeDamage(_damageData);
                break; // Multiple player colliders still receive only one hit.
            }
            OnImpact();
            ReturnToPool();
        }

        private static bool IsInLayerMask(int layer, LayerMask layerMask)
        {
            return (layerMask.value & (1 << layer)) != 0;
        }

        private static bool IsDamageableInLayerMask(IDamageable damageable, Collider fallbackCollider, LayerMask layerMask)
        {
            if (damageable is Component component)
                return IsInLayerMask(component.gameObject.layer, layerMask);

            return fallbackCollider != null && IsInLayerMask(fallbackCollider.gameObject.layer, layerMask);
        }

        private void SetCollidersEnabled(bool enabled)
        {
            var colliders = GetComponents<Collider>();
            foreach (var projectileCollider in colliders)
            {
                if (projectileCollider != null)
                    projectileCollider.enabled = enabled;
            }
        }
    }
}
