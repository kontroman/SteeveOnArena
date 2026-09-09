using Devotion.SDK.Services.Localization;
using TMPro;
using UnityEngine;

namespace MineArena.SDK.UI
{
    public sealed class LocalizedTextScope : MonoBehaviour
    {
        private void Awake() => Bind();
        private void OnEnable() => Bind();
        private void Bind()
        {
            foreach (var label in GetComponentsInChildren<TMP_Text>(true))
                LocalizedTextRenderer.Bind(label);
            foreach (var label in GetComponentsInChildren<UnityEngine.UI.Text>(true))
                if (label.GetComponent<LocalizedLegacyText>() == null) label.gameObject.AddComponent<LocalizedLegacyText>();
        }
    }
}
