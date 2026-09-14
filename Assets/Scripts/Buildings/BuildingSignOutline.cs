using UnityEngine;
using UnityEngine.Rendering;

namespace MineArena.Buildings
{
    // Attached to the sign so its visibility and lifetime follow construction state.
    public sealed class BuildingSignOutline : MonoBehaviour
    {
        public const float Radius = 2.5f;
        private Mesh mesh;
        private Transform outlineTransform;

        public static Vector3 GetGroundCenter(GameObject sign)
        {
            var center = sign.transform.position;
            var renderers = sign.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                var bounds = renderers[0].bounds;
                foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
                center = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            }
            return center;
        }

        private void Start()
        {
            var material = Resources.Load<Material>("BuildingSignOutline");
            if (material == null) return;

            var center = GetGroundCenter(gameObject) + Vector3.up * 0.035f;

            var outline = new GameObject("Animated build boundary");
            outline.layer = gameObject.layer;
            outline.transform.SetParent(transform, false);
            outline.transform.position = center;
            outlineTransform = outline.transform;
            var vertices = new Vector3[16];
            var uv = new Vector2[16];
            var triangles = new int[24];
            // Extend the mesh on both sides of the 0.18-unit core for a soft halo.
            const float outerRadius = Radius + 0.28f;
            const float innerRadius = Radius - 0.18f - 0.28f;
            var corners = new[] { new Vector3(-outerRadius, 0, -outerRadius), new Vector3(-outerRadius, 0, outerRadius),
                new Vector3(outerRadius, 0, outerRadius), new Vector3(outerRadius, 0, -outerRadius) };
            for (var side = 0; side < 4; side++)
            {
                var a = corners[side];
                var b = corners[(side + 1) % 4];
                var v = side * 4;
                vertices[v] = outline.transform.InverseTransformPoint(center + a);
                vertices[v + 1] = outline.transform.InverseTransformPoint(center + b);
                vertices[v + 2] = outline.transform.InverseTransformPoint(center + b * (innerRadius / outerRadius));
                vertices[v + 3] = outline.transform.InverseTransformPoint(center + a * (innerRadius / outerRadius));
                uv[v] = new Vector2(side, 0);
                uv[v + 1] = new Vector2(side + 1, 0);
                uv[v + 2] = new Vector2(side + 1, 1);
                uv[v + 3] = new Vector2(side, 1);
                var t = side * 6;
                triangles[t] = v; triangles[t + 1] = v + 1; triangles[t + 2] = v + 2;
                triangles[t + 3] = v; triangles[t + 4] = v + 2; triangles[t + 5] = v + 3;
            }
            mesh = new Mesh { name = "Build boundary 5m", vertices = vertices, uv = uv, triangles = triangles };
            mesh.RecalculateBounds();
            outline.AddComponent<MeshFilter>().sharedMesh = mesh;
            var meshRenderer = outline.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = material;
            meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
            meshRenderer.lightProbeUsage = LightProbeUsage.Off;
            meshRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        }

        private void Update()
        {
            if (outlineTransform == null) return;
            // Breathe around the outline's own center, without scaling the sign or trigger.
            float scale = 1f + 0.03f * Mathf.Sin(Time.time * 3f);
            outlineTransform.localScale = Vector3.one * scale;
        }

        private void OnDestroy()
        {
            if (mesh != null) Destroy(mesh);
        }
    }
}
