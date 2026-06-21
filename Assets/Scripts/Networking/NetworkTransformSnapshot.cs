using System;
using UnityEngine;

namespace MineArena.Networking
{
    [Serializable]
    public struct NetworkVector3Dto
    {
        public float x;
        public float y;
        public float z;

        public NetworkVector3Dto(Vector3 value)
        {
            x = value.x;
            y = value.y;
            z = value.z;
        }

        public Vector3 ToUnity() => new Vector3(x, y, z);
    }

    [Serializable]
    public struct NetworkQuaternionDto
    {
        public float x;
        public float y;
        public float z;
        public float w;

        public NetworkQuaternionDto(Quaternion value)
        {
            x = value.x;
            y = value.y;
            z = value.z;
            w = value.w;
        }

        public Quaternion ToUnity()
        {
            if (Mathf.Approximately(x, 0f) && Mathf.Approximately(y, 0f) &&
                Mathf.Approximately(z, 0f) && Mathf.Approximately(w, 0f))
            {
                return Quaternion.identity;
            }

            return new Quaternion(x, y, z, w);
        }
    }

    [Serializable]
    public class NetworkTransformSnapshot
    {
        public int playerId;
        public NetworkVector3Dto position;
        public NetworkQuaternionDto rotation;
        public NetworkVector3Dto velocity;
        public ulong tick;
        public long timestampUnixMs;

        public Vector3 Position => position.ToUnity();
        public Quaternion Rotation => rotation.ToUnity();
        public Vector3 Velocity => velocity.ToUnity();
    }
}
