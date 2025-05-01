using UnityEngine;

namespace MineArena.Game.UI
{
    public class BillboarbBar : MonoBehaviour
    {
        [SerializeField] private Camera _mainCamera;

        private void Awake()
        {
            if(!_mainCamera)
                _mainCamera = Camera.main;
        }

        private void Update()
        {
            LookAt(GetTransform());
        }

        private Transform GetTransform() => transform;

        private void LookAt(Transform transform)
        {
            if (_mainCamera == null)
                return;

            transform.LookAt(transform.position + _mainCamera.transform.forward);
        }
    }
}
