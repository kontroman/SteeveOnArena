using UnityEngine;
using System;
using Devotion.Basics;
using Divotion.Game.UI;

namespace Divotion.Game.Health
{
    public class Health : MonoBehaviour, IProgressBar
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
    }
}