using MineArena.Controllers;
using UnityEngine;
using UnityEngine.UI;

namespace MineArena.UI
{
    public sealed class ArenaExitUI : MonoBehaviour
    {
        [SerializeField] private Button _exitButton;
        [SerializeField] private GameObject _confirmation;
        [SerializeField] private Button _confirmButton;
        [SerializeField] private Button _cancelButton;
        private bool _paused;
        private float _previousTimeScale;

        private void Awake()
        {
            _exitButton.onClick.AddListener(OpenConfirmation);
            _confirmButton.onClick.AddListener(Confirm);
            _cancelButton.onClick.AddListener(Cancel);
            _confirmation.SetActive(false);
        }

        private void Update()
        {
            var level = LevelController.Current;
            bool visible = level != null && level.CanAbandon;
            _exitButton.gameObject.SetActive(visible);
            if (!visible && _paused) Cancel();
            if (_paused && Input.GetKeyDown(KeyCode.Escape)) Cancel();
        }

        private void OpenConfirmation()
        {
            if (_paused || LevelController.Current == null || !LevelController.Current.CanAbandon) return;
            _previousTimeScale = Time.timeScale;
            _paused = true;
            Time.timeScale = 0f;
            _confirmation.SetActive(true);
            transform.SetAsLastSibling();
        }

        private void Confirm()
        {
            var level = LevelController.Current;
            Cancel();
            if (level != null) level.AbandonLevel();
        }

        private void Cancel()
        {
            _confirmation.SetActive(false);
            if (!_paused) return;
            _paused = false;
            Time.timeScale = _previousTimeScale;
        }

        private void OnDisable() => Cancel();
    }
}
