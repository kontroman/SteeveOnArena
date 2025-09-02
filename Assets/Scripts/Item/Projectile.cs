using MineArena.Commands;
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
        [SerializeField] private float destroyDelay = 0.1f;

        private Transform _target;
        private DamageData _damageData;

        private void Start()
        {
            transform.LookAt(_target);
        }

        public void SetParameters(Transform target, DamageData damageData)
        {
            _damageData = damageData;
            _target = target;
        }

        private void Update()
        {
            transform.position += transform.forward * speed * Time.deltaTime;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
                OnHit();
        }

        private void OnHit()
        {
            var damageCommand = ScriptableObject.CreateInstance<DamageCommand>();
            damageCommand.Execute(_damageData);
            Destroy(gameObject);
        }
    }
}
