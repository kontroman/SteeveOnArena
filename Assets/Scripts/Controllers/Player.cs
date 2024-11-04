using System.Collections.Generic;
using UnityEngine;

namespace Devotion.Controllers
{
    public class Player : MonoBehaviour
    {
        private List<Component> _components;

        public static Player Instance { get; private set; }

        private void Start()
        {
            Instance = this;

            _components = new List<Component>(GetComponents<Component>());
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
    }
}
