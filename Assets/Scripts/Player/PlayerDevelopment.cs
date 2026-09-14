using System;
using Devotion.SDK.Controllers;
using Devotion.SDK.Services.SaveSystem.Progress;
using MineArena.Controllers;
using MineArena.Game.Health;
using MineArena.Interfaces;
using UnityEngine;

namespace MineArena.PlayerSystem
{
    public enum PlayerAttribute { Health, Movement, Attack, Luck, Defense }

    public sealed class PlayerDevelopment : MonoBehaviour, IDefenseProvider
    {
        public static event Action Changed;
        public const float HealthPerPoint = 2f;
        public const float MovementPerPoint = .005f;
        public const float AttackPerPoint = .01f;
        public const float LuckPerPoint = .005f;
        // One defense point contributes 1/100 to the incoming-damage divisor.
        public const float DefensePerPoint = .01f;
        private int[] _ranks = new int[5];
        private Health _health;
        private float _baseHealth;
        public static PlayerDataProgress Progress => GameRoot.Instance != null ? GameRoot.PlayerProgress?.PlayerDataProgress : null;
        public static PlayerDevelopment Current => Player.Instance != null ? Player.Instance.GetComponent<PlayerDevelopment>() : null;
        public int Rank(PlayerAttribute attribute) => _ranks[(int)attribute];
        public float MovementMultiplier => 1f + MovementPerPoint * Rank(PlayerAttribute.Movement);
        public float AttackMultiplier => 1f + AttackPerPoint * Rank(PlayerAttribute.Attack);
        public float CriticalChance => LuckPerPoint * Rank(PlayerAttribute.Luck);
        public float ModifyIncomingDamage(float damage) => damage / (1f + DefensePerPoint * Rank(PlayerAttribute.Defense));
        public float ModifyAttack(float damage) => damage * AttackMultiplier * (UnityEngine.Random.value < CriticalChance ? 1.5f : 1f);

        private void Awake()
        {
            _health = GetComponent<Health>();
            _baseHealth = _health != null ? _health.MaxValue : 100f;
        }
        private void OnEnable() { Player.ExperienceInitialized += Loaded; Apply(); }
        private void OnDisable() => Player.ExperienceInitialized -= Loaded;
        private void Loaded(PlayerExperience experience) => Apply();
        public void Apply()
        {
            _ranks = Progress?.CopyDevelopment() ?? new int[5];
            if (_health != null) _health.SetMaximumHealth(_baseHealth + HealthPerPoint * Rank(PlayerAttribute.Health));
            Changed?.Invoke();
        }
        public static bool Save(int[] ranks)
        {
            if (Progress == null || !Progress.TrySaveDevelopment(ranks)) return false;
            Current?.Apply();
            return true;
        }
    }
}
