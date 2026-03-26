using System;
using System.Collections;
using MineArena.Interfaces;
using UnityEngine;

namespace MineArena.AI
{
    public class MobAnimationController : MonoBehaviour, IMobComponent
    {
        private static readonly int WalkHash = Animator.StringToHash("Walking");
        private static readonly int AttackHash = Animator.StringToHash("Attack");
        private static readonly int DeathHash = Animator.StringToHash("Death");

        [Header("Animator")]
        [SerializeField] private Animator _animator;
        [SerializeField] private float _moveStartThreshold = 0.05f;
        [SerializeField] private float _moveStopThreshold = 0.02f;

        [Header("Death Animation")]
        [SerializeField] private float _deathAnimationDuration = 1f;
        [SerializeField] private float _sinkDelay = 1f;
        [SerializeField] private float _sinkDuration = 0.75f;
        [SerializeField] private float _sinkDepth = 2f;

        private Coroutine _deathRoutine;
        private bool _isDead;
        private bool _isMoving;

        public event Action AttackKeyframeReached;
        public event Action DeathSequenceFinished;

        private void OnEnable()
        {
            _isDead = false;
            StopAllCoroutines();

            if (_animator != null)
            {
                _animator.Rebind();
                _animator.Update(0f);
                _animator.SetBool(WalkHash, false);
            }

            _isMoving = false;
        }

        public void SetParameters(MobPreset preset)
        {
        }

        public void UpdateMoveState(Vector3 velocity, bool isStopped)
        {
            if (_isDead || _animator == null) return;

            var speedSqr = velocity.sqrMagnitude;
            var startSqr = _moveStartThreshold * _moveStartThreshold;
            var stopSqr = _moveStopThreshold * _moveStopThreshold;

            if (_isMoving)
            {
                if (isStopped || speedSqr < stopSqr)
                    SetWalking(false);
            }
            else
            {
                if (!isStopped && speedSqr > startSqr)
                    SetWalking(true);
            }
        }

        public void PlayAttack()
        {
            if (_isDead || _animator == null) return;

            _animator.ResetTrigger(AttackHash);
            _animator.SetTrigger(AttackHash);
        }

        public void PlayDeath()
        {
            if (_isDead) return;

            _isDead = true;

            if (_animator != null)
            {
                _animator.SetBool(WalkHash, false);
                _animator.ResetTrigger(AttackHash);
                _animator.SetTrigger(DeathHash);
            }

            if (_deathRoutine != null)
                StopCoroutine(_deathRoutine);

            _deathRoutine = StartCoroutine(DeathRoutine());
        }

        private IEnumerator DeathRoutine()
        {
            if (_deathAnimationDuration > 0f)
                yield return new WaitForSeconds(_deathAnimationDuration);

            yield return new WaitForSeconds(_sinkDelay);

            Vector3 startPosition = transform.position;
            Vector3 targetPosition = startPosition + Vector3.down * _sinkDepth;
            float elapsed = 0f;

            while (elapsed < _sinkDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / _sinkDuration);
                transform.position = Vector3.Lerp(startPosition, targetPosition, t);
                yield return null;
            }

            DeathSequenceFinished?.Invoke();
        }

        // Call from the attack animation keyframe event
        public void HandleAttackKeyframe()
        {
            if (_isDead) return;
            AttackKeyframeReached?.Invoke();
        }

        private void OnDisable()
        {
            AttackKeyframeReached = null;
            DeathSequenceFinished = null;
            _deathRoutine = null;
        }

        private void SetWalking(bool isWalking)
        {
            if (_isMoving == isWalking) return;
            _isMoving = isWalking;
            _animator.SetBool(WalkHash, _isMoving);
        }
    }
}
