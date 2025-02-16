using UnityEngine;
using UnityEngine.UI;

namespace Divotion.Game.UI
{
    public class BarText : BarView
    {
        [SerializeField] private Text _textBar;

        public override void DisplayAmount(float value, float maxValue)
        {
            _textBar.text = $"Çהמנמגüו:  {value} / {maxValue}";
        }
    }
}