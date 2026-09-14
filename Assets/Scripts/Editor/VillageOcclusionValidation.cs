using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using MineArena.Controllers;

namespace MineArena.Editor
{
    public static class VillageOcclusionValidation
    {
        [InitializeOnLoadMethod]
        private static void Watch()
        {
            EditorApplication.update += () =>
            {
                const string request = "Temp/village-occlusion.request";
                if (!File.Exists(request) || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
                File.Delete(request);
                Validate();
            };
        }

        [MenuItem("MineArena/Validation/Village Occlusion")]
        public static void Validate()
        {
            ValidateLocation("LocationVillage", "Temp/village-occlusion-result.txt");
            ValidateLocation("LocationMine", "Temp/mine-occlusion-result.txt");
        }

        private static void ValidateLocation(string location, string resultPath)
        {
            Material material = null;
            bool asyncCompilation = ShaderUtil.allowAsyncCompilation;
            try
            {
                ShaderUtil.allowAsyncCompilation = false;
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Arenas/" + location + ".prefab");
                var component = prefab.GetComponent<VillageOcclusion>();
                if (!component) throw new Exception("Missing VillageOcclusion on map root.");
                var data = new SerializedObject(component);
                var shader = data.FindProperty("_shader").objectReferenceValue as Shader;
                if (!shader) throw new Exception("Missing shader reference.");
                var renderer = prefab.GetComponent<Renderer>();
                if (!renderer) throw new Exception("Missing root map renderer.");
                var wood = data.FindProperty("_occludingMaterials");
                var eligible = Enumerable.Range(0, wood.arraySize)
                    .Select(i => wood.GetArrayElementAtIndex(i).objectReferenceValue as Material).ToArray();
                var matches = renderer.sharedMaterials.Where(m => m && eligible.Contains(m)).ToArray();
                if (matches.Length == 0) throw new Exception("No occluding material slots match.");
                if (location == "LocationMine" && !matches.Any(m => m.name == "Stone"))
                    throw new Exception("Mine stone walls are not included.");
                material = new Material(shader);
                for (int i = 0; i < material.passCount; i++)
                    if (!material.SetPass(i)) throw new Exception("Cannot activate shader pass " + i);
                var errors = ShaderUtil.GetShaderMessages(shader)
                    .Where(m => m.severity == UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error).ToArray();
                if (errors.Length > 0) throw new Exception(string.Join("\n", errors.Select(e => e.message)));
                File.WriteAllText(resultPath, "PASS: prefab references, root renderer, "
                    + matches.Length + " occluding slots, shader passes. Materials: "
                    + string.Join(", ", matches.Select(m => m.name).Distinct()));
            }
            catch (Exception exception)
            {
                File.WriteAllText(resultPath, "FAIL: " + exception);
                Debug.LogException(exception);
            }
            finally
            {
                ShaderUtil.allowAsyncCompilation = asyncCompilation;
                if (material) UnityEngine.Object.DestroyImmediate(material);
            }
        }
    }
}
