using System;
using System.Linq;
using Devotion.SDK.Base;
using Devotion.SDK.Controllers;
using MineArena.Controllers;
using MineArena.PlayerSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MineArena.Windows
{
    public sealed class PlayerStatsWindow : BaseWindow
    {
        [Serializable] public sealed class AttributeRow
        {
            public TMP_Text rank, effect;
            public Button minus, plus;
        }
        [SerializeField] private TMP_Text playerName, level, experience, points, feedback;
        [SerializeField] private Slider experienceBar;
        [SerializeField] private Button save, cancel;
        [SerializeField] private AttributeRow[] rows;
        private int[] _draft = new int[5];
        private int[] _saved = new int[5];
        private PlayerExperience _experience;
        public static readonly string[] Titles = { "Здоровье", "Движение", "Атака", "Удача", "Защита" };
        public static readonly string[] Descriptions = {
            "+2 к максимальному HP за очко. Лечение отдельно.",
            "+0,5% к скорости передвижения за очко.",
            "+1% к урону меча и лука за очко.",
            "+0,5% к шансу критического удара. Крит: урон ×1,5.",
            "+1 к защите за очко. Снижает урон вместе с бронёй."
        };
        private void Awake()
        {
            if (rows == null) return;
            for (int i = 0; i < rows.Length; i++)
            {
                int index = i;
                rows[i].minus.onClick.AddListener(() => Adjust(index, -1));
                rows[i].plus.onClick.AddListener(() => Adjust(index, 1));
            }
            save.onClick.AddListener(SaveChanges);
            cancel.onClick.AddListener(CloseWindow);
            playerName.richText = false;
        }
        private void OnEnable()
        {
            if (!Application.isPlaying || rows == null) return;
            Player.ExperienceInitialized += Bind;
            BeginDraft();
            Bind(Player.Instance?.Experience);
            PlayerIdentity.RequestName();
        }
        private void OnDisable()
        {
            Player.ExperienceInitialized -= Bind;
            if (_experience != null) _experience.OnExperienceChanged -= ExperienceChanged;
            _experience = null;
        }
        private void Bind(PlayerExperience value)
        {
            if (_experience != null) _experience.OnExperienceChanged -= ExperienceChanged;
            _experience = value;
            if (_experience != null) _experience.OnExperienceChanged += ExperienceChanged;
            BeginDraft();
        }
        private void BeginDraft()
        {
            _saved = PlayerDevelopment.Progress?.CopyDevelopment() ?? new int[5];
            _draft = (int[])_saved.Clone();
            if (feedback != null) feedback.text = "Распределите очки и нажмите «Сохранить».";
            Refresh();
        }
        private void ExperienceChanged(float current, float maximum) => Refresh();
        private int FreePoints => Mathf.Max(0, (_experience?.CurrentLevel ?? 1) - 1) - _draft.Sum();
        public void Adjust(int index, int delta)
        {
            if (index < 0 || index >= 5 || (delta != 1 && delta != -1)) return;
            if (delta > 0 && (FreePoints <= 0 || _draft[index] >= 50) || delta < 0 && _draft[index] <= _saved[index]) return;
            _draft[index] += delta;
            feedback.text = "Есть несохранённые изменения";
            Refresh();
        }
        public void SaveChanges()
        {
            if (!PlayerDevelopment.Save(_draft)) { feedback.text = "Не удалось применить: проверьте доступные очки."; Refresh(); return; }
            _saved = (int[])_draft.Clone();
            feedback.text = "Сохранено. Характеристики применены.";
            Refresh();
        }
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape)) CloseWindow();
            if (playerName != null) playerName.text = PlayerIdentity.DisplayName;
        }
        private void Refresh()
        {
            if (rows == null || rows.Length != 5 || points == null) return;
            int currentLevel = _experience?.CurrentLevel ?? 1;
            playerName.text = PlayerIdentity.DisplayName;
            level.text = "УРОВЕНЬ " + currentLevel;
            bool maximum = currentLevel >= PlayerExperience.MaxLevel;
            experience.text = maximum ? "Максимальный уровень" : $"Опыт: {_experience?.CurrentExperience ?? 0:N0} / {_experience?.ExperiencePerLevel ?? 60:N0}";
            experienceBar.value = maximum ? 1f : (_experience?.CurrentExperience ?? 0) / (float)(_experience?.ExperiencePerLevel ?? 60);
            points.text = "Очки развития: " + FreePoints;
            for (int i = 0; i < rows.Length; i++)
            {
                rows[i].rank.text = _draft[i] + " / 50";
                rows[i].effect.text = Effect(i, _draft[i]);
                rows[i].minus.interactable = _draft[i] > _saved[i];
                rows[i].plus.interactable = FreePoints > 0 && _draft[i] < 50;
                rows[i].rank.color = _draft[i] != _saved[i] ? new Color32(44, 133, 139, 255) : new Color32(81, 71, 55, 255);
            }
            save.interactable = PlayerDevelopment.Progress != null && !_draft.SequenceEqual(_saved) && FreePoints >= 0;
        }
        public static string Effect(int attribute, int rank) => attribute switch {
            0 => "+" + (rank * PlayerDevelopment.HealthPerPoint).ToString("0.#") + " HP",
            1 => "+" + (rank * PlayerDevelopment.MovementPerPoint * 100).ToString("0.#") + "% скорости",
            2 => "+" + (rank * PlayerDevelopment.AttackPerPoint * 100).ToString("0.#") + "% урона",
            3 => (rank * PlayerDevelopment.LuckPerPoint * 100).ToString("0.#") + "% крит. шанс",
            _ => "−" + (100f * (1f - 1f / (1f + PlayerDevelopment.DefensePerPoint * rank))).ToString("0.#") + "% урона"
        };
        public override void CloseWindow() => GameRoot.UIManager.CloseWindow<PlayerStatsWindow>();
    }
}
