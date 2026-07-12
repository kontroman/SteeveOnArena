using UnityEngine;

namespace MineArena.VFX
{
    [System.Serializable]
    public class VfxEntry
    {
        [SerializeField] private VfxId id;
        [SerializeField] private GameObject prefab;
        [SerializeField] private VfxParentMode defaultParentMode = VfxParentMode.World;
        [SerializeField] private Vector3 localOffset;
        [SerializeField] private Vector3 eulerOffset;
        [SerializeField] private Vector3 scale = Vector3.one;
        [SerializeField] private bool usePooling = true;
        [SerializeField] private bool autoRelease = true;
        [SerializeField, Min(0f)] private float fallbackLifetime = 3f;
        [SerializeField, Min(0)] private int preloadCount;
        [SerializeField, Min(1)] private int maxPoolSize = 32;

        public VfxId Id => id;
        public GameObject Prefab => prefab;
        public VfxParentMode DefaultParentMode => defaultParentMode;
        public Vector3 LocalOffset => localOffset;
        public Quaternion RotationOffset => Quaternion.Euler(eulerOffset);
        public Vector3 Scale => scale == Vector3.zero ? Vector3.one : scale;
        public bool UsePooling => usePooling;
        public bool AutoRelease => autoRelease;
        public float FallbackLifetime => fallbackLifetime;
        public int PreloadCount => preloadCount;
        public int MaxPoolSize => maxPoolSize;
    }
}
