using System;
using UnityEngine;

namespace MineArena.Networking
{
    [DisallowMultipleComponent]
    public class LocalNetworkPlayer : MonoBehaviour
    {
        private NetworkClientManager manager;
        private CharacterController characterController;
        private Rigidbody body;
        private int playerId;
        private float nextSendTime;
        private ulong localTick;
        private Vector3 lastSentPosition;
        private Quaternion lastSentRotation;
        private Vector3 lastSentVelocity;
        private Vector3 previousPosition;
        private bool hasLastSent;

        public int PlayerId => playerId;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            body = GetComponent<Rigidbody>();
            previousPosition = transform.position;
        }

        private void LateUpdate()
        {
            previousPosition = transform.position;
        }

        public void Bind(NetworkClientManager networkManager, int id)
        {
            manager = networkManager;
            playerId = id;
            lastSentPosition = transform.position;
            lastSentRotation = transform.rotation;
            lastSentVelocity = Vector3.zero;
            hasLastSent = false;
        }

        public bool TryBuildTransformUpdate(out TransformUpdate update)
        {
            update = null;

            if (manager == null || Time.unscaledTime < nextSendTime)
                return false;

            var velocity = GetVelocity();
            var positionChanged = !hasLastSent ||
                                  Vector3.Distance(transform.position, lastSentPosition) >= manager.PositionThreshold;
            var rotationChanged = !hasLastSent ||
                                  Quaternion.Angle(transform.rotation, lastSentRotation) >= manager.RotationThreshold;
            var velocityChanged = !hasLastSent ||
                                  Vector3.Distance(velocity, lastSentVelocity) >= manager.VelocityThreshold;

            if (!positionChanged && !rotationChanged && !velocityChanged)
                return false;

            localTick++;
            nextSendTime = Time.unscaledTime + manager.SendTransformInterval;
            lastSentPosition = transform.position;
            lastSentRotation = transform.rotation;
            lastSentVelocity = velocity;
            hasLastSent = true;

            update = new TransformUpdate
            {
                position = new NetworkVector3Dto(transform.position),
                rotation = new NetworkQuaternionDto(transform.rotation),
                velocity = new NetworkVector3Dto(velocity),
                clientTick = localTick,
                clientTimeUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };
            return true;
        }

        public void ApplyServerCorrection(NetworkTransformSnapshot correction)
        {
            if (correction == null)
                return;

            if (characterController != null)
                characterController.enabled = false;

            transform.SetPositionAndRotation(correction.Position, correction.Rotation);

            if (characterController != null)
                characterController.enabled = true;

            lastSentPosition = transform.position;
            lastSentRotation = transform.rotation;
            lastSentVelocity = correction.Velocity;
            hasLastSent = true;
        }

        private Vector3 GetVelocity()
        {
            if (characterController != null)
                return characterController.velocity;

            if (body != null)
                return body.velocity;

            var deltaTime = Mathf.Max(Time.deltaTime, 0.0001f);
            return (transform.position - previousPosition) / deltaTime;
        }
    }
}
