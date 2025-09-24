using MineArena.Game.Health;
using MineArena.Interfaces;
using MineArena.ObjectPools;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MineArena.AI
{
    public class MobHealth : Health, IMobComponent
    {
        public void SetParameters(MobPreset preset)
        {
            _maxHealth = preset.MaxHealth;
            _currentHealth = _maxHealth;
        }

        protected override void Die()
        {
            ObjectPoolsManager.Instance.Release<Mob>(gameObject);
            _currentHealth = _maxHealth;
        }
    }
}
