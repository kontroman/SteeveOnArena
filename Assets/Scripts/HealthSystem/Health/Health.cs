using UnityEngine;
using System;
using MineArena.Basics;
using MineArena.Game.UI;
using MineArena.Interfaces;
using MineArena.Structs;
using Devotion.SDK.Controllers;
using MineArena.Controllers;
using MineArena.PlayerSystem;

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
            SetCurrentValue(_currentHealth + value);
        }

        public void SetCurrentValue(float value, bool triggerDeath = true)
        {
            var wasAlive = _currentHealth > 0f;
            _currentHealth = DetermineValue(value);
            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);

            if (triggerDeath && wasAlive && _currentHealth <= 0f)
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
                if (Player.Instance != null && gameObject == Player.Instance.gameObject)
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
            if (Player.Instance != null && gameObject == Player.Instance.gameObject)
            {
                Player.Instance.GetComponentFromList<PlayerMovement>()?.SetDead();
                Player.Instance.GetComponentFromList<PlayerAttack>()?.SetComponentEnable(false);
                Player.Instance.GetComponentFromList<PlayerAnimatorController>()?.TriggerDeath();

                // Destroy(gameObject);
                return;
            }

            Destroy(gameObject);
        }
    }
}
