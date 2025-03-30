using Devotion.AI;
using System.Collections.Generic;
using UnityEngine;

namespace Devotion.ObjectPools
{
    public class MobPoolsPreset : MonoBehaviour
    {
        public int MaxPoolSize = 50;
        public int DefaultPoolSize = 10;
        public List<GameObject> Preset = new List<GameObject>();


        public void Start()
        {
            ObjectPoolsManager.Instance.InitPools(this);
            ObjectPoolsManager.Instance.Get<Zombie>();
            ObjectPoolsManager.Instance.Get<Skeleton>();
        }
    }
}
