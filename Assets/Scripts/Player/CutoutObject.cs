using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MineArena.Basics
{
    public class CutoutObject : MonoBehaviour
    {
        [SerializeField]
        private Transform targetObject;

        [SerializeField]
        private LayerMask wallMask;

        [SerializeField] private float _CutoutSize;
        [SerializeField] private float _FalloffSize;

        private Camera mainCamera;

        private void Awake()
        {
            mainCamera = GetComponent<Camera>();
        }

        private void Update()
        {
            if (targetObject == null) return;

            Vector2 cutoutPos = mainCamera.WorldToViewportPoint(targetObject.position);

            Vector3 offset = targetObject.position - transform.position;
            RaycastHit[] hitObjects = Physics.RaycastAll(transform.position, offset, offset.magnitude, wallMask);

            bool hitSomething = hitObjects.Length > 0;

            for (int i = 0; i < hitObjects.Length; ++i)
            {
                Material[] materials = hitObjects[i].transform.GetComponent<Renderer>().materials;

                for (int m = 0; m < materials.Length; ++m)
                {
                    materials[m].SetFloat("_CutoutEnabled", hitSomething ? 1f : 0f);

                    if (hitSomething)
                    {
                        materials[m].SetVector("_CutoutPos", cutoutPos);
                        materials[m].SetFloat("_CutoutSize", _CutoutSize);
                        materials[m].SetFloat("_FalloffSize", _FalloffSize);
                    }
                }
            }
        }
    }
}
