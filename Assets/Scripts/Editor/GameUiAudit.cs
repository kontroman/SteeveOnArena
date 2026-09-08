using System.IO;
using System.Linq;
using System.Text;
using Devotion.SDK.Base;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace MineArena.Editor
{
    public static class GameUiAudit
    {
        [MenuItem("MineArena/UI/Audit All Windows")]
        public static void Run()
        {
            var report = new StringBuilder();
            var paths = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/DevotionSDK/Prefabs/UI", "Assets/Prefabs/Windows", "Assets/Resources/Prefabs/Windows" })
                .Select(AssetDatabase.GUIDToAssetPath);
            foreach (var path in paths)
            {
                var root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var window = root.GetComponent<BaseWindow>();
                if (window == null) continue;
                report.AppendLine("\n" + path + " : " + window.GetType().FullName);
                foreach (var t in root.GetComponentsInChildren<RectTransform>(true))
                {
                    if (t.GetComponent<Image>() == null && t.GetComponent<Button>() == null && t.parent != root.transform) continue;
                    string local = AnimationUtility.CalculateTransformPath(t, root.transform);
                    if (local.Count(c => c == '/') > 2) continue;
                    var image = t.GetComponent<Image>();
                    string spriteName = image != null && image.sprite != null ? image.sprite.name : "-";
                    report.AppendLine(local + " size=" + t.sizeDelta + " pos=" + t.anchoredPosition + " anchors=" + t.anchorMin + "/" + t.anchorMax + " sprite=" + spriteName);
                }
                var so = new SerializedObject(window); var p = so.GetIterator();
                while (p.NextVisible(true))
                    if (p.propertyType == SerializedPropertyType.ObjectReference && p.objectReferenceValue != null)
                        report.AppendLine("REF " + p.propertyPath + " = " + p.objectReferenceValue.name);
            }
            Directory.CreateDirectory("Documentation/UI");
            File.WriteAllText("Documentation/UI/audit.txt", report.ToString());
            Debug.Log("[GameUI] Audit complete");
        }
    }
}
