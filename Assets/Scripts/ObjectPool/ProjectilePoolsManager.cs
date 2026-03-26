using UnityEngine;

namespace MineArena.ObjectPools
{
    public class ProjectilePoolsManager : MonoBehaviour
    {
        [SerializeField] ObjectPoolPreset _poolPreset;
        public void Awake()
        {
            ObjectPoolsManager.Instance.InitPool<Projectile>(_poolPreset);
        }
    }
}
