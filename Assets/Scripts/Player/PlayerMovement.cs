using MineArena.Basics;
using MineArena.Controllers;
using System;
using UnityEngine;

namespace MineArena.PlayerSystem
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMovement : MonoBehaviour
    {
        private CharacterController _characterController;
        private bool _canMove = true;
        private bool _isDead;

        private IPlayerAnimator _animator;
        private Transform _cameraTransform;
        private Vector3 _velocity;
        private bool _isGrounded;

        public static event Action<Transform> PlayerDied;
        public static bool IsPlayerDead { get; private set; }

        private void Awake()
        {
            IsPlayerDead = false;
        }

        private void Start()
        {
            _cameraTransform = Camera.main.transform;
            _characterController = GetComponent<CharacterController>();

            _animator = Controllers.Player.Instance?.GetComponentFromList<PlayerAnimatorController>() ??
                        Controllers.Player.Instance?.GetComponent<IPlayerAnimator>() ??
                        GetComponent<IPlayerAnimator>();
        }

        private void Update()
        {
            if (_isDead)
            {
                ApplyDeathGravity();
                return;
            }

            if (_canMove)
            {
                Vector3 horizontalMove = GetHorizontalMovement();
                ApplyGravityAndJump();

                Vector3 totalMovement = horizontalMove * Constants.PlayerSettings.Speed + new Vector3(0, _velocity.y, 0);
                _characterController.Move(totalMovement * Time.deltaTime);

                RotatePlayer(horizontalMove);
            }
        }

        private Vector3 GetHorizontalMovement()
        {
            float moveX = Input.GetAxis("Horizontal");
            float moveZ = Input.GetAxis("Vertical");

            Vector3 moveDirection = new Vector3(moveX, 0, moveZ);

            if (moveDirection.magnitude > 0.1f)
            {
                _animator?.SetRunning(true);
                moveDirection.Normalize();

                Vector3 cameraForward = _cameraTransform.forward;
                cameraForward.y = 0;
                Quaternion cameraRotation = Quaternion.LookRotation(cameraForward);
                moveDirection = cameraRotation * moveDirection;

                return moveDirection;
            }
            else
            {
                _animator?.SetRunning(false);
                return Vector3.zero;
            }
        }

        private void ApplyGravityAndJump()
        {
            _isGrounded = _characterController.isGrounded;

            if (_isGrounded)
            {
                _velocity.y = -2f;

                if (Input.GetButtonDown("Jump"))
                {
                    _velocity.y = Constants.PlayerSettings.JumpForce;
                }
            }

            _velocity.y += Constants.PlayerSettings.Gravity * Time.deltaTime;
        }

        private void ApplyDeathGravity()
        {
            if (_characterController == null)
                _characterController = GetComponent<CharacterController>();

            if (_characterController == null || !_characterController.enabled)
                return;

            _isGrounded = _characterController.isGrounded;

            if (_isGrounded && _velocity.y <= 0f)
            {
                _velocity.y = -2f;
            }
            else
            {
                _velocity.y += Constants.PlayerSettings.Gravity * Time.deltaTime;
            }

            _characterController.Move(new Vector3(0f, _velocity.y, 0f) * Time.deltaTime);
        }

        private void RotatePlayer(Vector3 moveDirection)
        {
            if (moveDirection != Vector3.zero && !Player.Instance.GetComponentFromList<RotationController>().IsRotating())
            {
                Player.Instance.GetComponentFromList<RotationController>().RotateToDirection(
                    moveDirection,
                    priority: 1,
                    duration: 0.2f
                );
            }
        }

        public void SetMovement(bool canMove)
        {
            _canMove = canMove && !_isDead;
        }

        public void SetDead()
        {
            if (_isDead)
                return;

            _isDead = true;
            IsPlayerDead = true;
            _canMove = false;
            _velocity = new Vector3(0f, -2f, 0f);
            _animator?.SetRunning(false);
            PlayerDied?.Invoke(transform);
        }
    }
}
