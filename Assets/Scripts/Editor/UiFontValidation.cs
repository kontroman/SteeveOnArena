using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace MineArena.Editor
{
    public static class UiFontValidation
    {
        [InitializeOnLoadMethod]
        private static void Watch() => EditorApplication.update += () =>
        {
            const string request = "Temp/ui-font-validation.request";
            if (!File.Exists(request) || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            File.Delete(request);
            try { Validate(); }
            catch (Exception e) { File.WriteAllText("Documentation/ui-font-validation.txt", e.ToString()); Debug.LogException(e); }
        };

        [MenuItem("MineArena/Validation/UI Fonts")]
        public static void Validate()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Main/4197-font SDF.asset");
            if (TMP_Settings.defaultFontAsset != font) throw new Exception("TMP default font mismatch");
            int labels = 0, legacy = 0;
            foreach (var path in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" }).Select(AssetDatabase.GUIDToAssetPath))
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                foreach (var text in prefab.GetComponentsInChildren<TMP_Text>(true))
                {
                    if (text.font != font) throw new Exception(path + ": " + text.name + " font mismatch");
                    if (text.fontSharedMaterial == null || text.fontSharedMaterial.mainTexture != font.atlasTexture)
                        throw new Exception(path + ": " + text.name + " atlas mismatch");
                    labels++;
                }
                foreach (var text in prefab.GetComponentsInChildren<Text>(true))
                {
                    if (text.font != font.sourceFontFile) throw new Exception(path + ": legacy font mismatch");
                    legacy++;
                }
            }
            if (!font.HasCharacters("АБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯабвгдеёжзийклмнопрстуфхцчшщъыьэюяABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789", out uint[] missing, false, true))
                throw new Exception("Missing Russian/English glyphs: " + string.Join(",", missing));
            var theme = Resources.Load<MineArena.UI.TutorialTheme>("UI/TutorialTheme");
            if (theme.BodyFont != font || theme.HeadingFont != font) throw new Exception("Tutorial fonts mismatch");
            Directory.CreateDirectory("Documentation");
            File.WriteAllText("Documentation/ui-font-validation.txt",
                $"PASS {labels} TMP labels and {legacy} legacy labels use 4197-font SDF\nPASS materials use the correct atlas\nPASS Russian and English glyphs\nPASS TMP default and tutorial theme\n");
            Debug.Log("UI font validation passed");
        }
    }
}
