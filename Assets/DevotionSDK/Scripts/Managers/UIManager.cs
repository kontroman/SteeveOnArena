using MineArena.Basics;
using Devotion.SDK.Base;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Devotion.SDK.Managers
{
    public class UIManager : BaseManager
    {
        [SerializeField] private List<BaseWindow> _windows;
        [SerializeField] private Canvas _mainCanvas;

        private readonly List<BaseWindow> _openedWindows = new List<BaseWindow>();
        private readonly Dictionary<Type, BaseWindow> _cachedWindows = new Dictionary<Type, BaseWindow>();

        public BaseWindow OpenWindow<T>() where T : BaseWindow
        {
            if (_mainCanvas == null)
                _mainCanvas = GameObject.FindGameObjectWithTag(Constants.GameTags.MainCanvas).GetComponent<Canvas>();

            DontDestroyOnLoad(_mainCanvas.gameObject);

            BaseWindow window = GetOrCreateWindow<T>();

            if (window == null) return null;

            if (!_openedWindows.Contains(window))
            {
                window.gameObject.SetActive(true);
                _openedWindows.Add(window);
            }

            return window;
        }

        public BaseWindow ShowWindow<T>() where T : BaseWindow
        {
            return OpenWindow<T>();
        }

        public void CloseWindow<T>() where T : BaseWindow
        {
            for (int i = 0; i < _openedWindows.Count; i++)
            {
                if (_openedWindows[i] is T window)
                {
                    window.gameObject.SetActive(false);
                    _openedWindows.RemoveAt(i);
                    return;
                }
            }
        }

        public void CloseAllWindows()
        {
            foreach (BaseWindow window in _openedWindows)
            {
                if (window != null)
                    window.gameObject.SetActive(false);
            }

            _openedWindows.Clear();
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

            GameObject windowInstance = Instantiate(prefab, _mainCanvas.transform);
            T windowComponent = windowInstance.GetComponent<T>();

            if (windowComponent != null)
            {
                windowComponent.gameObject.SetActive(false);
                RegisterWindow(windowComponent);
            }

            return windowComponent;
        }

        private GameObject FindWindowPrefab<T>() where T : BaseWindow
        {
            return _windows.Find(w => w.GetType() == typeof(T))?.gameObject;
        }

        protected T GetWindowByType<T>() where T : BaseWindow
        {
            Type type = typeof(T);

            if (_cachedWindows.TryGetValue(type, out BaseWindow cachedWindow))
            {
                return (T)cachedWindow;
            }

            BaseWindow windowPrefab = _windows.Find(w => w.GetType() == type);
            if (windowPrefab == null)
            {
                Debug.LogError($"Window of type '{type}' not found.");
                return null;
            }

            var newWindow = Instantiate(windowPrefab, _mainCanvas.transform);
            newWindow.gameObject.SetActive(false);
            _cachedWindows[type] = newWindow;

            return (T)newWindow;
        }

        public void RegisterWindow(BaseWindow window)
        {
            if (window == null || _windows.Contains(window)) return;

            window.transform.SetParent(_mainCanvas.transform, false);
            _windows.Add(window);
            _cachedWindows[window.GetType()] = window;
        }

        public void UnregisterWindow(BaseWindow window)
        {
            if (window == null || !_windows.Contains(window)) return;

            _windows.Remove(window);
            _cachedWindows.Remove(window.GetType());
        }
    }
}
