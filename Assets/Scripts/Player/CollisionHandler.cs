using System.Collections.Generic;
using UnityEngine;
using static Devotion.SDK.Helpers.ContainersHelper;

namespace Devotion.PlayerSystem
{
    public class CollisionHandler : MonoBehaviour
    {
        [SerializeField] private List<TagActionPair> tagActions;

        private void OnTriggerEnter(Collider other)
        {
            HandleInteraction(other.gameObject);
        }

        private void OnCollisionEnter(Collision collision)
        {
            HandleInteraction(collision.gameObject);
        }

        private void HandleInteraction(GameObject target)
        {
            foreach (var pair in tagActions)
            {
                if (pair.tag == target.tag)
                {
                    pair.action.Execute(target);
                    return;
                }
            }

            Debug.Log($"No action found for tag: {target.tag}");
        }
    }
}