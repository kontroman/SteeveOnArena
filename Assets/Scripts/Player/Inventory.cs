using Devotion.Item;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    private List<Item> items;

    private void Start()
    {
        items = new List<Item>();
    }

    public void GetItem(Item item)
    {
        items.Add(item);
        Debug.Log(item.Name);

    }   
}
