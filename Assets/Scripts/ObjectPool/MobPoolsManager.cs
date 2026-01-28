using MineArena.AI;
using UnityEngine;

namespace MineArena.ObjectPools
{
    public class MobPoolsManager : MonoBehaviour
    {
        [SerializeField] ObjectPoolPreset _poolPreset; 
        public void Start()
        {
            ObjectPoolsManager.Instance.InitPool<Mob>(_poolPreset);
            ObjectPoolsManager.Instance.Get<Zombie, Mob>();
            ObjectPoolsManager.Instance.Get<Skeleton, Mob>();
            ObjectPoolsManager.Instance.Get<Creeper, Mob>();
            ObjectPoolsManager.Instance.Get<Wolf, Mob>();
            ObjectPoolsManager.Instance.Get<Enderman, Mob>();
            ObjectPoolsManager.Instance.Get<Bear, Mob>();
            ObjectPoolsManager.Instance.Get<Spider, Mob>();
            ObjectPoolsManager.Instance.Get<Pillager, Mob>();
            ObjectPoolsManager.Instance.Get<ArcherPillager, Mob>();
            ObjectPoolsManager.Instance.Get<Witch, Mob>();
        }
    }
}
