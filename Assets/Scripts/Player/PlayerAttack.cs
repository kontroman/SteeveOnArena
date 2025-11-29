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

namespace MineArena.PlayerSystem
{
    [RequireComponent(typeof(Animator))]
    public class PlayerAttack : MonoBehaviour,
        IMessageSubscriber<GameMessages.NewSwordEquiped>
    {
        //TODO: change playerState from Player class
        [SerializeField] private Animator _animator;

        [Header("Configuration")]
        [SerializeField] private AttackConfig _config;

        private static readonly int AttackTrigger = Animator.StringToHash("Attack");

        private float _nextAttackTime;
        private ICommand _damageCommand;
        private bool _isEnabled;

        private void Awake()
        {
            MessageService.Subscribe(this);
            _isEnabled = true;

            _damageCommand = ScriptableObject.CreateInstance<DamageCommand>();

            if (_animator == null)
            {
                _animator = GetComponent<Animator>();
            }
        }

        private void OnDestroy()
        {
            MessageService.Unsubscribe(this);
        }

        private void Update()
        {
            if (!_isEnabled) return;

            if (Inputs.LKMPressed && Time.time >= _nextAttackTime)
            {
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

        private IEnumerator AttackRoutine(Vector3 targetPoint)
        {
            Vector3 attackDirection = (targetPoint - transform.position).normalized;

            Player.Instance.GetComponentFromList<RotationController>().RotateToDirection(attackDirection, 2, 0.2f);

            while (Player.Instance.GetComponentFromList<RotationController>().IsRotating(2))
            {
                yield return null;
            }

            StartCoroutine(PerformAttack());
        }

        private IEnumerator PerformAttack()
        {
            _nextAttackTime = Time.time + _config.Cooldown;

            _animator.SetTrigger(AttackTrigger);

            //TODO: create VFXManager
            GameRoot.GetManager<AudioManager>().PlayEffect("AttackSound");

            yield return new WaitForSeconds(_config.AnimationDelay);

            DetectHits();
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
    }
}
