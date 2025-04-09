using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Fusion;
using Fusion.Sockets;
using System.Text;
using Cysharp.Threading.Tasks;

namespace UnityDemo
{
    public class GameManager : MonoBehaviour, INetworkRunnerCallbacks
    {
        [SerializeField]
        private NetworkPrefabRef _playerPrefab;
        [SerializeField]
        Animator _menuAC;

        private Dictionary<PlayerRef, NetworkObject> _spawnedCharacters = new Dictionary<PlayerRef, NetworkObject>();
        public NetworkRunner _runner;
        private INetworkSceneManager _networkSceneManager;
        private int _animIDisConnected;

        static GameManager _instance;
        public static GameManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = FindFirstObjectByType<GameManager>();
                return _instance;
            }
        }

        private void Awake()
        {
            _animIDisConnected = Animator.StringToHash("isConnected");
        }

        public void OnSatrtGameAutoHostOrClient()
        {
            StartGame(GameMode.AutoHostOrClient);
        }

        public void OnSatrtGameHostClicked()
        {
            StartGame(GameMode.Host);
        }

        public void OnSatrtGameClientClicked()
        {
            StartGame(GameMode.Client);
        }

        async void StartGame(GameMode mode)
        {
            _runner = gameObject.AddComponent<NetworkRunner>();
            _runner.ProvideInput = true;

            NetworkSceneInfo sceneInfo = new NetworkSceneInfo();
            //SceneRef sceneEntry = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex);
            //if (sceneEntry.IsValid)
            //    sceneInfo.AddSceneRef(sceneEntry, LoadSceneMode.Additive);

            SceneRef scenePlay = SceneRef.FromIndex(2);
            if (scenePlay.IsValid)
                sceneInfo.AddSceneRef(scenePlay, LoadSceneMode.Additive);

            _networkSceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>();
            // Start or join (depends on gamemode) a session with a specific name
            var ret = await _runner.StartGame(new StartGameArgs()
            {
                GameMode = mode,
                SessionName = "TestRoom",
                Scene = sceneInfo,
                SceneManager = _networkSceneManager
            });
            SimpleLogger.Log($"StartGame: StartGame: {ret.Ok}");
            _menuAC.SetBool(_animIDisConnected, true);
        }

        public void OnShutdownGameClicked()
        {
            if (_runner == null)
            {
                DebugStrAppendLine($"StartGame Failed, can not get NetworkRunner");
                return;
            }

            _runner.Shutdown();
            Destroy(_runner);
            _runner = null;
            _spawnedCharacters.Clear();
            _menuAC.SetBool(_animIDisConnected, false);
        }

        public void OnInput(NetworkRunner runner, NetworkInput input)
        {
            // SimpleLogger.Log($"OnInput: input = {input}");
        }

        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
        {
            DebugStrAppendLine($"OnPlayerJoined: runner.IsServer = {runner.IsServer} ,player = {player.PlayerId}");

            if (runner.IsServer)
            {
                // Create a unique position for the player
                Vector3 spawnPosition = new Vector3((player.RawEncoded % runner.Config.Simulation.PlayerCount) * 3, 1, 0);
                NetworkObject networkPlayerObject = runner.Spawn(_playerPrefab, spawnPosition, Quaternion.identity, player);
                networkPlayerObject.GetComponent<PlayerNetworkModel>().NT_playerName = "Player_" + player.PlayerId.ToString();
                // Keep track of the player avatars for easy access
                _spawnedCharacters.Add(player, networkPlayerObject);
            }
        }

        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
        {
            DebugStrAppendLine($"OnPlayerLeft: runner.IsServer = {runner.IsServer} ,player = {player.PlayerId}");

            if (_spawnedCharacters.TryGetValue(player, out NetworkObject networkObject))
            {
                runner.Despawn(networkObject);
                _spawnedCharacters.Remove(player);
            }
        }

        public void OnSceneLoadDone(NetworkRunner runner)
        {
            DebugStrAppendLine($"OnSceneLoadDone");

        }

        public void OnSceneLoadStart(NetworkRunner runner)
        {
            DebugStrAppendLine($"OnSceneLoadStart");
        }

        public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
        {
            StringBuilder sb = new StringBuilder(500);
            for (int i = 0; i < sessionList.Count; i++)
            {
                var s = sessionList[i];
                sb.AppendLine($"Name = {s.Name}, PlayerCount = {s.PlayerCount}, IsOpen = {s.IsOpen}, IsValid = {s.IsValid}, IsVisible = {s.IsVisible}");
            }
            DebugStrAppendLine($"OnSessionListUpdated: sessionList: {sb.ToString()}");

        }

        public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
        {
            DebugStrAppendLine($"OnShutdown: shutdownReason = {shutdownReason}");

        }

        private void DebugStrAppendLine(string v)
        {
            SimpleLogger.Log(v);
        }

        #region

        public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
        {
            //DebugStrAppendLine($"OnInputMissing: player = {player.PlayerId},input = {input}");

        }

        public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
        {
            //DebugStrAppendLine($"OnObjectEnterAOI: obj = {obj}, player = {player.PlayerId}");

        }

        public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
        {
            //DebugStrAppendLine($"OnObjectExitAOI: obj = {obj}, player = {player.PlayerId}");

        }

        public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message)
        {
            DebugStrAppendLine($"OnUserSimulationMessage: message = {message}");

        }

        public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress)
        {
            //DebugStrAppendLine($"OnReliableDataProgress: player = {player.PlayerId}, key = {key}, progress = {progress}");

        }

        public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data)
        {
            //DebugStrAppendLine($"OnReliableDataReceived: player = {player.PlayerId}, key = {key}");

        }

        public void OnConnectedToServer(NetworkRunner runner)
        {
            DebugStrAppendLine($"OnConnectedToServer: Mode = {runner.Mode}");

        }

        public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
        {
            DebugStrAppendLine($"OnConnectFailed: remoteAddress = {remoteAddress}, reason = {reason}");

        }

        public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
        {
            DebugStrAppendLine($"OnConnectRequest: RemoteAddress = {request.RemoteAddress}");

        }

        public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data)
        {
            DebugStrAppendLine($"OnCustomAuthenticationResponse");

        }

        public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
        {
            DebugStrAppendLine($"OnDisconnectedFromServer: reason = {reason}");

        }

        public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
        {
            DebugStrAppendLine($"OnHostMigration");

        }


        #endregion

    }
}