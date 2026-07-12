using UnityEngine;

namespace MineArena.VFX
{
    public interface IVFXService
    {
        VfxHandle Play(VfxId id, Vector3 position);
        VfxHandle Play(VfxId id, Vector3 position, Quaternion rotation);
        VfxHandle Play(VfxId id, Transform parent);
        VfxHandle Play(VfxId id, Transform parent, Vector3 localOffset);
        VfxHandle Play(VfxId id, VfxPlayOptions options);
        void Stop(VfxHandle handle);
        void StopAllForOwner(Transform owner);
        void Preload(VfxId id, int count);
        void Warmup();
    }
}
