using System.Collections.Generic;
using UnityEngine;
using Devotion.SDK.GenericSingleton;
using MineArena.AI;
using System;

namespace MineArena.ObjectPools
{
    public class ObjectPoolsManager : Singleton<ObjectPoolsManager>
    {
        [SerializeField] private Dictionary<Type, ObjectPool> _pools;
        [SerializeField] private ObjectPoolPreset _preset;

        public void InitPool<T>(ObjectPoolPreset preset) where T: Component
        {
            _preset = preset;
            var prefabList = _preset.Preset;
            
            _pools ??= new Dictionary<Type, ObjectPool>();

            foreach (var prefab in prefabList)
            {
                var component = prefab.GetComponent<T>();
                Type type = component.GetType();
                var newPool = gameObject.AddComponent<ObjectPool>();
                newPool.Init(prefab, preset);
                _pools[type] = newPool;
            }

            DebugPools();
        }

        private void DebugPools()
        {
            if (_pools == null || _pools.Count == 0)
            {
                Debug.Log("Пулы не инициализированы или пусты.");
                return;
            }

            foreach (var kvp in _pools)
            {
                Type type = kvp.Key;
                ObjectPool pool = kvp.Value;

                // Выводим тип и ссылку на пул
                Debug.Log($"Тип: {type.Name}, Пул: {pool}", pool);
            }
        }

        public GameObject Get<T1, T2>() where T1 : T2
        {
            Type type = typeof(T1);

            if (_pools.TryGetValue(type, out ObjectPool pool))
            {
                return pool.GetFromPool();
            }

            Debug.LogError($"Pool for {type} not found!");
            return null;
        }

        public void Release<T>(GameObject gameObject) where T : Component
        {
            var component = gameObject.GetComponent<T>();
            Type type = component.GetType();

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
