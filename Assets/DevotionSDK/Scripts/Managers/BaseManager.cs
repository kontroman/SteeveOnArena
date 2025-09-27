using Devotion.SDK.Interfaces;
using UnityEngine;

namespace Devotion.SDK.Managers
{
    public class BaseManager : MonoBehaviour, IManager
    {
        public virtual void InitManager()
        {
            return;
        }
    }
}
