using System;
using System.Collections.Generic;
using UnityEngine;
using MineArena.PlayerSystem;
using PlayerHealth = MineArena.Game.Health.Health;

namespace MineArena.Networking
{
    [DisallowMultipleComponent]
    public class NetworkPlayerView : MonoBehaviour
    {
        private readonly List<BufferedTransformSnapshot> transformBuffer = new List<BufferedTransformSnapshot>(32);

        private int playerId;
        private bool isLocalPlayer;
        private float interpolationDelay = 0.16f;
        private float extrapolationLimit = 0.25f;
        private float snapDistance = 4f;
        private Animator animator;
        private NetworkAnimatorSync animatorSync;
        private int health = 100;
        private bool isAlive = true;
        private bool deathApplied;
        private bool hasHealthState;
        private PlayerCustomizationData customization;

        public int PlayerId => playerId;
        public bool IsLocalPlayer => isLocalPlayer;
        public int Health => health;
        public bool IsAlive => isAlive;
        public PlayerCustomizationData Customization => customization;

        private void Awake()
        {
            animator = GetComponentInChildren<Animator>();
            animatorSync = GetComponent<NetworkAnimatorSync>();
        }

        private void Update()
        {
            if (isLocalPlayer)
                return;

            ApplyBufferedTransform();
        }

        public void BindLocal(int id)
        {
            playerId = id;
            isLocalPlayer = true;
            gameObject.name = "LocalPlayer_" + id;
        }

        public void BindRemote(int id, float delay, float extrapolateLimit, float snap)
        {
            playerId = id;
            isLocalPlayer = false;
            interpolationDelay = Mathf.Max(0.16f, delay);
            extrapolationLimit = Mathf.Max(0f, extrapolateLimit);
            snapDistance = Mathf.Max(0f, snap);
        }

        public void ApplySnapshot(PlayerSnapshotDto snapshot, bool immediate)
        {
            if (snapshot == null)
                return;

            ApplyHealth(snapshot.health, snapshot.isAlive);

            if (snapshot.customization != null)
                ApplyCustomization(snapshot.customization.ToData());

            if (snapshot.animation != null)
                ApplyAnimation(snapshot.animation);

            if (snapshot.transform != null)
            {
                snapshot.transform.playerId = snapshot.playerId;
                if (isLocalPlayer)
                {
                    return;
                }

                if (immediate || transformBuffer.Count == 0)
                    transform.SetPositionAndRotation(snapshot.transform.Position, snapshot.transform.Rotation);

                AddSnapshot(snapshot.transform);
            }
        }

        public void ApplyAnimation(AnimationUpdate update)
        {
            if (update == null)
                return;

            var state = new AnimationStateDto
            {
                stateHash = update.stateHash,
                normalizedTime = update.normalizedTime,
                parameters = update.parameters,
                triggers = update.triggers,
                tick = update.clientTick
            };
            ApplyAnimation(state);
        }

        public void ApplyAnimation(AnimationStateDto state)
        {
            if (state == null)
                return;

            if (animatorSync == null)
                animatorSync = GetComponent<NetworkAnimatorSync>();

            if (animatorSync != null)
                animatorSync.ApplyRemoteState(state);
        }

        public void ApplyCustomization(PlayerCustomizationData data)
        {
            customization = data ?? PlayerCustomizationData.Default();
            Debug.Log("[NetworkPlayerView] ApplyCustomization playerId=" + playerId
                      + " weapon=" + customization.weaponId
                      + " skin=" + customization.skinId
                      + " armor=" + customization.armorId);
        }

        public void ApplyHealth(int newHealth, bool alive)
        {
            var becameAlive = hasHealthState && !isAlive && alive;
            var changed = !hasHealthState || health != newHealth || isAlive != alive;
            hasHealthState = true;
            health = newHealth;
            isAlive = alive;

            if (isAlive)
                deathApplied = false;

            if (changed)
            {
                SyncLocalHealth();
                Debug.Log("[NetworkPlayerView] Health playerId=" + playerId + " health=" + health + " alive=" + isAlive);
            }

            if (becameAlive && isLocalPlayer)
            {
                GetComponent<PlayerMovement>()?.SetAlive();
                GetComponent<PlayerAttack>()?.SetComponentEnable(true);
            }

            if (!isAlive && !deathApplied)
                ApplyDeath(0);
        }

        public void ApplyDeath(int killerPlayerId)
        {
            isAlive = false;
            health = 0;
            SyncLocalHealth();

            if (deathApplied)
                return;

            deathApplied = true;

            var playerAnimator = GetComponent<PlayerAnimatorController>();
            if (playerAnimator != null)
                playerAnimator.TriggerDeath();

            if (isLocalPlayer)
            {
                GetComponent<PlayerMovement>()?.SetDead();
                GetComponent<PlayerAttack>()?.SetComponentEnable(false);
            }

            Debug.Log("[NetworkPlayerView] PlayerDied playerId=" + playerId + " killer=" + killerPlayerId);
        }

        public void ApplyRespawn(NetworkTransformSnapshot snapshot, int newHealth)
        {
            isAlive = true;
            health = newHealth;
            hasHealthState = true;
            deathApplied = false;
            transformBuffer.Clear();
            SyncLocalHealth();

            if (snapshot != null)
            {
                var characterController = isLocalPlayer ? GetComponent<CharacterController>() : null;
                if (characterController != null)
                    characterController.enabled = false;

                transform.SetPositionAndRotation(snapshot.Position, snapshot.Rotation);

                if (characterController != null)
                    characterController.enabled = true;
            }

            if (isLocalPlayer)
            {
                GetComponent<PlayerMovement>()?.SetAlive();
                GetComponent<PlayerAttack>()?.SetComponentEnable(true);
            }

            Debug.Log("[NetworkPlayerView] PlayerRespawned playerId=" + playerId + " health=" + health);
        }

        private void SyncLocalHealth()
        {
            if (!isLocalPlayer)
                return;

            var playerHealth = GetComponent<PlayerHealth>() ?? GetComponentInChildren<PlayerHealth>();
            playerHealth?.SetCurrentValue(health, false);
        }

        private void AddSnapshot(NetworkTransformSnapshot snapshot)
        {
            if (snapshot == null)
                return;

            if (transformBuffer.Count > 0)
            {
                var last = transformBuffer[transformBuffer.Count - 1].Snapshot;
                if (snapshot.timestampUnixMs < last.timestampUnixMs)
                    return;

                if (snapshot.timestampUnixMs == last.timestampUnixMs &&
                    Vector3.Distance(snapshot.Position, last.Position) < 0.001f &&
                    Quaternion.Angle(snapshot.Rotation, last.Rotation) < 0.1f)
                {
                    return;
                }
            }

            transformBuffer.Add(new BufferedTransformSnapshot
            {
                Snapshot = snapshot,
                ReceivedAtUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            });
            transformBuffer.Sort((a, b) => a.ReceivedAtUnixMs.CompareTo(b.ReceivedAtUnixMs));

            if (transformBuffer.Count > 32)
                transformBuffer.RemoveRange(0, transformBuffer.Count - 32);
        }

        private void ApplyBufferedTransform()
        {
            if (transformBuffer.Count == 0)
                return;

            var targetTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - (long)(interpolationDelay * 1000f);

            while (transformBuffer.Count >= 2 && transformBuffer[1].ReceivedAtUnixMs <= targetTime)
                transformBuffer.RemoveAt(0);

            var sampledPosition = transform.position;
            var sampledRotation = transform.rotation;

            if (transformBuffer.Count >= 2 &&
                transformBuffer[0].ReceivedAtUnixMs <= targetTime &&
                transformBuffer[1].ReceivedAtUnixMs >= targetTime)
            {
                Interpolate(transformBuffer[0], transformBuffer[1], targetTime, out sampledPosition, out sampledRotation);
            }
            else
            {
                Extrapolate(transformBuffer[transformBuffer.Count - 1], targetTime, out sampledPosition, out sampledRotation);
            }

            if (Vector3.Distance(transform.position, sampledPosition) > snapDistance)
            {
                transform.SetPositionAndRotation(sampledPosition, sampledRotation);
                return;
            }

            transform.position = Vector3.Lerp(transform.position, sampledPosition, 20f * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, sampledRotation, 20f * Time.deltaTime);
        }

        private static void Interpolate(
            BufferedTransformSnapshot a,
            BufferedTransformSnapshot b,
            long targetTime,
            out Vector3 position,
            out Quaternion rotation)
        {
            var span = Mathf.Max(1f, b.ReceivedAtUnixMs - a.ReceivedAtUnixMs);
            var t = Mathf.Clamp01((targetTime - a.ReceivedAtUnixMs) / span);
            position = Vector3.Lerp(a.Snapshot.Position, b.Snapshot.Position, t);
            rotation = Quaternion.Slerp(a.Snapshot.Rotation, b.Snapshot.Rotation, t);
        }

        private void Extrapolate(
            BufferedTransformSnapshot bufferedSnapshot,
            long targetTime,
            out Vector3 position,
            out Quaternion rotation)
        {
            var snapshot = bufferedSnapshot.Snapshot;
            var deltaSeconds = Mathf.Clamp((targetTime - bufferedSnapshot.ReceivedAtUnixMs) / 1000f, 0f, extrapolationLimit);
            position = snapshot.Position + snapshot.Velocity * deltaSeconds;
            rotation = snapshot.Rotation;
        }

        private struct BufferedTransformSnapshot
        {
            public NetworkTransformSnapshot Snapshot;
            public long ReceivedAtUnixMs;
        }
    }
}
