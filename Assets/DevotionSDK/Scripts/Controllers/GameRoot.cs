using System.Collections.Generic;
using UnityEngine;
using Devotion.SDK.Managers;
using MineArena.Structs;
using Devotion.SDK.UI;
using Devotion.SDK.Services.SaveSystem.Progress;

namespace Devotion.SDK.Controllers
{
    public class GameRoot : MonoBehaviour
    {
        public static GameRoot Instance { get; private set; }

        [SerializeField] private GameConfig gameConfig; 
        [SerializeField] private List<BaseManager> _startManagers = new List<BaseManager>();
        [SerializeField] private PlayerProgress playerProgress;

        private Dictionary<System.Type, BaseManager> _managers = new Dictionary<System.Type, BaseManager>();

        public static GameConfig GameConfig => Instance.gameConfig;
        public static UIManager UIManager => GetManager<UIManager>();
        public static PlayerProgress PlayerProgress => PlayerProgress;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            foreach (var manager in _startManagers)
            {
                if (manager != null)
                {
                    System.Type type = manager.GetType();

                    LoadManager(manager, type);
                }
            }

            Debug.LogError("[TODO]: IPromise game initialization");
            UIManager.ShowWindow<PlayingWindow>();
        }

        public static T GetManager<T>() where T : BaseManager
        {
            System.Type type = typeof(T);

            if (Instance._managers.TryGetValue(type, out BaseManager manager))
            {
                return manager as T;
            }
            else
            {
                T loadedManager = Resources.Load<T>("ManagersPrefabs/" + type.Name);

                return LoadManager(loadedManager, type) as T;
            }
        }

        private static BaseManager LoadManager(BaseManager manager, System.Type type)
        {
            if (manager != null)
            {
                var instantiatedManager = Instantiate(manager);
                instantiatedManager.transform.parent = Instance.transform;
                Instance._managers[type] = instantiatedManager;

                Debug.Log($"Created {type.Name} by request");

                return manager;
            }
            else
            {
                Debug.Log($"Manager {type.Name} not found in ManagersPrefabs folder");

                return null;
            }
        }
    }
}
