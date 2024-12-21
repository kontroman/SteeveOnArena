using Devotion.Controllers;
using Devotion.Equipment;
using System.Collections.Generic;
using UnityEngine;

public class Shop : MonoBehaviour
{
    [SerializeField] private List<Sword> _swords = new();

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out Player player))
        {
            GameRoot.Instance.GetManager<Devotion.Managers.EquipmentItemManager>()
                .CreatEquipmentItem(transform.position, Devotion.Equipment.ItemTypes.Sword, _swords[0].LvlItem);
        }
    }
}