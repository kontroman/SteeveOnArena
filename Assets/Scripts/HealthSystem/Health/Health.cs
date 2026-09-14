using UnityEngine;
using System;
using MineArena.Basics;
using MineArena.Game.UI;
using MineArena.Interfaces;
using MineArena.Structs;
using Devotion.SDK.Controllers;
using MineArena.Controllers;
using MineArena.PlayerSystem;
using MineArena.VFX;

namespace MineArena.Game.Health
{
    public class Health : MonoBehaviour, IProgressBar, IDamageable
    {
        [SerializeField] protected float _currentHealth;
        
        [SerializeField] protected float _maxHealth;
        [SerializeField] private VfxId _hitVfxId = VfxId.Hit;

        public event Action<float, float> OnHealthChanged;

        public float MaxValue => _maxHealth;
        public float CurrentValue => _currentHealth;

        private void Start()
        {
            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
        }

        public void ChangeValue(float value)
        {
            // Ordinary healing must not revive a dead character outside the revival flow.
            if (_currentHealth <= 0f) return;
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
            if (damageData.Damage <= 0f || float.IsNaN(damageData.Damage)) return;
            if (_currentHealth <= 0f) return;
            var deathFlow = GetComponent<PlayerDeathFlow>();
            if (deathFlow != null && deathFlow.IsProtected) return;
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

            if (damageToApply > 0f)
                PlayHitVfx();

            // The introductory fight teaches attacking without stranding a first-time player in the death animation.
            if (MineArena.Managers.TutorialService.Expedition && Player.Instance != null && gameObject == Player.Instance.gameObject)
                damageToApply = Mathf.Min(damageToApply, Mathf.Max(0, _currentHealth - 1));
            ChangeValue(-damageToApply);
        }

        private void PlayHitVfx()
        {
            if (_hitVfxId == VfxId.None || GameRoot.Instance == null)
                return;

            var vfxManager = GameRoot.GetManager<VFXManager>();
            if (vfxManager == null)
                return;

            vfxManager.Play(_hitVfxId, GetHitVfxPosition(), Quaternion.identity);
        }

        private Vector3 GetHitVfxPosition()
        {
            var colliders = GetComponentsInChildren<Collider>();
            foreach (var hitCollider in colliders)
            {
                if (hitCollider != null && !hitCollider.isTrigger)
                    return hitCollider.bounds.center;
            }

            var renderer = GetComponentInChildren<Renderer>();
            return renderer != null ? renderer.bounds.center : transform.position;
        }

        public void RestoreFullHealth() => SetCurrentValue(_maxHealth, false);

        public void SetMaximumHealth(float maximum)
        {
            _maxHealth = Mathf.Max(1f, maximum);
            // Reallocating points never heals or revives the player.
            _currentHealth = Mathf.Min(_currentHealth, _maxHealth);
            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
        }

        protected virtual void Die()
        {
            if (Player.Instance != null && gameObject == Player.Instance.gameObject)
            {
                Player.Instance.GetComponentFromList<PlayerMovement>()?.SetDead();
                Player.Instance.GetComponentFromList<PlayerAttack>()?.SetComponentEnable(false);
                Player.Instance.GetComponentFromList<PlayerAnimatorController>()?.TriggerDeath();

                Player.Instance.GetComponent<PlayerDeathFlow>()?.BeginDeath();
                return;
            }

            Destroy(gameObject);
        }
    }
}
