using Sirenix.OdinInspector;
using UnityEngine;

namespace MineArena.Items
{
    public class AnimationDrop : MonoBehaviour
    {
        [SerializeField] private AnimationIDLE _animationIDEL;
        [SerializeField] private int _numberLayerGround = 3;

        [Header("Horizontal Forse")]
        [HideLabel, MinMaxSlider(20, 125, true)]
        [SerializeField] private Vector2 _forseHorizontal;

        [Header("Vertical Forse")]
        [HideLabel, MinMaxSlider(20, 125, true)]
        [SerializeField] private Vector2 _forseVertical;

        private bool _isGround = false;
        private Collider _collider;

        private Rigidbody _rigidbody;
        private Vector3[] _directions = new Vector3[] {
        Vector3.forward, Vector3.back, Vector3.right, Vector3.left};

        private void Awake()
        {
            //TODO: make it serializeField and remove GetComopnent

            _collider = GetComponent<Collider>();
            _animationIDEL = GetComponent<AnimationIDLE>();
            _rigidbody = GetComponent<Rigidbody>();
        }

        private void OnEnable()
        {
            StartAnimation();
        }

        public void StartAnimation()
        {
            _isGround = false;
            _collider.isTrigger = false;
            _rigidbody.isKinematic = false;
            _rigidbody.useGravity = true;
            _rigidbody.velocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
            _rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            _rigidbody.AddForce((_directions[Random.Range(0, _directions.Length)] *
                Random.Range(_forseHorizontal.x, _forseHorizontal.y)) + Vector3.up * Random.Range(_forseVertical.x, _forseVertical.y), ForceMode.Force);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.layer == _numberLayerGround)
            {
                if (_isGround == false)
                {
                    Land();
                }
            }
        }

        private void FixedUpdate()
        {
            if (_isGround || _rigidbody.velocity.y > 0f) return;
            var bounds = _collider.bounds;
            if (Physics.Raycast(bounds.center, Vector3.down, bounds.extents.y + 0.08f,
                1 << _numberLayerGround, QueryTriggerInteraction.Ignore)) Land();
        }

        private void Land()
        {
            _isGround = true;
            _rigidbody.velocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
            _rigidbody.collisionDetectionMode = CollisionDetectionMode.Discrete;
            _rigidbody.isKinematic = true;
            _collider.isTrigger = true;
            _animationIDEL.StartAnimation();
        }
    }
}
