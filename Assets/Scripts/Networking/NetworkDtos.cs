using System;
using System.Text;
using UnityEngine;

namespace MineArena.Networking
{
    [Serializable]
    public class NetworkEnvelopeHeader
    {
        public string type;
        public ulong sequence;
        public ulong ack;
        public int playerId;
        public string roomId;
        public long sentAtUnixMs;
    }

    public class NetworkIncomingMessage
    {
        public NetworkEnvelopeHeader header;
        public string payloadJson;
        public NetworkMessageType MessageType => header == null ? NetworkMessageType.Unknown : NetworkMessageTypes.Parse(header.type);
    }

    [Serializable]
    public class NetworkEnvelopeDto<TPayload>
    {
        public string type;
        public ulong sequence;
        public ulong ack;
        public int playerId;
        public string roomId;
        public long sentAtUnixMs;
        public TPayload payload;
    }

    [Serializable]
    public class ConnectRequest
    {
        public string name;
        public string roomId;
        public string resumeToken;
        public CustomizationDto customization;
    }

    [Serializable]
    public class ConnectResponse
    {
        public int playerId;
        public string resumeToken;
        public string roomId;
        public int serverTickRate;
        public int snapshotSendRate;
        public long serverTimeUnixMs;
    }

    [Serializable]
    public class JoinRoomRequest
    {
        public string roomId;
    }

    [Serializable]
    public class ErrorMessage
    {
        public string code;
        public string message;
    }

    [Serializable]
    public class PingMessage
    {
        public long clientTimeUnixMs;
    }

    [Serializable]
    public class PongMessage
    {
        public long clientTimeUnixMs;
        public long serverTimeUnixMs;
    }

    [Serializable]
    public class RoomJoinedMessage
    {
        public string roomId;
        public int maxPlayers;
        public PlayerSnapshotDto[] players;
    }

    [Serializable]
    public class RoomSnapshotMessage
    {
        public string roomId;
        public ulong tick;
        public long serverTimeUnixMs;
        public PlayerSnapshotDto[] players;
    }

    [Serializable]
    public class PlayerPresenceMessage
    {
        public string roomId;
        public PlayerSnapshotDto player;
    }

    [Serializable]
    public class PlayerLeftMessage
    {
        public int playerId;
        public string roomId;
        public string reason;
    }

    [Serializable]
    public class PlayerSnapshotDto
    {
        public int playerId;
        public string name;
        public int health;
        public bool isAlive;
        public int teamId;
        public NetworkTransformSnapshot transform;
        public AnimationStateDto animation;
        public CustomizationDto customization;
    }

    [Serializable]
    public class TransformUpdate
    {
        public NetworkVector3Dto position;
        public NetworkQuaternionDto rotation;
        public NetworkVector3Dto velocity;
        public ulong clientTick;
        public long clientTimeUnixMs;
    }

    [Serializable]
    public class TransformCorrectionMessage
    {
        public string reason;
        public NetworkTransformSnapshot transform;
    }

    [Serializable]
    public class AnimationUpdate
    {
        public int playerId;
        public int stateHash;
        public float normalizedTime;
        public bool changedOnly = true;
        public AnimationParameterDto[] parameters;
        public string[] triggers;
        public ulong clientTick;
    }

    [Serializable]
    public class AnimationStateDto
    {
        public int stateHash;
        public float normalizedTime;
        public AnimationParameterDto[] parameters;
        public string[] triggers;
        public ulong tick;
    }

    [Serializable]
    public class AnimationParameterDto
    {
        public string name;
        public string type;
        public bool boolValue;
        public int intValue;
        public float floatValue;
    }

    [Serializable]
    public class CustomizationUpdate
    {
        public CustomizationDto customization;
    }

    [Serializable]
    public class CustomizationDto
    {
        public string weaponId;
        public string skinId;
        public string armorId;
        public string[] cosmeticItems;
        public NetworkKeyValue[] customFields;

        public static CustomizationDto From(PlayerCustomizationData data)
        {
            if (data == null)
                data = PlayerCustomizationData.Default();

            return new CustomizationDto
            {
                weaponId = data.weaponId,
                skinId = data.skinId,
                armorId = data.armorId,
                cosmeticItems = data.cosmeticItems ?? Array.Empty<string>(),
                customFields = data.customFields ?? Array.Empty<NetworkKeyValue>()
            };
        }

        public PlayerCustomizationData ToData()
        {
            return new PlayerCustomizationData
            {
                weaponId = weaponId,
                skinId = skinId,
                armorId = armorId,
                cosmeticItems = cosmeticItems ?? Array.Empty<string>(),
                customFields = customFields ?? Array.Empty<NetworkKeyValue>()
            };
        }
    }

    [Serializable]
    public class HealthUpdate
    {
        public int playerId;
        public int health;
        public bool isAlive;
    }

    [Serializable]
    public class DamageRequest
    {
        public int targetPlayerId;
        public int damage;
        public string weaponId;
        public string attackId;
        public NetworkVector3Dto hitPoint;
        public long clientTimeUnixMs;
    }

    [Serializable]
    public class DamageEventMessage
    {
        public int attackerPlayerId;
        public int targetPlayerId;
        public int damage;
        public int healthAfter;
        public string weaponId;
        public string attackId;
        public long serverTimeUnixMs;
    }

    [Serializable]
    public class DeathEventMessage
    {
        public int playerId;
        public int killerPlayerId;
        public long respawnAtUnixMs;
    }

    [Serializable]
    public class RespawnEventMessage
    {
        public int playerId;
        public NetworkTransformSnapshot transform;
        public int health;
    }

    public static class NetworkJson
    {
        public static string SerializeEnvelope<TPayload>(
            string type,
            ulong sequence,
            ulong ack,
            int playerId,
            string roomId,
            TPayload payload)
        {
            var envelope = new NetworkEnvelopeDto<TPayload>
            {
                type = type,
                sequence = sequence,
                ack = ack,
                playerId = playerId,
                roomId = roomId,
                sentAtUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                payload = payload
            };

            var json = JsonUtility.ToJson(envelope);
            return ReplaceCustomFieldsWithObject(json);
        }

        public static bool TryParseIncoming(string json, out NetworkIncomingMessage message, out string error)
        {
            message = null;
            error = null;

            try
            {
                var header = JsonUtility.FromJson<NetworkEnvelopeHeader>(json);
                if (header == null || string.IsNullOrEmpty(header.type))
                {
                    error = "Missing envelope type.";
                    return false;
                }

                message = new NetworkIncomingMessage
                {
                    header = header,
                    payloadJson = ExtractPayloadObject(json)
                };
                return true;
            }
            catch (Exception exception)
            {
                error = exception.Message;
                return false;
            }
        }

        public static TPayload ReadPayload<TPayload>(NetworkIncomingMessage message)
        {
            if (message == null || string.IsNullOrEmpty(message.payloadJson))
                return default(TPayload);

            return JsonUtility.FromJson<TPayload>(message.payloadJson);
        }

        private static string ExtractPayloadObject(string json)
        {
            const string marker = "\"payload\"";
            var markerIndex = json.IndexOf(marker, StringComparison.Ordinal);
            if (markerIndex < 0)
                return "{}";

            var colonIndex = json.IndexOf(':', markerIndex + marker.Length);
            if (colonIndex < 0)
                return "{}";

            var start = colonIndex + 1;
            while (start < json.Length && char.IsWhiteSpace(json[start]))
                start++;

            if (start >= json.Length)
                return "{}";

            if (json[start] != '{')
                return "{}";

            var depth = 0;
            var inString = false;
            var escaped = false;

            for (var i = start; i < json.Length; i++)
            {
                var c = json[i];
                if (escaped)
                {
                    escaped = false;
                    continue;
                }

                if (c == '\\' && inString)
                {
                    escaped = true;
                    continue;
                }

                if (c == '"')
                {
                    inString = !inString;
                    continue;
                }

                if (inString)
                    continue;

                if (c == '{')
                    depth++;
                else if (c == '}')
                {
                    depth--;
                    if (depth == 0)
                        return json.Substring(start, i - start + 1);
                }
            }

            return "{}";
        }

        private static string ReplaceCustomFieldsWithObject(string json)
        {
            const string marker = "\"customFields\":[";
            var index = json.IndexOf(marker, StringComparison.Ordinal);
            if (index < 0)
                return json;

            var arrayStart = index + "\"customFields\":".Length;
            var arrayEnd = FindMatchingBracket(json, arrayStart, '[', ']');
            if (arrayEnd < arrayStart)
                return json;

            var arrayJson = json.Substring(arrayStart, arrayEnd - arrayStart + 1);
            var pairs = JsonUtility.FromJson<NetworkKeyValueArray>("{\"items\":" + arrayJson + "}");
            var builder = new StringBuilder();
            builder.Append("\"customData\":{");

            if (pairs != null && pairs.items != null)
            {
                for (var i = 0; i < pairs.items.Length; i++)
                {
                    if (string.IsNullOrEmpty(pairs.items[i].key))
                        continue;

                    if (builder[builder.Length - 1] != '{')
                        builder.Append(',');

                    builder.Append('"').Append(EscapeJson(pairs.items[i].key)).Append("\":\"");
                    builder.Append(EscapeJson(pairs.items[i].value)).Append('"');
                }
            }

            builder.Append('}');
            return json.Substring(0, index) + builder + json.Substring(arrayEnd + 1);
        }

        private static int FindMatchingBracket(string json, int start, char open, char close)
        {
            var depth = 0;
            var inString = false;
            var escaped = false;

            for (var i = start; i < json.Length; i++)
            {
                var c = json[i];
                if (escaped)
                {
                    escaped = false;
                    continue;
                }

                if (c == '\\' && inString)
                {
                    escaped = true;
                    continue;
                }

                if (c == '"')
                {
                    inString = !inString;
                    continue;
                }

                if (inString)
                    continue;

                if (c == open)
                    depth++;
                else if (c == close)
                {
                    depth--;
                    if (depth == 0)
                        return i;
                }
            }

            return -1;
        }

        private static string EscapeJson(string value)
        {
            return string.IsNullOrEmpty(value)
                ? string.Empty
                : value.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        [Serializable]
        private class NetworkKeyValueArray
        {
            public NetworkKeyValue[] items = Array.Empty<NetworkKeyValue>();
        }
    }
}
