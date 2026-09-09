using System;
using System.IO;
using System.Linq;
using MineArena.Platform;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace MineArena.Editor
{
    public static class YandexIntegrationValidation
    {
        [InitializeOnLoadMethod]
        private static void Watch() => EditorApplication.update += () =>
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            if (File.Exists("Temp/yandex-build.request") && !EditorApplication.isPlayingOrWillChangePlaymode)
            {
                File.Delete("Temp/yandex-build.request");
                try { Build(); }
                catch (Exception e) { File.WriteAllText("Documentation/yandex-build.txt", e.ToString()); Debug.LogException(e); }
                return;
            }
            const string request = "Temp/yandex-validation.request";
            if (!File.Exists(request)) return;
            File.Delete(request);
            try { Validate(); }
            catch (Exception e) { File.WriteAllText("Documentation/yandex-unity-validation.txt", e.ToString()); Debug.LogException(e); }
        };

        [MenuItem("MineArena/Yandex/Validate Integration")]
        public static void Validate()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            if (PlayerSettings.WebGL.template != "PROJECT:MineArena") throw new Exception("Wrong WebGL template");
            foreach (var name in new[] { "index.html", "yandex-sdk.js", "4197-font.ttf", "game-icon.png" })
                if (!File.Exists("Assets/WebGLTemplates/MineArena/" + name)) throw new Exception("Missing template asset " + name);
            if (!File.Exists("Assets/Plugins/WebGL/MineArenaPlatform.jslib")) throw new Exception("Missing JavaScript library");
            var catalog = Resources.Load<YandexPurchaseCatalog>("UI/YandexPurchaseCatalog");
            if (catalog == null) throw new Exception("Missing purchase mapping catalog");
            if (catalog.Products.Any(p => p == null || !p.IsValid)
                || catalog.Products.Select(p => p.ProductId).Distinct().Count() != catalog.Products.Count)
                throw new Exception("Invalid or duplicate purchase mapping");
            var progress = new Devotion.SDK.Services.SaveSystem.Progress.PlayerProgress("test");
            progress.PurchasesProgress.GrantedTokens.Add("test-receipt");
            var json = JsonUtility.ToJson(progress);
            var restored = new Devotion.SDK.Services.SaveSystem.Progress.PlayerProgress("test");
            JsonUtility.FromJsonOverwrite(json, restored);
            if (!restored.PurchasesProgress.GrantedTokens.Contains("test-receipt")) throw new Exception("Receipt is not saved");
            File.WriteAllText("Documentation/yandex-unity-validation.txt",
                "PASS template and jslib assets\nPASS receipt survives save/load\nPASS purchase mappings: " + catalog.Products.Count
                + "\n" + (catalog.Products.Count == 0 ? "PENDING merchant product IDs and reward mappings\n" : ""));
            Debug.Log("Yandex integration validation passed");
        }

        [MenuItem("MineArena/Yandex/Build WebGL")]
        public static void Build()
        {
            Validate();
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Exit Play Mode before building");
            var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.WebGL);
            if (defines.Split(';').Contains("DEVOTION_GODMODE"))
                throw new Exception("Remove DEVOTION_GODMODE from WebGL scripting defines before release");
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
                locationPathName = "output/YandexWebGL", target = BuildTarget.WebGL, options = BuildOptions.None
            });
            File.WriteAllText("Documentation/yandex-build.txt", report.summary.result + "\nErrors: " + report.summary.totalErrors);
            if (report.summary.result != BuildResult.Succeeded) throw new Exception("Yandex WebGL build failed");
        }
    }
}
