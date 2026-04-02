using MineArena.Commands;
using MineArena.ObjectPools;
using MineArena.Structs;
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

        private Transform _target;
        private DamageData _damageData;

        public void SetParameters(Transform target, DamageData damageData)
        {
            CancelInvoke(nameof(ReturnToPool));
            _damageData = damageData;
            _target = target;

            if (_target != null)
                transform.LookAt(_target);

            Invoke(nameof(ReturnToPool), destroyDelay);
        }

        private void Update()
        {
            transform.position += transform.forward * speed * Time.deltaTime;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_target != null)
            {
                var otherTransform = other.transform;
                if (otherTransform == _target || otherTransform.IsChildOf(_target))
                    OnHit();

                return;
            }

            if (other.CompareTag("Player"))
                OnHit();
        }

        private void OnHit()
        {
            CancelInvoke(nameof(ReturnToPool));
            var damageCommand = ScriptableObject.CreateInstance<DamageCommand>();
            damageCommand.Execute(_damageData);
            ReturnToPool();
        }

        private void ReturnToPool()
        {
            CancelInvoke(nameof(ReturnToPool));
            if (ObjectPoolsManager.Instance != null)
                ObjectPoolsManager.Instance.Release<Projectile>(gameObject);
        }

        private void OnDisable()
        {
            CancelInvoke(nameof(ReturnToPool));
            _target = null;
            _damageData = default;
        }
    }
}
