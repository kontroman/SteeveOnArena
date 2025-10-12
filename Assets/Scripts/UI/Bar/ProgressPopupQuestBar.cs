using MineArena.Game.UI;
using TMPro;
using UI.UIAchievement;
using UnityEngine;

public class ProgressPopupQuestBar : AbstractBar<AchievementPopup>
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