using System.Collections.Generic;
using UnityEngine;
using Devotion.SDK.Controllers;
using MineArena.Items;
using MineArena.Managers;
using MineArena.Basics;
using MineArena.Messages;

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
        [SerializeField] private bool _dropOnDeath;
        public bool DropOnDeath => _dropOnDeath;

        public void DropItems()
        {
            foreach (var dropEntry in _dropTable)
            {
                if (dropEntry.Item == null) continue;
                bool tutorialDrop = TutorialService.Expedition && TutorialService.Progress.Step == TutorialStep.Mine && GetComponent<InteractableObject>() != null;
                if (tutorialDrop || RollChance(dropEntry.DropChance))
                {
                    var cout = Random.Range(dropEntry.MinQuantity, dropEntry.MaxQuantity + 1);
                    if (tutorialDrop) cout = Mathf.Max(1, cout);

                    // Materials without a world pickup are collected directly.
                    if (dropEntry.Item.Prefab == null)
                    {
                        GameRoot.GetManager<InventoryManager>().AddItemById(dropEntry.Item.Name, cout);
                        MineArena.Controllers.LevelController.Current?.RegisterCollectedResource(dropEntry.Item, cout);
                        AchievementMessages.AchievementTargetTaken.Publish((dropEntry.Item, cout));
                        if (_isOneDrop) break;
                        continue;
                    }

                    for (int i = 0; i < cout; i++)
                    {
                        Instantiate(dropEntry.Item.Prefab, transform.position + Vector3.up * 0.35f, Quaternion.identity);
                        GameRoot.GetManager<AudioManager>().PlayEffect(Constants.AudioNames.DropResource);
                    }
                    if (_isOneDrop) break;
                }
            }
        }

        private bool RollChance(float chance)
        {
            return Random.Range(0f, 100f) <= chance;
        }
    }
}
