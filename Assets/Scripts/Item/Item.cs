using UnityEngine;

public class Item : MonoBehaviour
{
    [SerializeField] private string _name;

    private Item _item;

    public string Name => _name;

    private void Start()
    {
        _item = GetComponent<Item>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out Inventory playerInventory))
        {
            playerInventory.GetItem(_item);
            Destroy(gameObject);
        }
    }
}
