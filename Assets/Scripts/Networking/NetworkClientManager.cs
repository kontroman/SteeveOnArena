using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;
using MineArena.Controllers;
using MineArena.PlayerSystem;

namespace MineArena.Networking
{
    public class NetworkClientManager : MonoBehaviour
    {
        public static NetworkClientManager Instance { get; private set; }

        [Header("Server")]
        [SerializeField, Tooltip("MineArenaServer host or IP address.")]
        private string serverHost = "127.0.0.1";
        [SerializeField, Tooltip("MineArenaServer UDP port.")]
        private int serverPort = 7777;
        [SerializeField, Tooltip("Default room/arena id.")]
        private string roomId = "arena-1";
        [SerializeField, Tooltip("Reconnect automatically after timeout or socket error.")]
        private bool reconnectEnabled = true;
        [SerializeField, Tooltip("Seconds to wait for connect_response before retrying or failing.")]
        private float connectionTimeout = 5f;

        [Header("Player")]
        [SerializeField, Tooltip("Existing player prefab. Remote clones are stripped from local-only scripts at runtime.")]
        private GameObject playerPrefab;
        [SerializeField, Tooltip("Spawn point for local player when no existing player is present.")]
        private Transform localPlayerSpawnPoint;
        [SerializeField, Tooltip("Move an existing DontDestroyOnLoad player to the resolved arena spawn point on connect.")]
        private bool moveExistingLocalPlayerToSpawn = true;
        [SerializeField, Tooltip("Optional player name sent to server.")]
        private string playerName = "UnityPlayer";
        [SerializeField]
        private PlayerCustomizationData customization = new PlayerCustomizationData();

        [Header("Transform Sync")]
        [SerializeField, Min(1f), Tooltip("Local transform packets per second.")]
        private float sendTransformRate = 15f;
        [SerializeField, Tooltip("Minimum position delta before sending.")]
        private float positionThreshold = 0.03f;
        [SerializeField, Tooltip("Minimum rotation delta in degrees before sending.")]
        private float rotationThreshold = 1.5f;
        [SerializeField, Tooltip("Minimum velocity delta before sending.")]
        private float velocityThreshold = 0.05f;

        [Header("Remote Smoothing")]
        [SerializeField, Tooltip("Interpolation delay in seconds.")]
        private float interpolationDelay = 0.16f;
        [SerializeField, Tooltip("Maximum extrapolation time in seconds.")]
        private float extrapolationLimit = 0.25f;
        [SerializeField, Tooltip("Snap remote player when visual error is above this distance.")]
        private float snapDistance = 4f;

        [Header("Diagnostics")]
        [SerializeField]
        private bool dontDestroyOnLoad = true;
        [SerializeField]
        private bool debugLogs = true;

        private readonly Queue<NetworkIncomingMessage> incomingMessages = new Queue<NetworkIncomingMessage>();
        private readonly Queue<string> threadWarnings = new Queue<string>();
        private readonly object incomingLock = new object();
        private readonly object warningLock = new object();
        private readonly Dictionary<string, NetworkPlayerView> playersById = new Dictionary<string, NetworkPlayerView>();

        private UdpClient udpClient;
        private IPEndPoint serverEndPoint;
        private Thread receiveThread;
        private volatile bool receiveLoopRunning;
        private bool isConnecting;
        private bool isConnected;
        private float connectStartedAt;
        private float lastPacketReceivedAt;
        private float nextHeartbeatAt;
        private ulong sequence = 1;
        private ulong lastServerSequence;
        private int localPlayerId;
        private string resumeToken;
        private string activeRoomId;
        private Transform overrideSpawnPoint;
        private LocalNetworkPlayer localPlayer;
        private NetworkAnimatorSync localAnimatorSync;
        private bool portalJoinInProgress;

        public bool IsConnected => isConnected;
        public bool IsConnecting => isConnecting;
        public int LocalPlayerId => localPlayerId;
        public string RoomId => activeRoomId;
        public IReadOnlyDictionary<string, NetworkPlayerView> PlayersById => playersById;
        public float InterpolationDelay => interpolationDelay;
        public float ExtrapolationLimit => extrapolationLimit;
        public float SnapDistance => snapDistance;
        public float PositionThreshold => positionThreshold;
        public float RotationThreshold => rotationThreshold;
        public float VelocityThreshold => velocityThreshold;
        public float SendTransformInterval => 1f / Mathf.Max(1f, sendTransformRate);

        public event Action Connected;
        public event Action<string> RoomJoined;
        public event Action Disconnected;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            activeRoomId = roomId;

            if (dontDestroyOnLoad)
                DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            DrainIncomingMessages();
            DrainThreadWarnings();

            if (isConnecting && Time.unscaledTime - connectStartedAt > connectionTimeout)
            {
                LogWarning("Connection timeout.");
                if (reconnectEnabled)
                    Reconnect();
                else
                    Disconnect();
            }

            if (isConnected && Time.unscaledTime - lastPacketReceivedAt > connectionTimeout * 3f)
            {
                LogWarning("Server receive timeout.");
                if (reconnectEnabled)
                    Reconnect();
                else
                    Disconnect();
            }

            if (isConnected && Time.unscaledTime >= nextHeartbeatAt)
            {
                SendHeartbeat();
                nextHeartbeatAt = Time.unscaledTime + 3f;
            }

            SendLocalTransform();
            SendAnimationState();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;

            Disconnect();
        }

        public void Connect()
        {
            Connect(null, null);
        }

        public void Connect(string targetRoomId, Transform spawnPoint)
        {
            if (isConnected || isConnecting)
                return;

            if (playerPrefab == null)
                LogWarning("Player prefab is not assigned. Existing scene player will be used if found.");

            if (!string.IsNullOrEmpty(targetRoomId))
                activeRoomId = targetRoomId;

            overrideSpawnPoint = spawnPoint;
            serverEndPoint = new IPEndPoint(IPAddress.Parse(ResolveHost(serverHost)), serverPort);
            udpClient = new UdpClient();
            receiveLoopRunning = true;
            receiveThread = new Thread(ReceiveLoop) { IsBackground = true, Name = "MineArena UDP Receive" };
            receiveThread.Start();

            isConnecting = true;
            connectStartedAt = Time.unscaledTime;
            lastPacketReceivedAt = Time.unscaledTime;

            var request = new ConnectRequest
            {
                name = string.IsNullOrEmpty(playerName) ? SystemInfo.deviceName : playerName,
                roomId = activeRoomId,
                resumeToken = resumeToken,
                customization = CustomizationDto.From(customization)
            };

            Send(NetworkMessageTypes.Connect, request, false);
            Log("Connect sent to " + serverHost + ":" + serverPort + ", room=" + activeRoomId);
        }

        public void ConnectThroughPortal(string targetRoomId, string arenaSceneName, string spawnPointName, Transform fallbackSpawnPoint)
        {
            if (portalJoinInProgress)
                return;

            StartCoroutine(ConnectThroughPortalRoutine(targetRoomId, arenaSceneName, spawnPointName, fallbackSpawnPoint));
        }

        public void Disconnect()
        {
            if (udpClient != null && (isConnected || isConnecting))
            {
                Send(NetworkMessageTypes.Disconnect, new ErrorMessage { code = "client_disconnect", message = "Client disconnect." }, true);
            }

            receiveLoopRunning = false;

            if (udpClient != null)
            {
                udpClient.Close();
                udpClient = null;
            }

            isConnected = false;
            isConnecting = false;
            localPlayerId = 0;
            lastServerSequence = 0;

            foreach (var pair in new List<KeyValuePair<string, NetworkPlayerView>>(playersById))
            {
                if (pair.Value != null && !pair.Value.IsLocalPlayer)
                    Destroy(pair.Value.gameObject);
            }

            playersById.Clear();
            PvpZone.ClearAllPlayers();
            Disconnected?.Invoke();
            Log("Disconnected.");
        }

        public void JoinRoom(string targetRoomId)
        {
            if (string.IsNullOrEmpty(targetRoomId))
            {
                LogWarning("JoinRoom ignored: room id is empty.");
                return;
            }

            activeRoomId = targetRoomId;

            if (!isConnected)
            {
                LogWarning("JoinRoom queued as active room. Connect first.");
                return;
            }

            Send(NetworkMessageTypes.JoinRoom, new JoinRoomRequest { roomId = activeRoomId }, true);
            Log("JoinRoom sent: " + activeRoomId);
        }

        private IEnumerator ConnectThroughPortalRoutine(
            string targetRoomId,
            string arenaSceneName,
            string spawnPointName,
            Transform fallbackSpawnPoint)
        {
            portalJoinInProgress = true;

            Transform resolvedSpawn = fallbackSpawnPoint;
            if (!string.IsNullOrEmpty(arenaSceneName))
            {
                var loaded = false;
                yield return LoadArenaSceneRoutine(arenaSceneName, success => loaded = success);
                if (!loaded)
                {
                    portalJoinInProgress = false;
                    yield break;
                }

                yield return null;
                resolvedSpawn = ResolveSpawnPoint(spawnPointName) ?? fallbackSpawnPoint;
            }

            if (!isConnected && !isConnecting)
                Connect(targetRoomId, resolvedSpawn);

            var startedAt = Time.unscaledTime;
            while (!isConnected && Time.unscaledTime - startedAt < connectionTimeout)
                yield return null;

            if (!isConnected)
            {
                LogWarning("Portal connect timeout for room " + targetRoomId);
                portalJoinInProgress = false;
                yield break;
            }

            if (RoomId != targetRoomId)
                JoinRoom(targetRoomId);

            Log("Portal joined room " + targetRoomId + " in scene " + SceneManager.GetActiveScene().name);
            portalJoinInProgress = false;
        }

        private IEnumerator LoadArenaSceneRoutine(string sceneName, Action<bool> completed)
        {
            if (SceneManager.GetActiveScene().name == sceneName)
            {
                completed(true);
                yield break;
            }

            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                LogWarning("Cannot load arena scene '" + sceneName + "'. Add it to Build Settings or check the scene name.");
                completed(false);
                yield break;
            }

            Log("Loading arena scene " + sceneName);
            AsyncOperation operation;
            try
            {
                operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            }
            catch (Exception exception)
            {
                LogWarning("Scene load failed: " + exception.Message);
                completed(false);
                yield break;
            }

            if (operation == null)
            {
                LogWarning("Scene load failed: LoadSceneAsync returned null for " + sceneName);
                completed(false);
                yield break;
            }

            while (!operation.isDone)
                yield return null;

            Log("Arena scene loaded: " + sceneName);
            completed(true);
        }

        private Transform ResolveSpawnPoint(string spawnPointName)
        {
            if (string.IsNullOrEmpty(spawnPointName))
                return null;

            var spawnObject = GameObject.Find(spawnPointName);
            if (spawnObject == null)
            {
                LogWarning("Spawn point '" + spawnPointName + "' was not found in scene " + SceneManager.GetActiveScene().name);
                return null;
            }

            return spawnObject.transform;
        }

        public void SendLocalTransform()
        {
            if (!isConnected || localPlayer == null)
                return;

            if (localPlayer.TryBuildTransformUpdate(out var update))
                Send(NetworkMessageTypes.TransformUpdate, update, true);
        }

        public void SendAnimationState()
        {
            if (!isConnected || localAnimatorSync == null)
                return;

            if (localAnimatorSync.TryBuildAnimationUpdate(out var update))
                Send(NetworkMessageTypes.AnimationUpdate, update, true);
        }

        public void SendCustomization()
        {
            if (!isConnected)
                return;

            Send(NetworkMessageTypes.CustomizationUpdate, new CustomizationUpdate
            {
                customization = CustomizationDto.From(customization)
            }, true);
        }

        public void SendDamageRequest(int targetPlayerId, int damage, string weaponId, Vector3 hitPoint)
        {
            if (!isConnected || !PvpZone.CanDamage(localPlayerId, targetPlayerId))
                return;

            Send(NetworkMessageTypes.DamageRequest, new DamageRequest
            {
                targetPlayerId = targetPlayerId,
                damage = damage,
                weaponId = weaponId,
                attackId = Guid.NewGuid().ToString("N"),
                hitPoint = new NetworkVector3Dto(hitPoint),
                clientTimeUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            }, true);
        }

        public void HandleMessage(NetworkIncomingMessage message)
        {
            if (message == null || message.header == null)
                return;

            lastPacketReceivedAt = Time.unscaledTime;
            lastServerSequence = Math.Max(lastServerSequence, message.header.sequence);

            switch (message.MessageType)
            {
                case NetworkMessageType.ConnectResponse:
                    HandleConnectResponse(NetworkJson.ReadPayload<ConnectResponse>(message));
                    break;
                case NetworkMessageType.RoomJoined:
                    HandleRoomJoined(NetworkJson.ReadPayload<RoomJoinedMessage>(message));
                    break;
                case NetworkMessageType.RoomSnapshot:
                    HandleRoomSnapshot(NetworkJson.ReadPayload<RoomSnapshotMessage>(message));
                    break;
                case NetworkMessageType.PlayerJoined:
                    HandlePlayerJoined(NetworkJson.ReadPayload<PlayerPresenceMessage>(message));
                    break;
                case NetworkMessageType.PlayerLeft:
                    HandlePlayerLeft(NetworkJson.ReadPayload<PlayerLeftMessage>(message));
                    break;
                case NetworkMessageType.TransformCorrection:
                    HandleTransformCorrection(NetworkJson.ReadPayload<TransformCorrectionMessage>(message));
                    break;
                case NetworkMessageType.AnimationUpdate:
                    HandleAnimationUpdate(NetworkJson.ReadPayload<AnimationUpdate>(message));
                    break;
                case NetworkMessageType.CustomizationUpdate:
                    HandleCustomizationUpdate(NetworkJson.ReadPayload<PlayerPresenceMessage>(message));
                    break;
                case NetworkMessageType.DamageEvent:
                    HandleDamageEvent(NetworkJson.ReadPayload<DamageEventMessage>(message));
                    break;
                case NetworkMessageType.DeathEvent:
                    HandleDeathEvent(NetworkJson.ReadPayload<DeathEventMessage>(message));
                    break;
                case NetworkMessageType.RespawnEvent:
                    HandleRespawnEvent(NetworkJson.ReadPayload<RespawnEventMessage>(message));
                    break;
                case NetworkMessageType.Pong:
                    break;
                case NetworkMessageType.Heartbeat:
                    SendHeartbeat();
                    break;
                case NetworkMessageType.Error:
                    var error = NetworkJson.ReadPayload<ErrorMessage>(message);
                    LogWarning("Server error: " + error.code + " " + error.message);
                    break;
                default:
                    Log("Unhandled message: " + message.header.type);
                    break;
            }
        }

        public GameObject SpawnLocalPlayer()
        {
            GameObject playerObject = null;
            if (Player.Instance != null)
                playerObject = Player.Instance.gameObject;

            if (playerObject == null)
                playerObject = GameObject.FindGameObjectWithTag("Player");

            if (playerObject == null && playerPrefab != null)
            {
                var spawn = overrideSpawnPoint != null ? overrideSpawnPoint : localPlayerSpawnPoint;
                var position = spawn != null ? spawn.position : Vector3.zero;
                var rotation = spawn != null ? spawn.rotation : Quaternion.identity;
                playerObject = Instantiate(playerPrefab, position, rotation);
            }

            if (playerObject == null)
            {
                LogWarning("Cannot spawn or bind local player: no prefab and no scene player.");
                return null;
            }

            MoveLocalPlayerToSpawnIfNeeded(playerObject);

            localPlayer = playerObject.GetComponent<LocalNetworkPlayer>();
            if (localPlayer == null)
                localPlayer = playerObject.AddComponent<LocalNetworkPlayer>();

            localPlayer.Bind(this, localPlayerId);

            var view = playerObject.GetComponent<NetworkPlayerView>();
            if (view == null)
                view = playerObject.AddComponent<NetworkPlayerView>();

            view.BindLocal(localPlayerId);
            playersById[localPlayerId.ToString()] = view;

            localAnimatorSync = playerObject.GetComponent<NetworkAnimatorSync>();
            if (localAnimatorSync == null)
                localAnimatorSync = playerObject.AddComponent<NetworkAnimatorSync>();

            localAnimatorSync.Bind(this, localPlayerId, true);
            return playerObject;
        }

        private void MoveLocalPlayerToSpawnIfNeeded(GameObject playerObject)
        {
            if (!moveExistingLocalPlayerToSpawn)
                return;

            var spawn = overrideSpawnPoint != null ? overrideSpawnPoint : localPlayerSpawnPoint;
            if (spawn == null)
                return;

            var characterController = playerObject.GetComponent<CharacterController>();
            if (characterController != null)
                characterController.enabled = false;

            playerObject.transform.SetPositionAndRotation(spawn.position, spawn.rotation);

            if (characterController != null)
                characterController.enabled = true;
        }

        public NetworkPlayerView SpawnRemotePlayer(PlayerSnapshotDto snapshot)
        {
            if (snapshot == null || snapshot.playerId == localPlayerId)
                return null;

            var key = snapshot.playerId.ToString();
            if (playersById.TryGetValue(key, out var existing) && existing != null)
            {
                existing.ApplySnapshot(snapshot, false);
                return existing;
            }

            if (playerPrefab == null)
            {
                LogWarning("Cannot spawn remote player " + snapshot.playerId + ": playerPrefab is not assigned.");
                return null;
            }

            var remoteObject = InstantiateRemotePrefab(snapshot);
            var view = remoteObject.GetComponent<NetworkPlayerView>();
            if (view == null)
                view = remoteObject.AddComponent<NetworkPlayerView>();

            view.BindRemote(snapshot.playerId, interpolationDelay, extrapolationLimit, snapDistance);
            view.ApplySnapshot(snapshot, true);

            var animatorSync = remoteObject.GetComponent<NetworkAnimatorSync>();
            if (animatorSync == null)
                animatorSync = remoteObject.AddComponent<NetworkAnimatorSync>();

            animatorSync.Bind(this, snapshot.playerId, false);

            var healthSync = remoteObject.GetComponent<NetworkHealthSync>();
            if (healthSync == null)
                healthSync = remoteObject.AddComponent<NetworkHealthSync>();

            healthSync.Bind(view);
            playersById[key] = view;
            Log("Remote player spawned: " + snapshot.playerId);
            return view;
        }

        public void DespawnRemotePlayer(string playerId)
        {
            if (string.IsNullOrEmpty(playerId))
                return;

            if (!playersById.TryGetValue(playerId, out var view))
                return;

            playersById.Remove(playerId);
            if (view != null)
                PvpZone.RemovePlayer(view.PlayerId);
            if (view != null && !view.IsLocalPlayer)
                Destroy(view.gameObject);

            Log("Remote player despawned: " + playerId);
        }

        private void HandleConnectResponse(ConnectResponse response)
        {
            localPlayerId = response.playerId;
            resumeToken = response.resumeToken;
            activeRoomId = response.roomId;
            isConnecting = false;
            isConnected = true;
            nextHeartbeatAt = Time.unscaledTime + 3f;

            SpawnLocalPlayer();
            Connected?.Invoke();
            Log("Connected: playerId=" + localPlayerId + ", room=" + activeRoomId);
        }

        private void HandleRoomJoined(RoomJoinedMessage message)
        {
            if (message == null)
                return;

            activeRoomId = message.roomId;
            ApplyInitialPlayers(message.players);
            RoomJoined?.Invoke(activeRoomId);
            Log("Room joined: " + activeRoomId);
        }

        private void HandleRoomSnapshot(RoomSnapshotMessage message)
        {
            if (message == null)
                return;

            ApplyInitialPlayers(message.players);
        }

        private void ApplyInitialPlayers(PlayerSnapshotDto[] players)
        {
            if (players == null)
                return;

            for (var i = 0; i < players.Length; i++)
                ApplyPlayerSnapshot(players[i]);
        }

        private void ApplyPlayerSnapshot(PlayerSnapshotDto snapshot)
        {
            if (snapshot == null)
                return;

            if (snapshot.playerId == localPlayerId)
            {
                if (!playersById.TryGetValue(localPlayerId.ToString(), out var localView) || localView == null)
                    SpawnLocalPlayer();
                else
                    localView.ApplySnapshot(snapshot, true);
                return;
            }

            var view = SpawnRemotePlayer(snapshot);
            if (view != null)
                view.ApplySnapshot(snapshot, false);
        }

        private void HandlePlayerJoined(PlayerPresenceMessage message)
        {
            if (message == null || message.player == null || message.player.playerId == localPlayerId)
                return;

            SpawnRemotePlayer(message.player);
        }

        private void HandlePlayerLeft(PlayerLeftMessage message)
        {
            if (message == null)
                return;

            DespawnRemotePlayer(message.playerId.ToString());
        }

        private void HandleTransformCorrection(TransformCorrectionMessage message)
        {
            if (message == null || message.transform == null || localPlayer == null)
                return;

            localPlayer.ApplyServerCorrection(message.transform);
            LogWarning("Transform correction: " + message.reason);
        }

        private void HandleAnimationUpdate(AnimationUpdate message)
        {
            if (message == null || message.playerId == 0 || message.playerId == localPlayerId)
                return;

            if (playersById.TryGetValue(message.playerId.ToString(), out var view) && view != null)
                view.ApplyAnimation(message);
        }

        private void HandleCustomizationUpdate(PlayerPresenceMessage message)
        {
            if (message == null || message.player == null)
                return;

            ApplyPlayerSnapshot(message.player);
        }

        private void HandleDamageEvent(DamageEventMessage message)
        {
            if (message == null)
                return;

            if (playersById.TryGetValue(message.targetPlayerId.ToString(), out var view) && view != null)
                view.ApplyHealth(message.healthAfter, message.healthAfter > 0);

            Log("Damage: attacker=" + message.attackerPlayerId + ", target=" + message.targetPlayerId + ", damage=" + message.damage);
        }

        private void HandleDeathEvent(DeathEventMessage message)
        {
            if (message == null)
                return;

            if (playersById.TryGetValue(message.playerId.ToString(), out var view) && view != null)
                view.ApplyDeath(message.killerPlayerId);
        }

        private void HandleRespawnEvent(RespawnEventMessage message)
        {
            if (message == null)
                return;

            if (playersById.TryGetValue(message.playerId.ToString(), out var view) && view != null)
                view.ApplyRespawn(message.transform, message.health);
        }

        private GameObject InstantiateRemotePrefab(PlayerSnapshotDto snapshot)
        {
            var wasActive = playerPrefab.activeSelf;
            if (wasActive)
                playerPrefab.SetActive(false);

            var position = snapshot.transform != null ? snapshot.transform.Position : Vector3.zero;
            var rotation = snapshot.transform != null ? snapshot.transform.Rotation : Quaternion.identity;
            var remoteObject = Instantiate(playerPrefab, position, rotation);

            if (wasActive)
                playerPrefab.SetActive(true);

            StripLocalOnlyComponents(remoteObject);
            remoteObject.name = "RemotePlayer_" + snapshot.playerId;
            remoteObject.SetActive(true);
            return remoteObject;
        }

        private void StripLocalOnlyComponents(GameObject remoteObject)
        {
            var playerSingleton = remoteObject.GetComponent<Player>();
            if (playerSingleton != null)
                DestroyImmediate(playerSingleton);

            DestroyLocalOnlyComponent<PlayerAttack>(remoteObject);
            DestroyLocalOnlyComponent<PlayerEquipment>(remoteObject);
            DisableBehaviour<PlayerMovement>(remoteObject);
            DisableBehaviour<RotationController>(remoteObject);

            var cameras = remoteObject.GetComponentsInChildren<Camera>(true);
            for (var i = 0; i < cameras.Length; i++)
                cameras[i].enabled = false;

            var listeners = remoteObject.GetComponentsInChildren<AudioListener>(true);
            for (var i = 0; i < listeners.Length; i++)
                listeners[i].enabled = false;
        }

        private static void DisableBehaviour<T>(GameObject target) where T : Behaviour
        {
            var components = target.GetComponentsInChildren<T>(true);
            for (var i = 0; i < components.Length; i++)
                components[i].enabled = false;
        }

        private static void DestroyLocalOnlyComponent<T>(GameObject target) where T : Component
        {
            var components = target.GetComponentsInChildren<T>(true);
            for (var i = 0; i < components.Length; i++)
                DestroyImmediate(components[i]);
        }

        private void SendHeartbeat()
        {
            Send(NetworkMessageTypes.Heartbeat, new PingMessage
            {
                clientTimeUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            }, true);
        }

        private void Send<TPayload>(string type, TPayload payload, bool includePlayerId)
        {
            if (udpClient == null || serverEndPoint == null)
                return;

            try
            {
                var json = NetworkJson.SerializeEnvelope(
                    type,
                    sequence++,
                    lastServerSequence,
                    includePlayerId ? localPlayerId : 0,
                    activeRoomId,
                    payload);
                var bytes = Encoding.UTF8.GetBytes(json);
                udpClient.Send(bytes, bytes.Length, serverEndPoint);
            }
            catch (Exception exception)
            {
                LogWarning("Send failed: " + exception.Message);
            }
        }

        private void ReceiveLoop()
        {
            var any = new IPEndPoint(IPAddress.Any, 0);
            while (receiveLoopRunning)
            {
                try
                {
                    var bytes = udpClient.Receive(ref any);
                    var json = Encoding.UTF8.GetString(bytes);

                    if (!NetworkJson.TryParseIncoming(json, out var message, out var error))
                    {
                        LogWarningThreadSafe("Bad packet: " + error);
                        continue;
                    }

                    lock (incomingLock)
                        incomingMessages.Enqueue(message);
                }
                catch (SocketException)
                {
                    if (receiveLoopRunning)
                        LogWarningThreadSafe("UDP socket error.");
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (Exception exception)
                {
                    if (receiveLoopRunning)
                        LogWarningThreadSafe("Receive failed: " + exception.Message);
                }
            }
        }

        private void DrainIncomingMessages()
        {
            while (true)
            {
                NetworkIncomingMessage message = null;
                lock (incomingLock)
                {
                    if (incomingMessages.Count > 0)
                        message = incomingMessages.Dequeue();
                }

                if (message == null)
                    break;

                HandleMessage(message);
            }
        }

        private void DrainThreadWarnings()
        {
            while (true)
            {
                string warning = null;
                lock (warningLock)
                {
                    if (threadWarnings.Count > 0)
                        warning = threadWarnings.Dequeue();
                }

                if (warning == null)
                    break;

                LogWarning(warning);
            }
        }

        private void Reconnect()
        {
            var targetRoom = activeRoomId;
            var spawn = overrideSpawnPoint;
            Disconnect();
            activeRoomId = targetRoom;
            Connect(targetRoom, spawn);
        }

        private static string ResolveHost(string host)
        {
            if (IPAddress.TryParse(host, out var ipAddress))
                return ipAddress.ToString();

            var addresses = Dns.GetHostAddresses(host);
            if (addresses == null || addresses.Length == 0)
                throw new InvalidOperationException("Cannot resolve host: " + host);

            return addresses[0].ToString();
        }

        private void Log(string message)
        {
            if (debugLogs)
                Debug.Log("[NetworkClient] " + message);
        }

        private void LogWarning(string message)
        {
            Debug.LogWarning("[NetworkClient] " + message);
        }

        private void LogWarningThreadSafe(string message)
        {
            lock (warningLock)
                threadWarnings.Enqueue(message);
        }
    }
}
