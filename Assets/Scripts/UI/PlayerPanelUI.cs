using Devotion.SDK.Controllers;
using MineArena.Controllers;
using MineArena.PlayerSystem;
using MineArena.Windows;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MineArena.UI
{
    public sealed class PlayerPanelUI : MonoBehaviour
    {
        private TMP_Text _name;
        private TMP_Text _points;
        private PlayerExperience _experience;
        private float _noticeUntil, _nextRefresh;
        private void Awake()
        {
            foreach (var graphic in GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = false;
            var background = GetComponent<Image>() ?? gameObject.AddComponent<Image>();
            background.raycastTarget = true;
            var button = GetComponent<Button>() ?? gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            button.onClick.AddListener(Open);
            foreach (var text in GetComponentsInChildren<TMP_Text>(true))
                if (text.name == "PlayerName") { _name = text; _name.richText = false; }
            var hint = transform.Find("DevelopmentHint");
            if (hint != null) _points = hint.GetComponent<TMP_Text>();
            PlayerIdentity.RequestName();
        }
        private void OnEnable()
        {
            Player.ExperienceInitialized += Bind;
            PlayerDevelopment.Changed += Refresh;
            Bind(Player.Instance?.Experience);
        }
        private void OnDisable()
        {
            Player.ExperienceInitialized -= Bind;
            PlayerDevelopment.Changed -= Refresh;
            if (_experience != null) _experience.OnExperienceGained -= Gained;
            _experience = null;
        }
        private void Bind(PlayerExperience experience)
        {
            if (_experience != null) _experience.OnExperienceGained -= Gained;
            _experience = experience;
            if (_experience != null) _experience.OnExperienceGained += Gained;
            Refresh();
        }
        private void Gained(int amount, int levels)
        {
            if (_points == null) return;
            _points.text = levels > 0 ? "УРОВЕНЬ " + _experience.CurrentLevel + "!  +" + levels + " очк. развития" : "+" + amount + " опыта";
            _noticeUntil = Time.unscaledTime + (levels > 0 ? 5f : 2f);
        }
        private void Update()
        {
            if (Time.unscaledTime < _nextRefresh) return;
            _nextRefresh = Time.unscaledTime + .25f;
            Refresh();
        }
        private void Refresh()
        {
            if (_name != null) _name.text = PlayerIdentity.DisplayName;
            if (_points == null || Time.unscaledTime < _noticeUntil) return;
            var progress = PlayerDevelopment.Progress;
            int free = Mathf.Max(0, (Player.Instance?.Experience?.CurrentLevel ?? 1) - 1);
            if (progress != null) foreach (int rank in progress.CopyDevelopment()) free -= rank;
            _points.text = free > 0 ? "Развитие: +" + free + "  •  Нажмите" : "Характеристики  •  Нажмите";
        }
        public void Open()
        {
            if (!PlayerMovement.IsPlayerDead) GameRoot.UIManager?.OpenWindow<PlayerStatsWindow>();
        }
    }
}
