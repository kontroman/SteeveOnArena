using UnityEngine;

namespace Devotion.PlayerSystem
{
    public class SwordSystem : MonoBehaviour
    {
        [SerializeField] private Transform _rightHand;

        private GameObject _equippedWeapon;

        public void EquipWeapon(GameObject weaponPrefab)
        {
            if (_equippedWeapon != null)
            {
                Destroy(_equippedWeapon);
            }

            _equippedWeapon = Instantiate(weaponPrefab, _rightHand);
            _equippedWeapon.transform.localPosition = Vector3.zero;
            _equippedWeapon.transform.localRotation = Quaternion.identity;
        }

        public void UnequipWeapon()
        {
            if (_equippedWeapon != null)
            {
                Destroy(_equippedWeapon);
                _equippedWeapon = null;
            }
        }
    }
}
