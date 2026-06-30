using System;
using System.Collections;
using Devotion.SDK.Async;
using Devotion.SDK.Interfaces;
using UnityEngine;

namespace MineArena.Buildings
{
    public class BuildingCinematicCamera : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private float duration = 3f;
        [SerializeField] private float radius = 6f;
        [SerializeField] private float height = 3f;
        [SerializeField] private float startAngle = -35f;
        [SerializeField] private float endAngle = 35f;
        [SerializeField] private Vector3 lookAtOffset = new Vector3(0f, 1.5f, 0f);

        private Coroutine _playRoutine;
        private Promise _playPromise;
        private Camera _camera;
        private BuildingCinematicCameraSettings _runtimeSettings;

        public float Duration => duration;

        public IPromise Play(Transform targetOverride = null, Camera cameraOverride = null, BuildingCinematicCameraSettings settingsOverride = null)
        {
            if (_playRoutine != null)
            {
                Debug.LogWarning($"{nameof(BuildingCinematicCamera)}: previous camera routine interrupted.", this);
                StopCoroutine(_playRoutine);
            }

            _playPromise = new Promise();
            _camera = cameraOverride != null ? cameraOverride : GetComponent<Camera>();
            target = targetOverride != null ? targetOverride : target;
            _runtimeSettings = settingsOverride;

            if (_camera == null)
            {
                Debug.LogError($"{nameof(BuildingCinematicCamera)}: camera is not assigned.", this);
                _playPromise.Reject(new NullReferenceException($"{nameof(BuildingCinematicCamera)}: camera is not assigned."));
                return _playPromise;
            }

            if (target == null)
            {
                Debug.LogError($"{nameof(BuildingCinematicCamera)}: target is not assigned.", this);
                _playPromise.Reject(new NullReferenceException($"{nameof(BuildingCinematicCamera)}: target is not assigned."));
                return _playPromise;
            }

            Debug.Log($"{nameof(BuildingCinematicCamera)}: Play target={target.name}, camera={_camera.name}, duration={GetDuration()}, radius={GetRadius()}, height={GetHeight()}, angles={GetStartAngle()}->{GetEndAngle()}.", this);

            _playRoutine = StartCoroutine(PlayRoutine(_playPromise));
            return _playPromise;
        }

        public void SetToStart(Transform targetOverride = null, Camera cameraOverride = null, BuildingCinematicCameraSettings settingsOverride = null)
        {
            _camera = cameraOverride != null ? cameraOverride : GetComponent<Camera>();
            target = targetOverride != null ? targetOverride : target;
            _runtimeSettings = settingsOverride;

            if (_camera == null || target == null)
            {
                Debug.LogWarning($"{nameof(BuildingCinematicCamera)}: SetToStart skipped. camera={(_camera != null ? _camera.name : "null")}, target={(target != null ? target.name : "null")}.", this);
                return;
            }

            ApplyCameraPosition(0f);
            Debug.Log($"{nameof(BuildingCinematicCamera)}: set to start at {_camera.transform.position}.", this);
        }

        private IEnumerator PlayRoutine(Promise promise)
        {
            Debug.Log($"{nameof(BuildingCinematicCamera)}: started.");

            var elapsed = 0f;
            var safeDuration = Mathf.Max(0.01f, GetDuration());
            var midpointLogged = false;

            while (elapsed < safeDuration)
            {
                var t = Mathf.Clamp01(elapsed / safeDuration);
                ApplyCameraPosition(t);

                if (!midpointLogged && t >= 0.5f)
                {
                    midpointLogged = true;
                    Debug.Log($"{nameof(BuildingCinematicCamera)}: midpoint position {_camera.transform.position}.", this);
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            ApplyCameraPosition(1f);
            Debug.Log($"{nameof(BuildingCinematicCamera)}: end position {_camera.transform.position}.", this);
            _playRoutine = null;

            if (promise.State == Devotion.SDK.Enums.PromiseState.Pending)
                promise.Resolve();

            Debug.Log($"{nameof(BuildingCinematicCamera)}: finished.");
        }

        private void ApplyCameraPosition(float t)
        {
            if (_camera == null || target == null)
                return;

            var angle = Mathf.Lerp(GetStartAngle(), GetEndAngle(), t) * Mathf.Deg2Rad;
            var targetPosition = target.position;
            var offset = new Vector3(Mathf.Sin(angle) * GetRadius(), GetHeight(), Mathf.Cos(angle) * GetRadius());

            _camera.transform.position = targetPosition + offset;
            _camera.transform.LookAt(targetPosition + GetLookAtOffset());
        }

        private float GetDuration() => _runtimeSettings != null ? _runtimeSettings.Duration : duration;
        private float GetRadius() => _runtimeSettings != null ? _runtimeSettings.Radius : radius;
        private float GetHeight() => _runtimeSettings != null ? _runtimeSettings.Height : height;
        private float GetStartAngle() => _runtimeSettings != null ? _runtimeSettings.StartAngle : startAngle;
        private float GetEndAngle() => _runtimeSettings != null ? _runtimeSettings.EndAngle : endAngle;
        private Vector3 GetLookAtOffset() => _runtimeSettings != null ? _runtimeSettings.LookAtOffset : lookAtOffset;

        private void OnDisable()
        {
            if (_playRoutine != null)
            {
                StopCoroutine(_playRoutine);
                _playRoutine = null;
            }

            if (_playPromise != null && _playPromise.State == Devotion.SDK.Enums.PromiseState.Pending)
                _playPromise.Reject(new OperationCanceledException($"{nameof(BuildingCinematicCamera)} was disabled."));
        }
    }
}
