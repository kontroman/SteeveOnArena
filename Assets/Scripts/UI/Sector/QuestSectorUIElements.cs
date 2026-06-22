using Devotion.SDK.Services.Localization;
using MineArena.Basics;
using MineArena.Items;
using MineArena.UI;
using MineArena.UI.FortuneWheel;
using Structs;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Sector
{
    public class QuestSectorUIElements
    {
        private const string DropAmount = "PrizeBackground/DropAmount";
        private const string LegacyDropAmount = "DropAmount";
        private const string PrizeIcon = "PrizeBackground/PrizeIcon";
        private const string LegacyPrizeIcon = "PrizeBackground/IconPrize";
        private const string DirectPrizeIcon = "PrizeIcon";
        private const string DirectLegacyPrizeIcon = "IconPrize";
        private const string BlockIcon = "BlockIcon";
        private const string TaskContent = "TaskContent";
        private const string QuestName= "QuestName";
        
        private readonly Image _icon;
        private readonly ResourceIcon _blockIcon;
        private readonly TMP_Text _textContent;
        private readonly TMP_Text _nameQuest;
        private readonly TMP_Text _amount;

        public QuestSectorUIElements(GameObject questSector)
        {
            _icon = GetComponentFromPath<Image>(questSector, PrizeIcon, LegacyPrizeIcon, DirectPrizeIcon, DirectLegacyPrizeIcon);
            _blockIcon = GetComponentFromPath<ResourceIcon>(
                questSector,
                $"{PrizeIcon}/{BlockIcon}",
                $"{LegacyPrizeIcon}/{BlockIcon}",
                $"{DirectPrizeIcon}/{BlockIcon}",
                $"{DirectLegacyPrizeIcon}/{BlockIcon}");
            _textContent = GetComponentFromPath<TMP_Text>(questSector, TaskContent);
            _nameQuest = GetComponentFromPath<TMP_Text>(questSector, QuestName);
            _amount = GetComponentFromPath<TMP_Text>(questSector, DropAmount, LegacyDropAmount);
        }

        public void Configure(DataAchievement data)
        {
            SetIcon(data.ItemPrize);
            SetTextContent(LocalizationService.GetLocalizedText(data.TextTaskKey));
            SetQuestName(LocalizationService.GetLocalizedText(data.NameAchievementKey));
            SetAmount(data.Amount);
        }

        private void SetIcon(ItemPrize prize)
        {
            ItemConfig itemConfig = prize?.ItemConfig;
            var stackableItem = itemConfig as StackableItemConfig;
            bool useBlockIcon = itemConfig != null
                && itemConfig.BlockStyleIcon
                && stackableItem != null
                && _blockIcon != null;

            if (_blockIcon != null)
            {
                _blockIcon.gameObject.SetActive(useBlockIcon);
            }

            if (useBlockIcon)
            {
                _blockIcon.SetResource(stackableItem);

                if (_icon != null)
                {
                    _icon.enabled = false;
                    _icon.sprite = null;
                }

                return;
            }

            if (_icon != null)
            {
                _icon.enabled = true;
                _icon.sprite = prize?.Icon;
            }
        }

        private void SetTextContent(string text) => _textContent?.SetText(text);
        private void SetQuestName(string name) => _nameQuest?.SetText(name);
        private void SetAmount(int amount) => _amount?.SetText(amount.ToString());

        private static T GetComponentFromPath<T>(GameObject parent, params string[] paths) where T : Component
        {
            foreach (string path in paths)
            {
                var transform = parent.transform.Find(path);
                var component = transform?.GetComponent<T>();

                if (component != null)
                    return component;
            }

            return null;
        }
    }
}
