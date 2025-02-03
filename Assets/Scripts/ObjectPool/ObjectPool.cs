using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace Devotion.ObjectPools
{
    public class ObjectPool : MonoBehaviour
    {
        public GameObject[] objects;

        [SerializeField] private GameObject _objectPrefab;
        private ObjectPool<GameObject> _pool;

        private void Start()
        {
            var pool = new ObjectPool(_objectPrefab, 10, 20);

            pool.GetObj(pool.CreateObject());
        }

        public ObjectPool(GameObject prefab, int defaultSize, int maxSize)
        {
            _objectPrefab = prefab;
            _pool = new ObjectPool<GameObject>(createFunc: CreateObject,
                                               actionOnGet: GetObj,
                                               actionOnRelease: Release,
                                               actionOnDestroy: Remove,
                                               false,
                                               defaultCapacity: defaultSize,
                                               maxSize: maxSize
            );
        }

        private GameObject CreateObject()
        {
            var obj = Instantiate(_objectPrefab);
            obj.SetActive(false);
            return obj;
        }

        public void GetObj(GameObject obj)
        {
            obj?.SetActive(true);
        }

        public void Release(GameObject obj)
        {
            obj?.SetActive(false);
        }

        public void Remove(GameObject obj)
        {
            Destroy(gameObject);
        }

        public void ClearPool()
        {
            _pool.Clear();
        }
    }
}
