using System.Collections.Generic;
using UnityEngine;

namespace MineArena.VFX
{
    public static class VfxSurfaceColor
    {
        private static readonly Dictionary<Material, Color> MaterialColorCache = new Dictionary<Material, Color>();
        private const int SurfaceSampleTextureSize = 1;
        private const float SurfaceSampleCameraDistance = 0.08f;
        private const float SurfaceSampleCameraDepth = 0.16f;
        private const float SurfaceSampleOrthoSize = 0.04f;

        private static Camera _surfaceSampleCamera;
        private static RenderTexture _surfaceSampleRenderTexture;
        private static Texture2D _surfaceSampleReadback;

        public static bool TryGetColor(Collider collider, out Color color)
        {
            color = Color.white;

            if (collider == null)
                return false;

            var renderer = collider.GetComponentInParent<Renderer>();
            if (renderer == null || renderer.sharedMaterial == null)
                return false;

            color = GetMaterialColor(renderer.sharedMaterial);
            return true;
        }

        public static bool TryGetColor(RaycastHit hit, out Color color)
        {
            color = Color.white;

            if (TryGetRenderedSurfaceColor(hit, out color))
                return true;

            if (!TryGetRenderer(hit.collider, out var renderer))
                return false;

            Material material;
            Vector2 uv;
            bool hasUv;

            if (!TryGetHitMaterial(hit, renderer, out material))
                return false;

            hasUv = TryGetHitUv(hit, renderer.transform, out uv);

            Texture texture = GetMainTexture(material);
            if (texture != null && hasUv)
            {
                uv = ApplyTextureScaleOffset(material, uv);

                if (TryGetTexturePixelColor(texture, uv, out color))
                {
                    color = ApplyMaterialTint(material, color);
                    return true;
                }
            }

            color = GetShaderColor(material);
            color.a = 1f;
            return true;
        }

        private static bool TryGetRenderer(Collider collider, out Renderer renderer)
        {
            renderer = null;

            if (collider == null)
                return false;

            renderer = collider.GetComponent<Renderer>() ?? collider.GetComponentInParent<Renderer>();
            if (renderer == null || renderer.sharedMaterials == null || renderer.sharedMaterials.Length == 0)
                return false;

            return true;
        }

        private static Color GetMaterialColor(Material material)
        {
            if (material == null)
                return Color.white;

            if (MaterialColorCache.TryGetValue(material, out var cachedColor))
                return cachedColor;

            Color baseColor = GetShaderColor(material);
            baseColor.a = 1f;

            if (IsMeaningfulTint(baseColor))
            {
                MaterialColorCache[material] = baseColor;
                return baseColor;
            }

            Texture texture = GetMainTexture(material);
            Color textureColor = texture != null && TryGetAverageTextureColor(texture, out var average) ? average : Color.white;
            Color result = textureColor;
            result.a = 1f;

            MaterialColorCache[material] = result;
            return result;
        }

        private static bool TryGetHitMaterial(RaycastHit hit, Renderer renderer, out Material material)
        {
            material = null;

            if (renderer == null)
                return false;

            var materials = renderer.sharedMaterials;
            if (materials == null || materials.Length == 0)
                return false;

            if (hit.collider is BoxCollider && IsTopFace(hit, renderer.transform))
            {
                for (int i = 0; i < materials.Length; i++)
                {
                    var candidate = materials[i];
                    if (candidate != null && candidate.name.ToLowerInvariant().Contains("grass"))
                    {
                        material = candidate;
                        return true;
                    }
                }
            }

            if (materials.Length == 1)
            {
                material = materials[0];
                return material != null;
            }

            material = PickMostUsefulMaterial(materials);
            return material != null;
        }

        private static bool TryGetMeshSubmeshIndex(Mesh mesh, int triangleIndex, out int submeshIndex)
        {
            submeshIndex = -1;

            if (mesh == null || triangleIndex < 0)
                return false;

            int triangleOffset = 0;

            for (int i = 0; i < mesh.subMeshCount; i++)
            {
                int triangleCount = mesh.GetTriangles(i).Length / 3;

                if (triangleIndex >= triangleOffset && triangleIndex < triangleOffset + triangleCount)
                {
                    submeshIndex = i;
                    return true;
                }

                triangleOffset += triangleCount;
            }

            return false;
        }

        private static bool IsTopFace(RaycastHit hit, Transform rendererTransform)
        {
            if (rendererTransform == null)
                return Vector3.Dot(hit.normal, Vector3.up) > 0.5f;

            Vector3 localNormal = rendererTransform.InverseTransformDirection(hit.normal).normalized;
            return localNormal.y > 0.5f;
        }

        private static Material PickMostUsefulMaterial(Material[] materials)
        {
            Material best = null;
            float bestScore = float.MinValue;

            foreach (var material in materials)
            {
                if (material == null)
                    continue;

                Color color = GetShaderColor(material);
                float max = Mathf.Max(color.r, color.g, color.b);
                float min = Mathf.Min(color.r, color.g, color.b);
                float saturation = max <= 0.001f ? 0f : (max - min) / max;
                float score = saturation * 2f + max;

                if (material.name.ToLowerInvariant().Contains("grass"))
                    score += 3f;

                if (score > bestScore)
                {
                    best = material;
                    bestScore = score;
                }
            }

            return best;
        }

        private static Color GetShaderColor(Material material)
        {
            if (material.HasProperty("_BaseColor"))
                return material.GetColor("_BaseColor");

            if (material.HasProperty("_Color"))
                return material.GetColor("_Color");

            return Color.white;
        }

        private static Color ApplyMaterialTint(Material material, Color textureColor)
        {
            Color tint = GetShaderColor(material);

            if (IsNearlyWhite(tint))
            {
                textureColor.a = 1f;
                return textureColor;
            }

            Color result = new Color(textureColor.r * tint.r, textureColor.g * tint.g, textureColor.b * tint.b, 1f);
            return result;
        }

        private static bool IsNearlyWhite(Color color)
        {
            return color.r >= 0.98f && color.g >= 0.98f && color.b >= 0.98f;
        }

        private static bool IsMeaningfulTint(Color color)
        {
            float max = Mathf.Max(color.r, color.g, color.b);
            float min = Mathf.Min(color.r, color.g, color.b);
            float saturation = max <= 0.001f ? 0f : (max - min) / max;

            if (saturation >= 0.12f)
                return true;

            return max < 0.85f && min > 0.05f;
        }

        private static bool TryGetHitUv(RaycastHit hit, Transform rendererTransform, out Vector2 uv)
        {
            uv = hit.textureCoord;

            if (uv != Vector2.zero)
                return true;

            if (hit.collider is BoxCollider boxCollider)
                return TryGetBoxColliderUv(hit, boxCollider, rendererTransform, out uv);

            return false;
        }

        private static bool TryGetBoxColliderUv(RaycastHit hit, BoxCollider boxCollider, Transform rendererTransform, out Vector2 uv)
        {
            uv = Vector2.zero;

            if (boxCollider == null || rendererTransform == null)
                return false;

            Vector3 localPoint = rendererTransform.InverseTransformPoint(hit.point);
            Vector3 localNormal = rendererTransform.InverseTransformDirection(hit.normal).normalized;
            Vector3 center = boxCollider.center;
            Vector3 size = boxCollider.size;

            float x = Mathf.InverseLerp(center.x - size.x * 0.5f, center.x + size.x * 0.5f, localPoint.x);
            float y = Mathf.InverseLerp(center.y - size.y * 0.5f, center.y + size.y * 0.5f, localPoint.y);
            float z = Mathf.InverseLerp(center.z - size.z * 0.5f, center.z + size.z * 0.5f, localPoint.z);

            Vector3 absNormal = new Vector3(Mathf.Abs(localNormal.x), Mathf.Abs(localNormal.y), Mathf.Abs(localNormal.z));

            if (absNormal.y >= absNormal.x && absNormal.y >= absNormal.z)
                uv = new Vector2(x, z);
            else if (absNormal.x >= absNormal.z)
                uv = new Vector2(z, y);
            else
                uv = new Vector2(x, y);

            uv.x = Mathf.Repeat(uv.x, 1f);
            uv.y = Mathf.Repeat(uv.y, 1f);
            return true;
        }

        private static bool TryGetRenderedSurfaceColor(RaycastHit hit, out Color color)
        {
            color = Color.white;

            if (hit.collider == null)
                return false;

            EnsureSurfaceSampleCamera();

            if (_surfaceSampleCamera == null || _surfaceSampleRenderTexture == null || _surfaceSampleReadback == null)
                return false;

            Vector3 normal = hit.normal.sqrMagnitude > 0.0001f ? hit.normal.normalized : Vector3.up;
            Transform cameraTransform = _surfaceSampleCamera.transform;
            cameraTransform.SetPositionAndRotation(
                hit.point + normal * SurfaceSampleCameraDistance,
                Quaternion.LookRotation(-normal, GetCameraUp(normal)));

            _surfaceSampleCamera.cullingMask = 1 << hit.collider.gameObject.layer;
            _surfaceSampleCamera.nearClipPlane = 0.001f;
            _surfaceSampleCamera.farClipPlane = SurfaceSampleCameraDepth;
            _surfaceSampleCamera.orthographicSize = SurfaceSampleOrthoSize;

            RenderTexture previous = RenderTexture.active;

            try
            {
                _surfaceSampleCamera.Render();
                RenderTexture.active = _surfaceSampleRenderTexture;
                _surfaceSampleReadback.ReadPixels(new Rect(0, 0, SurfaceSampleTextureSize, SurfaceSampleTextureSize), 0, 0, false);
                _surfaceSampleReadback.Apply(false, false);

                color = _surfaceSampleReadback.GetPixel(0, 0);
                color.a = 1f;

                return color.maxColorComponent > 0.001f;
            }
            catch (UnityException)
            {
                return false;
            }
            finally
            {
                RenderTexture.active = previous;
            }
        }

        private static void EnsureSurfaceSampleCamera()
        {
            if (_surfaceSampleRenderTexture == null)
            {
                _surfaceSampleRenderTexture = new RenderTexture(SurfaceSampleTextureSize, SurfaceSampleTextureSize, 16, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default)
                {
                    name = "[VFX Surface Color Sample]",
                    antiAliasing = 1,
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp
                };
                _surfaceSampleRenderTexture.Create();
            }

            if (_surfaceSampleReadback == null)
            {
                _surfaceSampleReadback = new Texture2D(SurfaceSampleTextureSize, SurfaceSampleTextureSize, TextureFormat.RGBA32, false, false)
                {
                    name = "[VFX Surface Color Readback]",
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp
                };
            }

            if (_surfaceSampleCamera != null)
                return;

            var cameraObject = new GameObject("[VFX Surface Color Camera]")
            {
                hideFlags = HideFlags.HideAndDontSave
            };

            Object.DontDestroyOnLoad(cameraObject);
            _surfaceSampleCamera = cameraObject.AddComponent<Camera>();
            _surfaceSampleCamera.enabled = false;
            _surfaceSampleCamera.orthographic = true;
            _surfaceSampleCamera.aspect = 1f;
            _surfaceSampleCamera.clearFlags = CameraClearFlags.SolidColor;
            _surfaceSampleCamera.backgroundColor = Color.clear;
            _surfaceSampleCamera.allowHDR = false;
            _surfaceSampleCamera.allowMSAA = false;
            _surfaceSampleCamera.useOcclusionCulling = false;
            _surfaceSampleCamera.targetTexture = _surfaceSampleRenderTexture;
        }

        private static Vector3 GetCameraUp(Vector3 normal)
        {
            Vector3 up = Vector3.ProjectOnPlane(Vector3.up, normal);

            if (up.sqrMagnitude < 0.0001f)
                up = Vector3.ProjectOnPlane(Vector3.forward, normal);

            return up.sqrMagnitude < 0.0001f ? Vector3.right : up.normalized;
        }

        private static bool TryGetTexturePixelColor(Texture texture, Vector2 uv, out Color color)
        {
            color = Color.white;

            if (texture == null)
                return false;

            if (texture is Texture2D texture2D && TryGetReadableTexturePixelColor(texture2D, uv, out color))
                return true;

            return TryGetGpuTexturePixelColor(texture, uv, out color);
        }

        private static bool TryGetReadableTexturePixelColor(Texture2D texture, Vector2 uv, out Color color)
        {
            color = Color.white;

            try
            {
                color = texture.GetPixelBilinear(Mathf.Repeat(uv.x, 1f), Mathf.Repeat(uv.y, 1f));
                color.a = 1f;
                return true;
            }
            catch (UnityException)
            {
                return false;
            }
        }

        private static bool TryGetGpuTexturePixelColor(Texture texture, Vector2 uv, out Color color)
        {
            color = Color.white;

            RenderTexture previous = RenderTexture.active;
            RenderTexture renderTexture = null;
            Texture2D readableTexture = null;

            try
            {
                int width = Mathf.Max(1, texture.width);
                int height = Mathf.Max(1, texture.height);
                renderTexture = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
                Graphics.Blit(texture, renderTexture);

                int x = Mathf.Clamp(Mathf.RoundToInt(Mathf.Repeat(uv.x, 1f) * (width - 1)), 0, width - 1);
                int y = Mathf.Clamp(Mathf.RoundToInt(Mathf.Repeat(uv.y, 1f) * (height - 1)), 0, height - 1);

                RenderTexture.active = renderTexture;
                readableTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false, true);
                readableTexture.ReadPixels(new Rect(x, y, 1, 1), 0, 0, false);
                readableTexture.Apply(false, false);

                color = readableTexture.GetPixel(0, 0);
                color.a = 1f;
                return true;
            }
            catch (UnityException)
            {
                return false;
            }
            finally
            {
                RenderTexture.active = previous;

                if (renderTexture != null)
                    RenderTexture.ReleaseTemporary(renderTexture);

                if (readableTexture != null)
                    Object.Destroy(readableTexture);
            }
        }

        private static Texture GetMainTexture(Material material)
        {
            if (material.HasProperty("_BaseMap"))
                return material.GetTexture("_BaseMap");

            if (material.HasProperty("_MainTex"))
                return material.GetTexture("_MainTex");

            return null;
        }

        private static Vector2 ApplyTextureScaleOffset(Material material, Vector2 uv)
        {
            if (material == null)
                return uv;

            string property = material.HasProperty("_BaseMap") ? "_BaseMap" : material.HasProperty("_MainTex") ? "_MainTex" : null;

            if (string.IsNullOrEmpty(property))
                return uv;

            Vector2 scale = material.GetTextureScale(property);
            Vector2 offset = material.GetTextureOffset(property);
            return Vector2.Scale(uv, scale) + offset;
        }

        private static bool TryGetAverageTextureColor(Texture texture, out Color color)
        {
            color = Color.white;

            if (texture == null)
                return false;

            if (texture is Texture2D texture2D && TryGetReadableAverageTextureColor(texture2D, out color))
                return true;

            return TryGetGpuAverageTextureColor(texture, out color);
        }

        private static bool TryGetReadableAverageTextureColor(Texture2D texture, out Color color)
        {
            color = Color.white;

            try
            {
                int stepX = Mathf.Max(1, texture.width / 16);
                int stepY = Mathf.Max(1, texture.height / 16);
                Color sum = Color.black;
                int count = 0;

                for (int y = stepY / 2; y < texture.height; y += stepY)
                {
                    for (int x = stepX / 2; x < texture.width; x += stepX)
                    {
                        Color pixel = texture.GetPixel(x, y);
                        sum += pixel;
                        count++;
                    }
                }

                if (count == 0)
                    return false;

                color = sum / count;
                color.a = 1f;
                return true;
            }
            catch (UnityException)
            {
                return false;
            }
        }

        private static bool TryGetGpuAverageTextureColor(Texture texture, out Color color)
        {
            color = Color.white;

            RenderTexture previous = RenderTexture.active;
            RenderTexture renderTexture = null;
            Texture2D readableTexture = null;

            try
            {
                renderTexture = RenderTexture.GetTemporary(1, 1, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
                Graphics.Blit(texture, renderTexture);

                RenderTexture.active = renderTexture;
                readableTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false, true);
                readableTexture.ReadPixels(new Rect(0, 0, 1, 1), 0, 0, false);
                readableTexture.Apply(false, false);

                color = readableTexture.GetPixel(0, 0);
                color.a = 1f;
                return true;
            }
            catch (UnityException)
            {
                return false;
            }
            finally
            {
                RenderTexture.active = previous;

                if (renderTexture != null)
                    RenderTexture.ReleaseTemporary(renderTexture);

                if (readableTexture != null)
                    Object.Destroy(readableTexture);
            }
        }
    }
}
