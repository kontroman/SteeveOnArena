using UnityEngine;

namespace MineArena.UI.FortuneWheel
{
    [CreateAssetMenu(fileName = "WheelPrize", menuName = "Wheel/WheelPrize")]
    public class WheelPrize : ScriptableObject
    {
        public string SectorName;
        public Sprite ItemIcon;
        public int Amount;
    }
}