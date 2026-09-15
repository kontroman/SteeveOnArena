using System.IO;
using System.Linq;
using MineArena.Buildings;
using MineArena.Windows;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MineArena.Editor
{
    public static partial class GameUiBuilder
    {
        [InitializeOnLoadMethod]
        private static void WatchBuildingPreviews() => EditorApplication.update += () =>
        {
            const string request = "Temp/building-previews.request";
            if (!File.Exists(request) || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
            File.Delete(request);
            try
            {
                foreach (string name in new[] { "SmithBuilding", "StorageBuilding", "LumberjackBuilding" })
                {
                    var config = AssetDatabase.LoadAssetAtPath<BuildingConfig>("Assets/ScriptableObjects/Configs/Buildings/" + name + ".asset");
                    foreach (var level in config.Levels)
                    {
                        string path = AssetDatabase.GetAssetPath(level.Preview);
                        BakeBuilding(level.ModelPrefab, path, name == "LumberjackBuilding" ? 0f : 180f);
                        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                    }
                }
                File.WriteAllText("Temp/building-previews.result", "PASS");
            }
            catch (System.Exception e) { File.WriteAllText("Temp/building-previews.result", e.ToString()); }
        };

        [MenuItem("MineArena/UI/Refresh Building UI")]
        public static void RefreshBuildingUI()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            const string folder = "Assets/Art/UI/Buildings/";
            Directory.CreateDirectory(folder);
            var configs = AssetDatabase.FindAssets("t:BuildingConfig").Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<BuildingConfig>).ToArray();
            int count = 0;
            foreach (var config in configs)
            {
                var serialized = new SerializedObject(config);
                for (int i = 0; i < config.Levels.Count; i++)
                {
                    var level = config.Levels[i];
                    if (level.ModelPrefab == null) continue;
                    string path = folder + AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(config)) + "-" + i + ".png";
                    BakeBuilding(level.ModelPrefab, path, config.name == "SmithBuilding" || config.name == "StorageBuilding" ? 180f : 0f);
                    ImportSprite(path, 1024);
                    serialized.FindProperty("_levels").GetArrayElementAtIndex(i).FindPropertyRelative("_preview").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                    count++;
                }
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
            BuildPriceElement(); BuildBuilding(); AssetDatabase.SaveAssets();
            var scene = EditorSceneManager.NewPreviewScene();
            try
            {
                var root = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(UI + "BuildingWindow.prefab"), scene);
                foreach (var config in configs)
                {
                    root.GetComponent<BuildingWindow>().InitializeBuilding(config, null);
                    Canvas.ForceUpdateCanvases();
                    Render(root, "Documentation/UI/Building-" + config.name + ".png");
                    // Edit-mode preview instances are temporary, so clear rows synchronously between buildings.
                    var so = new SerializedObject(root.GetComponent<BuildingWindow>());
                    foreach (string field in new[] { "_priceTransform", "_opensTransform" })
                    {
                        var parent = (Transform)so.FindProperty(field).objectReferenceValue;
                        for (int i = parent.childCount - 1; i >= 0; i--) Object.DestroyImmediate(parent.GetChild(i).gameObject);
                    }
                }
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
            Debug.Log("[BuildingUI] Built panels and " + count + " model previews for " + configs.Length + " buildings.");
        }

        private static void BakeBuilding(GameObject prefab, string path, float yaw = 0f)
        {
            var utility = new PreviewRenderUtility();
            Texture2D texture = null;
            Material contactShadow = null;
            var previewMaterials = new System.Collections.Generic.Dictionary<Material, Material>();
            try
            {
                var meshes = prefab.GetComponentsInChildren<MeshFilter>(false).Where(m => m.sharedMesh != null && m.GetComponent<MeshRenderer>() != null && m.GetComponent<MeshRenderer>().enabled).ToArray();
                if (meshes.Length == 0) throw new System.InvalidOperationException("No building meshes: " + prefab.name);
                Bounds bounds = meshes[0].GetComponent<MeshRenderer>().bounds;
                foreach (var mesh in meshes) bounds.Encapsulate(mesh.GetComponent<MeshRenderer>().bounds);
                utility.camera.orthographic = true;
                utility.camera.orthographicSize = bounds.extents.magnitude * 1.12f;
                float distance = Mathf.Max(10, bounds.size.magnitude * 3);
                utility.camera.transform.position = bounds.center + Quaternion.Euler(0, yaw, 0) * new Vector3(1, 0.75f, -1).normalized * distance;
                utility.camera.transform.LookAt(bounds.center);
                float vertical = 0, horizontal = 0;
                for (int corner = 0; corner < 8; corner++)
                {
                    var delta = Vector3.Scale(bounds.extents, new Vector3((corner & 1) == 0 ? -1 : 1, (corner & 2) == 0 ? -1 : 1, (corner & 4) == 0 ? -1 : 1));
                    vertical = Mathf.Max(vertical, Mathf.Abs(Vector3.Dot(delta, utility.camera.transform.up)));
                    horizontal = Mathf.Max(horizontal, Mathf.Abs(Vector3.Dot(delta, utility.camera.transform.right)));
                }
                utility.camera.orthographicSize = Mathf.Max(vertical, horizontal / (768f / 576f)) * 1.08f;
                utility.camera.nearClipPlane = 0.01f; utility.camera.farClipPlane = distance * 3;
                utility.camera.clearFlags = CameraClearFlags.SolidColor;
                utility.camera.backgroundColor = Color.clear;
                foreach (var light in utility.lights) { light.intensity = 0; light.shadows = LightShadows.None; }
                var shader = Shader.Find("Hidden/MineArena/BuildingPreviewUnlit");
                if (shader == null) throw new System.InvalidOperationException("Building preview shader is missing.");
                utility.BeginPreview(new Rect(0, 0, 768, 576), GUIStyle.none);
                contactShadow = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
                contactShadow.SetFloat("_Shadow", 1);
                var shadowPosition = new Vector3(bounds.center.x, bounds.min.y - 0.02f, bounds.center.z);
                var shadowMatrix = Matrix4x4.TRS(shadowPosition, Quaternion.Euler(90, 0, 0), new Vector3(bounds.size.x * 1.25f, bounds.size.z * 1.25f, 1));
                utility.DrawMesh(Resources.GetBuiltinResource<Mesh>("Quad.fbx"), shadowMatrix, contactShadow, 0);
                foreach (var mesh in meshes)
                {
                    var materials = mesh.GetComponent<MeshRenderer>().sharedMaterials;
                    for (int sub = 0; sub < mesh.sharedMesh.subMeshCount; sub++)
                        if (materials.Length > sub && materials[sub] != null)
                        {
                            var source = materials[sub];
                            if (!previewMaterials.TryGetValue(source, out var unlit))
                            {
                                unlit = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
                                string map = source.HasProperty("_BaseMap") ? "_BaseMap" : "_MainTex";
                                if (source.HasProperty(map))
                                {
                                    unlit.mainTexture = source.GetTexture(map);
                                    unlit.mainTextureScale = source.GetTextureScale(map);
                                    unlit.mainTextureOffset = source.GetTextureOffset(map);
                                }
                                unlit.color = source.HasProperty("_BaseColor") ? source.GetColor("_BaseColor") : source.HasProperty("_Color") ? source.GetColor("_Color") : Color.white;
                                previewMaterials.Add(source, unlit);
                            }
                            utility.DrawMesh(mesh.sharedMesh, mesh.transform.localToWorldMatrix, unlit, sub);
                        }
                }
                utility.camera.Render();
                var previousTarget = RenderTexture.active;
                try
                {
                    RenderTexture.active = utility.camera.targetTexture;
                    texture = new Texture2D(768, 576, TextureFormat.RGBA32, false);
                    texture.ReadPixels(new Rect(0, 0, 768, 576), 0, 0);
                    texture.Apply();
                }
                finally { RenderTexture.active = previousTarget; }
                utility.EndPreview();
                File.WriteAllBytes(path, texture.EncodeToPNG());
            }
            finally
            {
                if (texture != null) Object.DestroyImmediate(texture);
                if (contactShadow != null) Object.DestroyImmediate(contactShadow);
                utility.Cleanup();
                foreach (var material in previewMaterials.Values) Object.DestroyImmediate(material);
            }
        }
    }
}
