using System;
using System.Collections;
using System.Collections.Generic;
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

        private static readonly Vector3[] CellSampleOffsets =
        {
            Vector3.zero,
            new Vector3(-1f, -1f, -1f),
            new Vector3(-1f, -1f, 1f),
            new Vector3(-1f, 1f, -1f),
            new Vector3(-1f, 1f, 1f),
            new Vector3(1f, -1f, -1f),
            new Vector3(1f, -1f, 1f),
            new Vector3(1f, 1f, -1f),
            new Vector3(1f, 1f, 1f)
        };

        private static readonly Vector3[] MaterialSampleOffsets =
        {
            Vector3.up,
            Vector3.down,
            Vector3.right,
            Vector3.left,
            Vector3.forward,
            Vector3.back
        };

        private static readonly Vector3[] MaterialSampleDirections =
        {
            Vector3.down,
            Vector3.up,
            Vector3.left,
            Vector3.right,
            Vector3.back,
            Vector3.forward
        };

        [Header("Source")]
        [SerializeField] private Renderer sourceRenderer;
        [SerializeField] private Collider sourceCollider;

        [Header("Blocks")]
        [SerializeField] private BlockGeometryMode blockGeometryMode = BlockGeometryMode.SourceMeshChunks;
        [SerializeField] private GameObject blockPrefab;
        [SerializeField] private BlockMaterialMode blockMaterialMode = BlockMaterialMode.SampleSourceSubMesh;
        [SerializeField] private MaterialBlockPrefabOverride[] materialPrefabOverrides;
        [SerializeField] private Vector3 cellSize = Vector3.one;
        [SerializeField] private float blockScaleMultiplier = 1f;
        [SerializeField] private Transform parentForBlocks;
        [SerializeField] private int maxBlocksSafetyLimit = 1500;

        [Header("Animation")]
        [SerializeField] private float fallHeightMin = 3f;
        [SerializeField] private float fallHeightMax = 7f;
        [SerializeField] private float animationDuration = 0.45f;
        [SerializeField] private float delayPerHeight = 0.35f;
        [SerializeField] private float randomDelay = 0.12f;
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

#if UNITY_EDITOR
        private bool editorPreviewPlaying;
        private double editorPreviewStartedAt;
#endif

        public int GeneratedBlockCount => blocks != null ? blocks.Count : 0;
        public Renderer SourceRenderer => sourceRenderer;
        public Collider SourceCollider => sourceCollider;
        public GameObject BlockPrefab => blockPrefab;
        public BlockGeometryMode GeometryMode => blockGeometryMode;
        public Vector3 CellSize => cellSize;
        public int MaxBlocksSafetyLimit => maxBlocksSafetyLimit;

        public void PlayRuntime()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning($"{nameof(BuildingConstructionEffect)}: PlayRuntime can only run in Play Mode.", this);
                return;
            }

            if (runtimeCoroutine != null)
                StopCoroutine(runtimeCoroutine);

            runtimeCoroutine = StartCoroutine(PlayRuntimeRoutine());
        }

        public void GeneratePreviewBlocks()
        {
            if (!ValidateSetup())
                return;

            ClearGeneratedBlocks(false);

            if (blockGeometryMode == BlockGeometryMode.SourceMeshChunks)
            {
                GenerateSourceMeshChunks();
                return;
            }

            GeneratePrefabGridBlocks();
        }

        private void GenerateSourceMeshChunks()
        {
            var meshFilter = sourceRenderer.GetComponent<MeshFilter>();
            var sourceMesh = meshFilter != null ? meshFilter.sharedMesh : null;

            if (sourceMesh == null)
            {
                Debug.LogError($"{nameof(BuildingConstructionEffect)}: SourceMeshChunks requires Source Renderer with MeshFilter and readable shared mesh.", this);
                return;
            }

            var sourceBounds = sourceRenderer.bounds;
            var chunkDataByCell = BuildSourceMeshChunkData(sourceMesh, sourceBounds);

            if (chunkDataByCell.Count > maxBlocksSafetyLimit)
            {
                Debug.LogWarning(
                    $"{nameof(BuildingConstructionEffect)}: source mesh produced {chunkDataByCell.Count} chunks, max safety limit is {maxBlocksSafetyLimit}. Generation stopped. Increase Cell Size or Max Blocks Safety Limit.",
                    this);
                return;
            }

            var blocksParent = GetOrCreateBlocksParent();
            var generated = new List<BlockState>(chunkDataByCell.Count);
            var sourceMaterials = sourceRenderer.sharedMaterials;

            foreach (var pair in chunkDataByCell)
            {
                var chunkData = pair.Value;
                var block = CreateSourceMeshChunkObject(blocksParent, chunkData, sourceMaterials);

                if (block == null)
                    continue;

                var state = CreateBlockState(block.transform, chunkData.Center, sourceBounds, Vector3.one * blockScaleMultiplier);
                ApplyBlockTransform(state, state.FinalPosition);
                generated.Add(state);
            }

            blocks = generated;
            SetSourceVisible(true);
            MarkDirty();
        }

        private Dictionary<CellKey, MeshChunkData> BuildSourceMeshChunkData(Mesh sourceMesh, Bounds sourceBounds)
        {
            var sourceTransform = sourceRenderer.transform;
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
                    var center = (w0 + w1 + w2) / 3f;
                    var key = GetCellKey(center, sourceBounds);

                    if (!chunks.TryGetValue(key, out var chunk))
                    {
                        chunk = new MeshChunkData(GetCellCenter(key, sourceBounds), subMeshCount);
                        chunks.Add(key, chunk);
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

        private GameObject CreateSourceMeshChunkObject(Transform blocksParent, MeshChunkData chunkData, Material[] sourceMaterials)
        {
            var block = new GameObject("SourceMesh_ConstructionChunk");
            block.transform.SetParent(blocksParent, false);
            block.transform.rotation = Quaternion.identity;

#if UNITY_EDITOR
            if (!Application.isPlaying)
                Undo.RegisterCreatedObjectUndo(block, "Create Building Mesh Chunk");
#endif

            var meshFilter = block.AddComponent<MeshFilter>();
            var meshRenderer = block.AddComponent<MeshRenderer>();
            var chunkMesh = chunkData.CreateMesh();
            meshFilter.sharedMesh = chunkMesh;
            meshRenderer.sharedMaterials = sourceMaterials;
            return block;
        }

        private void GeneratePrefabGridBlocks()
        {
            if (blockPrefab == null)
            {
                Debug.LogError($"{nameof(BuildingConstructionEffect)}: Block Prefab is not assigned.", this);
                return;
            }

            var sourceBounds = sourceRenderer.bounds;
            var gridCount = CalculateGridCount(sourceBounds, cellSize);
            var estimatedCells = (long)gridCount.x * gridCount.y * gridCount.z;

            if (estimatedCells > maxBlocksSafetyLimit)
            {
                Debug.LogWarning(
                    $"{nameof(BuildingConstructionEffect)}: calculated {estimatedCells} cells, max safety limit is {maxBlocksSafetyLimit}. Generation stopped. Increase Cell Size or Max Blocks Safety Limit.",
                    this);
                return;
            }

            var blocksParent = GetOrCreateBlocksParent();
            var generated = new List<BlockState>((int)Math.Min(estimatedCells, maxBlocksSafetyLimit));

            Physics.SyncTransforms();

            for (int y = 0; y < gridCount.y; y++)
            {
                for (int x = 0; x < gridCount.x; x++)
                {
                    for (int z = 0; z < gridCount.z; z++)
                    {
                        var center = new Vector3(
                            sourceBounds.min.x + (x + 0.5f) * cellSize.x,
                            sourceBounds.min.y + (y + 0.5f) * cellSize.y,
                            sourceBounds.min.z + (z + 0.5f) * cellSize.z);

                        if (!ShouldCreateBlock(center, cellSize))
                            continue;

                        var sourceMaterial = ResolveBlockMaterial(center);
                        var prefabForCell = ResolveBlockPrefab(sourceMaterial);
                        var block = CreateBlockInstance(blocksParent, prefabForCell);

                        if (block == null)
                            continue;

                        ApplyBlockMaterial(block, sourceMaterial);

                        var state = CreateBlockState(block.transform, center, sourceBounds, GetBlockScale(prefabForCell));
                        ApplyBlockTransform(state, state.FinalPosition);
                        generated.Add(state);
                    }
                }
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

        private IEnumerator PlayRuntimeRoutine()
        {
            GeneratePreviewBlocks();

            if (blocks == null || blocks.Count == 0)
            {
                runtimeCoroutine = null;
                yield break;
            }

            PrepareBlocksForAnimation();

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

        private BlockState CreateBlockState(Transform blockTransform, Vector3 finalPosition, Bounds sourceBounds, Vector3 scale)
        {
            var fallHeight = UnityEngine.Random.Range(fallHeightMin, fallHeightMax);
            var startPosition = finalPosition + Vector3.up * fallHeight;
            var normalizedHeight = GetNormalizedHeight(finalPosition, sourceBounds);

            return new BlockState
            {
                Transform = blockTransform,
                FinalPosition = finalPosition,
                StartPosition = startPosition,
                Scale = scale,
                HeightDelay = buildFromBottomToTop ? normalizedHeight * delayPerHeight : 0f,
                RandomDelay = randomDelay > 0f ? UnityEngine.Random.Range(0f, randomDelay) : 0f
            };
        }

        private void ApplyBlockTransform(BlockState block, Vector3 position)
        {
            block.Transform.position = position;
            SetWorldScale(block.Transform, block.Scale);
        }

        private Vector3 GetBlockScale(GameObject prefab)
        {
            var prefabScale = prefab != null ? prefab.transform.localScale : Vector3.one;
            return Vector3.Scale(prefabScale, cellSize) * blockScaleMultiplier;
        }

        private bool ShouldCreateBlock(Vector3 center, Vector3 size)
        {
            if (sourceCollider == null)
                return true;

            var cellBounds = new Bounds(center, size);

            if (!sourceCollider.bounds.Intersects(cellBounds))
                return false;

            var closestToCenter = sourceCollider.ClosestPoint(center);

            if (cellBounds.Contains(closestToCenter) || (closestToCenter - center).sqrMagnitude < 0.0001f)
                return true;

            var halfSize = size * 0.49f;

            for (int i = 0; i < CellSampleOffsets.Length; i++)
            {
                var sample = center + Vector3.Scale(CellSampleOffsets[i], halfSize);
                var closest = sourceCollider.ClosestPoint(sample);

                if ((closest - sample).sqrMagnitude < 0.0001f || cellBounds.Contains(closest))
                    return true;
            }

            return false;
        }

        private GameObject CreateBlockInstance(Transform blocksParent, GameObject prefab)
        {
            if (prefab == null)
                prefab = blockPrefab;

            GameObject block;

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                block = PrefabUtility.InstantiatePrefab(prefab, blocksParent) as GameObject;

                if (block == null)
                    block = Instantiate(prefab, blocksParent);

                Undo.RegisterCreatedObjectUndo(block, "Create Building Preview Block");
            }
            else
#endif
            {
                block = Instantiate(prefab, blocksParent);
            }

            block.name = $"{prefab.name}_ConstructionBlock";
            block.transform.rotation = prefab.transform.rotation;
            return block;
        }

        private void ApplyBlockMaterial(GameObject block, Material material)
        {
            if (blockMaterialMode == BlockMaterialMode.PrefabMaterial)
                return;

            if (material == null)
                return;

            var blockRenderers = block.GetComponentsInChildren<Renderer>();

            if (blockRenderers == null || blockRenderers.Length == 0)
                return;

            for (int i = 0; i < blockRenderers.Length; i++)
                blockRenderers[i].sharedMaterial = material;
        }

        private GameObject ResolveBlockPrefab(Material sourceMaterial)
        {
            if (sourceMaterial == null || materialPrefabOverrides == null)
                return blockPrefab;

            for (int i = 0; i < materialPrefabOverrides.Length; i++)
            {
                var item = materialPrefabOverrides[i];

                if (item == null || item.SourceMaterial == null || item.BlockPrefab == null)
                    continue;

                if (item.SourceMaterial == sourceMaterial)
                    return item.BlockPrefab;
            }

            return blockPrefab;
        }

        private Material ResolveBlockMaterial(Vector3 samplePosition)
        {
            if (blockMaterialMode == BlockMaterialMode.PrefabMaterial)
                return null;

            var material = ResolveSourceMaterial(samplePosition);

            if (material == null)
                return null;

            return material;
        }

        private Material ResolveSourceMaterial(Vector3 samplePosition)
        {
            if (sourceRenderer == null)
                return null;

            var sourceMaterials = sourceRenderer.sharedMaterials;

            if (sourceMaterials == null || sourceMaterials.Length == 0)
                return null;

            if (blockMaterialMode == BlockMaterialMode.SourceFirstMaterial)
                return sourceMaterials[0];

            if (blockMaterialMode != BlockMaterialMode.SampleSourceSubMesh)
                return null;

            if (TryGetSourceMaterialIndex(samplePosition, out var materialIndex) &&
                materialIndex >= 0 &&
                materialIndex < sourceMaterials.Length)
            {
                return sourceMaterials[materialIndex];
            }

            return sourceMaterials[0];
        }

        private bool TryGetSourceMaterialIndex(Vector3 samplePosition, out int materialIndex)
        {
            materialIndex = 0;

            if (sourceCollider is not MeshCollider meshCollider || meshCollider.sharedMesh == null)
                return false;

            var rayDistance = Mathf.Max(sourceRenderer.bounds.size.magnitude, cellSize.magnitude) + 1f;
            var bestDistance = float.PositiveInfinity;
            var bestTriangleIndex = -1;

            for (int i = 0; i < MaterialSampleOffsets.Length; i++)
            {
                var ray = new Ray(samplePosition + MaterialSampleOffsets[i] * rayDistance, MaterialSampleDirections[i]);

                if (!meshCollider.Raycast(ray, out var hit, rayDistance * 2f))
                    continue;

                if (hit.distance >= bestDistance)
                    continue;

                bestDistance = hit.distance;
                bestTriangleIndex = hit.triangleIndex;
            }

            if (bestTriangleIndex < 0)
                return false;

            materialIndex = GetSubMeshIndexForTriangle(meshCollider.sharedMesh, bestTriangleIndex);
            return true;
        }

        private static int GetSubMeshIndexForTriangle(Mesh mesh, int triangleIndex)
        {
            if (mesh == null || triangleIndex < 0)
                return 0;

            var triangleStart = triangleIndex * 3;
            var accumulatedIndices = 0;

            for (int subMeshIndex = 0; subMeshIndex < mesh.subMeshCount; subMeshIndex++)
            {
                var indexCount = (int)mesh.GetIndexCount(subMeshIndex);

                if (triangleStart >= accumulatedIndices && triangleStart < accumulatedIndices + indexCount)
                    return subMeshIndex;

                accumulatedIndices += indexCount;
            }

            return 0;
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

            if (blockGeometryMode == BlockGeometryMode.PrefabGridBlocks && blockPrefab == null)
            {
                Debug.LogError($"{nameof(BuildingConstructionEffect)}: Block Prefab is not assigned. It is required only for PrefabGridBlocks mode.", this);
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

        private static Vector3Int CalculateGridCount(Bounds bounds, Vector3 size)
        {
            return new Vector3Int(
                Mathf.Max(1, Mathf.CeilToInt(bounds.size.x / size.x)),
                Mathf.Max(1, Mathf.CeilToInt(bounds.size.y / size.y)),
                Mathf.Max(1, Mathf.CeilToInt(bounds.size.z / size.z)));
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

            if (sourceCollider == null)
                sourceCollider = GetComponentInChildren<Collider>();
        }

        private void OnDisable()
        {
            StopEditorPreview();
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

            Gizmos.color = new Color(0.2f, 0.75f, 1f, 0.35f);
            Gizmos.DrawCube(bounds.center, bounds.size);
            Gizmos.color = new Color(0.05f, 0.45f, 1f, 0.9f);
            Gizmos.DrawWireCube(bounds.center, bounds.size);

            DrawGridGizmos(bounds);
        }

        private void DrawGridGizmos(Bounds bounds)
        {
            var safeCellSize = new Vector3(
                Mathf.Max(MinimumCellSize, cellSize.x),
                Mathf.Max(MinimumCellSize, cellSize.y),
                Mathf.Max(MinimumCellSize, cellSize.z));
            var count = CalculateGridCount(bounds, safeCellSize);
            var maxLinesPerAxis = 24;
            var stepX = Mathf.Max(1, Mathf.CeilToInt(count.x / (float)maxLinesPerAxis));
            var stepY = Mathf.Max(1, Mathf.CeilToInt(count.y / (float)maxLinesPerAxis));
            var stepZ = Mathf.Max(1, Mathf.CeilToInt(count.z / (float)maxLinesPerAxis));

            Gizmos.color = new Color(1f, 0.82f, 0.2f, 0.55f);

            for (int x = 0; x <= count.x; x += stepX)
            {
                var px = bounds.min.x + x * safeCellSize.x;
                Gizmos.DrawLine(new Vector3(px, bounds.min.y, bounds.min.z), new Vector3(px, bounds.max.y, bounds.min.z));
                Gizmos.DrawLine(new Vector3(px, bounds.min.y, bounds.max.z), new Vector3(px, bounds.max.y, bounds.max.z));
            }

            for (int y = 0; y <= count.y; y += stepY)
            {
                var py = bounds.min.y + y * safeCellSize.y;
                Gizmos.DrawLine(new Vector3(bounds.min.x, py, bounds.min.z), new Vector3(bounds.max.x, py, bounds.min.z));
                Gizmos.DrawLine(new Vector3(bounds.min.x, py, bounds.max.z), new Vector3(bounds.max.x, py, bounds.max.z));
            }

            for (int z = 0; z <= count.z; z += stepZ)
            {
                var pz = bounds.min.z + z * safeCellSize.z;
                Gizmos.DrawLine(new Vector3(bounds.min.x, bounds.min.y, pz), new Vector3(bounds.max.x, bounds.min.y, pz));
                Gizmos.DrawLine(new Vector3(bounds.min.x, bounds.max.y, pz), new Vector3(bounds.max.x, bounds.max.y, pz));
            }
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

#if UNITY_EDITOR
        private void UpdateEditorPreview()
        {
            if (!editorPreviewPlaying)
            {
                EditorApplication.update -= UpdateEditorPreview;
                return;
            }

            var now = EditorApplication.timeSinceStartup;
            var elapsed = (float)(now - editorPreviewStartedAt);

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

        [Serializable]
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

        public enum BlockMaterialMode
        {
            PrefabMaterial,
            SourceFirstMaterial,
            SampleSourceSubMesh
        }

        public enum BlockGeometryMode
        {
            SourceMeshChunks,
            PrefabGridBlocks
        }

        [Serializable]
        public class MaterialBlockPrefabOverride
        {
            public Material SourceMaterial = null;
            public GameObject BlockPrefab = null;
        }
    }
}
