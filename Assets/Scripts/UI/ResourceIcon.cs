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
            SetSprite(resource != null ? resource.Icon : null);
        }

        public void SetSprite(Sprite sprite)
        {
            if (resourceImages == null)
                return;

            foreach (var item in resourceImages)
            {
                if (item == null)
                    continue;

                item.sprite = sprite;
                item.enabled = sprite != null;
                item.raycastTarget = false;
            }
        }
    }
}
