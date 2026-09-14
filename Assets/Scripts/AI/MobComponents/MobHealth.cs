using MineArena.Game.Health;
using MineArena.Interfaces;
using MineArena.ObjectPools;
using System.Collections;
using System.Collections.Generic;
using System;
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
        private bool _deathHandled;
        private int _experienceReward = -1;
        public int ExperienceReward => _experienceReward >= 0 ? _experienceReward :
            MineArena.PlayerSystem.PlayerExperience.MonsterReward(_preset != null ? _preset.MaxHealth : _maxHealth);
        public void SetExperienceReward(int amount) => _experienceReward = Mathf.Max(0, amount);

        public static event Action<MobHealth> MobDied;

        private void OnEnable() { _deathHandled = false; _experienceReward = -1; }

        private void OnDisable()
        {
            if (_mobAnimator != null)
                _mobAnimator.DeathSequenceFinished -= HandleDeathSequenceFinished;
        }

        private void Awake()
        {
            _mobAnimator = GetComponent<MobAnimationController>();
            _mobMovement = GetComponent<MobMovement>();
            _mobCombat = GetComponent<MobCombat>();
        }
        
        public void SetParameters(MobPreset preset)
        {
            _deathHandled = false;
            _experienceReward = -1;
            _preset = preset;
            _maxHealth = preset.MaxHealth;
            if (MineArena.Managers.TutorialService.Expedition) _maxHealth = Mathf.Min(_maxHealth, 25f);
            SetCurrentValue(_maxHealth, false);
            _mobAnimator?.SetParameters(preset);
        }

        protected override void Die()
        {
            if (_deathHandled) return;
            _deathHandled = true;
            MineArena.Controllers.Player.Instance?.Experience?.AddExperience(ExperienceReward);
            GetComponent<MobFeedback>()?.Death();
            var drops = GetComponent<MineArena.Drop.Dropable>();
            if (drops != null && drops.DropOnDeath) drops.DropItems();
            MobDied?.Invoke(this);
            AchievementMessages.AchievementTargetTaken.Publish((_preset, 1));

            if (_mobCombat != null)
                _mobCombat.HandleDeath();

            if (_mobMovement != null)
                _mobMovement.HandleDeath();

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

            ObjectPoolsManager.Instance.Release<Mob>(gameObject);
        }
    }
}
