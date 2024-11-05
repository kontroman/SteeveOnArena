using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UIElements;

namespace Devotion.Item
{
    public class AnimationDrop : MonoBehaviour
    {
        [SerializeField] private AnimationIDEL _animationIDEL;
        [SerializeField] private int _numberLayerGround = 3;

        [Header("Horizontal Forse")]
        [HideLabel, MinMaxSlider(150, 250, true)]
        [SerializeField] private Vector2 _forseHorizontal;

        [Header("Vertical Forse")]
        [HideLabel, MinMaxSlider(150, 200, true)]
        [SerializeField] private Vector2 _forseVertical;

        private bool _isGround = false;

        private Rigidbody _rigidbody;
        private Vector3[] _directions = new Vector3[] {
        Vector3.forward, Vector3.back, Vector3.right, Vector3.left};

        private void Awake()
        {
            _animationIDEL = GetComponent<AnimationIDEL>();
            _rigidbody = GetComponent<Rigidbody>();
        }

        private void OnEnable()
        {
            StartAnimation();
        }

        public void StartAnimation()
        {
            _rigidbody.AddForce((_directions[Random.Range(0, _directions.Length)] *
                Random.Range(_forseHorizontal.x, _forseHorizontal.y)) + Vector3.up * Random.Range(_forseVertical.x, _forseVertical.y));
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.layer == _numberLayerGround)
            {
                if (_isGround == false)
                {
                    Debug.LogError("Collizion");
                    _isGround = true;
                    _animationIDEL.StartAnimation();
                }
            }

        }


    }

}
