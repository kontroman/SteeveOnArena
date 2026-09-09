using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MineArena.Levels
{
    public class Arena : MonoBehaviour
    {
        [SerializeField] private Transform _playerSpawnPosition;
        [SerializeField] private Transform _portalSpawnPoint;
        [SerializeField, Tooltip("Use resources already placed in this arena instead of generating another layer at spawn markers.")]
        private bool _useAuthoredResources;
        [SerializeField, HideInInspector] private List<Transform> _oreSpawnPoints = new List<Transform>();

        public Transform PlayerSpawnPosition { get { return _playerSpawnPosition; } }
        public Transform PortalSpawnPoint { get { return _portalSpawnPoint; } }
        public bool UseAuthoredResources => _useAuthoredResources;
        public IReadOnlyList<Transform> OreSpawnPoints { get { return _oreSpawnPoints; } }

        private void Awake()
        {
            RefreshOreSpawnPoints();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            RefreshOreSpawnPoints();
        }
#endif

        private void RefreshOreSpawnPoints()
        {
            if (_oreSpawnPoints == null)
            {
                _oreSpawnPoints = new List<Transform>();
            }

            _oreSpawnPoints.Clear();

            var spawnPoints = GetComponentsInChildren<OreSpawnPoint>(true);
            foreach (var spawnPoint in spawnPoints)
            {
                if (spawnPoint != null)
                {
                    _oreSpawnPoints.Add(spawnPoint.transform);
                }
            }
        }

        //TODO: add particles to fire
    }
}
