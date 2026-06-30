using System;
using System.Collections;
using System.Collections.Generic;
using Devotion.SDK.Async;
using Devotion.SDK.Enums;
using Devotion.SDK.Interfaces;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace MineArena.Buildings
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public class BuildingConstructionEffect : MonoBehaviour
    {
        private const string AutoBlocksParentName = "[BuildingConstructionEffect Blocks]";
        private const string ChunkMeshNamePrefix = "ConstructionMeshChunk_";
        private const float MinimumCellSize = 0.05f;

        [Header("Source")]
        [SerializeField] private Renderer sourceRenderer;

        [Header("Chunks")]
        [SerializeField] private Vector3 cellSize = Vector3.one;
        [SerializeField] private float blockScaleMultiplier = 1f;
        [SerializeField] private Transform parentForBlocks;
        [SerializeField] private int maxBlocksSafetyLimit = 1500;

        [Header("Animation")]
        [SerializeField] private float fallHeightMin = 10f;
        [SerializeField] private float fallHeightMax = 15;
        [SerializeField] private float animationDuration = 1.5f;
        [SerializeField] private float delayPerHeight = 0.75f;
        [SerializeField] private float randomDelay = 0.75f;
        [SerializeField] private bool buildFromBottomToTop = true;
        [SerializeField] private AnimationCurve movementCurve = new AnimationCurve(
            new Keyframe(0f, 0f, 0f, 2.5f),
            new Keyframe(1f, 1f, 0f, 0f));

        [Header("Visibility")]
        [SerializeField] private bool hideSourceDuringPreview = true;
        [SerializeField] private bool showSourceOnComplete = true;
        [SerializeField] private bool clearBlocksOnComplete = true;

        [SerializeField, HideInInspector] private List<BlockState> blocks = new List<BlockState>();

        private Coroutine runtimeCoroutine;
        private Promise runtimePromise;

#if UNITY_EDITOR
        private bool editorPreviewPlaying;
        private double editorPreviewStartedAt;
#endif

        public int GeneratedBlockCount => blocks != null ? blocks.Count : 0;
        public Renderer SourceRenderer => sourceRenderer;
        public Vector3 CellSize => cellSize;
        public int MaxBlocksSafetyLimit => maxBlocksSafetyLimit;

        public void PlayRuntime()
        {
            PlayRuntimeAsync();
        }

        public IPromise PlayRuntimeAsync()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning($"{nameof(BuildingConstructionEffect)}: PlayRuntime can only run in Play Mode.", this);
                return Promise.ResolveAndReturn();
            }

            if (runtimeCoroutine != null)
            {
                StopCoroutine(runtimeCoroutine);
                RejectRuntimePromise(new OperationCanceledException($"{nameof(BuildingConstructionEffect)} runtime animation was interrupted."));
            }

            runtimePromise = new Promise();
            Debug.Log($"{nameof(BuildingConstructionEffect)}: runtime animation requested on {gameObject.name}.", this);
            runtimeCoroutine = StartCoroutine(PlayRuntimeRoutine(runtimePromise));
            return runtimePromise;
        }

        public void GeneratePreviewBlocks()
        {
            if (!ValidateSetup())
                return;

            ClearGeneratedBlocks(false);

            var meshFilter = sourceRenderer.GetComponent<MeshFilter>();
            var sourceMesh = meshFilter != null ? meshFilter.sharedMesh : null;

            if (sourceMesh == null)
            {
                Debug.LogError($"{nameof(BuildingConstructionEffect)}: Source Renderer must be on an object with MeshFilter.", this);
                return;
            }

            Dictionary<CellKey, MeshChunkData> chunkDataByCell;

            try
            {
                chunkDataByCell = BuildSourceMeshChunkData(sourceMesh);
            }
            catch (Exception exception)
            {
                Debug.LogError($"{nameof(BuildingConstructionEffect)}: failed to read source mesh. Enable Read/Write on the imported mesh if needed. {exception.Message}", this);
                return;
            }

            if (chunkDataByCell.Count > maxBlocksSafetyLimit)
            {
                Debug.LogWarning(
                    $"{nameof(BuildingConstructionEffect)}: generated chunk count {chunkDataByCell.Count} exceeds Max Blocks Safety Limit {maxBlocksSafetyLimit}. Increase Cell Size or the limit.",
                    this);
                return;
            }

            var blocksParent = GetOrCreateBlocksParent();
            var generated = new List<BlockState>(chunkDataByCell.Count);
            var sourceBounds = sourceRenderer.bounds;
            var sourceMaterials = sourceRenderer.sharedMaterials;

            foreach (var pair in chunkDataByCell)
            {
                var chunkData = pair.Value;
                var chunkObject = CreateChunkObject(blocksParent, chunkData, sourceMaterials);

                if (chunkObject == null)
                    continue;

                var state = CreateBlockState(chunkObject.transform, chunkData.Center, sourceBounds);
                ApplyBlockTransform(state, state.FinalPosition);
                generated.Add(state);
            }

            blocks = generated;
            SetSourceVisible(true);
            MarkDirty();
        }

        public void PlayEditorPreview()
        {
#if UNITY_EDITOR
            if (Application.isPlaying)
            {
                PlayRuntime();
                return;
            }

            if (blocks == null || blocks.Count == 0)
                GeneratePreviewBlocks();

            if (blocks == null || blocks.Count == 0)
                return;

            PrepareBlocksForAnimation();

            if (hideSourceDuringPreview)
                SetSourceVisible(false);

            editorPreviewStartedAt = EditorApplication.timeSinceStartup;
            editorPreviewPlaying = true;
            EditorApplication.update -= UpdateEditorPreview;
            EditorApplication.update += UpdateEditorPreview;
            MarkDirty();
#else
            Debug.LogWarning($"{nameof(BuildingConstructionEffect)}: editor preview is only available in the Unity Editor.", this);
#endif
        }

        public void StopEditorPreview()
        {
#if UNITY_EDITOR
            editorPreviewPlaying = false;
            EditorApplication.update -= UpdateEditorPreview;
            MarkDirty();
#endif
        }

        public void ClearPreviewBlocks()
        {
            StopEditorPreview();
            ClearGeneratedBlocks(true);
        }

        public void ResetToFinalPositions()
        {
            StopEditorPreview();

            if (blocks == null)
                return;

            foreach (var block in blocks)
            {
                if (block == null || block.Transform == null)
                    continue;

                ApplyBlockTransform(block, block.FinalPosition);
            }

            SetSourceVisible(true);
            MarkDirty();
        }

        private Dictionary<CellKey, MeshChunkData> BuildSourceMeshChunkData(Mesh sourceMesh)
        {
            var sourceTransform = sourceRenderer.transform;
            var sourceBounds = sourceRenderer.bounds;
            var sourceVertices = sourceMesh.vertices;
            var sourceNormals = sourceMesh.normals;
            var sourceUv = sourceMesh.uv;
            var hasNormals = sourceNormals != null && sourceNormals.Length == sourceVertices.Length;
            var hasUv = sourceUv != null && sourceUv.Length == sourceVertices.Length;
            var chunks = new Dictionary<CellKey, MeshChunkData>();
            var subMeshCount = sourceMesh.subMeshCount;

            for (int subMeshIndex = 0; subMeshIndex < subMeshCount; subMeshIndex++)
            {
                var triangles = sourceMesh.GetTriangles(subMeshIndex);

                for (int i = 0; i + 2 < triangles.Length; i += 3)
                {
                    var i0 = triangles[i];
                    var i1 = triangles[i + 1];
                    var i2 = triangles[i + 2];
                    var w0 = sourceTransform.TransformPoint(sourceVertices[i0]);
                    var w1 = sourceTransform.TransformPoint(sourceVertices[i1]);
                    var w2 = sourceTransform.TransformPoint(sourceVertices[i2]);
                    var triangleCenter = (w0 + w1 + w2) / 3f;
                    var cellKey = GetCellKey(triangleCenter, sourceBounds);

                    if (!chunks.TryGetValue(cellKey, out var chunk))
                    {
                        chunk = new MeshChunkData(GetCellCenter(cellKey, sourceBounds), subMeshCount);
                        chunks.Add(cellKey, chunk);
                    }

                    var n0 = hasNormals ? sourceTransform.TransformDirection(sourceNormals[i0]).normalized : Vector3.zero;
                    var n1 = hasNormals ? sourceTransform.TransformDirection(sourceNormals[i1]).normalized : Vector3.zero;
                    var n2 = hasNormals ? sourceTransform.TransformDirection(sourceNormals[i2]).normalized : Vector3.zero;
                    var uv0 = hasUv ? sourceUv[i0] : Vector2.zero;
                    var uv1 = hasUv ? sourceUv[i1] : Vector2.zero;
                    var uv2 = hasUv ? sourceUv[i2] : Vector2.zero;

                    chunk.AddTriangle(subMeshIndex, w0, w1, w2, n0, n1, n2, uv0, uv1, uv2, hasNormals);
                }
            }

            return chunks;
        }

        private CellKey GetCellKey(Vector3 worldPosition, Bounds sourceBounds)
        {
            return new CellKey(
                Mathf.FloorToInt((worldPosition.x - sourceBounds.min.x) / cellSize.x),
                Mathf.FloorToInt((worldPosition.y - sourceBounds.min.y) / cellSize.y),
                Mathf.FloorToInt((worldPosition.z - sourceBounds.min.z) / cellSize.z));
        }

        private Vector3 GetCellCenter(CellKey key, Bounds sourceBounds)
        {
            return new Vector3(
                sourceBounds.min.x + (key.X + 0.5f) * cellSize.x,
                sourceBounds.min.y + (key.Y + 0.5f) * cellSize.y,
                sourceBounds.min.z + (key.Z + 0.5f) * cellSize.z);
        }

        private GameObject CreateChunkObject(Transform blocksParent, MeshChunkData chunkData, Material[] sourceMaterials)
        {
            var chunkObject = new GameObject("ConstructionMeshChunk");
            chunkObject.transform.SetParent(blocksParent, false);
            chunkObject.transform.rotation = Quaternion.identity;

#if UNITY_EDITOR
            if (!Application.isPlaying)
                Undo.RegisterCreatedObjectUndo(chunkObject, "Create Building Mesh Chunk");
#endif

            var meshFilter = chunkObject.AddComponent<MeshFilter>();
            var meshRenderer = chunkObject.AddComponent<MeshRenderer>();
            meshFilter.sharedMesh = chunkData.CreateMesh();
            meshRenderer.sharedMaterials = sourceMaterials;
            return chunkObject;
        }

        private IEnumerator PlayRuntimeRoutine(Promise promise)
        {
            GeneratePreviewBlocks();

            if (blocks == null || blocks.Count == 0)
            {
                Debug.LogWarning($"{nameof(BuildingConstructionEffect)}: no generated blocks, runtime animation will finish immediately.", this);
                runtimeCoroutine = null;
                ResolveRuntimePromise(promise);
                yield break;
            }

            PrepareBlocksForAnimation();
            Debug.Log($"{nameof(BuildingConstructionEffect)}: runtime animation started with {blocks.Count} blocks, totalDuration={GetTotalAnimationTime():0.00}s.", this);

            if (hideSourceDuringPreview)
                SetSourceVisible(false);

            var elapsed = 0f;
            var totalDuration = GetTotalAnimationTime();

            while (elapsed < totalDuration)
            {
                UpdateAnimation(elapsed);
                elapsed += Time.deltaTime;
                yield return null;
            }

            UpdateAnimation(totalDuration);
            CompletePreview();
            runtimeCoroutine = null;
            Debug.Log($"{nameof(BuildingConstructionEffect)}: runtime animation finished.", this);
            ResolveRuntimePromise(promise);
        }

        private void ResolveRuntimePromise(Promise promise)
        {
            if (promise != null && promise.State == PromiseState.Pending)
                promise.Resolve();

            if (runtimePromise == promise)
                runtimePromise = null;
        }

        private void RejectRuntimePromise(Exception exception)
        {
            if (runtimePromise != null && runtimePromise.State == PromiseState.Pending)
                runtimePromise.Reject(exception);

            runtimePromise = null;
        }

        private void PrepareBlocksForAnimation()
        {
            if (blocks == null)
                return;

            var sourceBounds = sourceRenderer != null ? sourceRenderer.bounds : default;

            foreach (var block in blocks)
            {
                if (block == null || block.Transform == null)
                    continue;

                block.RandomDelay = randomDelay > 0f ? UnityEngine.Random.Range(0f, randomDelay) : 0f;
                block.HeightDelay = buildFromBottomToTop ? GetNormalizedHeight(block.FinalPosition, sourceBounds) * delayPerHeight : 0f;
                ApplyBlockTransform(block, block.StartPosition);
            }
        }

        private void UpdateAnimation(float elapsed)
        {
            if (blocks == null)
                return;

            foreach (var block in blocks)
            {
                if (block == null || block.Transform == null)
                    continue;

                var blockElapsed = elapsed - block.TotalDelay;
                var t = animationDuration <= 0f ? 1f : Mathf.Clamp01(blockElapsed / animationDuration);
                var curvedT = movementCurve != null ? movementCurve.Evaluate(t) : t;
                var position = Vector3.LerpUnclamped(block.StartPosition, block.FinalPosition, curvedT);
                ApplyBlockTransform(block, position);
            }
        }

        private float GetTotalAnimationTime()
        {
            var totalDuration = Mathf.Max(0.01f, animationDuration);

            if (blocks == null)
                return totalDuration;

            foreach (var block in blocks)
            {
                if (block == null)
                    continue;

                totalDuration = Mathf.Max(totalDuration, block.TotalDelay + animationDuration);
            }

            return totalDuration;
        }

        private void CompletePreview()
        {
            ResetBlocksToFinalPositionOnly();

            if (showSourceOnComplete)
                SetSourceVisible(true);

            if (clearBlocksOnComplete)
                ClearGeneratedBlocks(false);

            MarkDirty();
        }

        private void ResetBlocksToFinalPositionOnly()
        {
            if (blocks == null)
                return;

            foreach (var block in blocks)
            {
                if (block == null || block.Transform == null)
                    continue;

                ApplyBlockTransform(block, block.FinalPosition);
            }
        }

        private BlockState CreateBlockState(Transform blockTransform, Vector3 finalPosition, Bounds sourceBounds)
        {
            var fallHeight = UnityEngine.Random.Range(fallHeightMin, fallHeightMax);
            var normalizedHeight = GetNormalizedHeight(finalPosition, sourceBounds);

            return new BlockState
            {
                Transform = blockTransform,
                FinalPosition = finalPosition,
                StartPosition = finalPosition + Vector3.up * fallHeight,
                Scale = Vector3.one * blockScaleMultiplier,
                HeightDelay = buildFromBottomToTop ? normalizedHeight * delayPerHeight : 0f,
                RandomDelay = randomDelay > 0f ? UnityEngine.Random.Range(0f, randomDelay) : 0f
            };
        }

        private void ApplyBlockTransform(BlockState block, Vector3 position)
        {
            block.Transform.position = position;
            block.Transform.rotation = Quaternion.identity;
            SetWorldScale(block.Transform, block.Scale);
        }

        private Transform GetOrCreateBlocksParent()
        {
            if (parentForBlocks != null)
                return parentForBlocks;

            var existing = transform.Find(AutoBlocksParentName);

            if (existing != null)
            {
                NormalizeGeneratedParent(existing);
                return existing;
            }

            var container = new GameObject(AutoBlocksParentName);
            container.transform.SetParent(transform, false);
            NormalizeGeneratedParent(container.transform);

#if UNITY_EDITOR
            if (!Application.isPlaying)
                Undo.RegisterCreatedObjectUndo(container, "Create Building Preview Blocks Parent");
#endif

            return container.transform;
        }

        private static void NormalizeGeneratedParent(Transform target)
        {
            target.position = Vector3.zero;
            target.rotation = Quaternion.identity;
            SetWorldScale(target, Vector3.one);
        }

        private void ClearGeneratedBlocks(bool restoreSource)
        {
            if (blocks != null)
            {
                for (int i = blocks.Count - 1; i >= 0; i--)
                {
                    var block = blocks[i];

                    if (block == null || block.Transform == null)
                        continue;

                    DestroyBlockObject(block.Transform.gameObject);
                }

                blocks.Clear();
            }

            TryDestroyEmptyAutoParent();

            if (restoreSource)
                SetSourceVisible(true);

            MarkDirty();
        }

        private void DestroyBlockObject(GameObject block)
        {
            if (block == null)
                return;

            DestroyGeneratedMesh(block);

#if UNITY_EDITOR
            if (!Application.isPlaying)
                Undo.DestroyObjectImmediate(block);
            else
#endif
                Destroy(block);
        }

        private void DestroyGeneratedMesh(GameObject block)
        {
            var meshFilter = block.GetComponent<MeshFilter>();

            if (meshFilter == null || meshFilter.sharedMesh == null || !meshFilter.sharedMesh.name.StartsWith(ChunkMeshNamePrefix, StringComparison.Ordinal))
                return;

            var mesh = meshFilter.sharedMesh;
            meshFilter.sharedMesh = null;

#if UNITY_EDITOR
            if (!Application.isPlaying)
                DestroyImmediate(mesh);
            else
#endif
                Destroy(mesh);
        }

        private void TryDestroyEmptyAutoParent()
        {
            if (parentForBlocks != null)
                return;

            var autoParent = transform.Find(AutoBlocksParentName);

            if (autoParent == null || autoParent.childCount > 0)
                return;

            DestroyBlockObject(autoParent.gameObject);
        }

        private bool ValidateSetup()
        {
            if (sourceRenderer == null)
            {
                Debug.LogError($"{nameof(BuildingConstructionEffect)}: Source Renderer is not assigned.", this);
                return false;
            }

            if (cellSize.x < MinimumCellSize || cellSize.y < MinimumCellSize || cellSize.z < MinimumCellSize)
            {
                Debug.LogWarning(
                    $"{nameof(BuildingConstructionEffect)}: Cell Size is too small. Each axis must be at least {MinimumCellSize}.",
                    this);
                return false;
            }

            if (maxBlocksSafetyLimit <= 0)
            {
                Debug.LogWarning($"{nameof(BuildingConstructionEffect)}: Max Blocks Safety Limit must be greater than zero.", this);
                return false;
            }

            return true;
        }

        private static float GetNormalizedHeight(Vector3 position, Bounds bounds)
        {
            if (bounds.size.y <= 0.0001f)
                return 0f;

            return Mathf.Clamp01((position.y - bounds.min.y) / bounds.size.y);
        }

        private void SetSourceVisible(bool visible)
        {
            if (sourceRenderer == null)
                return;

#if UNITY_EDITOR
            if (!Application.isPlaying && sourceRenderer.enabled != visible)
                Undo.RecordObject(sourceRenderer, "Change Building Source Visibility");
#endif

            sourceRenderer.enabled = visible;
        }

        private void OnEnable()
        {
            if (sourceRenderer == null)
                sourceRenderer = GetComponentInChildren<Renderer>();
        }

        private void OnDisable()
        {
            StopEditorPreview();

            if (runtimeCoroutine != null)
            {
                StopCoroutine(runtimeCoroutine);
                runtimeCoroutine = null;
            }

            RejectRuntimePromise(new OperationCanceledException($"{nameof(BuildingConstructionEffect)} was disabled."));
        }

        private void OnValidate()
        {
            cellSize = new Vector3(
                Mathf.Max(MinimumCellSize, cellSize.x),
                Mathf.Max(MinimumCellSize, cellSize.y),
                Mathf.Max(MinimumCellSize, cellSize.z));

            blockScaleMultiplier = Mathf.Max(0.01f, blockScaleMultiplier);
            fallHeightMin = Mathf.Max(0f, fallHeightMin);
            fallHeightMax = Mathf.Max(fallHeightMin, fallHeightMax);
            animationDuration = Mathf.Max(0.01f, animationDuration);
            delayPerHeight = Mathf.Max(0f, delayPerHeight);
            randomDelay = Mathf.Max(0f, randomDelay);
            maxBlocksSafetyLimit = Mathf.Max(1, maxBlocksSafetyLimit);
        }

        private void OnDrawGizmosSelected()
        {
            if (sourceRenderer == null)
                return;

            var bounds = sourceRenderer.bounds;

            Gizmos.color = new Color(0.2f, 0.75f, 1f, 0.25f);
            Gizmos.DrawCube(bounds.center, bounds.size);
            Gizmos.color = new Color(0.05f, 0.45f, 1f, 0.9f);
            Gizmos.DrawWireCube(bounds.center, bounds.size);
        }

        private void MarkDirty()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                EditorUtility.SetDirty(this);

                if (gameObject.scene.IsValid())
                    EditorSceneManager.MarkSceneDirty(gameObject.scene);
            }
#endif
        }

        private static void SetWorldScale(Transform target, Vector3 worldScale)
        {
            var parent = target.parent;

            if (parent == null)
            {
                target.localScale = worldScale;
                return;
            }

            var parentScale = parent.lossyScale;
            target.localScale = new Vector3(
                SafeDivide(worldScale.x, parentScale.x),
                SafeDivide(worldScale.y, parentScale.y),
                SafeDivide(worldScale.z, parentScale.z));
        }

        private static float SafeDivide(float value, float divisor)
        {
            return Mathf.Abs(divisor) > 0.0001f ? value / divisor : value;
        }

#if UNITY_EDITOR
        private void UpdateEditorPreview()
        {
            if (!editorPreviewPlaying)
            {
                EditorApplication.update -= UpdateEditorPreview;
                return;
            }

            var elapsed = (float)(EditorApplication.timeSinceStartup - editorPreviewStartedAt);

            UpdateAnimation(elapsed);
            SceneView.RepaintAll();

            if (elapsed >= GetTotalAnimationTime())
            {
                editorPreviewPlaying = false;
                EditorApplication.update -= UpdateEditorPreview;
                CompletePreview();
            }
        }
#endif

        private readonly struct CellKey : IEquatable<CellKey>
        {
            public readonly int X;
            public readonly int Y;
            public readonly int Z;

            public CellKey(int x, int y, int z)
            {
                X = x;
                Y = y;
                Z = z;
            }

            public bool Equals(CellKey other)
            {
                return X == other.X && Y == other.Y && Z == other.Z;
            }

            public override bool Equals(object obj)
            {
                return obj is CellKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    var hash = X;
                    hash = (hash * 397) ^ Y;
                    hash = (hash * 397) ^ Z;
                    return hash;
                }
            }
        }

        private class MeshChunkData
        {
            private readonly List<Vector3> vertices = new List<Vector3>();
            private readonly List<Vector3> normals = new List<Vector3>();
            private readonly List<Vector2> uv = new List<Vector2>();
            private readonly List<int>[] trianglesBySubMesh;
            private bool hasNormals = true;

            public Vector3 Center { get; }

            public MeshChunkData(Vector3 center, int subMeshCount)
            {
                Center = center;
                trianglesBySubMesh = new List<int>[subMeshCount];

                for (int i = 0; i < trianglesBySubMesh.Length; i++)
                    trianglesBySubMesh[i] = new List<int>();
            }

            public void AddTriangle(
                int subMeshIndex,
                Vector3 w0,
                Vector3 w1,
                Vector3 w2,
                Vector3 n0,
                Vector3 n1,
                Vector3 n2,
                Vector2 uv0,
                Vector2 uv1,
                Vector2 uv2,
                bool sourceHasNormals)
            {
                var vertexIndex = vertices.Count;
                vertices.Add(w0 - Center);
                vertices.Add(w1 - Center);
                vertices.Add(w2 - Center);
                uv.Add(uv0);
                uv.Add(uv1);
                uv.Add(uv2);

                if (sourceHasNormals)
                {
                    normals.Add(n0);
                    normals.Add(n1);
                    normals.Add(n2);
                }
                else
                {
                    hasNormals = false;
                }

                trianglesBySubMesh[subMeshIndex].Add(vertexIndex);
                trianglesBySubMesh[subMeshIndex].Add(vertexIndex + 1);
                trianglesBySubMesh[subMeshIndex].Add(vertexIndex + 2);
            }

            public Mesh CreateMesh()
            {
                var mesh = new Mesh
                {
                    name = $"{ChunkMeshNamePrefix}{Center.x:0.###}_{Center.y:0.###}_{Center.z:0.###}"
                };

                if (vertices.Count > 65535)
                    mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

                mesh.SetVertices(vertices);

                if (hasNormals && normals.Count == vertices.Count)
                    mesh.SetNormals(normals);

                if (uv.Count == vertices.Count)
                    mesh.SetUVs(0, uv);

                mesh.subMeshCount = trianglesBySubMesh.Length;

                for (int i = 0; i < trianglesBySubMesh.Length; i++)
                    mesh.SetTriangles(trianglesBySubMesh[i], i, false);

                if (!hasNormals || normals.Count != vertices.Count)
                    mesh.RecalculateNormals();

                mesh.RecalculateBounds();
                return mesh;
            }
        }

        [Serializable]
        private class BlockState
        {
            public Transform Transform;
            public Vector3 FinalPosition;
            public Vector3 StartPosition;
            public Vector3 Scale;
            public float HeightDelay;
            public float RandomDelay;

            public float TotalDelay => HeightDelay + RandomDelay;
        }
    }
}
