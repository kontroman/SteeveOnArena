using System.Collections.Generic;
using Devotion.SDK.Managers;
using Devotion.SDK.Services;
using UnityEngine;
using UnityEngine.Pool;

namespace MineArena.VFX
{
    public class VFXManager : BaseManager, IVFXService
    {
        [SerializeField] private VfxDatabase database;
        [SerializeField] private Transform poolRoot;
        [SerializeField] private bool warmupOnInit = true;

        private readonly Dictionary<VfxId, ObjectPool<VfxInstance>> _pools = new Dictionary<VfxId, ObjectPool<VfxInstance>>();
        private readonly Dictionary<VfxId, VfxEntry> _entries = new Dictionary<VfxId, VfxEntry>();
        private readonly Dictionary<Transform, HashSet<VfxInstance>> _instancesByOwner = new Dictionary<Transform, HashSet<VfxInstance>>();
        private bool _initialized;
        private bool _initializing;

        public override void InitManager()
        {
            base.InitManager();
            ServiceLocator.Register<IVFXService>(this);
            EnsureInitialized();
        }

        private void Awake()
        {
            ServiceLocator.Register<IVFXService>(this);

            if (poolRoot == null)
            {
                var root = new GameObject("[VFX Pool]");
                root.transform.SetParent(transform, false);
                poolRoot = root.transform;
            }

            EnsureInitialized();
        }

        private void EnsureInitialized()
        {
            if (_initialized || _initializing)
                return;

            _initializing = true;
            _entries.Clear();
            _pools.Clear();

            if (database == null)
            {
                Debug.LogError("[VFXManager] VfxDatabase is not assigned.", this);
                _initializing = false;
                return;
            }

            database.Initialize();

            foreach (var entry in database.Effects)
            {
                if (entry == null || entry.Id == VfxId.None || entry.Prefab == null)
                    continue;

                _entries[entry.Id] = entry;

                if (entry.UsePooling)
                    _pools[entry.Id] = CreatePool(entry);
            }

            _initialized = true;
            _initializing = false;

            if (warmupOnInit)
                Warmup();
        }

        public VfxHandle Play(VfxId id, Vector3 position)
        {
            return Play(id, VfxPlayOptions.At(position));
        }

        public VfxHandle Play(VfxId id, Vector3 position, Quaternion rotation)
        {
            return Play(id, VfxPlayOptions.At(position, rotation));
        }

        public VfxHandle Play(VfxId id, Transform parent)
        {
            return Play(id, VfxPlayOptions.On(parent));
        }

        public VfxHandle Play(VfxId id, Transform parent, Vector3 localOffset)
        {
            return Play(id, VfxPlayOptions.On(parent, localOffset));
        }

        public VfxHandle Play(VfxId id, VfxPlayOptions options)
        {
            if (!TryGetEntry(id, out var entry))
                return null;

            var instance = GetInstance(entry);
            if (instance == null)
                return null;

            ConfigureTransform(instance.transform, entry, options);

            bool followOwner = options.Owner != null && GetParentMode(entry, options) == VfxParentMode.Follow;
            Vector3 localOffset = options.HasLocalOffset ? options.LocalOffset : entry.LocalOffset;

            instance.gameObject.SetActive(true);
            instance.Play(this, entry, options.Owner, followOwner, localOffset, options.ColorOverride);
            RegisterOwner(instance, options.Owner);

            return new VfxHandle(instance);
        }

        public void Stop(VfxHandle handle)
        {
            if (handle == null || handle.Instance == null)
                return;

            handle.Instance.Stop();
        }

        public void StopAllForOwner(Transform owner)
        {
            if (owner == null || !_instancesByOwner.TryGetValue(owner, out var instances))
                return;

            var buffer = ListPool<VfxInstance>.Get();
            buffer.AddRange(instances);

            foreach (var instance in buffer)
            {
                if (instance != null)
                    instance.Stop();
            }

            ListPool<VfxInstance>.Release(buffer);
        }

        public void Preload(VfxId id, int count)
        {
            if (count <= 0)
                return;

            if (!_entries.TryGetValue(id, out var entry) && !TryGetEntry(id, out entry))
                return;

            if (!entry.UsePooling)
                return;

            if (!_pools.TryGetValue(id, out var pool))
                _pools[id] = pool = CreatePool(entry);

            var buffer = ListPool<VfxInstance>.Get();

            for (int i = 0; i < count; i++)
                buffer.Add(pool.Get());

            foreach (var instance in buffer)
                pool.Release(instance);

            ListPool<VfxInstance>.Release(buffer);
        }

        public void Warmup()
        {
            foreach (var entry in _entries.Values)
            {
                if (entry != null && entry.UsePooling && entry.PreloadCount > 0)
                    Preload(entry.Id, entry.PreloadCount);
            }
        }

        internal void Release(VfxInstance instance)
        {
            if (instance == null || !instance.IsActive)
                return;

            UnregisterOwner(instance);

            var entry = instance.Entry;
            instance.MarkReleased();

            if (entry != null && entry.UsePooling && _pools.TryGetValue(entry.Id, out var pool))
            {
                pool.Release(instance);
                return;
            }

            Destroy(instance.gameObject);
        }

        private bool TryGetEntry(VfxId id, out VfxEntry entry)
        {
            EnsureInitialized();

            if (_entries.TryGetValue(id, out entry))
                return true;

            if (database != null && database.TryGet(id, out entry))
            {
                _entries[id] = entry;
                return true;
            }

            Debug.LogError($"[VFXManager] VFX id '{id}' is not registered.", this);
            return false;
        }

        private VfxInstance GetInstance(VfxEntry entry)
        {
            if (entry.UsePooling)
            {
                if (!_pools.TryGetValue(entry.Id, out var pool))
                    _pools[entry.Id] = pool = CreatePool(entry);

                return pool.Get();
            }

            return CreateInstance(entry);
        }

        private ObjectPool<VfxInstance> CreatePool(VfxEntry entry)
        {
            return new ObjectPool<VfxInstance>(
                () => CreateInstance(entry),
                instance => instance.gameObject.SetActive(true),
                instance =>
                {
                    instance.transform.SetParent(poolRoot, false);
                    instance.gameObject.SetActive(false);
                },
                instance =>
                {
                    if (instance != null)
                        Destroy(instance.gameObject);
                },
                false,
                Mathf.Max(1, entry.PreloadCount),
                Mathf.Max(1, entry.MaxPoolSize));
        }

        private VfxInstance CreateInstance(VfxEntry entry)
        {
            var instanceObject = Instantiate(entry.Prefab, poolRoot);
            instanceObject.name = $"{entry.Prefab.name} [{entry.Id}]";
            instanceObject.SetActive(false);

            var instance = instanceObject.GetComponent<VfxInstance>();
            if (instance == null)
                instance = instanceObject.AddComponent<VfxInstance>();

            instance.CacheComponents();
            return instance;
        }

        private void ConfigureTransform(Transform instanceTransform, VfxEntry entry, VfxPlayOptions options)
        {
            var parentMode = GetParentMode(entry, options);
            var localOffset = options.HasLocalOffset ? options.LocalOffset : entry.LocalOffset;

            if (options.Owner != null && parentMode == VfxParentMode.Parent)
            {
                instanceTransform.SetParent(options.Owner, false);
                instanceTransform.localPosition = localOffset;
                instanceTransform.localRotation = entry.RotationOffset;
            }
            else
            {
                instanceTransform.SetParent(poolRoot, true);

                Vector3 position = options.Position ?? (options.Owner != null ? options.Owner.TransformPoint(localOffset) : localOffset);
                Quaternion rotation = options.Rotation ?? (options.Owner != null ? options.Owner.rotation : Quaternion.identity);

                instanceTransform.SetPositionAndRotation(position, rotation * entry.RotationOffset);
            }

            instanceTransform.localScale = entry.Scale;
        }

        private VfxParentMode GetParentMode(VfxEntry entry, VfxPlayOptions options)
        {
            return options.ParentModeOverride ?? entry.DefaultParentMode;
        }

        private void RegisterOwner(VfxInstance instance, Transform owner)
        {
            if (owner == null || instance == null)
                return;

            if (!_instancesByOwner.TryGetValue(owner, out var instances))
            {
                instances = new HashSet<VfxInstance>();
                _instancesByOwner[owner] = instances;
            }

            instances.Add(instance);
        }

        private void UnregisterOwner(VfxInstance instance)
        {
            if (instance == null || instance.Owner == null)
                return;

            if (!_instancesByOwner.TryGetValue(instance.Owner, out var instances))
                return;

            instances.Remove(instance);

            if (instances.Count == 0)
                _instancesByOwner.Remove(instance.Owner);
        }
    }
}
