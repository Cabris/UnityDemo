using Cinemachine;
using Fusion;
using System;
using UnityEngine;
using UnityEngine.Windows;
using static Fusion.NetworkRunner;
using static UnityEngine.EventSystems.PointerEventData;

namespace UnityDemo
{
    public class Player : NetworkBehaviour, IAfterSpawned, IPlayerJoined, IPlayerLeft
    {
        [SerializeField]
        CinemachineVirtualCamera _playerVirtualCamera;
        private NetworkCharacterController _cc;

        [Networked, OnChangedRender(nameof(OnPlayerNameChanged))]
        public string PlayerName { get; set; }

        [Networked] public NetworkButtons ButtonsPrevious { get; set; }

        [SerializeField] TMPro.TMP_Text _playNameText;

        [SerializeField] private Ball _prefabBall;

        [Networked] private TickTimer _attackDelay { get; set; }

        private void Awake()
        {
            _cc = GetComponent<NetworkCharacterController>();
            SimpleLogger.Log("Player Awake");
        }
        private void OnPlayerNameChanged()
        {
            if (_playNameText != null)
                _playNameText.text = PlayerName;
        }

        private void Start()
        {
            SimpleLogger.Log("Player Start");
        }

        public override void Spawned()
        {
            base.Spawned();
            SimpleLogger.Log($"Player Spawned: HasInputAuthority = {Object.HasInputAuthority}, HasStateAuthority = {Object.HasStateAuthority}");
            if (Object.HasInputAuthority)
            {
                //controlled player
                _playerVirtualCamera = FindFirstObjectByType<CinemachineVirtualCamera>();
                if (_playerVirtualCamera == null)
                {
                    SimpleLogger.Log($"Player Spawned Error: link VirtualCamera Rrror _playerVirtualCamera = null");
                }
                else
                    SetupCameraControl();
                PlayerName = "Default_" + Runner.LocalPlayer.PlayerId.ToString();
            }
            else
            {
                //other player

            }

            if (_playNameText != null)
                _playNameText.text = PlayerName;
        }

        private void UpdateMovement(in NetworkInputData data,
            in NetworkButtons pressed, in NetworkButtons released)
        {
            //only on pressed 
            if (pressed.IsSet(PlayerInputButtons.Jump))
            {
                _cc.Jump();
            }
            Vector3 inputDirection = new Vector3(data.moveDelta.x, 0.0f, data.moveDelta.y).normalized;
            _cc.Move(5 * inputDirection * Runner.DeltaTime);
        }

        private void UpdateAttack(in NetworkButtons buttons)
        {
            //always on pressing
            if (buttons.IsSet(PlayerInputButtons.Attack) && _attackDelay.ExpiredOrNotRunning(Runner))
            {
                _attackDelay = TickTimer.CreateFromSeconds(Runner, 0.5f);
                Runner.Spawn(_prefabBall,
                transform.position + transform.forward, Quaternion.LookRotation(transform.forward),
                Object.InputAuthority, OnBeforeBallSpawned);

                void OnBeforeBallSpawned(NetworkRunner runner, NetworkObject obj)
                {
                    if (obj.TryGetComponent(out Ball ball))
                    {
                        ball.Init();
                    }
                }
            }
        }

        public override void FixedUpdateNetwork()
        {
            if (GetInput(out NetworkInputData data))
            {
                NetworkButtons buttons = data.buttons;
                NetworkButtons pressed = buttons.GetPressed(ButtonsPrevious);
                NetworkButtons released = buttons.GetReleased(ButtonsPrevious);
                ButtonsPrevious = buttons;

                if (_cc != null)
                {
                    UpdateMovement(data, pressed, released);

                    if (HasStateAuthority)//only host can spawn ball
                    {
                        UpdateAttack(buttons);
                    }
                }
            }
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            base.Despawned(runner, hasState);
            SimpleLogger.Log("Player Despawned");
        }

        private void SetupCameraControl()
        {
            var cameraTarget = Helper.SearchByTag(gameObject, "CinemachineTarget");
            if (cameraTarget == null)
            {
                SimpleLogger.Log($"Spawned SetupCameraControl Error: cameraTarget = null");
            }
            else
            {
                _playerVirtualCamera.Follow = cameraTarget.transform;
            }
        }

        public void AfterSpawned()
        {
            SimpleLogger.Log("Player AfterSpawned");
        }

        public void PlayerJoined(PlayerRef player)
        {
            SimpleLogger.Log($"Player PlayerJoined: PlayerId = {player.PlayerId}");
        }

        public void PlayerLeft(PlayerRef player)
        {
            SimpleLogger.Log($"Player PlayerLeft: PlayerId = {player.PlayerId}");
        }

        private void OnEnable()
        {
            SimpleLogger.Log("Player OnEnable");
        }

        private void OnDisable()
        {
            SimpleLogger.Log("Player OnDisable");
        }

        private void OnDestroy()
        {
            SimpleLogger.Log("Player OnDestroy");
        }
    }
}