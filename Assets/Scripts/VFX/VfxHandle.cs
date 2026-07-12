namespace MineArena.VFX
{
    public sealed class VfxHandle
    {
        internal VfxInstance Instance { get; }

        public bool IsValid => Instance != null && Instance.IsActive;

        internal VfxHandle(VfxInstance instance)
        {
            Instance = instance;
        }
    }
}
