using UnityEngine;
using MineArena.Game.Health;

namespace MineArena.PlayerSystem
{
    /// <summary>
    /// Centralized animator driver for the player. Caches parameter hashes and exposes intent-based methods.
    /// Health decrease automatically triggers the damage animation; death/victory hooks stay manual.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class PlayerAnimatorController : MonoBehaviour, IPlayerAnimator
    {
        [Header("Animator parameter names")]
        [SerializeField] private string _runBoolParameter = "isRunning";
        [SerializeField] private string _attackTriggerParameter = "Attack";
        [SerializeField] private string _handItemParameter = "HandItemState";
        [SerializeField] private string _miningTriggerParameter = "Mining";
        [SerializeField] private string _chestOpeningTriggerParameter = "ChestOpening";
        [SerializeField] private string _damageTriggerParameter = "Damage";
        [SerializeField] private string _deathTriggerParameter = "Death";
        [SerializeField] private string _victoryTriggerParameter = "Victory";

        [Header("Animation states")]
        [SerializeField] private string _miningStateName = "PlayerMiningAnimation";
        [SerializeField] private int _miningLayer = 0;

        [Header("Optional references")]
        [SerializeField] private Health _health;

        public Animator Animator => _animator;

        private Animator _animator;

        private int _runParamHash;
        private int _attackTriggerHash;
        private int _handItemParamHash;
        private int _miningTriggerHash;
        private int _chestOpeningTriggerHash;
        private int _damageTriggerHash;
        private int _deathTriggerHash;
        private int _victoryTriggerHash;

        private float _lastHealth;

        private void Awake()
        {
            _animator = GetComponent<Animator>();

            _runParamHash = Animator.StringToHash(_runBoolParameter);
            _attackTriggerHash = Animator.StringToHash(_attackTriggerParameter);
            _handItemParamHash = Animator.StringToHash(_handItemParameter);
            _miningTriggerHash = Animator.StringToHash(_miningTriggerParameter);
            _chestOpeningTriggerHash = Animator.StringToHash(_chestOpeningTriggerParameter);
            _damageTriggerHash = Animator.StringToHash(_damageTriggerParameter);
            _deathTriggerHash = Animator.StringToHash(_deathTriggerParameter);
            _victoryTriggerHash = Animator.StringToHash(_victoryTriggerParameter);

            if (_health == null)
                _health = GetComponent<Health>();

            if (_health != null)
            {
                _lastHealth = _health.CurrentValue;
                _health.OnHealthChanged += HandleHealthChanged;
            }
        }

        private void OnDestroy()
        {
            if (_health != null)
                _health.OnHealthChanged -= HandleHealthChanged;
        }

        private void HandleHealthChanged(float current, float max)
        {
            if (current < _lastHealth && current > 0f)
            {
                TriggerDamage();
            }

            _lastHealth = current;
        }

        public bool IsRunning()
        {
            if (_animator == null)
                return false;

            return _animator.GetBool(_runParamHash);
        }

        public void SetRunning(bool isRunning)
        {
            _animator?.SetBool(_runParamHash, isRunning);
        }

        public void TriggerAttack()
        {
            if (_animator == null)
                return;

            _animator.ResetTrigger(_attackTriggerHash);
            _animator.SetTrigger(_attackTriggerHash);
        }

        public void TriggerDamage()
        {
            _animator?.SetTrigger(_damageTriggerHash);
        }

        public void TriggerDeath()
        {
            _animator?.SetTrigger(_deathTriggerHash);
        }

        public void TriggerVictory()
        {
            _animator?.SetTrigger(_victoryTriggerHash);
        }

        public void PlayMiningAnimation(string stateName, int layer)
        {
            if (_animator == null)
                return;

            if (!string.IsNullOrWhiteSpace(stateName))
                _animator.Play(stateName, layer, 0f);
            else
                _animator.SetTrigger(_miningTriggerHash);
        }

        public void ResetMiningAnimation()
        {
            _animator?.ResetTrigger(_miningTriggerHash);
        }

        public void TriggerChestOpening()
        {
            _animator?.SetTrigger(_chestOpeningTriggerHash);
        }

        public void ResetChestOpening()
        {
            _animator?.ResetTrigger(_chestOpeningTriggerHash);
        }

        public void SetHandItemState(HandItemType type)
        {
            _animator?.SetInteger(_handItemParamHash, (int)type);
        }
    }
}
