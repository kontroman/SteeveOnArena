using Devotion.SDK.Base;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Devotion.SDK.Managers
{
    public class UIManager : BaseManager
    {
        [SerializeField] private List<BaseWindow> windows;
        [SerializeField] private Canvas mainCanvas;

        private readonly List<BaseWindow> openedWindows = new List<BaseWindow>();
        private readonly Dictionary<Type, BaseWindow> cachedWindows = new Dictionary<Type, BaseWindow>();

        public void OpenWindow<T>() where T : BaseWindow
        {
            BaseWindow window = GetOrCreateWindow<T>();

            if (window == null) return;

            if (!openedWindows.Contains(window))
            {
                window.gameObject.SetActive(true);
                openedWindows.Add(window);
            }
        }

        public void CloseWindow<T>() where T : BaseWindow
        {
            BaseWindow window = GetWindowByType<T>();

            if (window != null && openedWindows.Contains(window))
            {
                window.gameObject.SetActive(false);
                openedWindows.Remove(window);
            }
        }

        public void CloseAllWindows()
        {
            foreach (BaseWindow window in openedWindows)
            {
                window.gameObject.SetActive(false);
            }

            openedWindows.Clear();
        }

        private T GetOrCreateWindow<T>() where T : BaseWindow
        {
            T window = GetWindowByType<T>();

            if (window == null)
            {
                window = CreateWindow<T>();
            }

            return window;
        }

        private T CreateWindow<T>() where T : BaseWindow
        {
            GameObject prefab = FindWindowPrefab<T>();

            if (prefab == null)
            {
                Debug.LogError($"Window prefab of type '{typeof(T)}' not found.");
                return null;
            }

            GameObject windowInstance = Instantiate(prefab, mainCanvas.transform);
            T windowComponent = windowInstance.GetComponent<T>();

            if (windowComponent != null)
            {
                RegisterWindow(windowComponent);
            }

            return windowComponent;
        }

        private GameObject FindWindowPrefab<T>() where T : BaseWindow
        {
            return windows.Find(w => w.GetType() == typeof(T))?.gameObject;
        }

        protected T GetWindowByType<T>() where T : BaseWindow
        {
            Type type = typeof(T);

            if (cachedWindows.ContainsKey(type))
            {
                return (T)cachedWindows[type];
            }

            BaseWindow window = windows.Find(w => w.GetType() == type);

            if (window != null)
            {
                cachedWindows[type] = window;
            }

            return (T)window;
        }

        public void RegisterWindow(BaseWindow window)
        {
            if (window == null || windows.Contains(window)) return;

            window.transform.SetParent(mainCanvas.transform, false);
            windows.Add(window);
        }

        public void UnregisterWindow(BaseWindow window)
        {
            if (window == null || !windows.Contains(window)) return;

            windows.Remove(window);
            cachedWindows.Remove(window.GetType());
        }
    }
}
