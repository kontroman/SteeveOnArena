using TMPro;
using UnityEngine;

namespace MineArena.Windows.Crafting
{
    public class CraftingSection : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _title;
        [SerializeField] private RectTransform _itemsRoot;

        public RectTransform SectionRect => (RectTransform)transform;
        public RectTransform ItemsRoot => _itemsRoot != null ? _itemsRoot : (RectTransform)transform;

        public void SetTitle(string title)
        {
            if (_title != null)
            {
                _title.text = title;
            }
        }
    }
}
