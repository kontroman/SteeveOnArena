using UnityEngine;

namespace Divotion.Game.UI
{
    public class BillboarbBar : MonoBehaviour
    {
        [SerializeField] private Camera _mainCamera;

        private void Update()
        {
            LookAt();
        }

        private void LookAt()
        {
            if (_mainCamera != null)
                transform.LookAt(transform.position + _mainCamera.transform.forward);
        }
    }
}
