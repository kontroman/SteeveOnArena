using MineArena.Items;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MineArena.UI
{
    public class InventoryCellUI : MonoBehaviour
    {
        [SerializeField] private Image _icon;
        [SerializeField] private TextMeshProUGUI _counter;
        [SerializeField] private ResourceIcon _icon2;

        private Items.Item _item;
        private RectTransform _iconRectTransform;
        private RectTransform _blockableIconRectTransform;
        private CanvasGroup _iconCanvasGroup;
        private CanvasGroup _blockableCanvasGroup;
        private bool _usesBlockableIcon;

        public Items.Item Item => _item;
        public bool HasItem => _item != null;
        public Image Icon => _icon;
        public RectTransform ActiveIconRectTransform => _usesBlockableIcon && _blockableIconRectTransform != null ? _blockableIconRectTransform : _iconRectTransform;
        public CanvasGroup ActiveIconCanvasGroup => _usesBlockableIcon && _blockableCanvasGroup != null ? _blockableCanvasGroup : _iconCanvasGroup;

        private void Awake()
        {
            _iconRectTransform = _icon != null ? _icon.GetComponent<RectTransform>() : null;
            _iconCanvasGroup = EnsureCanvasGroup(_iconRectTransform);

            if (_icon2 != null)
            {
                _blockableIconRectTransform = _icon2.GetComponent<RectTransform>();
                _blockableCanvasGroup = EnsureCanvasGroup(_blockableIconRectTransform);
                _icon2.gameObject.SetActive(false);
            }

            if (_icon != null)
            {
                _icon.enabled = false;
            }

            if (_counter != null)
            {
                _counter.enabled = false;
            }
        }

        public void Setup(Items.Item item)
        {
            _item = item;
            bool useBlockableIcon = ShouldUseBlockableIcon(item);
            _usesBlockableIcon = useBlockableIcon;

            if (useBlockableIcon && item is StackableItem blockableItem)
            {
                if (_icon2 != null)
                {
                    _icon2.gameObject.SetActive(true);
                    _icon2.SetResource(blockableItem.Config);
                }

                if (_icon != null)
                {
                    _icon.sprite = null;
                    _icon.gameObject.SetActive(false);
                    _icon.enabled = false;
                }
            }
            else
            {
                if (_icon2 != null)
                {
                    _icon2.gameObject.SetActive(false);
                }

                if (_icon != null)
                {
                    _icon.sprite = item.Icon;
                    _icon.gameObject.SetActive(true);
                    _icon.enabled = true;
                }

                _usesBlockableIcon = false;
            }

            if (_counter != null)
            {
                if (item is StackableItem stackableItem)
                {
                    _counter.text = stackableItem.CurrentStack.ToString();
                    _counter.enabled = true;
                }
                else
                {
                    _counter.text = string.Empty;
                    _counter.enabled = false;
                }
            }
        }

        public void Clear()
        {
            _item = null;
            _usesBlockableIcon = false;

            if (_icon2 != null)
            {
                _icon2.gameObject.SetActive(false);
            }

            if (_icon != null)
            {
                _icon.sprite = null;
                _icon.gameObject.SetActive(true);
                _icon.enabled = false;
            }

            if (_counter != null)
            {
                _counter.text = string.Empty;
                _counter.enabled = false;
            }
        }

        private static bool ShouldUseBlockableIcon(Items.Item item)
        {
            if (item is StackableItem stackableItem)
            {
                return stackableItem.Config != null && stackableItem.Config.BlockStyleIcon;
            }

            return false;
        }

        private static CanvasGroup EnsureCanvasGroup(RectTransform rectTransform)
        {
            if (rectTransform == null)
                return null;

            var canvasGroup = rectTransform.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = rectTransform.gameObject.AddComponent<CanvasGroup>();
            }

            canvasGroup.blocksRaycasts = true;
            return canvasGroup;
        }
    }
}
