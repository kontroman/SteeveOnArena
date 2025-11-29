using UnityEngine;
using System;
using MineArena.Basics;
using MineArena.Game.UI;
using MineArena.Interfaces;
using MineArena.Structs;
using Devotion.SDK.Controllers;
using MineArena.Controllers;

namespace MineArena.Game.Health
{
    public class Health : MonoBehaviour, IProgressBar, IDamageable
    {
        [SerializeField] protected float _currentHealth;
        
        [SerializeField] protected float _maxHealth;

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
                Die();
        }

        private float DetermineValue(float currentValue)
            => Mathf.Clamp(currentValue, Constants.PlayerSettings.MinHealth, _maxHealth);

        public void TakeDamage(DamageData damageData)
        {
#if UNITY_EDITOR || DEVOTION_GODMODE
            var config = GameRoot.GameConfig;
            if (config != null && config.GodModeInvulnerability)
            {
                if (gameObject == Player.Instance.gameObject)
                    return;
            }
#endif
            var damageToApply = damageData.Damage;

            if (damageToApply > 0f)
            {
                foreach (var provider in GetComponents<IDefenseProvider>())
                {
                    damageToApply = provider.ModifyIncomingDamage(damageToApply);
                }
            }

            ChangeValue(-damageToApply);
        }

        protected virtual void Die()
        {
            Destroy(gameObject);
        }
    }
}
