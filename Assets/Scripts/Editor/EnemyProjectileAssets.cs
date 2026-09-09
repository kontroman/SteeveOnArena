using System.IO;
using UnityEditor;
using UnityEngine;

namespace MineArena.Editor
{
    public static class EnemyProjectileAssets
    {
        private const string Folder = "Assets/Prefabs/Objects/Projectile/";

        [InitializeOnLoadMethod]
        private static void Watch() => EditorApplication.update += () =>
        {
            const string request = "Temp/enemy-projectile-assets.request";
            if (!File.Exists(request) || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
            File.Delete(request);
            BuildArrowhead();
            BuildPixelMaterials();
        };

        [MenuItem("MineArena/Combat/Rebuild Arrowhead")]
        public static void BuildArrowhead()
        {
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(Folder + "Arrowhead.asset");
            bool create = mesh == null;
            if (create) mesh = new Mesh();
            mesh.Clear();
            mesh.name = "Arrowhead";
            Vector3[] corners = {
                new Vector3(0, 0, 0.7f), new Vector3(0, 0, -0.5f),
                new Vector3(0.4f, 0, -0.05f), new Vector3(0, 0.15f, -0.05f),
                new Vector3(-0.4f, 0, -0.05f), new Vector3(0, -0.15f, -0.05f)
            };
            int[] faces = { 0,2,3, 0,3,4, 0,4,5, 0,5,2, 1,3,2, 1,4,3, 1,5,4, 1,2,5 };
            var vertices = new Vector3[faces.Length];
            var triangles = new int[faces.Length];
            var uv = new Vector2[faces.Length];
            for (int i = 0; i < faces.Length; i++)
            {
                vertices[i] = corners[faces[i]];
                triangles[i] = i;
                uv[i] = new Vector2(vertices[i].x / 0.8f + 0.5f, (vertices[i].z + 0.5f) / 1.2f);
            }
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.uv = uv;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            if (create) AssetDatabase.CreateAsset(mesh, Folder + "Arrowhead.asset");
            else EditorUtility.SetDirty(mesh);
            var root = PrefabUtility.LoadPrefabContents(Folder + "Arrow.prefab");
            try
            {
                var head = root.transform.Find("Arrowhead") ?? root.transform.Find("Sphere");
                head.name = "Arrowhead";
                head.localRotation = Quaternion.identity;
                head.localScale = new Vector3(0.16f, 0.16f, 0.22f);
                head.GetComponent<MeshFilter>().sharedMesh = mesh;
                PrefabUtility.SaveAsPrefabAsset(root, Folder + "Arrow.prefab");
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            AssetDatabase.SaveAssets();
        }

        [MenuItem("MineArena/Combat/Rebuild Pixel Projectile Materials")]
        public static void BuildPixelMaterials()
        {
            string[] names = { "ArrowWood", "ArrowFeather", "ArrowTip", "WitchPotion", "PotionGlass" };
            string[][] palettes = {
                new[] { "765232", "62432A", "8B6540", "987348" },
                new[] { "918B7B", "686458", "A8A18D", "7E786B" },
                new[] { "59696E", "36464D", "7C8C90", "485B62" },
                new[] { "694082", "4E305F", "805098", "976AA7" },
                new[] { "6F9798", "507A7E", "A2BBBB", "82A6A5" }
            };
            var shader = Shader.Find("Universal Render Pipeline/Simple Lit");
            if (shader == null) throw new System.InvalidOperationException("URP Simple Lit shader not imported");
            for (int kind = 0; kind < names.Length; kind++)
            {
                var colors = new Color[4];
                for (int i = 0; i < colors.Length; i++) ColorUtility.TryParseHtmlString("#" + palettes[kind][i], out colors[i]);
                var texture = new Texture2D(16, 16, TextureFormat.RGBA32, false);
                for (int y = 0; y < 16; y++)
                for (int x = 0; x < 16; x++)
                {
                    int shade = 0;
                    switch (kind)
                    {
                        case 0: // Narrow grain with staggered dark notches.
                            shade = x % 5 == 0 ? 1 : x % 5 == 1 ? 2 : 0;
                            if ((x + y / 4 * 3) % 13 == 0 && y % 4 < 2) shade = 1;
                            break;
                        case 1: // A dark quill and stepped diagonal feather barbs.
                            shade = x == 7 || x == 8 ? 1 : (y + Mathf.Abs(x - 7)) % 4 == 0 ? 3 : 0;
                            if (x < 2 || x > 13) shade = 2;
                            break;
                        case 2: // Dull iron, with a stepped bevel instead of a white highlight.
                            shade = x < 2 || y < 2 ? 1 : x > 12 || y > 12 ? 2 : (x / 3 + y / 3) % 4 == 0 ? 3 : 0;
                            break;
                        case 3: // Purple liquid with distinct square bubbles and dark lower sediment.
                            shade = y < 3 ? 1 : y == 11 || y == 12 ? 2 : 0;
                            if ((x / 2 + y / 2 * 3) % 11 == 0) shade = 2;
                            if ((x == 3 || x == 4) && (y == 8 || y == 9)) shade = 3;
                            if ((x == 10 || x == 11) && (y == 5 || y == 6)) shade = 1;
                            break;
                        case 4:
                            shade = x < 2 || y < 2 ? 1 : x == 3 || x == 4 ? 2 : y % 5 == 0 ? 3 : 0;
                            break;
                    }
                    texture.SetPixel(x, y, colors[shade]);
                }
                texture.Apply();
                string path = Folder + names[kind] + "-pixel.png";
                File.WriteAllBytes(path, texture.EncodeToPNG());
                Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = TextureImporterType.Default;
                importer.filterMode = FilterMode.Point;
                importer.wrapMode = TextureWrapMode.Repeat;
                importer.mipmapEnabled = true;
                importer.anisoLevel = 0;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
                var material = AssetDatabase.LoadAssetAtPath<Material>(Folder + names[kind] + ".mat");
                if (material == null)
                {
                    material = new Material(shader);
                    AssetDatabase.CreateAsset(material, Folder + names[kind] + ".mat");
                }
                material.shader = shader;
                material.shaderKeywords = new string[0];
                material.color = Color.white;
                material.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                material.SetColor("_BaseColor", Color.white);
                material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(path));
                material.SetFloat("_Smoothness", 0);
                material.SetFloat("_SpecularHighlights", 1);
                material.SetColor("_SpecColor", Color.black);
                material.SetColor("_EmissionColor", Color.black);
                material.enableInstancing = true;
                EditorUtility.SetDirty(material);
            }
            var potion = PrefabUtility.LoadPrefabContents(Folder + "Potion.prefab");
            try
            {
                potion.transform.Find("Cylinder").GetComponent<MeshRenderer>().sharedMaterial =
                    AssetDatabase.LoadAssetAtPath<Material>(Folder + "PotionGlass.mat");
                PrefabUtility.SaveAsPrefabAsset(potion, Folder + "Potion.prefab");
            }
            finally { PrefabUtility.UnloadPrefabContents(potion); }
            AssetDatabase.SaveAssets();
        }
    }
}
