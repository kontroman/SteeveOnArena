using System;

namespace MineArena.Networking
{
    public enum NetworkMessageType
    {
        Unknown,
        Connect,
        ConnectResponse,
        Disconnect,
        Error,
        Ping,
        Pong,
        Heartbeat,
        JoinRoom,
        RoomJoined,
        PlayerJoined,
        PlayerLeft,
        RoomSnapshot,
        TransformUpdate,
        TransformCorrection,
        AnimationUpdate,
        CustomizationUpdate,
        DamageRequest,
        DamageEvent,
        DeathEvent,
        RespawnEvent,
        CollisionEvent,
        ServerMetrics
    }

    public static class NetworkMessageTypes
    {
        public const string Connect = "connect";
        public const string ConnectResponse = "connect_response";
        public const string Disconnect = "disconnect";
        public const string Error = "error";
        public const string Ping = "ping";
        public const string Pong = "pong";
        public const string Heartbeat = "heartbeat";
        public const string JoinRoom = "join_room";
        public const string RoomJoined = "room_joined";
        public const string PlayerJoined = "player_joined";
        public const string PlayerLeft = "player_left";
        public const string RoomSnapshot = "room_snapshot";
        public const string TransformUpdate = "transform_update";
        public const string TransformCorrection = "transform_correction";
        public const string AnimationUpdate = "animation_update";
        public const string CustomizationUpdate = "customization_update";
        public const string DamageRequest = "damage_request";
        public const string DamageEvent = "damage_event";
        public const string DeathEvent = "death_event";
        public const string RespawnEvent = "respawn_event";
        public const string CollisionEvent = "collision_event";
        public const string ServerMetrics = "server_metrics";

        public static NetworkMessageType Parse(string value)
        {
            switch (value)
            {
                case Connect: return NetworkMessageType.Connect;
                case ConnectResponse: return NetworkMessageType.ConnectResponse;
                case Disconnect: return NetworkMessageType.Disconnect;
                case Error: return NetworkMessageType.Error;
                case Ping: return NetworkMessageType.Ping;
                case Pong: return NetworkMessageType.Pong;
                case Heartbeat: return NetworkMessageType.Heartbeat;
                case JoinRoom: return NetworkMessageType.JoinRoom;
                case RoomJoined: return NetworkMessageType.RoomJoined;
                case PlayerJoined: return NetworkMessageType.PlayerJoined;
                case PlayerLeft: return NetworkMessageType.PlayerLeft;
                case RoomSnapshot: return NetworkMessageType.RoomSnapshot;
                case TransformUpdate: return NetworkMessageType.TransformUpdate;
                case TransformCorrection: return NetworkMessageType.TransformCorrection;
                case AnimationUpdate: return NetworkMessageType.AnimationUpdate;
                case CustomizationUpdate: return NetworkMessageType.CustomizationUpdate;
                case DamageRequest: return NetworkMessageType.DamageRequest;
                case DamageEvent: return NetworkMessageType.DamageEvent;
                case DeathEvent: return NetworkMessageType.DeathEvent;
                case RespawnEvent: return NetworkMessageType.RespawnEvent;
                case CollisionEvent: return NetworkMessageType.CollisionEvent;
                case ServerMetrics: return NetworkMessageType.ServerMetrics;
                default: return NetworkMessageType.Unknown;
            }
        }

        public static string ToWireValue(NetworkMessageType type)
        {
            switch (type)
            {
                case NetworkMessageType.Connect: return Connect;
                case NetworkMessageType.ConnectResponse: return ConnectResponse;
                case NetworkMessageType.Disconnect: return Disconnect;
                case NetworkMessageType.Error: return Error;
                case NetworkMessageType.Ping: return Ping;
                case NetworkMessageType.Pong: return Pong;
                case NetworkMessageType.Heartbeat: return Heartbeat;
                case NetworkMessageType.JoinRoom: return JoinRoom;
                case NetworkMessageType.RoomJoined: return RoomJoined;
                case NetworkMessageType.PlayerJoined: return PlayerJoined;
                case NetworkMessageType.PlayerLeft: return PlayerLeft;
                case NetworkMessageType.RoomSnapshot: return RoomSnapshot;
                case NetworkMessageType.TransformUpdate: return TransformUpdate;
                case NetworkMessageType.TransformCorrection: return TransformCorrection;
                case NetworkMessageType.AnimationUpdate: return AnimationUpdate;
                case NetworkMessageType.CustomizationUpdate: return CustomizationUpdate;
                case NetworkMessageType.DamageRequest: return DamageRequest;
                case NetworkMessageType.DamageEvent: return DamageEvent;
                case NetworkMessageType.DeathEvent: return DeathEvent;
                case NetworkMessageType.RespawnEvent: return RespawnEvent;
                case NetworkMessageType.CollisionEvent: return CollisionEvent;
                case NetworkMessageType.ServerMetrics: return ServerMetrics;
                default: throw new ArgumentOutOfRangeException("type", type, "Unsupported message type.");
            }
        }
    }
}
