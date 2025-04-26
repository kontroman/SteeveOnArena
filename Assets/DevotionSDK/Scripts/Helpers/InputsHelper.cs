using UnityEngine;

namespace Devotion.SDK.Helpers
{
    public static class Inputs
    {
        private static KeyCode _lkmKey = KeyCode.Mouse0;
        private static KeyCode _rkmKey = KeyCode.Mouse1;

        public static bool LKMPressed => Input.GetKeyDown(_lkmKey);
        public static bool LKMReleased => Input.GetKeyUp(_lkmKey);
        public static bool LKMHolding => Input.GetKey(_lkmKey);
        public static bool RKMPressed => Input.GetKeyDown(_rkmKey);
        public static bool RKMReleased => Input.GetKeyUp(_rkmKey);
        public static bool RKMHolding => Input.GetKey(_rkmKey);

        public static void SetLKMKey(KeyCode key)
        {
            _lkmKey = key;
        }
        public static void SetRKMKey(KeyCode key)
        {
            _rkmKey = key;
        }
    }
}