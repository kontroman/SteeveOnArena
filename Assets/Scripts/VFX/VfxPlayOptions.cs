using UnityEngine;

namespace MineArena.VFX
{
    public readonly struct VfxPlayOptions
    {
        public readonly Transform Owner;
        public readonly Vector3? Position;
        public readonly Quaternion? Rotation;
        public readonly Vector3 LocalOffset;
        public readonly bool HasLocalOffset;
        public readonly VfxParentMode? ParentModeOverride;
        public readonly Color? ColorOverride;

        private VfxPlayOptions(Transform owner, Vector3? position, Quaternion? rotation, Vector3 localOffset, bool hasLocalOffset, VfxParentMode? parentModeOverride, Color? colorOverride)
        {
            Owner = owner;
            Position = position;
            Rotation = rotation;
            LocalOffset = localOffset;
            HasLocalOffset = hasLocalOffset;
            ParentModeOverride = parentModeOverride;
            ColorOverride = colorOverride;
        }

        public static VfxPlayOptions At(Vector3 position, Quaternion? rotation = null)
        {
            return new VfxPlayOptions(null, position, rotation, Vector3.zero, false, null, null);
        }

        public static VfxPlayOptions On(Transform owner, Vector3 localOffset = default, VfxParentMode? parentModeOverride = null)
        {
            return new VfxPlayOptions(owner, null, null, localOffset, true, parentModeOverride, null);
        }

        public VfxPlayOptions WithColor(Color color)
        {
            return new VfxPlayOptions(Owner, Position, Rotation, LocalOffset, HasLocalOffset, ParentModeOverride, color);
        }
    }
}
