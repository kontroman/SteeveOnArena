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
        
        public void SetParameters(MobPreset preset)
        {
            _preset = preset;
            _maxHealth = preset.MaxHealth;
            _currentHealth = _maxHealth;
        }

        protected override void Die()
        {
            ObjectPoolsManager.Instance.Release<Mob>(gameObject);
            _currentHealth = _maxHealth;
            
            QuestMessages.QuestTargetTaken.Publish((_preset,1)); // test quest
        }
    }
}
