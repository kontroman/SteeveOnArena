using UnityEngine;
using System.Collections;
using Devotion.Interfaces;
using Devotion.Structs;
using Devotion.Commands;
using Devotion.SDK.Helpers;
using Devotion.Managers;
using Devotion.SDK.Controllers;
using Devotion.Messages.MessageService;
using Devotion.Messages;
using Devotion.Controllers;

namespace Devotion.PlayerSystem
{
    [RequireComponent(typeof(Animator))]
    public class PlayerAttack : MonoBehaviour,
        IMessageSubscriber<GameMessages.NewSwordEquiped>
    {
        //TODO: change playerState from Player class
        [SerializeField] private Animator _animator;

        [Header("Configuration")]
        [SerializeField] private AttackConfig _config;

        private float _nextAttackTime;
        private ICommand _damageCommand;
        private bool _isEnabled;

        private void Awake()
        {
            MessageService.Subscribe(this);
            _isEnabled = true;

            _damageCommand = ScriptableObject.CreateInstance<DamageCommand>();
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

                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    StartCoroutine(AttackRoutine(hit.point));
                }
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

            //TODO: create VFXManager
            //TODO: change animation to Attack;

            GameRoot.Instance.GetManager<AudioManager>().PlayEffect("AttackSound");

            yield return new WaitForSeconds(_config.AnimationDelay);

            DetectHits();
        }

        private void DetectHits()
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, _config.Radius, _config.AttackableLayers);

            foreach (Collider hit in hits)
            {
                Vector3 directionToTarget = hit.transform.position - transform.position;
                float angle = Vector3.Angle(transform.forward, directionToTarget);

                if (angle <= _config.Angle / 2)
                {
                    if (hit.TryGetComponent<IDamageable>(out IDamageable damageable))
                    {
                        _damageCommand.Execute(new DamageData(
                            _config.BaseDamage,
                            damageable
                        ));
                    }
                }
            }
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