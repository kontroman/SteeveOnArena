using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.FortuneWheel
{
    public class WheelConstructor : MonoBehaviour
    {
        private const string ItemIcon = "ItemIcon";
        private const string ItemText = "ItemText";
        private const float AngelDeviation = 90f;

        [SerializeField] private GameObject _sectorUIPrefab;
        [SerializeField] private float _radius = 360f;

        public void Create(Transform wheelContainer, List<WheelPrize> items)
        {
            int sectorCount = items.Count;
            float angleStep = 360f / sectorCount;

            for (int i = 0; i < sectorCount; i++)
            {
                WheelPrize data = items[i];
                GameObject sectorUI = Instantiate(_sectorUIPrefab, wheelContainer);
                RectTransform rectTransformSector = sectorUI.GetComponent<RectTransform>();

                SetTransformSector(rectTransformSector, angleStep, i);
                SettingSector(sectorUI, data);
            }
        }

        public void SettingSector(GameObject sector, WheelPrize data)
        {
            Transform itemIcon = sector.transform.Find(ItemIcon);
            Transform itemText = sector.transform.Find(ItemText);
            Image icon = itemIcon?.GetComponent<Image>();
            TMP_Text label = itemText?.GetComponent<TMP_Text>();
            if (icon) icon.sprite = data.ItemIcon;
            if (label) label.text = data.SectorName;
        }


        private void SetTransformSector(RectTransform sectorRT, float angleStep, int i)
        {
            if (sectorRT != null)
            {
                sectorRT.sizeDelta = new Vector2(_radius / 2, _radius / 2);
                float angle = angleStep * i;
                float rad = Mathf.Deg2Rad * (angle + AngelDeviation);
                Vector2 pos = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * _radius;
                sectorRT.localPosition = pos;
                sectorRT.localRotation = Quaternion.Euler(0, 0, angleStep * i);
            }
        }
    }
}