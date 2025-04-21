using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Fusion;
using Fusion.Sockets;
using System.Text;
using Cysharp.Threading.Tasks;
using static Unity.Collections.Unicode;

namespace UnityDemo
{
    public interface IGameManager
    {
        void OnControlPlayerInitialize(IPlayerNetworkModel model);
        int GetPlayerChararctorID();
        string GetPlayerName();
        void OnOnPlayerReadyToSpawner(PlayerSpawnData data);
        NetworkRunner NetworkRunner { get; }
    }

    public class GameManager : MonoBehaviour, INetworkRunnerCallbacks, IGameManager
    {
        [SerializeField] PlayerCharactorDefines _characterDefines;
        [SerializeField] Animator _menuAC;
        [SerializeField] CharactorSelecter _charactorSelecter;
        [SerializeField] PlayerHUDController _playerHUD;
        [SerializeField] PlayerSpawner _playerSpawnerPrefab;
        private Dictionary<PlayerRef, NetworkObject> _spawnedCharacters = new Dictionary<PlayerRef, NetworkObject>();
        public NetworkRunner _runner;
        private INetworkSceneManager _networkSceneManager;
        private int _animIDisConnected;
        private const int _playSceneIndex = 2; // play scene index in build settings
        static GameManager _instance;
        public static IGameManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = FindFirstObjectByType<GameManager>();
                return _instance;
            }
        }

        public NetworkRunner NetworkRunner => _runner;

        private void Awake()
        {
            _animIDisConnected = Animator.StringToHash("isConnected");
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Q))
            {
                ShutdownGame();
            }
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

            SceneRef scenePlay = SceneRef.FromIndex(_playSceneIndex);
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
            _charactorSelecter.gameObject.SetActive(false);
        }

        public void OnShutdownGameClicked()
        {
            if (_runner == null)
            {
                DebugStrAppendLine($"StartGame Failed, can not get NetworkRunner");
                return;
            }
            ShutdownGame();
        }

        private async void ShutdownGame()
        {
            SceneRef scenePlay = SceneRef.FromIndex(_playSceneIndex);
            if (_runner.IsSceneAuthority)
                await _runner.UnloadScene(scenePlay);
            else
                await SceneManager.UnloadSceneAsync(_playSceneIndex);
            await _runner.Shutdown(false);
            Destroy(_runner);
            _runner = null;
            _spawnedCharacters.Clear();
            _menuAC.SetBool(_animIDisConnected, false);
            _charactorSelecter.gameObject.SetActive(true);
        }

        public void OnInput(NetworkRunner runner, NetworkInput input)
        {
            // SimpleLogger.Log($"OnInput: input = {input}");
        }

        public int GetPlayerChararctorID()
        {
            if (_charactorSelecter == null)
            {
                return -1;
            }
            return _charactorSelecter._selectedIndex;
        }

        public string GetPlayerName()
        {
            return "";
        }

        public void OnPlayerJoined(NetworkRunner runner, PlayerRef playerRef)
        {
            DebugStrAppendLine($"OnPlayerJoined: runner.IsServer = {runner.IsServer} ,player = {playerRef.PlayerId}");

            if (runner.IsServer)
            {
                Vector3 spawnPosition = new Vector3((playerRef.RawEncoded % runner.Config.Simulation.PlayerCount) * 3, 1, 0);
                var spawner = runner.Spawn(_playerSpawnerPrefab, spawnPosition, Quaternion.identity, playerRef);
            }
        }

        public void OnOnPlayerReadyToSpawner(PlayerSpawnData data)
        {
            Debug.Log($"OnPlayerSpawnerSpawned: PlayerId = {data.PlayerRef.PlayerId}");

            if (_runner.IsServer)
            {
                SpawnPlayer(_runner, data);
                var spawner = _runner.FindObject(data.SpawnerID);
                if (spawner == null)
                {
                    Debug.LogError($"Can not find Object with ID {data.SpawnerID}");
                    return;
                }
                _runner.Despawn(spawner);
            }
        }

        private void SpawnPlayer(NetworkRunner runner, PlayerSpawnData data)
        {
            Debug.Log($"SpawnPlayer: PlayerId = {data.PlayerRef.PlayerId}");
            var playerRef = data.PlayerRef;
            // Create a unique position for the player
            var playerPrefab = _characterDefines._playerCharactors[data.CharactorID].prefab;
            Vector3 spawnPosition = data.SpawnPosition;
            NetworkObject networkPlayerObject = runner.Spawn(playerPrefab, spawnPosition, Quaternion.identity, playerRef);
            networkPlayerObject.GetComponent<PlayerNetworkModel>().NT_playerName = data.Name.Value;
            // Keep track of the player avatars for easy access
            _spawnedCharacters.Add(playerRef, networkPlayerObject);
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

        public void OnControlPlayerInitialize(IPlayerNetworkModel model)
        {
            if (_playerHUD != null)
                _playerHUD.SetModel(model);
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