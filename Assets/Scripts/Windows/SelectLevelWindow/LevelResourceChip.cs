using MineArena.Items;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using MineArena.UI;

namespace MineArena.Windows.SelectLevel
{
    [ExecuteAlways]
    public sealed class LevelResourceChip : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private ResourceIcon blockIcon;
        [SerializeField] private TMP_Text label;
        [SerializeField] private TMP_Text amount;

        private void OnEnable() => FitContents();
        private void OnRectTransformDimensionsChange() => FitContents();

        public void FitContents()
        {
            var root = transform as RectTransform;
            if (root == null || icon == null || label == null || amount == null) return;
            float size = Mathf.Max(0f, Mathf.Min(45f, root.rect.height - 16f, root.rect.width - 16f));
            FitIcon(icon.rectTransform, size);
            if (blockIcon != null) FitIcon((RectTransform)blockIcon.transform, size);
            float left = 8f + size + 8f;
            bool hasAmount = amount.gameObject.activeSelf;
            float textHeight = Mathf.Max(0f, root.rect.height - 12f);
            FitText(label.rectTransform, left, 6f, hasAmount ? textHeight / 2f : textHeight);
            FitText(amount.rectTransform, left, 6f + textHeight / 2f, textHeight / 2f);
        }

        private static void FitIcon(RectTransform rect, float size)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0f, .5f);
            rect.pivot = new Vector2(.5f, .5f);
            rect.localScale = Vector3.one;
            rect.sizeDelta = new Vector2(size, size);
            rect.anchoredPosition = new Vector2(8f + size / 2f, 0f);
        }

        private static void FitText(RectTransform rect, float left, float top, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(left, -top);
            rect.sizeDelta = new Vector2(-left - 8f, height);
        }

        public void Bind(ItemConfig item, int? quantity)
        {
            FitContents();
            icon.sprite = item.Icon;
            icon.enabled = icon.sprite != null;
            bool isBlock = item.BlockStyleIcon && item is StackableItemConfig && blockIcon != null;
            if (blockIcon != null)
            {
                blockIcon.gameObject.SetActive(isBlock);
                if (isBlock) blockIcon.SetResource((StackableItemConfig)item);
            }
            icon.gameObject.SetActive(!isBlock);
            label.text = string.IsNullOrWhiteSpace(item.DisplayName) ? item.name : item.DisplayName;
            amount.text = quantity.HasValue ? "+" + quantity.Value : "";
            amount.gameObject.SetActive(quantity.HasValue);
            FitContents();
        }
    }
}
