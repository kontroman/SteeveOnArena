using System;
using System.Collections;
using System.Collections.Generic;
using Devotion.SDK.Async;
using Devotion.SDK.Controllers;
using Devotion.SDK.Enums;
using Devotion.SDK.Interfaces;
using MineArena.Controllers;
using MineArena.PlayerSystem;
using MineArena.Windows;
using UnityEngine;

namespace MineArena.Buildings
{
    public class BuildingConstructionSequence : MonoBehaviour
    {
        [SerializeField] private BuildingCinematicCamera cinematicCamera;
        [SerializeField] private Camera targetCamera;
        [SerializeField] private float fadeDuration = 0.75f;

        private bool _isPlaying;

        public IPromise PlayForBuilding(BuildingConfig config, GameObject buildingInstance, Transform buildingPlace)
        {
            var promise = new Promise();

            if (_isPlaying)
            {
                promise.Reject(new InvalidOperationException($"{nameof(BuildingConstructionSequence)} is already playing."));
                return promise;
            }

            StartCoroutine(PlayRoutine(config, buildingInstance, buildingPlace, promise));
            return promise;
        }

        private IEnumerator PlayRoutine(BuildingConfig config, GameObject buildingInstance, Transform buildingPlace, Promise promise)
        {
            _isPlaying = true;
            var player = Player.Instance;
            var playerTransform = player != null ? player.transform : null;
            var movement = player != null ? player.GetComponentFromList<PlayerMovement>() : null;
            var attack = player != null ? player.GetComponentFromList<PlayerAttack>() : null;
            var camera = targetCamera != null ? targetCamera : Camera.main;
            var cameraController = FindObjectOfType<CameraController>();
            var cameraTransform = camera != null ? camera.transform : null;
            var disabledCameraDrivers = DisableCameraDrivers(camera);
            var oldCameraPosition = cameraTransform != null ? cameraTransform.position : Vector3.zero;
            var oldCameraRotation = cameraTransform != null ? cameraTransform.rotation : Quaternion.identity;
            var cameraSettings = GetCameraSettings(buildingPlace);
            var effect = buildingInstance != null ? buildingInstance.GetComponentInChildren<BuildingConstructionEffect>(true) : null;
            Exception failure = null;

            Debug.Log($"{nameof(BuildingConstructionSequence)}: started for {config?.BuildingName ?? buildingInstance?.name ?? "building"}, instance={buildingInstance?.name ?? "null"}, place={buildingPlace?.name ?? "null"}.", this);

            if (playerTransform == null)
                Debug.LogWarning($"{nameof(BuildingConstructionSequence)}: Player.Instance was not found; input lock and player reposition will be skipped.", this);

            if (camera == null)
                Debug.LogWarning($"{nameof(BuildingConstructionSequence)}: Camera.main was not found and Target Camera is not assigned.", this);
            else
                Debug.Log($"{nameof(BuildingConstructionSequence)}: using camera {camera.name}.", this);

            SetPlayerInput(movement, attack, false);
            cameraController?.SetFollowing(false);
            HideBuildingBeforeConstruction(effect);
            Debug.Log($"{nameof(BuildingConstructionSequence)}: player input locked; camera following disabled={cameraController != null}.", this);

            var blackWindow = GameRoot.UIManager != null ? GameRoot.UIManager.OpenWindow<BlackWindow>() as BlackWindow : null;
            if (blackWindow == null)
                failure = new NullReferenceException($"{nameof(BuildingConstructionSequence)}: {nameof(BlackWindow)} is not available.");
            else
            {
                blackWindow.DoCasualFade(true, 0f);
                Debug.Log($"{nameof(BuildingConstructionSequence)}: {nameof(BlackWindow)} found, fading to black.", this);
            }

            if (failure == null)
                yield return WaitForPromise(blackWindow.DoFade(false, fadeDuration), ex => failure = ex);

            var target = buildingInstance != null ? buildingInstance.transform : buildingPlace;

            if (failure == null)
            {
                MovePlayerToBuildPoint(buildingPlace, playerTransform);
                Debug.Log($"{nameof(BuildingConstructionSequence)}: camera moved to cinematic start around {target?.name ?? "null"}.", this);
                EnsureCinematicCamera().SetToStart(target, camera, cameraSettings);
            }

            if (failure == null)
            {
                Debug.Log($"{nameof(BuildingConstructionSequence)}: fading from black before cinematic.", this);
                yield return WaitForPromise(blackWindow.DoFade(true, fadeDuration), ex => failure = ex);
            }

            if (failure == null)
            {
                var cameraPromise = EnsureCinematicCamera().Play(target, camera, cameraSettings);

                if (effect == null)
                    Debug.LogWarning($"{nameof(BuildingConstructionSequence)}: no {nameof(BuildingConstructionEffect)} found under {buildingInstance?.name ?? "null"}; construction effect will be skipped.", this);
                else
                    Debug.Log($"{nameof(BuildingConstructionSequence)}: found {nameof(BuildingConstructionEffect)} on {effect.gameObject.name}.", effect);

                var effectPromise = effect != null ? effect.PlayRuntimeAsync() : Promise.ResolveAndReturn();

                Debug.Log($"{nameof(BuildingConstructionSequence)}: camera and construction effect started.");
                yield return WaitForAll(cameraPromise, effectPromise, ex => failure = ex);
            }

            if (failure == null)
            {
                Debug.Log($"{nameof(BuildingConstructionSequence)}: cinematic/effect completed, fading to black for restore.", this);
                yield return WaitForPromise(blackWindow.DoFade(false, fadeDuration), ex => failure = ex);
            }

            RestoreCamera(cameraTransform, oldCameraPosition, oldCameraRotation);
            RestoreCameraDrivers(disabledCameraDrivers);
            RestoreBuildingIfConstructionDidNotShowIt(effect);
            cameraController?.SetFollowing(true);
            SetPlayerInput(movement, attack, true);
            Debug.Log($"{nameof(BuildingConstructionSequence)}: camera and player input restored.", this);

            if (blackWindow != null)
                yield return WaitForPromise(blackWindow.DoFade(true, fadeDuration), ex => Debug.LogWarning(ex.Message));

            _isPlaying = false;

            if (failure != null)
            {
                Debug.LogError($"{nameof(BuildingConstructionSequence)} failed: {failure.Message}", this);
                if (promise.State == PromiseState.Pending)
                    promise.Reject(failure);
            }
            else
            {
                Debug.Log($"{nameof(BuildingConstructionSequence)}: finished.");
                if (promise.State == PromiseState.Pending)
                    promise.Resolve();
            }
        }

        private BuildingCinematicCamera EnsureCinematicCamera()
        {
            if (cinematicCamera == null)
                cinematicCamera = GetComponent<BuildingCinematicCamera>() ?? gameObject.AddComponent<BuildingCinematicCamera>();

            return cinematicCamera;
        }

        private void HideBuildingBeforeConstruction(BuildingConstructionEffect effect)
        {
            var sourceRenderer = effect != null ? effect.SourceRenderer : null;

            if (sourceRenderer == null)
                return;

            sourceRenderer.enabled = false;
            Debug.Log($"{nameof(BuildingConstructionSequence)}: hidden construction source renderer {sourceRenderer.name} before fade.", effect);
        }

        private static void RestoreBuildingIfConstructionDidNotShowIt(BuildingConstructionEffect effect)
        {
            var sourceRenderer = effect != null ? effect.SourceRenderer : null;

            if (sourceRenderer != null && !sourceRenderer.enabled)
                sourceRenderer.enabled = true;
        }

        private void MovePlayerToBuildPoint(Transform buildingPlace, Transform playerTransform)
        {
            var buildPoint = GetPlayerBuildPoint(buildingPlace);

            if (buildPoint == null)
            {
                Debug.LogWarning($"{nameof(BuildingConstructionSequence)}: {buildingPlace?.name ?? "building place"} has no {nameof(BuildingZone)}.{nameof(BuildingZone.PlayerPositionOnBuild)} assigned; player will not be moved.", this);
                return;
            }

            if (playerTransform == null)
                return;
            
            Debug.Log($"{nameof(BuildingConstructionSequence)}: moving player to build point {buildPoint.name}.", this);

            var characterController = playerTransform.GetComponent<CharacterController>();
            var wasEnabled = characterController != null && characterController.enabled;

            if (characterController != null)
                characterController.enabled = false;

            playerTransform.position = buildPoint.position;
            playerTransform.rotation = buildPoint.rotation;

            if (characterController != null)
                characterController.enabled = wasEnabled;
        }

        private static Transform GetPlayerBuildPoint(Transform buildingPlace)
        {
            var zone = buildingPlace != null ? buildingPlace.GetComponent<BuildingZone>() : null;

            if (zone != null && zone.PlayerPositionOnBuild != null)
                return zone.PlayerPositionOnBuild;

            return null;
        }

        private static BuildingCinematicCameraSettings GetCameraSettings(Transform buildingPlace)
        {
            var zone = buildingPlace != null ? buildingPlace.GetComponent<BuildingZone>() : null;
            return zone != null ? zone.CinematicCameraSettings : null;
        }

        private static void SetPlayerInput(PlayerMovement movement, PlayerAttack attack, bool enabled)
        {
            movement?.SetMovement(enabled);
            attack?.SetComponentEnable(enabled);
        }

        private static void RestoreCamera(Transform cameraTransform, Vector3 position, Quaternion rotation)
        {
            if (cameraTransform == null)
                return;

            cameraTransform.position = position;
            cameraTransform.rotation = rotation;
        }

        private List<Behaviour> DisableCameraDrivers(Camera camera)
        {
            var disabled = new List<Behaviour>();

            if (camera == null)
                return disabled;

            var behaviours = camera.GetComponents<Behaviour>();

            foreach (var behaviour in behaviours)
            {
                if (behaviour == null || !behaviour.enabled)
                    continue;

                var typeName = behaviour.GetType().Name;

                if (typeName != "CinemachineBrain")
                    continue;

                behaviour.enabled = false;
                disabled.Add(behaviour);
                Debug.Log($"{nameof(BuildingConstructionSequence)}: disabled camera driver {typeName} during cinematic.", this);
            }

            return disabled;
        }

        private static void RestoreCameraDrivers(List<Behaviour> disabled)
        {
            if (disabled == null)
                return;

            foreach (var behaviour in disabled)
            {
                if (behaviour != null)
                    behaviour.enabled = true;
            }
        }

        private static IEnumerator WaitForPromise(IPromise promise, Action<Exception> onRejected)
        {
            if (promise == null)
                yield break;

            var complete = false;
            promise.Then(() => complete = true)
                .Catch(ex =>
                {
                    onRejected?.Invoke(ex);
                    complete = true;
                });

            while (!complete)
                yield return null;
        }

        private static IEnumerator WaitForAll(IPromise first, IPromise second, Action<Exception> onRejected)
        {
            var firstComplete = first == null;
            var secondComplete = second == null;

            first?.Then(() => firstComplete = true)
                .Catch(ex =>
                {
                    onRejected?.Invoke(ex);
                    firstComplete = true;
                });

            second?.Then(() => secondComplete = true)
                .Catch(ex =>
                {
                    onRejected?.Invoke(ex);
                    secondComplete = true;
                });

            while (!firstComplete || !secondComplete)
                yield return null;
        }
    }
}
