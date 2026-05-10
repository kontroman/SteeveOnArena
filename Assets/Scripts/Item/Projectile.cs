using MineArena.Commands;
using MineArena.Interfaces;
using MineArena.ObjectPools;
using MineArena.Structs;
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

        private Transform _target;
        private DamageData _damageData;
        private LayerMask _attackableLayers;
        private Transform _owner;
        private bool _hasHit;
        private bool _isMoving;
        private bool _stickOnCollision;

        public void SetParameters(Transform target, DamageData damageData)
        {
            CancelInvoke(nameof(ReturnToPool));
            _damageData = damageData;
            _target = target;
            _attackableLayers = default;
            _owner = null;
            _hasHit = false;
            _isMoving = true;
            _stickOnCollision = false;
            SetCollidersEnabled(true);

            if (_target != null)
                transform.LookAt(_target);

            Invoke(nameof(ReturnToPool), destroyDelay);
        }

        public void SetParameters(Vector3 direction, DamageData damageData, LayerMask attackableLayers, Transform owner = null)
        {
            CancelInvoke(nameof(ReturnToPool));
            _damageData = damageData;
            _target = null;
            _attackableLayers = attackableLayers;
            _owner = owner;
            _hasHit = false;
            _isMoving = true;
            _stickOnCollision = true;
            SetCollidersEnabled(true);

            if (direction.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(direction.normalized);

            Invoke(nameof(ReturnToPool), destroyDelay);
        }

        private void Update()
        {
            if (!_isMoving)
                return;

            transform.position += transform.forward * speed * Time.deltaTime;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_hasHit || IsOwner(other.transform))
                return;

            if (_target != null)
            {
                var otherTransform = other.transform;
                if (otherTransform == _target || otherTransform.IsChildOf(_target))
                    OnHit(other);

                return;
            }

            if (_attackableLayers.value != 0)
            {
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

            var damageCommand = ScriptableObject.CreateInstance<DamageCommand>();
            damageCommand.Execute(_damageData);

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
            CancelInvoke(nameof(ReturnToPool));
            var projectileType = GetType();

            if (ObjectPoolsManager.Instance != null && ObjectPoolsManager.Instance.HasPool(projectileType))
            {
                ObjectPoolsManager.Instance.Release<Projectile>(gameObject);
                return;
            }

            Destroy(gameObject);
        }

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
            transform.SetParent(null, true);
            SetCollidersEnabled(true);
        }

        private bool IsOwner(Transform other)
        {
            return _owner != null && (other == _owner || other.IsChildOf(_owner));
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
