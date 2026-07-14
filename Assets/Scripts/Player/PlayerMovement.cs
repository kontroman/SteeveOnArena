using MineArena.Basics;
using MineArena.Controllers;
using MineArena.VFX;
using MineArena.Managers;
using Devotion.SDK.Controllers;
using Devotion.SDK.Services;
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
        private PlayerAttack _playerAttack;
        private Transform _cameraTransform;
        private Vector3 _velocity;
        private bool _isGrounded;
        private bool _jumpStarted;

        [Header("Landing VFX")]
        [SerializeField] private VfxId _landingVfxId = VfxId.JumpLanding;
        [SerializeField] private LayerMask _landingSurfaceMask = ~0;
        [SerializeField, Min(0.1f)] private float _landingRaycastDistance = 2f;
        [SerializeField] private Vector3 _landingVfxOffset = new Vector3(0f, 0.03f, 0f);

        [Header("Footsteps")]
        [SerializeField, Min(0.05f)] private float _footstepInterval = 0.42f;
        private float _nextFootstepTime;

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
            _playerAttack = Controllers.Player.Instance?.GetComponentFromList<PlayerAttack>() ??
                            GetComponent<PlayerAttack>();
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
                bool wasGrounded = _characterController.isGrounded;
                Vector3 horizontalMove = GetHorizontalMovement();
                ApplyGravityAndJump();

                Vector3 totalMovement = horizontalMove * Constants.PlayerSettings.Speed + new Vector3(0, _velocity.y, 0);
                _characterController.Move(totalMovement * Time.deltaTime);
                HandleLanding(wasGrounded);
                HandleFootsteps(horizontalMove);

                RotatePlayer(horizontalMove);
            }
            else
            {
                ApplyGravity();
                _characterController.Move(new Vector3(0f, _velocity.y, 0f) * Time.deltaTime);
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
                    _jumpStarted = true;
                    GameRoot.GetManager<AudioManager>()?.PlayEffect(Constants.AudioNames.Jump);
                }
            }

            ApplyGravity();
        }

        private void ApplyGravity()
        {
            if (_characterController == null)
                _characterController = GetComponent<CharacterController>();

            if (_characterController == null || !_characterController.enabled)
                return;

            if (_characterController.isGrounded && _velocity.y <= 0f)
                _velocity.y = -2f;

            _velocity.y += Constants.PlayerSettings.Gravity * Time.deltaTime;
        }

        private void HandleLanding(bool wasGrounded)
        {
            if (!_jumpStarted || wasGrounded || !_characterController.isGrounded)
                return;

            _jumpStarted = false;
            GameRoot.GetManager<AudioManager>()?.PlayEffect(Constants.AudioNames.Landing);

            if (!TryGetLandingSurface(out var hit))
                return;

            Color particleColor = VfxSurfaceColor.TryGetColor(hit, out var surfaceColor)
                ? surfaceColor
                : Color.white;

            var vfxService = ResolveVfxService();
            vfxService?.Play(
                _landingVfxId,
                VfxPlayOptions.At(hit.point + _landingVfxOffset, Quaternion.identity).WithColor(particleColor));
        }

        private bool TryGetLandingSurface(out RaycastHit bestHit)
        {
            Vector3 origin = transform.position + Vector3.up * 0.25f;
            var hits = Physics.RaycastAll(origin, Vector3.down, _landingRaycastDistance, _landingSurfaceMask, QueryTriggerInteraction.Ignore);
            bestHit = default;

            float bestDistance = float.MaxValue;

            foreach (var hit in hits)
            {
                if (hit.collider == null || hit.collider.transform.IsChildOf(transform))
                    continue;

                if (hit.distance >= bestDistance)
                    continue;

                bestHit = hit;
                bestDistance = hit.distance;
            }

            return bestHit.collider != null;
        }

        private static IVFXService ResolveVfxService()
        {
            try
            {
                return ServiceLocator.Resolve<IVFXService>();
            }
            catch (Exception)
            {
                return GameRoot.GetManager<VFXManager>();
            }
        }

        private void HandleFootsteps(Vector3 horizontalMove)
        {
            if (horizontalMove.sqrMagnitude <= 0.01f || !_characterController.isGrounded)
            {
                _nextFootstepTime = Time.time;
                return;
            }

            if (Time.time < _nextFootstepTime)
                return;

            GameRoot.GetManager<AudioManager>()?.PlayRandomEffect(Constants.AudioNames.Footsteps);
            _nextFootstepTime = Time.time + _footstepInterval;
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
            if (_playerAttack != null && _playerAttack.IsAttacking)
                return;

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

        public void SetAlive()
        {
            _isDead = false;
            IsPlayerDead = false;
            _canMove = true;
            _velocity = new Vector3(0f, -2f, 0f);
        }
    }
}
