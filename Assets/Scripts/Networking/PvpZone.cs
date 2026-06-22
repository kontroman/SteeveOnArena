using System.Collections.Generic;
using UnityEngine;

namespace MineArena.Networking
{
    [DisallowMultipleComponent]
    public sealed class PvpZone : MonoBehaviour
    {
        private static readonly Dictionary<string, Dictionary<int, int>> OccupancyByZone =
            new Dictionary<string, Dictionary<int, int>>();

        private static readonly HashSet<PvpZone> Instances = new HashSet<PvpZone>();

        [SerializeField, Tooltip("Unique for separate zones. Use the same id on colliders that form one zone.")]
        private string zoneId = "pvp-zone";

        [SerializeField]
        private bool debugLogs;

        private readonly Dictionary<int, HashSet<Collider>> contactsByPlayer =
            new Dictionary<int, HashSet<Collider>>();

        private Collider zoneCollider;
        private string runtimeZoneId;

        public string ZoneId => runtimeZoneId;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            OccupancyByZone.Clear();
            Instances.Clear();
        }

        private void Reset()
        {
            var collider = GetComponent<Collider>();
            if (collider != null)
                collider.isTrigger = true;
        }

        private void OnValidate()
        {
            var collider = GetComponent<Collider>();
            if (collider != null)
                collider.isTrigger = true;
        }

        private void Awake()
        {
            runtimeZoneId = string.IsNullOrWhiteSpace(zoneId) ? gameObject.name : zoneId.Trim();
            zoneCollider = GetComponent<Collider>();

            if (zoneCollider == null)
            {
                Debug.LogError("[PvpZone] Collider is required on " + gameObject.name, this);
                enabled = false;
                return;
            }

            if (!zoneCollider.isTrigger)
            {
                Debug.LogWarning("[PvpZone] Collider was changed to Is Trigger on " + gameObject.name, this);
                zoneCollider.isTrigger = true;
            }

            Instances.Add(this);
        }

        private void OnDisable()
        {
            ClearTrackedPlayers();
            Instances.Remove(this);
        }

        private void OnEnable()
        {
            if (!string.IsNullOrEmpty(runtimeZoneId))
                Instances.Add(this);
        }

        private void OnTriggerEnter(Collider other)
        {
            RegisterContact(other);
        }

        private void OnTriggerStay(Collider other)
        {
            // Also handles a NetworkPlayerView added after the collider entered the zone.
            RegisterContact(other);
        }

        private void OnTriggerExit(Collider other)
        {
            if (!TryGetPlayerId(other, out var playerId) ||
                !contactsByPlayer.TryGetValue(playerId, out var contacts))
            {
                return;
            }

            contacts.Remove(other);
            if (contacts.Count > 0)
                return;

            contactsByPlayer.Remove(playerId);
            LeaveZone(playerId);
        }

        public static bool CanDamage(int attackerPlayerId, int targetPlayerId)
        {
            if (attackerPlayerId <= 0 || targetPlayerId <= 0 || attackerPlayerId == targetPlayerId)
                return false;

            foreach (var occupants in OccupancyByZone.Values)
            {
                if (occupants.TryGetValue(attackerPlayerId, out var attackerCount) && attackerCount > 0 &&
                    occupants.TryGetValue(targetPlayerId, out var targetCount) && targetCount > 0)
                {
                    return true;
                }
            }

            return false;
        }

        public static void RemovePlayer(int playerId)
        {
            if (playerId <= 0)
                return;

            var zones = new List<PvpZone>(Instances);
            for (var i = 0; i < zones.Count; i++)
                zones[i]?.RemoveTrackedPlayer(playerId);
        }

        public static void ClearAllPlayers()
        {
            var zones = new List<PvpZone>(Instances);
            for (var i = 0; i < zones.Count; i++)
                zones[i]?.contactsByPlayer.Clear();

            OccupancyByZone.Clear();
        }

        private void RegisterContact(Collider other)
        {
            if (!TryGetPlayerId(other, out var playerId))
                return;

            if (!contactsByPlayer.TryGetValue(playerId, out var contacts))
            {
                contacts = new HashSet<Collider>();
                contactsByPlayer.Add(playerId, contacts);
            }

            if (!contacts.Add(other) || contacts.Count != 1)
                return;

            EnterZone(playerId);
        }

        private static bool TryGetPlayerId(Collider other, out int playerId)
        {
            playerId = 0;
            if (other == null)
                return false;

            var view = other.GetComponentInParent<NetworkPlayerView>();
            if (view == null || view.PlayerId <= 0)
                return false;

            playerId = view.PlayerId;
            return true;
        }

        private void EnterZone(int playerId)
        {
            if (!OccupancyByZone.TryGetValue(runtimeZoneId, out var occupants))
            {
                occupants = new Dictionary<int, int>();
                OccupancyByZone.Add(runtimeZoneId, occupants);
            }

            occupants.TryGetValue(playerId, out var count);
            occupants[playerId] = count + 1;

            if (debugLogs)
                Debug.Log("[PvpZone] Player " + playerId + " entered " + runtimeZoneId, this);
        }

        private void LeaveZone(int playerId)
        {
            if (!OccupancyByZone.TryGetValue(runtimeZoneId, out var occupants) ||
                !occupants.TryGetValue(playerId, out var count))
            {
                return;
            }

            if (count <= 1)
                occupants.Remove(playerId);
            else
                occupants[playerId] = count - 1;

            if (occupants.Count == 0)
                OccupancyByZone.Remove(runtimeZoneId);

            if (debugLogs)
                Debug.Log("[PvpZone] Player " + playerId + " left " + runtimeZoneId, this);
        }

        private void RemoveTrackedPlayer(int playerId)
        {
            if (!contactsByPlayer.Remove(playerId))
                return;

            LeaveZone(playerId);
        }

        private void ClearTrackedPlayers()
        {
            if (contactsByPlayer.Count == 0)
                return;

            var playerIds = new List<int>(contactsByPlayer.Keys);
            contactsByPlayer.Clear();

            for (var i = 0; i < playerIds.Count; i++)
                LeaveZone(playerIds[i]);
        }
    }
}
