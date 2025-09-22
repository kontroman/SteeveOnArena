using UnityEngine;
using UnityEngine.Pool;

namespace MineArena.ObjectPools
{
    public class ObjectPool : MonoBehaviour
    {
        public GameObject[] objects;

        [SerializeField] private GameObject _objectPrefab;
        private ObjectPool<GameObject> _pool;

        public void Init(GameObject prefab, ObjectPoolPreset preset)
        {
            _objectPrefab = prefab;
            _pool = new ObjectPool<GameObject>(createFunc: () => OnCreateObject(prefab),
                                               actionOnGet: (mob) => OnGetObj(mob),
                                               actionOnRelease: (mob) => OnRelease(mob),
                                               actionOnDestroy: (mob) => OnRemove(mob),
                                               false,
                                               defaultCapacity: preset.DefaultPoolSize,
                                               maxSize: preset.MaxPoolSize
            );
        }
        
        public GameObject GetFromPool()
        {
            return _pool.Get();
        }

        public void Release(GameObject gameObject)
        {
            _pool.Release(gameObject);
        }

        private GameObject OnCreateObject(GameObject mob)
        {
            var obj = Instantiate(_objectPrefab);
            obj.SetActive(false);
            return obj;
        }

        private void OnGetObj(GameObject obj)
        {
            obj?.SetActive(true);
        }

        private void OnRelease(GameObject obj)
        {
            obj?.SetActive(false);
        }

        private void OnRemove(GameObject obj)
        {
            Destroy(gameObject);
        }

        public void ClearPool()
        {
            _pool.Clear();
        }
    }
}
