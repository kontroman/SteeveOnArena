using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DevotionSDK.Managers
{
    /// <summary>
    /// Drives a vertical reveal effect by advancing a clip plane through all child renderers.
    /// Works in play mode and edit mode (requires materials that expose _RevealHeight/_RevealFeather).
    /// </summary>
    [ExecuteAlways]
    public class ConstructionRevealController : MonoBehaviour
    {
        private const string RevealHeightProperty = "_RevealHeight";
        private const string RevealFeatherProperty = "_RevealFeather";
        [SerializeField]
        private Shader revealShader;

        [SerializeField]
        private bool autoAssignRevealShader = true;

        [SerializeField]
        private bool restoreOriginalMaterialsOnDisable = true;

        [SerializeField, Min(0.01f)]
        private float stepHeight = 0.5f;

        [SerializeField, Min(0.01f)]
        private float stepInterval = 0.2f;

        [SerializeField, Min(0f)]
        private float feather = 0.05f;

        [SerializeField]
        private bool playOnEnable = true;

        [SerializeField]
        private bool loop = false;

        [SerializeField]
        private bool useManualProgress = false;

        [SerializeField, Range(0f, 1f)]
        private float manualProgress = 0f;

        [SerializeField]
        private bool autoRefreshBounds = true;

        [SerializeField, HideInInspector]
        private List<RendererMaterialSnapshot> originalMaterialSnapshots = new List<RendererMaterialSnapshot>();

        private Renderer[] _renderers = System.Array.Empty<Renderer>();
        private MaterialPropertyBlock _propertyBlock;
        private float _minWorldY;
        private float _maxWorldY;
        private float _currentHeight;
        private bool _isPlaying;
        private double _nextStepTime;

        private readonly Dictionary<Renderer, RendererState> _rendererStates = new Dictionary<Renderer, RendererState>();

        private float SafeFeather => Mathf.Max(0.0001f, feather);
        private float RevealFloor => _minWorldY - SafeFeather;
        private float RevealCeiling => _maxWorldY + SafeFeather;

        [System.Serializable]
        private class RendererMaterialSnapshot
        {
            public Renderer Renderer;
            public Material[] Materials = System.Array.Empty<Material>();
        }

        private class RendererState
        {
            public Renderer Renderer;
            public RendererMaterialSnapshot Snapshot;
            public Material[] RevealMaterials = System.Array.Empty<Material>();
            public readonly List<Material> OwnedMaterials = new List<Material>();
            public bool UsingReveal;
        }

        private void OnEnable()
        {
            CacheRenderers();
            RecalculateBounds();

            EnsureRevealShaderReference();
            UpdateMaterialAssignments();

            _propertyBlock ??= new MaterialPropertyBlock();

            if (playOnEnable && !useManualProgress)
            {
                ResetProgress();
                _isPlaying = true;
                ScheduleNextStep();
            }
            else
            {
                _isPlaying = false;
                ApplyProgress(useManualProgress ? manualProgress : 1f);
            }

            ApplyProperties();
        }

        private void OnDisable()
        {
            if (restoreOriginalMaterialsOnDisable)
            {
                RestoreAllOriginalMaterials();
            }

            if (_renderers == null)
                return;

            foreach (var renderer in _renderers)
            {
                if (renderer == null)
                    continue;

                renderer.SetPropertyBlock(null);
            }
        }

        private void OnDestroy()
        {
            if (restoreOriginalMaterialsOnDisable)
            {
                RestoreAllOriginalMaterials();
            }
        }

        private void Update()
        {
            if (_renderers == null || _renderers.Length == 0)
                return;

            if (autoRefreshBounds && transform.hasChanged)
            {
                transform.hasChanged = false;
                RecalculateBounds();
            }

            if (useManualProgress)
            {
                _isPlaying = false;
                ApplyProgress(manualProgress);
                ApplyProperties();
                return;
            }

            if (!_isPlaying)
                return;

            if (GetTime() < _nextStepTime)
                return;

            var ceiling = RevealCeiling;
            var nextHeight = Mathf.Min(_currentHeight + stepHeight, ceiling);
            ApplyHeight(nextHeight);
            ApplyProperties();

            if (nextHeight >= ceiling - 0.0001f)
            {
                if (loop)
                {
                    ResetProgress();
                    ApplyProperties();
                }
                else
                {
                    _isPlaying = false;
                    return;
                }
            }

            ScheduleNextStep();
        }

        public void Play()
        {
            useManualProgress = false;
            _isPlaying = true;
            ResetProgress();
            ApplyProperties();
            ScheduleNextStep();
        }

        public void Stop()
        {
            _isPlaying = false;
        }

        public void ResetProgress()
        {
            ApplyHeight(RevealFloor);
        }

        public void ApplyProgress(float progress)
        {
            progress = Mathf.Clamp01(progress);
            var height = Mathf.Lerp(RevealFloor, RevealCeiling, progress);
            ApplyHeight(height);
        }

        private void ApplyHeight(float height)
        {
            _currentHeight = Mathf.Clamp(height, RevealFloor, RevealCeiling);
        }

        private void ApplyProperties()
        {
            if (_renderers == null)
                return;

            _propertyBlock ??= new MaterialPropertyBlock();

            foreach (var renderer in _renderers)
            {
                if (renderer == null)
                    continue;

                renderer.GetPropertyBlock(_propertyBlock);
                _propertyBlock.SetFloat(RevealHeightProperty, _currentHeight);
                _propertyBlock.SetFloat(RevealFeatherProperty, feather);
                renderer.SetPropertyBlock(_propertyBlock);
            }
        }

        private void EnsureRevealShaderReference()
        {
            if (revealShader == null)
            {
                var defaultShader = Shader.Find(GetDefaultRevealShaderName());
                if (defaultShader != null && defaultShader.isSupported)
                {
                    revealShader = defaultShader;
                }
            }

            if (revealShader != null && !revealShader.isSupported)
            {
                Debug.LogWarning($"Reveal shader '{revealShader.name}' is not supported on this platform. Reveal effect disabled.", this);
                revealShader = null;
            }
        }

        private void UpdateMaterialAssignments()
        {
            PruneRendererStates();

            if (_renderers == null || _renderers.Length == 0)
                return;

            foreach (var renderer in _renderers)
            {
                if (renderer == null)
                    continue;

                var state = GetOrCreateRendererState(renderer);

                if (!autoAssignRevealShader || revealShader == null)
                {
                    CaptureOriginalMaterials(state);
                    if (state.UsingReveal)
                    {
                        RestoreOriginalMaterials(state);
                    }
                    continue;
                }

                CaptureOriginalMaterials(state);

                var originals = state.Snapshot?.Materials;
                if (originals == null || originals.Length == 0)
                    continue;

                if (!state.UsingReveal ||
                    state.RevealMaterials == null ||
                    state.RevealMaterials.Length != originals.Length)
                {
                    AssignRevealMaterials(state);
                }
            }
        }

        private void RestoreAllOriginalMaterials()
        {
            foreach (var state in _rendererStates.Values)
            {
                RestoreOriginalMaterials(state);
            }
        }

        private void PruneRendererStates()
        {
            if (_rendererStates.Count == 0 && originalMaterialSnapshots.Count == 0)
                return;

            var validRenderers = new HashSet<Renderer>(_renderers ?? System.Array.Empty<Renderer>());
            var toRemove = System.Array.Empty<Renderer>();

            if (_rendererStates.Count > 0)
            {
                var removalList = new List<Renderer>();
                foreach (var pair in _rendererStates)
                {
                    var renderer = pair.Key;
                    if (renderer == null || !validRenderers.Contains(renderer))
                    {
                        RestoreOriginalMaterials(pair.Value);
                        removalList.Add(renderer);
                    }
                }

                if (removalList.Count > 0)
                {
                    toRemove = removalList.ToArray();
                }
            }

            if (toRemove.Length > 0)
            {
                foreach (var renderer in toRemove)
                {
                    _rendererStates.Remove(renderer);
                }
            }

            for (int i = originalMaterialSnapshots.Count - 1; i >= 0; i--)
            {
                var snapshot = originalMaterialSnapshots[i];
                if (snapshot == null || snapshot.Renderer == null || !validRenderers.Contains(snapshot.Renderer))
                {
                    originalMaterialSnapshots.RemoveAt(i);
                }
            }
        }

        private RendererState GetOrCreateRendererState(Renderer renderer)
        {
            if (!_rendererStates.TryGetValue(renderer, out var state))
            {
                state = new RendererState
                {
                    Renderer = renderer,
                    Snapshot = GetOrCreateSnapshot(renderer),
                    RevealMaterials = System.Array.Empty<Material>()
                };
                _rendererStates[renderer] = state;
            }
            else
            {
                state.Renderer = renderer;
                state.Snapshot = GetOrCreateSnapshot(renderer);
            }

            return state;
        }

        private RendererMaterialSnapshot GetOrCreateSnapshot(Renderer renderer)
        {
            if (renderer == null)
                return null;

            for (int i = 0; i < originalMaterialSnapshots.Count; i++)
            {
                var snapshot = originalMaterialSnapshots[i];
                if (snapshot != null && snapshot.Renderer == renderer)
                {
                    return snapshot;
                }
            }

            var newSnapshot = new RendererMaterialSnapshot
            {
                Renderer = renderer,
                Materials = CloneMaterials(renderer.sharedMaterials)
            };
            originalMaterialSnapshots.Add(newSnapshot);
            return newSnapshot;
        }

        private void CaptureOriginalMaterials(RendererState state)
        {
            if (state == null || state.Renderer == null || state.Snapshot == null)
                return;

            if (!state.UsingReveal)
            {
                state.Snapshot.Materials = CloneMaterials(state.Renderer.sharedMaterials);
            }
        }

        private void AssignRevealMaterials(RendererState state)
        {
            if (state?.Renderer == null || state.Snapshot == null)
                return;

            var originals = state.Snapshot.Materials;
            if (originals == null || originals.Length == 0)
                return;

            if (revealShader == null)
            {
                RestoreOriginalMaterials(state);
                return;
            }

            DestroyOwnedMaterials(state);

            var revealSet = new Material[originals.Length];
            for (int i = 0; i < originals.Length; i++)
            {
                revealSet[i] = CreateRevealMaterial(originals[i], state);
            }

            state.RevealMaterials = revealSet;
            state.Renderer.sharedMaterials = state.RevealMaterials;
            state.UsingReveal = true;
        }

        private void RestoreOriginalMaterials(RendererState state)
        {
            if (state == null)
                return;

            var snapshot = state.Snapshot;
            if (state.Renderer != null && snapshot != null && snapshot.Materials != null && snapshot.Materials.Length > 0)
            {
                state.Renderer.sharedMaterials = snapshot.Materials;
            }

            state.RevealMaterials = System.Array.Empty<Material>();
            state.UsingReveal = false;
            DestroyOwnedMaterials(state);
        }

        private void DestroyOwnedMaterials(RendererState state)
        {
            if (state == null || state.OwnedMaterials.Count == 0)
                return;

            foreach (var material in state.OwnedMaterials)
            {
                DestroyMaterial(material);
            }

            state.OwnedMaterials.Clear();
        }

        private Material CreateRevealMaterial(Material original, RendererState state)
        {
            if (original == null)
                return null;

            if (revealShader == null || original.HasProperty(RevealHeightProperty))
                return original;

            if (original.shader == revealShader)
                return original;

            var revealMaterial = new Material(original)
            {
                name = $"{original.name}_Reveal"
            };

            revealMaterial.shader = revealShader;
            CopyMaterialProperties(original, revealMaterial);
            revealMaterial.shaderKeywords = original.shaderKeywords;
            revealMaterial.renderQueue = original.renderQueue;
            revealMaterial.enableInstancing = original.enableInstancing;
            revealMaterial.doubleSidedGI = original.doubleSidedGI;
            revealMaterial.globalIlluminationFlags = original.globalIlluminationFlags;

            state.OwnedMaterials.Add(revealMaterial);
            return revealMaterial;
        }

        private static Material[] CloneMaterials(Material[] source)
        {
            if (source == null || source.Length == 0)
                return System.Array.Empty<Material>();

            var clone = new Material[source.Length];
            System.Array.Copy(source, clone, source.Length);
            return clone;
        }

        private static void DestroyMaterial(Material material)
        {
            if (material == null)
                return;

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                Object.DestroyImmediate(material);
            }
            else
            {
                Object.Destroy(material);
            }
#else
            Object.Destroy(material);
#endif
        }

        private static void CopyMaterialProperties(Material source, Material target)
        {
            if (source == null || target == null)
                return;

            var targetShader = target.shader;
            if (targetShader == null)
                return;

            int propertyCount = targetShader.GetPropertyCount();
            for (int i = 0; i < propertyCount; i++)
            {
                var propertyName = targetShader.GetPropertyName(i);
                if (!source.HasProperty(propertyName))
                    continue;

                switch (targetShader.GetPropertyType(i))
                {
                    case ShaderPropertyType.Color:
                        target.SetColor(propertyName, source.GetColor(propertyName));
                        break;
                    case ShaderPropertyType.Vector:
                        target.SetVector(propertyName, source.GetVector(propertyName));
                        break;
                    case ShaderPropertyType.Float:
                    case ShaderPropertyType.Range:
#if UNITY_2021_2_OR_NEWER
                    case ShaderPropertyType.Int:
#endif
                        target.SetFloat(propertyName, source.GetFloat(propertyName));
                        break;
                    case ShaderPropertyType.Texture:
                        CopyTextureProperty(source, target, targetShader, i, propertyName);
                        break;
                }
            }

            // Handle common property aliases so legacy shaders retain their data.
            ApplyPropertyAlias(source, target, "_Color", "_BaseColor");
            ApplyPropertyAlias(source, target, "_BaseColor", "_Color");
            ApplyPropertyAlias(source, target, "_Glossiness", "_Smoothness");
            ApplyPropertyAlias(source, target, "_Smoothness", "_Glossiness");
            ApplyTextureAlias(source, target, "_MainTex", "_BaseMap");
            ApplyTextureAlias(source, target, "_BaseMap", "_MainTex");
            ApplyTextureAlias(source, target, "_BumpMap", "_NormalMap");
            ApplyTextureAlias(source, target, "_NormalMap", "_BumpMap");
            ApplyTextureAlias(source, target, "_MetallicGlossMap", "_SpecGlossMap");
            ApplyTextureAlias(source, target, "_SpecGlossMap", "_MetallicGlossMap");
        }

        private static void CopyTextureProperty(Material source, Material target, Shader shader, int index, string propertyName)
        {
            var texture = source.GetTexture(propertyName);
            target.SetTexture(propertyName, texture);

            var dimension = shader.GetPropertyTextureDimension(index);
            if (dimension == TextureDimension.Tex2D)
            {
                target.SetTextureScale(propertyName, source.GetTextureScale(propertyName));
                target.SetTextureOffset(propertyName, source.GetTextureOffset(propertyName));
            }
        }

        private static void ApplyPropertyAlias(Material source, Material target, string sourceName, string targetName)
        {
            if (!target.HasProperty(targetName) || !source.HasProperty(sourceName) || source.HasProperty(targetName))
                return;

            if (!TryGetShaderProperty(target.shader, targetName, out _, out var propertyType))
                return;

            switch (propertyType)
            {
                case ShaderPropertyType.Color:
                    target.SetColor(targetName, source.GetColor(sourceName));
                    break;
                case ShaderPropertyType.Vector:
                    target.SetVector(targetName, source.GetVector(sourceName));
                    break;
                case ShaderPropertyType.Float:
                case ShaderPropertyType.Range:
#if UNITY_2021_2_OR_NEWER
                case ShaderPropertyType.Int:
#endif
                    target.SetFloat(targetName, source.GetFloat(sourceName));
                    break;
            }
        }

        private static void ApplyTextureAlias(Material source, Material target, string sourceName, string targetName)
        {
            if (!target.HasProperty(targetName) || !source.HasProperty(sourceName) || source.HasProperty(targetName))
                return;

            if (!TryGetShaderProperty(target.shader, targetName, out var propertyIndex, out var propertyType))
                return;

            if (propertyType != ShaderPropertyType.Texture)
                return;

            var texture = source.GetTexture(sourceName);
            target.SetTexture(targetName, texture);

            var dimension = target.shader.GetPropertyTextureDimension(propertyIndex);
            if (dimension == TextureDimension.Tex2D)
            {
                target.SetTextureScale(targetName, source.GetTextureScale(sourceName));
                target.SetTextureOffset(targetName, source.GetTextureOffset(sourceName));
            }
        }

        private static bool TryGetShaderProperty(Shader shader, string propertyName, out int index, out ShaderPropertyType propertyType)
        {
            index = -1;
            propertyType = default;

            if (shader == null)
                return false;

            int count = shader.GetPropertyCount();
            for (int i = 0; i < count; i++)
            {
                if (shader.GetPropertyName(i) == propertyName)
                {
                    index = i;
                    propertyType = shader.GetPropertyType(i);
                    return true;
                }
            }

            return false;
        }

        private static string GetDefaultRevealShaderName()
        {
            var renderPipeline = GraphicsSettings.currentRenderPipeline;
            if (renderPipeline != null)
            {
                var pipelineType = renderPipeline.GetType().FullName;
                if (!string.IsNullOrEmpty(pipelineType) &&
                    pipelineType.Contains("UniversalRenderPipeline"))
                {
                    return "Devotion/URP/ConstructionReveal";
                }
            }

            return "Devotion/ConstructionReveal";
        }

        private void CacheRenderers()
        {
            _renderers = GetComponentsInChildren<Renderer>(true);
        }

        private void RecalculateBounds()
        {
            if (_renderers == null || _renderers.Length == 0)
            {
                _minWorldY = _maxWorldY = transform.position.y;
                return;
            }

            var bounds = _renderers[0].bounds;
            for (int i = 1; i < _renderers.Length; i++)
            {
                var renderer = _renderers[i];
                if (renderer == null)
                    continue;

                bounds.Encapsulate(renderer.bounds);
            }

            _minWorldY = bounds.min.y;
            _maxWorldY = bounds.max.y;
            _currentHeight = Mathf.Clamp(_currentHeight, RevealFloor, RevealCeiling);
        }

        private void ScheduleNextStep()
        {
            _nextStepTime = GetTime() + stepInterval;
        }

        private static double GetTime()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                return EditorApplication.timeSinceStartup;
#endif
            return Time.realtimeSinceStartup;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            CacheRenderers();
            RecalculateBounds();
            EnsureRevealShaderReference();
            UpdateMaterialAssignments();

            if (useManualProgress)
            {
                ApplyProgress(manualProgress);
            }

            ApplyProperties();
        }
#endif
    }
}
