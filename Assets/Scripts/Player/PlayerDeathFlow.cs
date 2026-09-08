using System;
using Devotion.SDK.Controllers;
using MineArena.Basics;
using MineArena.Controllers;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MineArena.PlayerSystem
{
    // The SDK adapter must report true only after its reward callback.
    public interface IReviveAdsProvider
    {
        void ShowReviveAd(Action<bool> completed);
    }

    public class PlayerDeathFlow : MonoBehaviour
    {
        private enum State { Alive, Choosing, Advertising, Returning }
        private State _state;
        private int _request;
        private float _protectedUntil, _adDeadline;
        private GameObject _window;
        private Button _revive, _village;
        private TextMeshProUGUI _status;
        public bool IsProtected => _state != State.Alive || Time.time < _protectedUntil;

        private void OnEnable() => SceneManager.sceneLoaded += SceneLoaded;
        private void OnDisable()
        {
            SceneManager.sceneLoaded -= SceneLoaded;
            ++_request;
            if (_window != null) Destroy(_window);
        }
        private void SceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mode == LoadSceneMode.Additive) return;
            ++_request;
            _state = State.Alive;
            if (_window != null) Destroy(_window);
        }
        private void Update()
        {
            if (_state == State.Advertising && Time.realtimeSinceStartup >= _adDeadline)
                FinishAd(_request, false);
        }
        public void BeginDeath()
        {
            if (_state != State.Alive) return;
            _state = State.Choosing;
            ++_request;
            GameRoot.UIManager?.CloseAllWindows();
            GameRoot.UIManager?.EnsureInputSystem();
            BuildWindow();
        }
        private IReviveAdsProvider FindProvider()
        {
            foreach (var component in FindObjectsOfType<MonoBehaviour>())
                if (component is IReviveAdsProvider provider) return provider;
            return null;
        }
        public void RequestRevive()
        {
            if (_state != State.Choosing) return;
            var provider = FindProvider();
            if (provider == null) { _status.text = "Реклама недоступна. Можно вернуться в деревню."; return; }
            _state = State.Advertising;
            int request = ++_request;
            _adDeadline = Time.realtimeSinceStartup + 120f;
            _revive.interactable = _village.interactable = false;
            _status.text = "Ожидание награды за рекламу…";
            try { provider.ShowReviveAd(success => FinishAd(request, success)); }
            catch (Exception ex) { Debug.LogException(ex); FinishAd(request, false); }
        }
        private void FinishAd(int request, bool success)
        {
            if (this == null || request != _request || _state != State.Advertising) return;
            ++_request;
            if (!success)
            {
                _state = State.Choosing;
                _revive.interactable = _village.interactable = true;
                _status.text = "Награда не получена. Повторите попытку или вернитесь в деревню.";
                return;
            }
            _state = State.Alive;
            _protectedUntil = Time.time + 3f;
            Player.Instance.RestoreAt(transform.position, transform.rotation);
            Destroy(_window);
            GameRoot.UIManager?.ShowWindow<Devotion.SDK.UI.PlayingWindow>();
            if (LevelController.Current != null) GameRoot.UIManager?.ShowWindow<MineArena.Windows.LevelProgressWindow>();
        }
        public void ReturnToVillage()
        {
            if (_state != State.Choosing) return;
            _state = State.Returning;
            ++_request;
            _revive.interactable = _village.interactable = false;
            try
            {
                if (SceneManager.GetActiveScene().name == Constants.SceneNames.PlayerBaseScene)
                {
                    Player.Instance.ReturnToVillageSpawn();
                    _state = State.Alive;
                    Destroy(_window);
                    GameRoot.UIManager?.ShowWindow<Devotion.SDK.UI.PlayingWindow>();
                }
                else SceneManager.LoadSceneAsync(Constants.SceneNames.PlayerBaseScene);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                _state = State.Choosing;
                _revive.interactable = _village.interactable = true;
                _status.text = "Не удалось загрузить деревню. Попробуйте ещё раз.";
            }
        }
        private void BuildWindow()
        {
            _window = new GameObject("DeathWindow", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            _window.transform.SetParent(transform, false);
            var canvas = _window.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 30000;
            var scaler = _window.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = .5f;
            var prefab = Resources.Load<MineArena.Windows.DeathWindow>("UI/DeathWindow");
            if (prefab == null) throw new InvalidOperationException("UI/DeathWindow prefab is missing.");
            var view = Instantiate(prefab, _window.transform, false);
            view.Setup(RequestRevive, ReturnToVillage);
            _revive = view.ReviveButton;
            _village = view.VillageButton;
            _status = view.StatusText;
            _revive.interactable = FindProvider() != null;
            if (!_revive.interactable) _status.text = "Реклама сейчас недоступна. Вернитесь в деревню и попробуйте снова позже.";
        }
    }
}