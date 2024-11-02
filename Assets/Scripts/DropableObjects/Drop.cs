using UnityEngine;

namespace Devotion.Drop
{
    public class Drop : MonoBehaviour
    {
        [SerializeField] private Items.Item _item;
        [SerializeField] private int _lowerBound;
        [SerializeField] private int _upperBound;

        public Items.Item Item => _item;
        public int LowerBound => _lowerBound;
        public int UpperBound => _upperBound;
    }
}
