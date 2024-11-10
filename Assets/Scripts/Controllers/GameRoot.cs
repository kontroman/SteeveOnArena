using System.Collections.Generic;
using UnityEngine;
using Devotion.Managers;

namespace Devotion.Controllers
{
    public class GameRoot : MonoBehaviour
    {
        public static GameRoot Instance { get; private set; }

        [SerializeField] private List<BaseManager> _startManagers = new();

        private Dictionary<System.Type, BaseManager> _managers = new Dictionary<System.Type, BaseManager>();

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
        }

        public T GetManager<T>() where T : BaseManager
        {
            System.Type type = typeof(T);

            if (_managers.TryGetValue(type, out BaseManager manager))
            {
                return manager as T;
            }
            else
            {
                T loadedManager = Resources.Load<T>("ManagersPrefabs/" + type.Name);

                return LoadManager(loadedManager, type) as T;
            }
        }

        private BaseManager LoadManager(BaseManager manager, System.Type type)
        {
            if (manager != null)
            {
                var instantiatedManager = Instantiate(manager);
                instantiatedManager.transform.parent = transform;
                _managers[type] = instantiatedManager;

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
