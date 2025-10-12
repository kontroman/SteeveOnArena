using MineArena.Basics;
using Structs;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Sector
{
    public class QuestSectorUIElements
    {
        private const string DropAmount = "DropAmount";
        private const string IconPrize = "IconPrize";
        private const string TaskContent = "TaskContent";
        private const string QuestName= "QuestName";
        
        private readonly Image _icon;
        private readonly TMP_Text _textContent;
        private readonly TMP_Text _nameQuest;
        private readonly TMP_Text _amount;

        public QuestSectorUIElements(GameObject questSector)
        {
            _icon = GetComponentFromPath<Image>(questSector, IconPrize);
            _textContent = GetComponentFromPath<TMP_Text>(questSector, TaskContent);
            _nameQuest = GetComponentFromPath<TMP_Text>(questSector, QuestName);
            _amount = GetComponentFromPath<TMP_Text>(questSector, DropAmount);
        }

        public void Configure(DataAchievement data)
        {
            SetIcon(data.ItemPrize?.Icon);
            SetTextContent(data.TextTask);
            SetQuestName(data.NameQuest);
            SetAmount(data.Amount);
        }

        private void SetIcon(Sprite icon) => _icon.sprite = icon;
        private void SetTextContent(string text) => _textContent?.SetText(text);
        private void SetQuestName(string name) => _nameQuest?.SetText(name);
        private void SetAmount(int amount) => _amount?.SetText(amount.ToString());

        private static T GetComponentFromPath<T>(GameObject parent, string path) where T : Component
        {
            var transform = parent.transform.Find(path);
            return transform?.GetComponent<T>();
        }
    }
}