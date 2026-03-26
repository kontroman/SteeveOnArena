using MineArena.Game.Health;
using MineArena.Interfaces;
using MineArena.ObjectPools;
using System.Collections;
using System.Collections.Generic;
using MineArena.Messages;
using UnityEngine;

namespace MineArena.AI
{
    public class MobHealth : Health, IMobComponent
    {
        private MobPreset _preset;
        private MobAnimationController _mobAnimator;
        private MobMovement _mobMovement;
        private MobCombat _mobCombat;

        private void Awake()
        {
            _mobAnimator = GetComponent<MobAnimationController>();
            _mobMovement = GetComponent<MobMovement>();
            _mobCombat = GetComponent<MobCombat>();
        }
        
        public void SetParameters(MobPreset preset)
        {
            _preset = preset;
            _maxHealth = preset.MaxHealth;
            _currentHealth = _maxHealth;
            _mobAnimator?.SetParameters(preset);
        }

        protected override void Die()
        {
            AchievementMessages.AchievementTargetTaken.Publish((_preset, 1));

            if (_mobCombat != null)
                _mobCombat.CancelAttack();

            if (_mobMovement != null)
                _mobMovement.Stop();

            if (_mobAnimator != null)
            {
                _mobAnimator.DeathSequenceFinished += HandleDeathSequenceFinished;
                _mobAnimator.PlayDeath();
                return;
            }

            HandleDeathSequenceFinished();
        }

        private void HandleDeathSequenceFinished()
        {
            if (_mobAnimator != null)
                _mobAnimator.DeathSequenceFinished -= HandleDeathSequenceFinished;

            _currentHealth = _maxHealth;
            ObjectPoolsManager.Instance.Release<Mob>(gameObject);
        }
    }
}
