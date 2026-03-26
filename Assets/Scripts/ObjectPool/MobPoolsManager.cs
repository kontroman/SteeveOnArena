using MineArena.AI;
using UnityEngine;

namespace MineArena.ObjectPools
{
    public class MobPoolsManager : MonoBehaviour
    {
        [SerializeField] ObjectPoolPreset _poolPreset;
        public void Awake()
        {
            ObjectPoolsManager.Instance.InitPool<Mob>(_poolPreset);
        }
    }
}
