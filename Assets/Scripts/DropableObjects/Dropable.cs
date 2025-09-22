using System.Collections.Generic;
using UnityEngine;
using Devotion.SDK.Controllers;
using MineArena.Items;
using MineArena.Managers;
using MineArena.Basics;

namespace MineArena.Drop
{
    public class Dropable : MonoBehaviour
    {
        [System.Serializable]
        public class DropEntry
        {
            public ItemConfig Item;

            [Range(0, 100)] public float DropChance;
            public int MinQuantity = 1;
            public int MaxQuantity = 1;
        }

        [Header("List drops")]
        [SerializeField] private List<DropEntry> _dropTable = new List<DropEntry>();

        [Header("Drop only one or more items")]
        [SerializeField] private bool _isOneDrop;

        public void DropItems()
        {
            foreach (var dropEntry in _dropTable)
            {
                if (RollChance(dropEntry.DropChance))
                {
                    var cout = Random.Range(dropEntry.MinQuantity, dropEntry.MaxQuantity + 1);

                    for (int i = 0; i < cout; i++)
                    {
                        var obj = Instantiate(dropEntry.Item.Prefab);
                        obj.transform.position = transform.position;
                        GameRoot.GetManager<AudioManager>().PlayEffect(Constants.AudioNames.DropResource);
                    }
                }
            }
        }

        private bool RollChance(float chance)
        {
            return Random.Range(0f, 100f) <= chance;
        }
    }
}
