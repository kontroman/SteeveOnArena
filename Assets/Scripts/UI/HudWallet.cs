using Devotion.SDK.Controllers;
using MineArena.Managers;
using TMPro;
using UnityEngine;

namespace MineArena.UI
{
    public sealed class HudWallet : MonoBehaviour
    {
        [SerializeField] private ShopCatalog catalog;
        [SerializeField] private TMP_Text text;
        [SerializeField] private bool amountOnly;
        private float _next;
        private void Update()
        {
            if (Time.unscaledTime < _next) return;
            _next = Time.unscaledTime + 0.5f;
            if (catalog == null || catalog.Currency == null) return;
            int amount = GameRoot.GetManager<InventoryManager>()?.GetItemAmount(catalog.Currency.Name) ?? 0;
            text.text = amountOnly ? amount.ToString() : catalog.Currency.DisplayName + ": " + amount;
        }
    }
}
