using MineArena.Buildings;
using UnityEditor;
using UnityEngine;

namespace MineArena.Editor
{
    [CustomEditor(typeof(BuildingConstructionEffect))]
    public class BuildingConstructionEffectEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawDefaultInspector();
            serializedObject.ApplyModifiedProperties();

            var effect = (BuildingConstructionEffect)target;

            EditorGUILayout.Space(10f);
            EditorGUILayout.LabelField("Preview Tools", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox($"Generated blocks: {effect.GeneratedBlockCount}", MessageType.Info);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                if (GUILayout.Button("Generate Preview Blocks"))
                {
                    Undo.RecordObject(effect, "Generate Building Preview Blocks");
                    effect.GeneratePreviewBlocks();
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Play Editor Preview"))
                    {
                        Undo.RecordObject(effect, "Play Building Editor Preview");
                        RecordSourceRenderer(effect, "Hide Building Source Renderer");
                        effect.PlayEditorPreview();
                    }

                    if (GUILayout.Button("Stop Editor Preview"))
                    {
                        Undo.RecordObject(effect, "Stop Building Editor Preview");
                        effect.StopEditorPreview();
                    }
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Reset To Final Positions"))
                    {
                        Undo.RecordObject(effect, "Reset Building Preview Blocks");
                        RecordSourceRenderer(effect, "Show Building Source Renderer");
                        effect.ResetToFinalPositions();
                    }

                    if (GUILayout.Button("Clear Preview Blocks"))
                    {
                        Undo.RecordObject(effect, "Clear Building Preview Blocks");
                        RecordSourceRenderer(effect, "Show Building Source Renderer");
                        effect.ClearPreviewBlocks();
                    }
                }
            }

            EditorGUILayout.Space(6f);
            DrawSetupWarnings(effect);
        }

        private static void RecordSourceRenderer(BuildingConstructionEffect effect, string undoName)
        {
            if (effect.SourceRenderer != null)
                Undo.RecordObject(effect.SourceRenderer, undoName);
        }

        private static void DrawSetupWarnings(BuildingConstructionEffect effect)
        {
            if (effect.SourceRenderer == null)
                EditorGUILayout.HelpBox("Source Renderer is not assigned.", MessageType.Warning);

            if (effect.GeometryMode == BuildingConstructionEffect.BlockGeometryMode.PrefabGridBlocks && effect.BlockPrefab == null)
                EditorGUILayout.HelpBox("Block Prefab is not assigned. It is required for PrefabGridBlocks mode.", MessageType.Warning);

            if (effect.GeometryMode == BuildingConstructionEffect.BlockGeometryMode.PrefabGridBlocks && effect.SourceCollider == null)
                EditorGUILayout.HelpBox("Source Collider is empty. Blocks will be generated for the whole Renderer.bounds box.", MessageType.None);

            if (effect.CellSize.x <= 0.05f || effect.CellSize.y <= 0.05f || effect.CellSize.z <= 0.05f)
                EditorGUILayout.HelpBox("Cell Size is very small and may create too many blocks.", MessageType.Warning);

            if (effect.MaxBlocksSafetyLimit < 100)
                EditorGUILayout.HelpBox("Max Blocks Safety Limit is low; generation may stop on medium sized buildings.", MessageType.None);
        }
    }
}
