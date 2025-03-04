using UnityEngine;

namespace Devotion.SDK.GenericSingleton
{
    public class Singleton<T> : MonoBehaviour where T : Component
    {
        public static T _instance;

        public static T Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindAnyObjectByType<T>();

                    if (_instance == null)
                    {
                        GameObject gameObject = new GameObject("Auto-generated" + typeof(T));
                        _instance = gameObject.AddComponent<T>();
                    }
                }

                return _instance;
            }
        }
    }
}
