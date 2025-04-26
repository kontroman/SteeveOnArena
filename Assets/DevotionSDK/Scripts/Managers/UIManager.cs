using Devotion.Basics;
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
                _mainCanvas = GameObject.FindGameObjectWithTag(Constants.GameTags.MainCanvas).GetComponent<Canvas>(); ;

            BaseWindow window = GetOrCreateWindow<T>();

            if (window == null) return null;

            if (!_openedWindows.Contains(window))
            {
                window.gameObject.SetActive(true);
                _openedWindows.Add(window);
            }

            return window;
        }

        public void CloseWindow<T>() where T : BaseWindow
        {
            BaseWindow window = GetWindowByType<T>();

            if (window != null && _openedWindows.Contains(window))
            {
                window.gameObject.SetActive(false);
                _openedWindows.Remove(window);
            }
        }

        public void CloseAllWindows()
        {
            foreach (BaseWindow window in _openedWindows)
            {
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

            if (_cachedWindows.ContainsKey(type))
            {
                return (T)_cachedWindows[type];
            }

            BaseWindow window = _windows.Find(w => w.GetType() == type);

            var newWindow = Instantiate(window, _mainCanvas.transform);

            if (window != null)
            {
                _cachedWindows[type] = newWindow;
            }

            return (T)newWindow;
        }

        public void RegisterWindow(BaseWindow window)
        {
            if (window == null || _windows.Contains(window)) return;

            window.transform.SetParent(_mainCanvas.transform, false);
            _windows.Add(window);
        }

        public void UnregisterWindow(BaseWindow window)
        {
            if (window == null || !_windows.Contains(window)) return;

            _windows.Remove(window);
            _cachedWindows.Remove(window.GetType());
        }
    }
}
