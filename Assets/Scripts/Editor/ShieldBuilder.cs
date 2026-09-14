using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using MineArena.Items;
using MineArena.PlayerSystem;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MineArena.Editor
{
    public static class ShieldBuilder
    {
        private const string Folder = "Assets/Art/Shield";
        [InitializeOnLoadMethod]
        private static void Watch() => EditorApplication.update += () =>
        {
            const string request = "Temp/shield-build.request";
            if (!File.Exists(request) || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
            File.Delete(request);
            try { Build(); }
            catch (Exception e) { File.WriteAllText("Temp/shield-build-result.txt", e.ToString()); Debug.LogException(e); }
        };

        [MenuItem("MineArena/Build Shield")]
        public static void Build()
        {
            Directory.CreateDirectory(Folder);
            AssetDatabase.Refresh();
            const string texturePath = "Assets/Textures/Blocks/Crafting/shield_base_nopattern.png";
            var importer = (TextureImporter)AssetImporter.GetAtPath(texturePath);
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            var material = AssetDatabase.LoadAssetAtPath<Material>(Folder + "/Shield.mat");
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                AssetDatabase.CreateAsset(material, Folder + "/Shield.mat");
            }
            material.mainTexture = texture;
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0);
            var mesh = CreateMesh();
            var existingMesh = AssetDatabase.LoadAssetAtPath<Mesh>(Folder + "/Shield.asset");
            if (existingMesh == null) AssetDatabase.CreateAsset(mesh, Folder + "/Shield.asset");
            else
            {
                // Update native mesh buffers too; CopySerialized can leave an old GPU UV buffer alive.
                existingMesh.Clear();
                existingMesh.vertices = mesh.vertices;
                existingMesh.uv = mesh.uv;
                existingMesh.triangles = mesh.triangles;
                existingMesh.normals = mesh.normals;
                existingMesh.bounds = mesh.bounds;
                existingMesh.UploadMeshData(false);
                EditorUtility.SetDirty(existingMesh);
                UnityEngine.Object.DestroyImmediate(mesh);
                mesh = existingMesh;
            }
            var go = new GameObject("Shield", typeof(MeshFilter), typeof(MeshRenderer));
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            go.GetComponent<MeshRenderer>().sharedMaterial = material;
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, Folder + "/Shield.prefab");
            UnityEngine.Object.DestroyImmediate(go);

            // Decode source pixels directly: no render-target colour-space conversion.
            // The plate is a 12 x 22 x 1 cuboid. Its front starts AFTER the 1px side strip.
            var copy = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            copy.LoadImage(File.ReadAllBytes(texturePath));
            int unit = copy.width / 64;
            var iconTexture = new Texture2D(24 * unit, 24 * unit, TextureFormat.RGBA32, false);
            iconTexture.SetPixels32(new Color32[iconTexture.width * iconTexture.height]);
            iconTexture.SetPixels(6 * unit, 1 * unit, 12 * unit, 22 * unit,
                copy.GetPixels(unit, copy.height - 23 * unit, 12 * unit, 22 * unit));
            iconTexture.Apply();
            File.WriteAllBytes(Folder + "/ShieldIcon.png", iconTexture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(copy); UnityEngine.Object.DestroyImmediate(iconTexture);
            AssetDatabase.ImportAsset(Folder + "/ShieldIcon.png");
            var iconImporter = (TextureImporter)AssetImporter.GetAtPath(Folder + "/ShieldIcon.png");
            iconImporter.textureType = TextureImporterType.Sprite; iconImporter.spriteImportMode = SpriteImportMode.Single;
            iconImporter.filterMode = FilterMode.Point; iconImporter.mipmapEnabled = false;
            iconImporter.textureCompression = TextureImporterCompression.Uncompressed; iconImporter.SaveAndReimport();
            var icon = AssetDatabase.LoadAssetAtPath<Sprite>(Folder + "/ShieldIcon.png");
            var shield = AssetDatabase.LoadAssetAtPath<ArmorConfig>(Folder + "/ShieldItem.asset");
            if (shield == null) { shield = ScriptableObject.CreateInstance<ArmorConfig>(); AssetDatabase.CreateAsset(shield, Folder + "/ShieldItem.asset"); }
            var so = new SerializedObject(shield);
            so.FindProperty("_name").stringValue = "Shield";
            so.FindProperty("_displayName").stringValue = "Щит";
            so.FindProperty("_description").stringValue = "Во второй руке. Удерживайте ПКМ: блок ударов, стрел, зелий и взрывов спереди. Во время стрельбы из лука щит убран.";
            so.FindProperty("_slot").enumValueIndex = (int)ArmorSlot.OffHand;
            so.FindProperty("_resist").intValue = 0;
            so.FindProperty("_prefab").objectReferenceValue = prefab;
            so.FindProperty("_icon").objectReferenceValue = icon;
            so.FindProperty("_material").objectReferenceValue = material;
            var costs = so.FindProperty("_craftCosts"); costs.arraySize = 2;
            var resources = new[] { "Planks", "IronIngot" };
            for (int i = 0; i < 2; i++)
            {
                costs.GetArrayElementAtIndex(i).FindPropertyRelative("_resource").objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<StackableItemConfig>("Assets/ScriptableObjects/Configs/Drops/" + resources[i] + ".asset");
                costs.GetArrayElementAtIndex(i).FindPropertyRelative("_amount").intValue = i == 0 ? 6 : 1;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            var database = AssetDatabase.LoadAssetAtPath<ItemDatabase>("Assets/ScriptableObjects/Configs/Game/ItemDatabase.asset");
            var db = new SerializedObject(database); var items = db.FindProperty("allItems");
            if (!database.AllItems.Contains(shield)) { items.arraySize++; items.GetArrayElementAtIndex(items.arraySize - 1).objectReferenceValue = shield; db.ApplyModifiedPropertiesWithoutUndo(); }
            const string inventoryPath = "Assets/DevotionSDK/Prefabs/UI/InventoryWindow.prefab";
            var inventory = PrefabUtility.LoadPrefabContents(inventoryPath);
            try
            {
                ArrangeEquipment(inventory);
                PrefabUtility.SaveAsPrefabAsset(inventory, inventoryPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(inventory); }
            EditorUtility.SetDirty(material); AssetDatabase.SaveAssets();
            if (!PlayerShield.FacesSource(Vector3.forward, Vector3.zero, Vector3.forward) || PlayerShield.FacesSource(Vector3.forward, Vector3.zero, Vector3.back))
                throw new Exception("Shield direction validation failed");
            if (shield.CraftCosts.Any(c => c.Resource == null || c.Amount <= 0) || shield.Icon == null || shield.Prefab == null)
                throw new Exception("Shield asset validation failed");
            File.WriteAllText("Temp/shield-build-result.txt", "PASS: shield mesh, material, prefab, icon, recipe, database, off-hand inventory slot and direction checks.");
        }

        public static void ArrangeEquipment(GameObject inventory)
        {
            var transforms = inventory.GetComponentsInChildren<RectTransform>(true);
            var boots = transforms.First(t => t.name == "EquipBoots");
            var offHand = transforms.FirstOrDefault(t => t.name == "EquipOffHand");
            if (offHand == null)
            {
                offHand = UnityEngine.Object.Instantiate(boots, boots.parent);
                offHand.name = "EquipOffHand";
                foreach (var child in offHand.Cast<Transform>().ToArray())
                    UnityEngine.Object.DestroyImmediate(child.gameObject);
            }
            foreach (var hint in offHand.GetComponentsInChildren<TMP_Text>(true))
                UnityEngine.Object.DestroyImmediate(hint.gameObject);
            var placeholderTransform = offHand.Find("EmptyShieldIcon");
            var placeholder = placeholderTransform != null ? placeholderTransform.GetComponent<Image>() : null;
            if (placeholder == null)
            {
                var placeholderObject = new GameObject("EmptyShieldIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                placeholderObject.layer = offHand.gameObject.layer;
                placeholderObject.transform.SetParent(offHand, false);
                placeholder = placeholderObject.GetComponent<Image>();
            }
            placeholder.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Folder + "/ShieldIcon.png");
            placeholder.color = new Color(.14150941f, .14150941f, .14150941f, .56078434f);
            placeholder.preserveAspect = true;
            placeholder.raycastTarget = false;
            placeholder.rectTransform.anchorMin = Vector2.zero;
            placeholder.rectTransform.anchorMax = Vector2.one;
            placeholder.rectTransform.offsetMin = new Vector2(19, 19);
            placeholder.rectTransform.offsetMax = new Vector2(-19, -19);
            placeholder.transform.SetAsFirstSibling();
            string[] names = { "EquipHelmet", "EquipChest", "EquipLeggins", "EquipBoots", "EquipOffHand" };
            for (int i = 0; i < names.Length; i++)
            {
                var slot = i == 4 ? offHand : transforms.First(t => t.name == names[i]);
                slot.anchorMin = slot.anchorMax = slot.pivot = new Vector2(0, 1);
                slot.anchoredPosition = new Vector2(38, -181 - i * 98);
                slot.sizeDelta = new Vector2(82, 82);
            }
        }

        private static Mesh CreateMesh()
        {
            var vertices = new List<Vector3>(); var uv = new List<Vector2>(); var triangles = new List<int>();
            void Face(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Rect pixels)
            {
                int start = vertices.Count; vertices.AddRange(new[] { a, b, c, d });
                uv.AddRange(new[] { new Vector2(pixels.xMin / 64, 1 - pixels.yMax / 64), new Vector2(pixels.xMax / 64, 1 - pixels.yMax / 64), new Vector2(pixels.xMax / 64, 1 - pixels.yMin / 64), new Vector2(pixels.xMin / 64, 1 - pixels.yMin / 64) });
                triangles.AddRange(new[] { start, start + 1, start + 2, start, start + 2, start + 3 });
            }
            void Box(Vector3 center, Vector3 size, Vector2 origin, Vector3 texelSize)
            {
                float u = origin.x, v = origin.y, w = texelSize.x, h = texelSize.y, depth = texelSize.z;
                var front = new Rect(u + depth, v + depth, w, h);
                var back = new Rect(u + 2 * depth + w, v + depth, w, h);
                var left = new Rect(u, v + depth, depth, h);
                var right = new Rect(u + depth + w, v + depth, depth, h);
                var top = new Rect(u + depth, v, w, depth);
                var bottom = new Rect(u + depth + w, v, w, depth);
                var lo = center - size / 2; var hi = center + size / 2;
                Face(new Vector3(lo.x,lo.y,hi.z),new Vector3(hi.x,lo.y,hi.z),new Vector3(hi.x,hi.y,hi.z),new Vector3(lo.x,hi.y,hi.z),front);
                Face(new Vector3(hi.x,lo.y,lo.z),new Vector3(lo.x,lo.y,lo.z),new Vector3(lo.x,hi.y,lo.z),new Vector3(hi.x,hi.y,lo.z),back);
                Face(new Vector3(lo.x,lo.y,lo.z),new Vector3(lo.x,lo.y,hi.z),new Vector3(lo.x,hi.y,hi.z),new Vector3(lo.x,hi.y,lo.z),left);
                Face(new Vector3(hi.x,lo.y,hi.z),new Vector3(hi.x,lo.y,lo.z),new Vector3(hi.x,hi.y,lo.z),new Vector3(hi.x,hi.y,hi.z),right);
                Face(new Vector3(lo.x,hi.y,hi.z),new Vector3(hi.x,hi.y,hi.z),new Vector3(hi.x,hi.y,lo.z),new Vector3(lo.x,hi.y,lo.z),top);
                Face(new Vector3(lo.x,lo.y,lo.z),new Vector3(hi.x,lo.y,lo.z),new Vector3(hi.x,lo.y,hi.z),new Vector3(lo.x,lo.y,hi.z),bottom);
            }
            Box(Vector3.zero, new Vector3(.6f,1.1f,.05f), Vector2.zero, new Vector3(12,22,1));
            Box(new Vector3(0,0,-.175f),new Vector3(.1f,.3f,.3f),new Vector2(26,0),new Vector3(2,6,6));
            var mesh = new Mesh { name = "MinecraftShield" }; mesh.SetVertices(vertices); mesh.SetUVs(0,uv); mesh.SetTriangles(triangles,0); mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
        }
    }
}
