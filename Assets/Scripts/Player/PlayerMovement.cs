using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Devotion.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMovement : MonoBehaviour
    {
        [SerializeField] private float _speed = 5f;
        [SerializeField] private float _rotationSpeed = 10f;
        public float MoveSpeed => _speed;

        private CharacterController _characterController;
        private bool _canMove = true;

        private void Start()
        {
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
                moveDirection.Normalize();
                _characterController.Move(moveDirection * MoveSpeed * Time.deltaTime);

                Vector3 targetDirection = new Vector3(moveX, 0, moveZ);
                if (targetDirection != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, _rotationSpeed * Time.deltaTime);
                }
            }
        }

        public void SetMovement(bool canMove)
        {
            _canMove = canMove;
        }
    }
}
