using System;
using UnityEngine;

namespace MineArena.Buildings
{
    [Serializable]
    public class BuildingCinematicCameraSettings
    {
        [SerializeField] private float duration = 3f;
        [SerializeField] private float radius = 6f;
        [SerializeField] private float height = 3f;
        [SerializeField] private float startAngle = -35f;
        [SerializeField] private float endAngle = 35f;
        [SerializeField] private Vector3 lookAtOffset = new Vector3(0f, 1.5f, 0f);

        public float Duration => duration;
        public float Radius => radius;
        public float Height => height;
        public float StartAngle => startAngle;
        public float EndAngle => endAngle;
        public Vector3 LookAtOffset => lookAtOffset;
    }
}
