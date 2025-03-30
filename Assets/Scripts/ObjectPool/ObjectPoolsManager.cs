using System.Collections.Generic;
using UnityEngine;
using Devotion.SDK.GenericSingleton;
using Devotion.AI;
using System;

namespace Devotion.ObjectPools
{
    public class ObjectPoolsManager : Singleton<ObjectPoolsManager>
    {
        [SerializeField] private Dictionary<Type, ObjectPool> _pools;
        [SerializeField] private MobPoolsPreset _mobPreset;

        public void InitPools(MobPoolsPreset preset)
        {
            _mobPreset = preset;
            var mobList = _mobPreset.Preset;
            _pools = new Dictionary<Type, ObjectPool>();
            foreach (var prefab in mobList)
            {
                var mobComponent = prefab.GetComponent<Mob>();
                Type mobType = mobComponent.GetType();
                var newPool = gameObject.AddComponent<ObjectPool>();
                newPool.Init(prefab, preset);
                _pools[mobType] = newPool;
            }
        }

        public GameObject Get<T>() where T : Mob
        {
            Type type = typeof(T);

            if (_pools.TryGetValue(type, out ObjectPool pool))
            {
                return pool.GetFromPool();
            }

            Debug.LogError($"Pool for {type} not found!");
            return null;
        }

        public void Release(GameObject gameObject)
        {
            var mobComponent = gameObject.GetComponent<Mob>();
            Type type = mobComponent.GetType();

            if (_pools.TryGetValue(type, out ObjectPool pool))
            {
                pool.Release(gameObject);
            }
            else
            {
                Debug.LogError($"Pool for {type} not found!");
            }
        }

    }
}
