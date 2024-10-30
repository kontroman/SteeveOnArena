using Devotion.Controllers;
using UnityEngine;

public class DestroyObj : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out Inventory inventoryPlayer))
            Destroy(gameObject);
    }
}
