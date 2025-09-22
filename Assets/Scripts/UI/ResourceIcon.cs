using MineArena.Items;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MineArena.UI
{
    public class ResourceIcon : MonoBehaviour
    {
        [SerializeField] private List<Image> resourceImages;

        public void SetResource(StackableItemConfig resource)
        {
            foreach (var item in resourceImages)
            {
                item.sprite = resource.Icon;
            }
        }
    }
}