using Devotion.Basics;
using DG.Tweening;
using UnityEngine;

namespace Devotion.PlayerSystem
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMovement : MonoBehaviour
    {
        private CharacterController _characterController;
        private bool _canMove = true;

        private Transform _cameraTransform;
        private Vector3 _velocity;
        private bool _isGrounded;

        private void Start()
        {
            _cameraTransform = Camera.main.transform;
            _characterController = GetComponent<CharacterController>();
        }

        private void Update()
        {
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
                Controllers.Player.Instance.GetComponentFromList<Animator>().SetBool("isRunning", true);
                moveDirection.Normalize();

                Vector3 cameraForward = _cameraTransform.forward;
                cameraForward.y = 0;
                Quaternion cameraRotation = Quaternion.LookRotation(cameraForward);
                moveDirection = cameraRotation * moveDirection;

                return moveDirection;
            }
            else
            {
                Controllers.Player.Instance.GetComponentFromList<Animator>().SetBool("isRunning", false);
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

        private void RotatePlayer(Vector3 moveDirection)
        {
            if (moveDirection != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Constants.PlayerSettings.RotationSpeed * Time.deltaTime);
            }
        }

        public void SetMovement(bool canMove)
        {
            _canMove = canMove;
        }

        public void RotatePlayerToTarget(Transform target)
        {
            float duration = 0.5f;

            Vector3 direction = (target.position - transform.position).normalized;
            direction.y = 0;

            Quaternion targetRotation = Quaternion.LookRotation(direction);
            Vector3 targetEulerAngles = new Vector3(0, targetRotation.eulerAngles.y, 0);

            transform.DORotate(targetEulerAngles, duration, RotateMode.FastBeyond360);
        }
    }
}