using Devotion.SDK.Controllers;
using UnityEngine;

namespace MineArena.UI
{
    public sealed class PlaytimeRewardClock : MonoBehaviour
    {
        private float _saveTimer;
        private void Update()
        {
            if (!Application.isFocused || Time.timeScale <= 0 || GameRoot.PlayerProgress == null) return;
            GameRoot.PlayerProgress.PlaytimeGiftProgress.SecondsPlayed += Time.unscaledDeltaTime;
            _saveTimer += Time.unscaledDeltaTime;
            if (_saveTimer >= 30) Flush();
        }
        private void OnDisable() => Flush();
        private void OnApplicationPause(bool paused) { if (paused) Flush(); }
        private void OnApplicationQuit() => Flush();
        private void Flush()
        {
            if (_saveTimer <= 0) return;
            GameRoot.PlayerProgress?.PlaytimeGiftProgress?.Save();
            _saveTimer = 0;
        }
    }
}
