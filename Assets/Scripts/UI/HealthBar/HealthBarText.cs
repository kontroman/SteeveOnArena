using UnityEngine;
using UnityEngine.UI;

public class HealthBarText : HealthView
{
    [SerializeField] private Text _textBar;

    public override void DisplayAmount(float value)
    {
        _textBar.text = $"Çהמנמגüו:  {value} / {Health.MaxHealth}";
    }
}