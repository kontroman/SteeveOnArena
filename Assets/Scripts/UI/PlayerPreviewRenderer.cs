using MineArena.Controllers;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MineArena.UI
{
    [DisallowMultipleComponent]
    public class PlayerPreviewRenderer : MonoBehaviour
    {
        private const int TextureWidth = 512;
        private const int TextureHeight = 768;
        private const string PreviewCameraName = "InventoryPreviewCamera";
        private const string PreviewLayerName = "PlayerPreview";

        [SerializeField] private RawImage _target;
        private Camera _previewCamera;
        private RenderTexture _renderTexture;
        private readonly Dictionary<GameObject, int> _originalLayers = new();
        private Player _currentPlayer;
        private bool _cameraStateCaptured;
        private bool _cameraGameObjectWasActive;
        private bool _cameraComponentWasEnabled;
        private bool _audioListenerWasEnabled;
        private int _cameraCullingMask;
        private RenderTexture _cameraTargetTexture;
        private bool _missingCameraWarningLogged;

        public void Initialize()
        {
            EnsureTarget();
            EnsureRenderTexture();
            EnsureCamera();
        }

        private void OnEnable()
        {
            Initialize();

            ApplyPreviewLayerToPlayer();
            EnablePreviewCamera();
        }

        private void OnDisable()
        {
            RestorePreviewCameraState();
            RestorePlayerLayers();
        }

        private void OnDestroy()
        {
            RestorePreviewCameraState();
            RestorePlayerLayers();

            if (_renderTexture != null)
            {
                _renderTexture.Release();
                Destroy(_renderTexture);
            }
        }

        private void LateUpdate()
        {
            if (_previewCamera == null)
            {
                EnsureCamera();
            }

            ApplyPreviewLayerToPlayer();
            ConfigurePreviewCamera();
            EnablePreviewCamera();
        }

        private void EnsureTarget()
        {
            if (_target != null)
            {
                PrepareTarget(_target);
                return;
            }

            var targetTransform = transform.Find("PlayerPreviewRender");
            _target = targetTransform != null ? targetTransform.GetComponent<RawImage>() : null;

            if (_target == null)
                _target = GetComponentInChildren<RawImage>(true);

            if (_target == null)
            {
                var targetObject = new GameObject("PlayerPreviewRender", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
                targetObject.transform.SetParent(transform, false);
                _target = targetObject.GetComponent<RawImage>();

                var rectTransform = _target.rectTransform;
                rectTransform.anchorMin = Vector2.zero;
                rectTransform.anchorMax = Vector2.one;
                rectTransform.offsetMin = Vector2.zero;
                rectTransform.offsetMax = Vector2.zero;
                rectTransform.localScale = Vector3.one;
            }

            PrepareTarget(_target);
        }

        private void EnsureRenderTexture()
        {
            if (_renderTexture != null)
            {
                if (_target != null && _target.texture != _renderTexture)
                    _target.texture = _renderTexture;

                return;
            }

            _renderTexture = new RenderTexture(TextureWidth, TextureHeight, 16, RenderTextureFormat.ARGB32)
            {
                name = "InventoryPlayerPreview",
                antiAliasing = 2
            };
            _renderTexture.Create();

            if (_target != null)
                _target.texture = _renderTexture;
        }

        private static void PrepareTarget(RawImage target)
        {
            if (target == null)
                return;

            target.raycastTarget = false;
            target.color = Color.white;

            if (!target.gameObject.activeSelf)
                target.gameObject.SetActive(true);
        }

        private void EnsureCamera()
        {
            var player = Player.Instance;
            if (player == null)
                return;

            if (_previewCamera != null && _previewCamera.transform.IsChildOf(player.transform))
            {
                ConfigurePreviewCamera();
                return;
            }

            var cameraTransform = FindChildByName(player.transform, PreviewCameraName);
            _previewCamera = cameraTransform != null ? cameraTransform.GetComponent<Camera>() : null;

            if (_previewCamera == null)
            {
                if (!_missingCameraWarningLogged)
                {
                    Debug.LogWarning($"[PlayerPreviewRenderer] Camera '{PreviewCameraName}' not found on player.");
                    _missingCameraWarningLogged = true;
                }
                return;
            }

            CapturePreviewCameraState();
            _missingCameraWarningLogged = false;
            ConfigurePreviewCamera();
        }

        private void ConfigurePreviewCamera()
        {
            if (_previewCamera == null)
                return;

            _previewCamera.clearFlags = CameraClearFlags.SolidColor;
            _previewCamera.backgroundColor = new Color(0f, 0f, 0f, 0f);
            _previewCamera.allowHDR = false;
            _previewCamera.allowMSAA = true;
            _previewCamera.targetTexture = _renderTexture;

            var audioListener = _previewCamera.GetComponent<AudioListener>();
            if (audioListener != null)
                audioListener.enabled = false;

            var previewLayer = LayerMask.NameToLayer(PreviewLayerName);
            if (previewLayer >= 0)
                _previewCamera.cullingMask = 1 << previewLayer;
            else
                Debug.LogWarning($"[PlayerPreviewRenderer] Layer '{PreviewLayerName}' not found. Preview camera culling mask was not changed.");
        }

        private void CapturePreviewCameraState()
        {
            if (_previewCamera == null || _cameraStateCaptured)
                return;

            _cameraGameObjectWasActive = _previewCamera.gameObject.activeSelf;
            _cameraComponentWasEnabled = _previewCamera.enabled;
            _cameraCullingMask = _previewCamera.cullingMask;
            _cameraTargetTexture = _previewCamera.targetTexture;
            var audioListener = _previewCamera.GetComponent<AudioListener>();
            _audioListenerWasEnabled = audioListener != null && audioListener.enabled;
            _cameraStateCaptured = true;
        }

        private void EnablePreviewCamera()
        {
            if (_previewCamera == null)
                return;

            CapturePreviewCameraState();

            if (!_previewCamera.gameObject.activeSelf)
                _previewCamera.gameObject.SetActive(true);

            _previewCamera.enabled = true;
        }

        private void RestorePreviewCameraState()
        {
            if (_previewCamera == null || !_cameraStateCaptured)
                return;

            _previewCamera.enabled = _cameraComponentWasEnabled;
            _previewCamera.cullingMask = _cameraCullingMask;
            _previewCamera.targetTexture = _cameraTargetTexture;

            var audioListener = _previewCamera.GetComponent<AudioListener>();
            if (audioListener != null)
                audioListener.enabled = _audioListenerWasEnabled;

            _previewCamera.gameObject.SetActive(_cameraGameObjectWasActive);
            _cameraStateCaptured = false;
        }

        private void ApplyPreviewLayerToPlayer()
        {
            var player = Player.Instance;
            if (player == null)
                return;

            if (_currentPlayer != player)
                RestorePlayerLayers();

            _currentPlayer = player;

            var previewLayer = LayerMask.NameToLayer(PreviewLayerName);
            if (previewLayer < 0)
                return;

            foreach (var renderer in player.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null)
                    continue;

                var target = renderer.gameObject;
                if (!_originalLayers.ContainsKey(target))
                    _originalLayers.Add(target, target.layer);

                target.layer = previewLayer;
            }
        }

        private void RestorePlayerLayers()
        {
            foreach (var entry in _originalLayers)
            {
                if (entry.Key != null)
                    entry.Key.layer = entry.Value;
            }

            _originalLayers.Clear();
            _currentPlayer = null;
        }

        private static Transform FindChildByName(Transform root, string childName)
        {
            if (root == null)
                return null;

            foreach (Transform child in root)
            {
                if (child.name == childName)
                    return child;

                var nested = FindChildByName(child, childName);
                if (nested != null)
                    return nested;
            }

            return null;
        }
    }
}
