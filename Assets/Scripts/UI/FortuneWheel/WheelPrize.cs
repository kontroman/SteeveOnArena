using UnityEngine;

namespace UI.FortuneWheel
{
    [CreateAssetMenu(fileName = "NewSectorData", menuName = "Wheel/WheelPrize")]
    public class WheelPrize : ScriptableObject
    {
        public string SectorName;
        public Sprite ItemIcon;
    }
}