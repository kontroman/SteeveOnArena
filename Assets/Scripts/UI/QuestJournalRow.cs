using System;
using Achievements;
using Devotion.SDK.Services.Localization;
using MineArena.Items;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MineArena.UI
{
    public sealed class QuestJournalRow : MonoBehaviour
    {
        [SerializeField] private TMP_Text title, status, progressText;
        [SerializeField] private Slider progress;
        [SerializeField] private Image background, stripe, flatIcon;
        [SerializeField] private ResourceIcon blockIcon;
        [SerializeField] private Button button;
        [SerializeField] private Sprite normal, selected;
        public void Bind(Achievement quest, bool isSelected, Action click)
        {
            title.text = Title(quest);
            bool ready = quest.CanTakePrize && !quest.IsCompleted;
            status.text = quest.IsCompleted ? "Награда получена" : ready ? "Можно забрать награду" : "В работе";
            Color accent = quest.IsCompleted ? new Color32(139, 151, 127, 255) : ready ? new Color32(193, 124, 48, 255) : new Color32(61, 134, 146, 255);
            stripe.color = status.color = accent;
            progress.value = quest.MaxValueProgress > 0 ? (float)quest.CurrentValueProgress / quest.MaxValueProgress : 0;
            progressText.text = Math.Min(quest.CurrentValueProgress, quest.MaxValueProgress) + " / " + quest.MaxValueProgress;
            background.sprite = isSelected ? selected : normal;
            var item = quest.Data.ItemTarget as ItemConfig;
            var questIcon = quest.Data.QuestIcon;
            bool cube = questIcon == null && item is StackableItemConfig && item.BlockStyleIcon;
            blockIcon.gameObject.SetActive(cube);
            if (cube) blockIcon.SetResource((StackableItemConfig)item);
            flatIcon.sprite = questIcon != null ? questIcon : item != null ? item.Icon : (quest.Data.ItemTarget as MineArena.AI.MobPreset)?.Icon;
            flatIcon.gameObject.SetActive(!cube && flatIcon.sprite != null);
            status.text = quest.Data.DifficultyLabel + " · " + status.text;
            button.onClick.RemoveAllListeners(); button.onClick.AddListener(() => click());
        }
        public static string Title(Achievement quest)
        {
            string key = quest.Data.NameAchievementKey;
            if (LocalizationService.TryGetLocalizedText(key, out var localized)) return localized;
            if (!string.IsNullOrWhiteSpace(quest.Data.Title)) return quest.Data.Title;
            return quest.Data.ItemTarget is ItemConfig item ? "Добыча: " + item.DisplayName : "Поручение №" + (quest.ID + 1);
        }
        public static string Description(Achievement quest)
        {
            string target = quest.Data.ItemTarget is ItemConfig item ? item.DisplayName : quest.Data.ItemTarget?.Name ?? "цель задания";
            string key = quest.Data.TextTaskKey;
            if (LocalizationService.TryGetLocalizedText(key, out var localized))
            { try { return string.Format(localized, quest.MaxValueProgress, target); } catch (FormatException) { } }
            if (!string.IsNullOrWhiteSpace(quest.Data.Description)) return string.Format(quest.Data.Description, quest.MaxValueProgress, target);
            return "Соберите «" + target + "»: " + quest.MaxValueProgress + ".";
        }
    }
}
