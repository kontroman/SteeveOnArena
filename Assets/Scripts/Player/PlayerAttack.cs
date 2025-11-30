using UnityEngine;
using System.Collections;
using MineArena.Interfaces;
using MineArena.Structs;
using MineArena.Commands;
using Devotion.SDK.Helpers;
using MineArena.Managers;
using Devotion.SDK.Controllers;
using MineArena.Messages.MessageService;
using MineArena.Messages;
using MineArena.Controllers;
using MineArena.PlayerSystem;
using UnityEngine.EventSystems;

namespace MineArena.PlayerSystem
{
    [RequireComponent(typeof(Animator))]
    public class PlayerAttack : MonoBehaviour,
        IMessageSubscriber<GameMessages.NewSwordEquiped>
    {
        //TODO: change playerState from Player class
        [SerializeField] private PlayerAnimatorController _animatorController;
        [SerializeField] private string _attackStateName = "Attack";
        [SerializeField] private int _attackLayerIndex = 0;
        [SerializeField] private float _attackStateFailSafe = 1.5f;

        [Header("Configuration")]
        [SerializeField] private AttackConfig _config;
        [SerializeField] private PlayerEquipment _equipment;

        private float _nextAttackTime;
        private ICommand _damageCommand;
        private bool _isEnabled;
        private bool _isAttacking;
        private IPlayerAnimator _animator;
        private Animator _rawAnimator;

        private void Awake()
        {
            MessageService.Subscribe(this);
            _isEnabled = true;

            _damageCommand = ScriptableObject.CreateInstance<DamageCommand>();

            _animator = _animatorController ?? GetComponent<IPlayerAnimator>();
            _rawAnimator = (_animator as PlayerAnimatorController)?.Animator ?? GetComponent<Animator>();

            if (_equipment == null)
            {
                _equipment = GetComponent<PlayerEquipment>();
            }
        }

        private void OnDestroy()
        {
            MessageService.Unsubscribe(this);
        }

        private void Update()
        {
            if (!_isEnabled || _isAttacking) return;

            if (Inputs.LKMPressed && Time.time >= _nextAttackTime)
            {
                if (IsPointerOverUi())
                {
                    return;
                }

                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                Vector3 targetPoint;

                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    targetPoint = hit.point;
                }
                else
                {
                    // fallback: attack straight ahead if nothing was hit so animation still plays
                    targetPoint = transform.position + transform.forward;
                }

                StartCoroutine(AttackRoutine(targetPoint));
            }
        }

        private static bool IsPointerOverUi()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }

        private IEnumerator AttackRoutine(Vector3 targetPoint)
        {
            _isAttacking = true;

            Vector3 attackDirection = (targetPoint - transform.position).normalized;

            bool isRunning = _animator?.IsRunning() ?? false;

            if (!isRunning)
            {
                Player.Instance.GetComponentFromList<RotationController>().RotateToDirection(attackDirection, 2, 0.2f);

                while (Player.Instance.GetComponentFromList<RotationController>().IsRotating(2))
                {
                    yield return null;
                }
            }

            if (_equipment != null)
            {
                _equipment.SetActiveHandItem(HandItemType.Sword);
                var swordAttack = _equipment.GetSwordAttackConfig();
                if (swordAttack != null)
                {
                    _config = swordAttack;
                }
            }

            StartCoroutine(PerformAttack());
        }

        private IEnumerator PerformAttack()
        {
            var activeConfig = _config ?? _equipment?.GetSwordAttackConfig();
            if (activeConfig == null)
            {
                _isAttacking = false;
                yield break;
            }

            _config = activeConfig;

            _nextAttackTime = Time.time + _config.Cooldown;

            _animator?.TriggerAttack();

            //TODO: create VFXManager
            GameRoot.GetManager<AudioManager>().PlayEffect("AttackSound");

            yield return new WaitForSeconds(_config.AnimationDelay);

            DetectHits();

            try
            {
                yield return WaitForAttackAnimation(activeConfig);
            }
            finally
            {
                _isAttacking = false;
            }
        }

        private void DetectHits()
        {
            Vector3 offset = -transform.forward * 0.5f;
            Vector3 attackOrigin = transform.position + offset;

            Collider[] hits = Physics.OverlapSphere(attackOrigin, _config.Radius, _config.AttackableLayers);

            foreach (Collider hit in hits)
            {
                Vector3 directionToTarget = hit.transform.position - attackOrigin;
                float angle = Vector3.Angle(transform.forward, directionToTarget);

                if (angle <= _config.Angle / 2)
                {
                    if (hit.TryGetComponent<IDamageable>(out IDamageable damageable))
                    {
                        var damageToDeal = _config.BaseDamage;

#if UNITY_EDITOR || DEVOTION_GODMODE
                        var config = GameRoot.GameConfig;
                        if (config != null && config.GodModeOneHitKill)
                        {
                            damageToDeal = 9999999f;
                        }
#endif
                        _damageCommand.Execute(new DamageData(
                            damageToDeal,
                            damageable
                        ));
                    }
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (_config == null) return;

            Vector3 offset = -transform.forward * 0.5f;
            Vector3 attackOrigin = transform.position + offset;

            Gizmos.color = new Color(1, 0, 0, 0.3f);
            Gizmos.DrawWireSphere(attackOrigin, _config.Radius);

            Vector3 forward = transform.forward * _config.Radius;
            Quaternion leftRayRotation = Quaternion.AngleAxis(-_config.Angle / 2, Vector3.up);
            Quaternion rightRayRotation = Quaternion.AngleAxis(_config.Angle / 2, Vector3.up);

            Vector3 leftRayDirection = leftRayRotation * forward;
            Vector3 rightRayDirection = rightRayRotation * forward;

            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(attackOrigin, leftRayDirection);
            Gizmos.DrawRay(attackOrigin, rightRayDirection);
        }


        public void SetComponentEnable(bool value)
        {
            _isEnabled = value;
        }

        public void OnMessage(GameMessages.NewSwordEquiped message)
        {
            _config = message.Model;
        }

        private IEnumerator WaitForAttackAnimation(AttackConfig activeConfig)
        {
            if (_rawAnimator == null || string.IsNullOrWhiteSpace(_attackStateName))
            {
                yield return null;
                yield break;
            }

            int layer = Mathf.Clamp(_attackLayerIndex, 0, _rawAnimator.layerCount - 1);

            bool stateStarted = false;
            float failSafeTime = Mathf.Max(_attackStateFailSafe, activeConfig?.AnimationDelay ?? 0f);
            float elapsed = 0f;

            while (true)
            {
                var info = _rawAnimator.GetCurrentAnimatorStateInfo(layer);

                if (info.IsName(_attackStateName))
                {
                    stateStarted = true;

                    if (info.normalizedTime >= 1f && !_rawAnimator.IsInTransition(layer))
                    {
                        break;
                    }
                }
                else if (stateStarted)
                {
                    // left attack state early
                    break;
                }

                elapsed += Time.deltaTime;
                if (elapsed >= failSafeTime)
                {
                    break;
                }

                yield return null;
            }
        }
    }
}
