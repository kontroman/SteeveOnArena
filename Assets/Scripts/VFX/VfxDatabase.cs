using System.Collections.Generic;
using UnityEngine;

namespace MineArena.VFX
{
    [CreateAssetMenu(fileName = "VfxDatabase", menuName = "MineArena/VFX/VFX Database")]
    public class VfxDatabase : ScriptableObject
    {
        [SerializeField] private List<VfxEntry> effects = new List<VfxEntry>();

        private Dictionary<VfxId, VfxEntry> _effectsById;

        public IReadOnlyList<VfxEntry> Effects => effects;

        public void Initialize()
        {
            _effectsById = new Dictionary<VfxId, VfxEntry>();

            if (effects == null)
                return;

            foreach (var effect in effects)
            {
                if (effect == null || effect.Id == VfxId.None)
                    continue;

                if (effect.Prefab == null)
                {
                    Debug.LogWarning($"[VfxDatabase] VFX '{effect.Id}' has no prefab assigned.", this);
                    continue;
                }

                if (_effectsById.ContainsKey(effect.Id))
                {
                    Debug.LogWarning($"[VfxDatabase] Duplicate VFX id '{effect.Id}' ignored.", this);
                    continue;
                }

                _effectsById.Add(effect.Id, effect);
            }
        }

        public bool TryGet(VfxId id, out VfxEntry entry)
        {
            if (_effectsById == null)
                Initialize();

            return _effectsById.TryGetValue(id, out entry);
        }
    }
}
