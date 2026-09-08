using UnityEngine;
namespace MineArena.Items
{
    public class HarvestPlantVisual : MonoBehaviour
    {
        [SerializeField] private Material _plantMaterial;
        private void Awake()
        {
            if (_plantMaterial == null) return;
            var original = GetComponent<MeshRenderer>();
            if (original != null) original.enabled = false;
            for (int i = 0; i < 2; i++)
            {
                var plant = GameObject.CreatePrimitive(PrimitiveType.Quad);
                plant.name = "PlantPlane";
                plant.transform.SetParent(transform, false);
                plant.transform.localPosition = new Vector3(0, 0.3f, 0);
                plant.transform.localRotation = Quaternion.Euler(0, i * 90f, 0);
                plant.transform.localScale = new Vector3(1.3f, 1.6f, 1);
                Destroy(plant.GetComponent<Collider>());
                plant.GetComponent<MeshRenderer>().sharedMaterial = _plantMaterial;
            }
        }
    }
}
