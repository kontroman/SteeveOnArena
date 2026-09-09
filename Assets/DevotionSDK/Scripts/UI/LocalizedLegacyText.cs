using Devotion.SDK.Services.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MineArena.SDK.UI
{
    [RequireComponent(typeof(Text))]
    public sealed class LocalizedLegacyText : MonoBehaviour
    {
        private Text label;
        private string source, rendered;
        private int revision = -1;
        private Font originalFont;
        private void Awake() { label = GetComponent<Text>(); originalFont = label.font; source = label.text; }
        private void OnEnable() => LateUpdate();
        private void LateUpdate()
        {
            if (label == null) return;
            if (label.text != rendered) source = label.text;
            else if (revision == LocalizedTextRenderer.Revision) return;
            revision = LocalizedTextRenderer.Revision;
            rendered = LocalizedTextRenderer.Translate(source);
            label.font = originalFont;
            if (originalFont != null && rendered != null)
                foreach (char character in rendered)
                    if (char.IsLetter(character) && !originalFont.HasCharacter(character))
                    {
                        var fallbacks = TMP_Settings.defaultFontAsset.fallbackFontAssetTable;
                        if (fallbacks != null && fallbacks.Count > 0) label.font = fallbacks[0].sourceFontFile;
                        break;
                    }
            label.text = rendered;
        }
    }
}
