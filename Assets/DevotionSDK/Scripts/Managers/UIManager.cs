using MineArena.Basics;
using Devotion.SDK.Base;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Devotion.SDK.Managers
{
    public class UIManager : BaseManager
    {
        [SerializeField] private List<BaseWindow> _windows = new List<BaseWindow>();
        [SerializeField] private Canvas _mainCanvas;

        private readonly List<BaseWindow> _openedWindows = new List<BaseWindow>();
        private readonly Dictionary<Type, BaseWindow> _cachedWindows = new Dictionary<Type, BaseWindow>();
        private readonly Dictionary<Type, BaseWindow> _tutorialPending = new Dictionary<Type, BaseWindow>();
        public bool HasOpenDialog => _openedWindows.Exists(w => w != null && w.gameObject.activeInHierarchy && !IsTransition(w.GetType()));
        private EventSystem _fallbackInput;
        private void OnEnable() { SceneManager.sceneLoaded += SceneLoaded; SceneManager.sceneUnloaded += SceneUnloaded; }
        private void OnDisable() { SceneManager.sceneLoaded -= SceneLoaded; SceneManager.sceneUnloaded -= SceneUnloaded; }
        private void SceneLoaded(Scene scene, LoadSceneMode mode) { if (Application.isPlaying) EnsureInputSystem(); }
        private void SceneUnloaded(Scene scene) { if (_fallbackInput != null) _fallbackInput.gameObject.SetActive(false); }
        public EventSystem EnsureInputSystem()
        {
            return ResolveInputSystem(FindObjectsOfType<EventSystem>());
        }
        private EventSystem ResolveInputSystem(EventSystem[] candidates)
        {
            foreach (var input in candidates)
            {
                if (input == _fallbackInput || !input.isActiveAndEnabled) continue;
                if (input.GetComponent<BaseInputModule>() == null) input.gameObject.AddComponent<StandaloneInputModule>();
                if (_fallbackInput != null) _fallbackInput.gameObject.SetActive(false);
                if (Application.isPlaying) EventSystem.current = input;
                return input;
            }
            if (_fallbackInput == null)
            {
                var inputObject = new GameObject("Persistent UI input");
                inputObject.SetActive(false);
                inputObject.transform.SetParent(transform, false);
                _fallbackInput = inputObject.AddComponent<EventSystem>();
                inputObject.AddComponent<StandaloneInputModule>();
            }
            _fallbackInput.gameObject.SetActive(true);
            if (Application.isPlaying) EventSystem.current = _fallbackInput;
            return _fallbackInput;
        }

        private static bool IsTransition(Type type) => type.Name == "PlayingWindow" || type.Name == "LoadingWindow" || type.Name == "BlackWindow" || type.Name == "LevelProgressWindow";
        public void CloseTutorialDialogs()
        {
            foreach (var window in FindObjectsOfType<BaseWindow>())
                if (!IsTransition(window.GetType())) window.CloseWindow();
        }
        private void Update()
        {
            if (Application.isPlaying && (EventSystem.current == null || !EventSystem.current.isActiveAndEnabled)) EnsureInputSystem();
            if (MineArena.Managers.TutorialService.AwaitingConfirmation || _tutorialPending.Count == 0) return;
            // CraftingWindow also has a standalone opening path, outside the cached-window list.
            foreach (var shown in FindObjectsOfType<BaseWindow>())
                if (!IsTransition(shown.GetType())) return;
            var pending = new List<KeyValuePair<Type, BaseWindow>>(_tutorialPending);
            foreach (var entry in pending)
            {
                _tutorialPending.Remove(entry.Key);
                if (entry.Value == null || !MineArena.Managers.TutorialService.AllowWindow(entry.Key)) continue;
                Activate(entry.Value);
                break;
            }
        }
        private void Activate(BaseWindow window)
        {
            if (MineArena.Managers.TutorialService.Active && !IsTransition(window.GetType()))
                foreach (var other in new List<BaseWindow>(_openedWindows))
                    if (other != null && other != window && !IsTransition(other.GetType()))
                    { other.gameObject.SetActive(false); _openedWindows.Remove(other); }
            if (!_openedWindows.Contains(window)) { window.gameObject.SetActive(true); _openedWindows.Add(window); }
            window.transform.SetAsLastSibling();
        }

        public BaseWindow OpenWindow<T>() where T : BaseWindow
        {
            if (!MineArena.Managers.TutorialService.AllowWindow(typeof(T))) return null;
            EnsureMainCanvas();
            if (Application.isPlaying) EnsureInputSystem();

            BaseWindow window = GetOrCreateWindow<T>();

            if (window == null) return null;
            if (MineArena.Managers.TutorialService.AwaitingConfirmation && !IsTransition(typeof(T)))
            {
                // Return the inactive instance so callers can bind building data before deferred activation.
                if (window.gameObject.activeSelf) window.gameObject.SetActive(false);
                _openedWindows.Remove(window);
                _tutorialPending[typeof(T)] = window;
                return window;
            }
            Activate(window);

            return window;
        }

        private void EnsureMainCanvas(Canvas[] candidates = null)
        {
            if (_mainCanvas == null)
            {
                // FindGameObjectWithTag skips inactive objects and may return null between scenes.
                foreach (var canvas in candidates ?? FindObjectsOfType<Canvas>(true))
                    if (canvas.gameObject.scene.IsValid() && canvas.CompareTag(Constants.GameTags.MainCanvas))
                    {
                        _mainCanvas = canvas;
                        break;
                    }
            }
            if (_mainCanvas == null)
            {
                var root = new GameObject("MainCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                root.tag = Constants.GameTags.MainCanvas;
                _mainCanvas = root.GetComponent<Canvas>();
                _mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                var scaler = root.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = .5f;
            }
            if (!_mainCanvas.gameObject.activeInHierarchy && _mainCanvas.transform.parent != null)
                _mainCanvas.transform.SetParent(null, true);
            _mainCanvas.gameObject.SetActive(true);
            _mainCanvas.enabled = true;
            if (_mainCanvas.GetComponent<GraphicRaycaster>() == null) _mainCanvas.gameObject.AddComponent<GraphicRaycaster>();
            if (Application.isPlaying)
            {
                // Persist only the UI root, even if a scene supplied a nested canvas.
                _mainCanvas.transform.SetParent(null, true);
                DontDestroyOnLoad(_mainCanvas.gameObject);
            }
        }

        public BaseWindow ShowWindow<T>() where T : BaseWindow
        {
            return OpenWindow<T>();
        }

        public void CloseWindow<T>() where T : BaseWindow
        {
            _tutorialPending.Remove(typeof(T));
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
            _tutorialPending.Clear();
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
            return _windows?.Find(w => w != null && w.GetType() == typeof(T))?.gameObject;
        }

        protected T GetWindowByType<T>() where T : BaseWindow
        {
            Type type = typeof(T);

            if (_cachedWindows.TryGetValue(type, out BaseWindow cachedWindow))
            {
                if (cachedWindow != null) return (T)cachedWindow;
                _cachedWindows.Remove(type);
                _openedWindows.RemoveAll(w => w == null);
            }

            BaseWindow windowPrefab = _windows?.Find(w => w != null && w.GetType() == type);
            if (windowPrefab == null)
            {
                Debug.LogError($"Window of type '{type}' not found.");
                return null;
            }

            // Prevent prefab OnEnable from opening child views before its queued turn.
            var staging = new GameObject("Inactive window staging");
            staging.transform.SetParent(_mainCanvas.transform, false);
            staging.SetActive(false);
            var newWindow = Instantiate(windowPrefab, staging.transform);
            newWindow.gameObject.SetActive(false);
            newWindow.transform.SetParent(_mainCanvas.transform, false);
            if (Application.isPlaying) Destroy(staging); else DestroyImmediate(staging);
            _cachedWindows[type] = newWindow;

            return (T)newWindow;
        }

        public void RegisterWindow(BaseWindow window)
        {
            _windows ??= new List<BaseWindow>();
            if (window == null || _windows.Contains(window)) return;
            EnsureMainCanvas();
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
