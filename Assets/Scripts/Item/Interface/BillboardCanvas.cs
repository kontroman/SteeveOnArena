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
        private Canvas _canvasComponent;
        private bool _visible;
        private UnityEngine.UI.Image _miningIcon;
        private Sprite _pickaxeSprite;
        private Vector2 _pickaxeSize;
        private GameObject _cancelIcon;

        public void SetMining(bool mining)
        {
            if (_miningIcon == null) return;
            if (mining && _cancelIcon == null)
            {
                _cancelIcon = new GameObject("Cancel cross", typeof(RectTransform));
                _cancelIcon.transform.SetParent(_miningIcon.transform, false);
                foreach (float angle in new[] { -45f, 45f })
                {
                    var stroke = new GameObject("Stroke", typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Image));
                    stroke.transform.SetParent(_cancelIcon.transform, false);
                    var rect = (RectTransform)stroke.transform;
                    rect.sizeDelta = new Vector2(0.28f, 0.055f);
                    rect.localRotation = Quaternion.Euler(0, 0, angle);
                    var image = stroke.GetComponent<UnityEngine.UI.Image>();
                    image.color = new Color(1f, 0.32f, 0.25f);
                    image.material = _miningIcon.material;
                    image.raycastTarget = false;
                }
            }
            _miningIcon.enabled = !mining;
            _miningIcon.sprite = _pickaxeSprite;
            _miningIcon.color = Color.white;
            _miningIcon.rectTransform.sizeDelta = _pickaxeSize;
            if (_cancelIcon != null) _cancelIcon.SetActive(mining);
        }

        private void Start()
        {
            _mainCamera = Camera.main;
            var interactable = GetComponent<InteractableObject>();
            var prefab = interactable != null && interactable.IsMineable
                ? Resources.Load<GameObject>("UI/MiningPrompt") : null;
            if (prefab == null) prefab = _billboardCanvas;
            if (prefab == null) return;
            _canvas = Instantiate(prefab, transform);
            _miningIcon = _canvas.transform.Find("Iron pickaxe")?.GetComponent<UnityEngine.UI.Image>();
            if (_miningIcon != null)
            {
                _pickaxeSprite = _miningIcon.sprite;
                _pickaxeSize = _miningIcon.rectTransform.sizeDelta;
            }
            _canvasComponent = _canvas.GetComponent<Canvas>();
            _canvasComponent.worldCamera = _mainCamera;

            _canvas.transform.localPosition = _canvas.transform.localPosition + _posOffset;
            _canvas.transform.localScale = _canvas.transform.localScale + _sizeOffset;
            _canvas.SetActive(_visible && !HideOnMobile);
        }

        private void LateUpdate()
        {
            if (_canvas == null)
                return;
            if (HideOnMobile) { HideUI(); return; }

            if (_mainCamera == null)
                _mainCamera = Camera.main;

            if (_mainCamera == null)
                return;

            _canvas.transform.rotation = _mainCamera.transform.rotation;
            if (_canvasComponent != null) _canvasComponent.worldCamera = _mainCamera;
        }

        private bool HideOnMobile => GetComponent<InteractableObject>()?.IsMineable == true &&
            (Application.isMobilePlatform || MineArena.UI.MobileGameInput.Enabled ||
             (!Application.isEditor && Input.touchSupported) || MineArena.UI.MobileGameInput.PreviewInEditor);

        public void ShowUI()
        {
            if (HideOnMobile) { HideUI(); return; }
            _visible = true;
            if (_canvas)
                _canvas.SetActive(true);
        }

        public void HideUI()
        {
            _visible = false;
            if(_canvas)
                _canvas.SetActive(false);
        }
    }
}
