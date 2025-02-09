using UnityEngine;

public class Billboard : MonoBehaviour
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
