using System.Collections.Generic;
using UnityEngine;

namespace Devotion.Controllers
{
    public class GameRoot : MonoBehaviour
    {
        public static GameRoot Instance { get; private set; }

        public List<IManager> _managers = new List<IManager>();

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

        public T GetManager<T>() where T : class, IManager
        {
            T manager = _managers.Find(m => m is T) as T;

            if (manager != null)
            {
                return manager;
            }
            else
            {
                manager = Resources.Load("ManagersPrefabs/" + typeof(T).Name) as T;

                if (manager != null)
                    _managers.Add(manager);
                else
                    Debug.LogWarning($"Manager {typeof(T).Name} not found in ManagersPrefabs folder");

                return manager;
            }
        }
    }
}
