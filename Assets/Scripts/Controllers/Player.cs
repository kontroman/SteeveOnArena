using Devotion.SDK.Controllers;
using Devotion.SDK.Services.SaveSystem.Progress;
using MineArena.Messages;
using MineArena.Messages.MessageService;
using MineArena.PlayerSystem;
using MineArena.UI;
using MineArena.Windows.Crafting;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using Cinemachine;

namespace MineArena.Controllers
{
    public class Player : MonoBehaviour,
        IMessageSubscriber<Devotion.SDK.Messages.Player.PlayerProgressLoaded>
    {
        private List<Component> _components;

        public static Player Instance { get; private set; }
        public static event Action<PlayerExperience> ExperienceInitialized;

        public PlayerExperience Experience { get; private set; }
        private Vector3 _lobbyPosition;
        private Quaternion _lobbyRotation;
        private bool _visitedExpedition;
        private Camera _sceneCamera;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                if (gameObject.scene.name == MineArena.Basics.Constants.SceneNames.PlayerBaseScene)
                {
                    Instance._lobbyPosition = transform.position;
                    Instance._lobbyRotation = transform.rotation;
                }
                Destroy(gameObject);
                return;
            }

            Instance = this;
            if (GetComponent<PlayerDeathFlow>() == null) gameObject.AddComponent<PlayerDeathFlow>();
            _lobbyPosition = transform.position; _lobbyRotation = transform.rotation;
            SceneManager.sceneLoaded += HandleSceneLoaded;

            _components = new List<Component>(GetComponents<Component>());
            Experience = new PlayerExperience(GetPlayerDataProgress());
            ExperienceInitialized?.Invoke(Experience);

            MessageService.Subscribe(this);

            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                MessageService.Unsubscribe(this);
                SceneManager.sceneLoaded -= HandleSceneLoaded;
                Instance = null;
            }
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // Gameplay has no camera of its own. Carry one rig, replacing it on return to base.
            foreach (var root in scene.GetRootGameObjects())
            foreach (var candidate in root.GetComponentsInChildren<Camera>(true))
            {
                if (!candidate.CompareTag("MainCamera") || !candidate.isActiveAndEnabled) continue;
                if (_sceneCamera != null && _sceneCamera != candidate)
                {
                    _sceneCamera.gameObject.SetActive(false);
                    Destroy(_sceneCamera.gameObject);
                }
                _sceneCamera = candidate;
                DontDestroyOnLoad(candidate.transform.root.gameObject);
            }
            if (scene.name == MineArena.Basics.Constants.SceneNames.GameplayScene) _visitedExpedition = true;
            BindSceneCameras(scene);
            if (_sceneCamera != null) BindSceneCameras(_sceneCamera.gameObject.scene);
            if (_visitedExpedition && scene.name == MineArena.Basics.Constants.SceneNames.PlayerBaseScene)
                StartCoroutine(RestoreLobby());
        }

        private IEnumerator RestoreLobby()
        {
            // Let scene Awake/Start complete and discard duplicate scene singletons first.
            yield return null;
            RestoreAt(_lobbyPosition, _lobbyRotation);
            GameRoot.GetManager<MineArena.Managers.BuildingManager>()?.InitManager();
            GameRoot.UIManager.ShowWindow<Devotion.SDK.UI.PlayingWindow>();
        }

        public void ReturnToVillageSpawn() => RestoreAt(_lobbyPosition, _lobbyRotation);

        public void RestoreAt(Vector3 position, Quaternion rotation)
        {
            var controller = GetComponent<CharacterController>();
            if (controller != null) controller.enabled = false;
            transform.SetPositionAndRotation(position, rotation);
            if (controller != null) controller.enabled = true;
            GetComponent<MineArena.Game.Health.Health>()?.RestoreFullHealth();
            GetComponent<PlayerAnimatorController>()?.ResetAfterDeath();
            GetComponent<PlayerMovement>()?.SetAlive();
            GetComponent<PlayerAttack>()?.SetComponentEnable(true);
            BindSceneCameras(Application.isPlaying ? SceneManager.GetActiveScene() : gameObject.scene);
            if (_sceneCamera != null) BindSceneCameras(_sceneCamera.gameObject.scene);
        }

        private void BindSceneCameras(Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
            foreach (var camera in root.GetComponentsInChildren<CinemachineVirtualCameraBase>(true))
            {
                bool playerCamera = camera.GetComponent<CameraZoomController>() != null;
                if (playerCamera || camera.Follow == null || camera.Follow.GetComponentInParent<Player>() != null) camera.Follow = transform;
                if (playerCamera || camera.LookAt != null && camera.LookAt.GetComponentInParent<Player>() != null) camera.LookAt = transform;
                camera.PreviousStateIsValid = false;
            }
        }

        private void Update()
        {
            if (PlayerMovement.IsPlayerDead) return;
            if(Input.GetKeyDown(KeyCode.I)) 
                GameRoot.UIManager.OpenWindow<InventoryWindow>();

            if (Input.GetKeyDown(KeyCode.B))
                CraftingWindow.Toggle();
        }

        public T GetComponentFromList<T>() where T : Component
        {
            foreach (var component in _components)
            {
                if (component is T)
                {
                    return component as T;
                }
            }
            return null;
        }

        public T AddComponentToList<T>() where T : Component
        {
            T newComponent = gameObject.AddComponent<T>();
            _components.Add(newComponent);
            return newComponent;
        }

        public void RemoveComponentFromList<T>() where T : Component
        {
            T component = GetComponentFromList<T>();
            if (component != null)
            {
                _components.Remove(component);
                Destroy(component);
            }
        }

        public void OnMessage(Devotion.SDK.Messages.Player.PlayerProgressLoaded message)
        {
            Experience ??= new PlayerExperience();
            Experience.BindProgress(GetPlayerDataProgress());
            ExperienceInitialized?.Invoke(Experience);
        }

        private static PlayerDataProgress GetPlayerDataProgress()
        {
            if (GameRoot.Instance == null || GameRoot.PlayerProgress == null)
                return null;

            return GameRoot.PlayerProgress.PlayerDataProgress;
        }
    }
}
