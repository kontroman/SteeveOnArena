using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
[AddComponentMenu("MineArena/Rendering/Object Outline")]
public sealed class ObjectOutline : MonoBehaviour
{
    [SerializeField] private Material _lineMaterial;
    [SerializeField] private Color _outlineColor = Color.white;
    [SerializeField, Min(0f)] private float _lineWidth = 0.025f;
    [SerializeField, Range(0.02f, 0.5f)] private float _cornerLength = 0.28f;
    [SerializeField, Min(0f)] private float _boundsPadding = 0.03f;
    [SerializeField] private bool _includeChildren = true;
    [SerializeField] private Renderer[] _renderers;

    private const string OutlineRootName = "CornerOutline";
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    private readonly List<GameObject> _segments = new List<GameObject>();
    private Material _runtimeLineMaterial;
    private Transform _outlineRoot;

    private void Reset()
    {
        CacheRenderers();
    }

    private void Awake()
    {
        if (_renderers == null || _renderers.Length == 0)
            CacheRenderers();
    }

    private void OnEnable()
    {
        BuildOutline();
    }

    private void OnDisable()
    {
        ClearOutline();
    }

    private void OnDestroy()
    {
        ClearOutline();
    }

    private void OnValidate()
    {
        if (!Application.isPlaying || !enabled)
            return;

        BuildOutline();
    }

    public void SetOutlineEnabled(bool isEnabled)
    {
        enabled = isEnabled;
    }

    [ContextMenu("Rebuild Outline")]
    public void RebuildOutline()
    {
        if (!enabled)
            return;

        BuildOutline();
    }

    private void CacheRenderers()
    {
        _renderers = _includeChildren
            ? GetComponentsInChildren<Renderer>(includeInactive: true)
            : GetComponents<Renderer>();
    }

    private void BuildOutline()
    {
        ClearOutline();

        if (_renderers == null || _renderers.Length == 0)
            CacheRenderers();

        if (!TryCalculateLocalBounds(out Bounds localBounds))
            return;

        if (_runtimeLineMaterial == null && !CreateRuntimeMaterial())
            return;

        var root = new GameObject(OutlineRootName);
        root.hideFlags = HideFlags.HideAndDontSave;
        root.transform.SetParent(transform, false);
        _outlineRoot = root.transform;

        Vector3 min = localBounds.min - Vector3.one * _boundsPadding;
        Vector3 max = localBounds.max + Vector3.one * _boundsPadding;
        Vector3 size = max - min;

        float lengthX = Mathf.Min(size.x * _cornerLength, size.x * 0.5f);
        float lengthY = Mathf.Min(size.y * _cornerLength, size.y * 0.5f);
        float lengthZ = Mathf.Min(size.z * _cornerLength, size.z * 0.5f);

        for (int x = 0; x < 2; x++)
        {
            for (int y = 0; y < 2; y++)
            {
                for (int z = 0; z < 2; z++)
                {
                    Vector3 corner = new Vector3(
                        x == 0 ? min.x : max.x,
                        y == 0 ? min.y : max.y,
                        z == 0 ? min.z : max.z);

                    AddSegment(corner, corner + new Vector3(x == 0 ? lengthX : -lengthX, 0f, 0f));
                    AddSegment(corner, corner + new Vector3(0f, y == 0 ? lengthY : -lengthY, 0f));
                    AddSegment(corner, corner + new Vector3(0f, 0f, z == 0 ? lengthZ : -lengthZ));
                }
            }
        }
    }

    private bool TryCalculateLocalBounds(out Bounds localBounds)
    {
        localBounds = default;
        bool hasBounds = false;

        foreach (var targetRenderer in _renderers)
        {
            if (targetRenderer == null
                || targetRenderer is ParticleSystemRenderer
                || targetRenderer is LineRenderer
                || targetRenderer.transform == _outlineRoot
                || (_outlineRoot != null && targetRenderer.transform.IsChildOf(_outlineRoot)))
            {
                continue;
            }

            Bounds rendererBounds = targetRenderer.bounds;
            Vector3 min = rendererBounds.min;
            Vector3 max = rendererBounds.max;

            EncapsulateWorldPoint(transform.InverseTransformPoint(new Vector3(min.x, min.y, min.z)), ref localBounds, ref hasBounds);
            EncapsulateWorldPoint(transform.InverseTransformPoint(new Vector3(min.x, min.y, max.z)), ref localBounds, ref hasBounds);
            EncapsulateWorldPoint(transform.InverseTransformPoint(new Vector3(min.x, max.y, min.z)), ref localBounds, ref hasBounds);
            EncapsulateWorldPoint(transform.InverseTransformPoint(new Vector3(min.x, max.y, max.z)), ref localBounds, ref hasBounds);
            EncapsulateWorldPoint(transform.InverseTransformPoint(new Vector3(max.x, min.y, min.z)), ref localBounds, ref hasBounds);
            EncapsulateWorldPoint(transform.InverseTransformPoint(new Vector3(max.x, min.y, max.z)), ref localBounds, ref hasBounds);
            EncapsulateWorldPoint(transform.InverseTransformPoint(new Vector3(max.x, max.y, min.z)), ref localBounds, ref hasBounds);
            EncapsulateWorldPoint(transform.InverseTransformPoint(new Vector3(max.x, max.y, max.z)), ref localBounds, ref hasBounds);
        }

        return hasBounds;
    }

    private static void EncapsulateWorldPoint(Vector3 point, ref Bounds bounds, ref bool hasBounds)
    {
        if (!hasBounds)
        {
            bounds = new Bounds(point, Vector3.zero);
            hasBounds = true;
            return;
        }

        bounds.Encapsulate(point);
    }

    private void AddSegment(Vector3 start, Vector3 end)
    {
        var segmentObject = new GameObject("CornerSegment");
        segmentObject.hideFlags = HideFlags.HideAndDontSave;
        segmentObject.transform.SetParent(_outlineRoot, false);

        var lineRenderer = segmentObject.AddComponent<LineRenderer>();
        lineRenderer.useWorldSpace = false;
        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, start);
        lineRenderer.SetPosition(1, end);
        lineRenderer.startWidth = _lineWidth;
        lineRenderer.endWidth = _lineWidth;
        lineRenderer.startColor = _outlineColor;
        lineRenderer.endColor = _outlineColor;
        lineRenderer.alignment = LineAlignment.View;
        lineRenderer.textureMode = LineTextureMode.Stretch;
        lineRenderer.numCapVertices = 0;
        lineRenderer.numCornerVertices = 0;
        lineRenderer.shadowCastingMode = ShadowCastingMode.Off;
        lineRenderer.receiveShadows = false;
        lineRenderer.material = _runtimeLineMaterial;

        _segments.Add(segmentObject);
    }

    private bool CreateRuntimeMaterial()
    {
        if (_lineMaterial != null)
        {
            _runtimeLineMaterial = new Material(_lineMaterial);
        }
        else
        {
            Shader lineShader = Shader.Find("Sprites/Default")
                                ?? Shader.Find("Universal Render Pipeline/Unlit")
                                ?? Shader.Find("Unlit/Color");

            if (lineShader == null)
            {
                Debug.LogError("No compatible line shader was found for object outline.", this);
                return false;
            }

            _runtimeLineMaterial = new Material(lineShader);
        }

        _runtimeLineMaterial.name = $"{name}_CornerOutline";
        _runtimeLineMaterial.hideFlags = HideFlags.HideAndDontSave;

        if (_runtimeLineMaterial.HasProperty(ColorId))
            _runtimeLineMaterial.SetColor(ColorId, _outlineColor);

        if (_runtimeLineMaterial.HasProperty(BaseColorId))
            _runtimeLineMaterial.SetColor(BaseColorId, _outlineColor);

        return true;
    }

    private void ClearOutline()
    {
        foreach (var segment in _segments)
        {
            if (segment == null)
                continue;

            DestroyUnityObject(segment);
        }

        _segments.Clear();

        if (_outlineRoot != null)
        {
            DestroyUnityObject(_outlineRoot.gameObject);
            _outlineRoot = null;
        }

        if (_runtimeLineMaterial != null)
        {
            DestroyUnityObject(_runtimeLineMaterial);
            _runtimeLineMaterial = null;
        }
    }

    private static void DestroyUnityObject(Object target)
    {
        if (Application.isPlaying)
            Destroy(target);
        else
            DestroyImmediate(target);
    }
}
