using TMPro;
using UI.Quest;
using UnityEngine;

namespace MineArena.Game.UI
{
    public class ProgressQuestBar : AbstractBar<QuestVisualizer>
    {
        [SerializeField] private TextMeshProUGUI _textBar;

        public override void UpdateBar(float currentValue, float maxValue)
        {
            if (currentValue >= maxValue)
                currentValue = maxValue;

            base.UpdateBar(currentValue, maxValue);
            _textBar.text = currentValue + " / " + maxValue;
        }

        protected override void SubscribeToChange()
        {
            if (TargetSystem != null)
                TargetSystem.OnValueChanged += UpdateBar;
        }

        private void OnDestroy()
        {
            if (TargetSystem != null)
                TargetSystem.OnValueChanged += UpdateBar;
        }
    }
}