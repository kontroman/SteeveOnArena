using System.Collections.Generic;
using MineArena.Basics;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MineArena.UI.FortuneWheel
{
    public class WheelConstructor : MonoBehaviour
    {
        [SerializeField] private GameObject _sectorUIPrefab;
        [SerializeField] private float _radius = 240f;

        [Header("Drop Chance")] [HideLabel, MinMaxSlider(10, 100, true)] [SerializeField]
        private Vector2 _dropChance;

        public void Create(Transform wheelContainer, List<ItemPrize> items)
        {
            int sectorCount = items.Count;
            float angleStep = 360f / sectorCount;

            for (int i = 0; i < sectorCount; i++)
            {
                ItemPrize itemPrize = items[i];
                itemPrize.Construct();
                //data.Amount = (int)Random.Range(_dropChance.x, _dropChance.y);
                GameObject sectorUI = Instantiate(_sectorUIPrefab, wheelContainer);
                RectTransform rectTransformSector = sectorUI.GetComponent<RectTransform>();

                SetTransformSector(rectTransformSector, angleStep, i);
                SettingSector(sectorUI, itemPrize);
            }
        }

        public void SettingSector(GameObject sector, ItemPrize data)
        {
            Transform itemIcon = sector.transform.Find(Constants.FortuneWheel.ItemIcon);
            Transform itemText = sector.transform.Find(Constants.FortuneWheel.ItemText);
            Transform itemAmount = sector.transform.Find(Constants.FortuneWheel.ItemAmount);

            Image icon = itemIcon?.GetComponent<Image>();
            TMP_Text label = itemText?.GetComponent<TMP_Text>();
            TMP_Text amount = itemAmount?.GetComponent<TMP_Text>();

            if (icon) icon.sprite = data.Icon;
            if (label) label.text = data.Name;
            if (amount) amount.text = data.Amount.ToString();
        }

        private void SetTransformSector(RectTransform sectorRT, float angleStep, int i)
        {
            if (sectorRT != null)
            {
                sectorRT.sizeDelta = new Vector2(_radius / 2, _radius / 2);

                float angle = angleStep * i;
                float rad = Mathf.Deg2Rad * (angle + Constants.FortuneWheel.AngelDeviation);
                Vector2 pos = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * _radius;

                sectorRT.localPosition = pos;
                sectorRT.localRotation = Quaternion.Euler(0, 0, angleStep * i);
            }
        }
    }
}