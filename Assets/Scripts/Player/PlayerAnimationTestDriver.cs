using UnityEngine;

namespace MineArena.PlayerSystem
{
    [RequireComponent(typeof(Animator))]
    public class PlayerAnimationTestDriver : MonoBehaviour
    {
        [Header("Animator parameter names")]
        [SerializeField] private string _runBoolParameter = "isRunning";
        [SerializeField] private string _attackTriggerParameter = "Attack";

        [Header("Input bindings")]
        [SerializeField] private KeyCode _toggleRunKey = KeyCode.LeftShift;
        [SerializeField] private KeyCode _attackKey = KeyCode.Mouse0;
        [SerializeField] private KeyCode _stopRunKey = KeyCode.Space;

        [Header("Optional auto-run from WASD")]
        [SerializeField] private bool _driveRunFromInput = true;
        [SerializeField, Range(0f, 1f)] private float _runInputThreshold = 0.2f;

        private Animator _animator;
        private int _runParamHash;
        private int _attackParamHash;

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            _runParamHash = Animator.StringToHash(_runBoolParameter);
            _attackParamHash = Animator.StringToHash(_attackTriggerParameter);
        }

        private void Update()
        {
            if (_driveRunFromInput)
            {
                float moveX = Input.GetAxisRaw("Horizontal");
                float moveZ = Input.GetAxisRaw("Vertical");
                bool isMoving = new Vector2(moveX, moveZ).sqrMagnitude > _runInputThreshold * _runInputThreshold;
                _animator.SetBool(_runParamHash, isMoving);
            }

            if (Input.GetKeyDown(_toggleRunKey))
            {
                bool newState = !_animator.GetBool(_runParamHash);
                _animator.SetBool(_runParamHash, newState);
            }

            if (Input.GetKeyDown(_stopRunKey))
            {
                _animator.SetBool(_runParamHash, false);
            }

            if (Input.GetKeyDown(_attackKey))
            {
                _animator.SetTrigger(_attackParamHash);
            }
        }
    }
}
