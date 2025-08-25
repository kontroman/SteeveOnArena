using UnityEngine;
using System;
using MineArena.Basics;
using MineArena.Game.UI;
using MineArena.Interfaces;
using MineArena.Structs;

namespace MineArena.Game.Health
{
    public class Health : MonoBehaviour, IProgressBar, IDamageable
    {
        [SerializeField] private float _currentHealth;
        [SerializeField] private float _maxHealth;

        public event Action<float, float> OnHealthChanged;

        public float MaxValue => _maxHealth;
        public float CurrentValue => _currentHealth;

        private void Start()
        {
            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
        }

        public void ChangeValue(float value)
        {
            _currentHealth += value;
            _currentHealth = DetermineValue(_currentHealth);

            if (_currentHealth >= _maxHealth)
                _currentHealth = _maxHealth;

            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);

            if (_currentHealth <= 0)
                Destroy(gameObject);
        }

        private float DetermineValue(float currentValue)
            => Mathf.Clamp(currentValue, Constants.PlayerSettings.MinHealth, _maxHealth);

        public void TakeDamage(DamageData damageData)
        {
            ChangeValue(-damageData.Damage);
        }
    }
}