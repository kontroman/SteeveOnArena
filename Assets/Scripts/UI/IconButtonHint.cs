using UnityEngine;
using UnityEngine.EventSystems;

namespace MineArena.UI
{
    public sealed class IconButtonHint : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
    {
        [SerializeField] private GameObject hint;
        private bool _hovered, _selected;
        public void OnPointerEnter(PointerEventData e) { _hovered = true; Refresh(); }
        public void OnPointerExit(PointerEventData e) { _hovered = false; Refresh(); }
        public void OnSelect(BaseEventData e) { _selected = true; Refresh(); }
        public void OnDeselect(BaseEventData e) { _selected = false; Refresh(); }
        private void Refresh() { if (hint != null) { hint.SetActive(_hovered || _selected); if (hint.activeSelf) transform.SetAsLastSibling(); } }
        private void OnDisable() { _hovered = _selected = false; if (hint != null) hint.SetActive(false); }
    }
}
