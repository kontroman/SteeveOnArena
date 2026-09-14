using MineArena.Windows;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MineArena.UI
{
    public sealed class StorageCellInteraction : MonoBehaviour, IPointerClickHandler
    {
        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.dragging || (eventData.button != PointerEventData.InputButton.Left &&
                eventData.button != PointerEventData.InputButton.Right)) return;
            GetComponentInParent<StorageWindow>()?.Transfer(GetComponent<InventoryCellUI>(),
                eventData.button == PointerEventData.InputButton.Right);
        }
    }
}
