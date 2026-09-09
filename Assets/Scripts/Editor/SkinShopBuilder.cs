using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using MineArena.Buildings;

namespace MineArena.Editor
{
    public static partial class SkinShopBuilder
    {
        [InitializeOnLoadMethod]
        private static void Watch() => EditorApplication.update += () =>
        {
            const string request = "Temp/skin-shop.request";
            if (!File.Exists(request) || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
            string action = File.ReadAllText(request); File.Delete(request);
            try
            {
                if (action.Trim() == "refresh") AssetDatabase.Refresh();
                else if (action.Trim() == "build") Build();
                else Probe();
            }
            catch (Exception e) { File.WriteAllText("Documentation/skin-shop-validation.txt", e.ToString()); }
        };

        private static void Probe()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/Scenes/SampleScene.unity");
            bool opened = !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Additive);
            try
            {
                var log = new StringBuilder();
                foreach (var root in scene.GetRootGameObjects())
                {
                    log.AppendLine("Root " + root.name + " " + root.transform.position);
                    foreach (var zone in root.GetComponentsInChildren<BuildingZone>(true))
                        log.AppendLine("ZONE " + zone.name + " " + zone.transform.position + " parent " + zone.transform.parent?.name + " config " + zone.Config?.name);
                }
                var player = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player/Player.prefab");
                foreach (var r in player.GetComponentsInChildren<Renderer>(true))
                    log.AppendLine("PLAYER " + r.name + " " + r.GetType().Name + " " + string.Join(",", r.sharedMaterials.Select(m => m.name + " texture " + m.mainTexture?.name)));
                foreach (var config in AssetDatabase.LoadAssetAtPath<BuildingsDatabase>("Assets/ScriptableObjects/Configs/Game/BuildingsDatabase.asset").AllBuildings)
                    log.AppendLine("BUILD " + config.name + " " + config.BuildingRotation.eulerAngles + " " + config.GetCurrentLevel().ModelPrefab.name);
                var colliders = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Collider>(true)).ToArray();
                foreach (var point in new[] { new Vector3(12, 50, 10), new Vector3(-10,50,10),new Vector3(-12,50,49),new Vector3(16,50,49),new Vector3(10,50,3) })
                    foreach (var c in colliders)
                        if (c.Raycast(new Ray(point, Vector3.down), out var hit, 60)) log.AppendLine("GROUND " + point + " " + c.name + " " + hit.point);
                var camGO = new GameObject("Site probe camera", typeof(Camera));
                var cam = camGO.GetComponent<Camera>();
                cam.transform.position = new Vector3(0,90,25); cam.transform.rotation = Quaternion.Euler(90,0,0);
                cam.orthographic = true; cam.orthographicSize = 40;
                var rt = new RenderTexture(1200,1200,24); cam.targetTexture = rt; cam.Render();
                var old = RenderTexture.active; RenderTexture.active = rt; var tex = new Texture2D(1200,1200); tex.ReadPixels(new Rect(0,0,1200,1200),0,0); tex.Apply();
                File.WriteAllBytes("Documentation/UI/SkinShop-site-before.png", tex.EncodeToPNG());
                RenderTexture.active = old; cam.targetTexture = null; UnityEngine.Object.DestroyImmediate(rt); UnityEngine.Object.DestroyImmediate(tex); UnityEngine.Object.DestroyImmediate(camGO);
                File.WriteAllText("Documentation/skin-shop-probe.txt", log.ToString());
            }
            finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
        }
    }
}
