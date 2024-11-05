using DG.Tweening;
using UnityEngine;

namespace Devotion.PlayerSystem
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMovement : MonoBehaviour
    {
        [SerializeField] private float _speed = 5f;
        [SerializeField] private float _rotationSpeed = 10f;

        public float MoveSpeed => _speed;
        public float RotationSpeed => _rotationSpeed;

        private CharacterController _characterController;
        private bool _canMove = true;

        private Transform _cameraTransform;

        private void Start()
        {
            _cameraTransform = Camera.main.transform;
            _characterController = GetComponent<CharacterController>();
        }

        private void Update()
        {
            if (_canMove)
            {
                Move();
            }
        }

        private void Move()
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

                _characterController.Move(moveDirection * MoveSpeed * Time.deltaTime);

                if (moveDirection != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, _rotationSpeed * Time.deltaTime);
                }
            }
            else
            {
                Controllers.Player.Instance.GetComponentFromList<Animator>().SetBool("isRunning", false);
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
