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
            ObjectPoolsManager.Instance.Get<Pillager, Mob>();
        }
    }
}
