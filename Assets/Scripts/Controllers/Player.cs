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

namespace MineArena.Controllers
{
    public class Player : MonoBehaviour,
        IMessageSubscriber<Devotion.SDK.Messages.Player.PlayerProgressLoaded>
    {
        private List<Component> _components;

        public static Player Instance { get; private set; }
        public static event Action<PlayerExperience> ExperienceInitialized;

        public PlayerExperience Experience { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            _components = new List<Component>(GetComponents<Component>());
            Experience = new PlayerExperience(GetPlayerDataProgress());
            ExperienceInitialized?.Invoke(Experience);

            MessageService.Subscribe(this);

            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (Instance == this)
                MessageService.Unsubscribe(this);
        }

        private void Update()
        {
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
