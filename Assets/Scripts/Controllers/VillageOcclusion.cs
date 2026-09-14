using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

namespace MineArena.Controllers
{
    [DisallowMultipleComponent]
    public sealed class VillageOcclusion : MonoBehaviour
    {
        [SerializeField] private Shader _shader;
        [FormerlySerializedAs("_woodMaterials")]
        [SerializeField] private Material[] _occludingMaterials;
        [SerializeField, Min(0.1f)] private float _radius = 1.4f;
        [SerializeField, Min(0.01f)] private float _feather = 0.45f;
        [SerializeField] private Vector3 _targetOffset = new Vector3(0, 1, 0);
        [SerializeField, Min(0)] private float _depthMargin = 0.4f;
        [SerializeField] private float _floorClearance = 0.15f;

        private Renderer _renderer;
        private Material[] _originalMaterials;
        private readonly List<Material> _instances = new List<Material>();
        private static readonly int TargetId = Shader.PropertyToID("_VillageTarget");
        private static readonly int CutoutId = Shader.PropertyToID("_VillageCutout");

        private void OnEnable()
        {
            _renderer = GetComponent<Renderer>();
            if (!_renderer || !_shader || _occludingMaterials == null) return;
            _originalMaterials = _renderer.sharedMaterials;
            var replacements = (Material[])_originalMaterials.Clone();
            var eligible = new HashSet<Material>(_occludingMaterials);
            var clones = new Dictionary<Material, Material>();
            for (int i = 0; i < replacements.Length; i++)
            {
                var source = replacements[i];
                if (!source || !eligible.Contains(source)) continue;
                if (!clones.TryGetValue(source, out var clone))
                {
                    clone = new Material(source) { shader = _shader, name = source.name + " (Village Occlusion)" };
                    clones.Add(source, clone);
                    _instances.Add(clone);
                }
                replacements[i] = clone;
            }
            _renderer.sharedMaterials = replacements;
            RenderPipelineManager.beginCameraRendering += BeforeCamera;
        }

        private void BeforeCamera(ScriptableRenderContext context, Camera camera)
        {
            var player = Player.Instance;
            bool active = player && player.gameObject.activeInHierarchy && camera == Camera.main
                && camera.cameraType == CameraType.Game;
            Vector4 target = active ? (Vector4)(player.transform.position + _targetOffset) : Vector4.zero;
            target.w = active ? 1 : 0;
            var cutout = new Vector4(_radius, Mathf.Min(_feather, _radius), _depthMargin,
                active ? player.transform.position.y + _floorClearance : 0);
            foreach (var material in _instances)
            {
                material.SetVector(TargetId, target);
                material.SetVector(CutoutId, cutout);
            }
        }

        private void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= BeforeCamera;
            if (_renderer && _originalMaterials != null) _renderer.sharedMaterials = _originalMaterials;
            foreach (var material in _instances) Destroy(material);
            _instances.Clear();
            _originalMaterials = null;
        }
    }
}
