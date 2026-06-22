using UnityEngine;

namespace MineArena.Items
{
    public class BillboardCanvas : MonoBehaviour
    {
        [SerializeField] private Vector3 _posOffset;
        [SerializeField] private Vector3 _sizeOffset;
        [SerializeField] private GameObject _billboardCanvas;

        private Camera _mainCamera;
        private GameObject _canvas;

        private void Start()
        {
            _mainCamera = Camera.main;
            _canvas = Instantiate(_billboardCanvas, transform);
            _canvas.GetComponent<Canvas>().worldCamera = _mainCamera;

            _canvas.transform.localPosition = _canvas.transform.localPosition + _posOffset;
            _canvas.transform.localScale = _canvas.transform.localScale + _sizeOffset;
        }

        private void Update()
        {
            if (_canvas == null)
                return;

            if (_mainCamera == null)
                _mainCamera = Camera.main;

            if (_mainCamera == null)
                return;

            Vector3 directionToCamera = (_mainCamera.transform.position - _canvas.transform.position).normalized;
            _canvas.transform.rotation = Quaternion.LookRotation(directionToCamera);
            _canvas.transform.rotation = Quaternion.Euler(0, 180 + _canvas.transform.rotation.eulerAngles.y, 0);
        }

        public void ShowUI()
        {
            if (_canvas)
                _canvas.SetActive(true);
        }

        public void HideUI()
        {
            if(_canvas)
                _canvas.SetActive(false);
        }
    }
}
