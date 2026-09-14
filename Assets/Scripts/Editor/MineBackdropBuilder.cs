using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace MineArena.Editor
{
    public static class MineBackdropBuilder
    {
        [InitializeOnLoadMethod]
        private static void Watch() => EditorApplication.update += () =>
        {
            const string request = "Temp/mine-backdrop.request";
            if (!File.Exists(request) || EditorApplication.isCompiling || EditorApplication.isUpdating
                || EditorApplication.isPlayingOrWillChangePlaymode) return;
            File.Delete(request);
            try { Build(); }
            catch (Exception e) { File.WriteAllText("Temp/mine-backdrop-result.txt", e.ToString()); Debug.LogException(e); }
        };

        [MenuItem("MineArena/UI/Build Mine Stone Backdrop")]
        public static void Build()
        {
            const string prefabPath = "Assets/Prefabs/Arenas/LocationMine.prefab";
            const string materialPath = "Assets/Textures/Materials/Stone/MineBackdrop.mat";
            var root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                var bounds = root.GetComponent<Renderer>().bounds;
                var mesh = root.GetComponent<MeshFilter>().sharedMesh;
                var vertices = mesh.vertices.Select(root.transform.TransformPoint).ToArray();
                var spawn = root.GetComponentsInChildren<Transform>().First(t => t.name == "PlayerSpawnPoint");
                // Use the main upward-facing floor, not bevel vertices or wall bottoms.
                var floorAreas = new Dictionary<float, float>();
                var triangles = mesh.triangles;
                for (int i = 0; i < triangles.Length; i += 3)
                {
                    var a = vertices[triangles[i]];
                    var b = vertices[triangles[i + 1]];
                    var c = vertices[triangles[i + 2]];
                    var normal = Vector3.Cross(b - a, c - a);
                    if (normal.normalized.y < 0.99f || a.y > spawn.position.y + 0.1f) continue;
                    float height = Mathf.Round(a.y * 100) / 100;
                    floorAreas.TryGetValue(height, out float area);
                    floorAreas[height] = area + normal.magnitude * 0.5f;
                }
                float floor = floorAreas.OrderByDescending(entry => entry.Value).First().Key;
                float width = Mathf.Ceil(bounds.size.x + 20);
                float depth = Mathf.Ceil(bounds.size.z + 20);
                var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                if (material == null)
                {
                    material = new Material(AssetDatabase.LoadAssetAtPath<Material>("Assets/Textures/Materials/Stone/Stone.mat"));
                    AssetDatabase.CreateAsset(material, materialPath);
                }
                material.name = "MineBackdrop";
                material.SetTextureScale("_BaseMap", new Vector2(width, depth));
                material.SetTextureScale("_MainTex", new Vector2(width, depth));
                material.SetFloat("_Smoothness", 0);
                EditorUtility.SetDirty(material);
                var existing = root.transform.Find("Stone backdrop");
                var plane = existing != null ? existing.gameObject : GameObject.CreatePrimitive(PrimitiveType.Plane);
                plane.name = "Stone backdrop";
                plane.transform.SetParent(null, false);
                plane.transform.SetPositionAndRotation(new Vector3(bounds.center.x, floor - 0.15f, bounds.center.z), Quaternion.identity);
                plane.transform.localScale = new Vector3(width / 10, 1, depth / 10);
                plane.transform.SetParent(root.transform, true);
                if (plane.TryGetComponent<Collider>(out var collider)) UnityEngine.Object.DestroyImmediate(collider);
                var renderer = plane.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                AssetDatabase.SaveAssets();
                File.WriteAllText("Temp/mine-backdrop-result.txt", $"PASS: stone backdrop {width} x {depth}, one tile per world unit; floor {floor}, backdrop {plane.transform.position.y}, spawn {spawn.position}, bounds {bounds}. No collider; separate opaque renderer.");
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
    }
}
