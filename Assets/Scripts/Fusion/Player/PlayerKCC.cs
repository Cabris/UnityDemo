using Fusion;
using UnityEngine;
using Fusion.Addons.SimpleKCC;
using static UnityDemo.AnimationController;
using System;
using UnityEngine.UIElements;

namespace UnityDemo
{
    [RequireComponent(typeof(SimpleKCC))]
    public class PlayerKCC : NetworkBehaviour
    {
        [Header("Controller References")]
        public SimpleKCC KCC;
        public CameraController _cameraController;
        public AnimationController _animationController;
        public AudioController _audioController;
        public PlayerInputCollector _playerInputCollector;
        public EquipmentController _equipmentController;
        public DamageController _damageController;

        private WeaponMovementController _weaponController = new WeaponMovementController();
        private MovementController _movementController = new MovementController();

        [SerializeField]
        private PlayerControllerProperties properties = new PlayerControllerProperties();
        [SerializeField]
        private PlayerNetworkModel _model;

        [SerializeField] private Transform _aimTarget;
        [SerializeField] private TMPro.TextMeshProUGUI _playNameText;

        private bool _hasStateAuthority;
        private bool _hasInputAuthority;
        private Vector3 SpawnedPosition = default;

        private void Start()
        {
            if (_model == null)
                _model = GetComponent<PlayerNetworkModel>();
            if (_model == null)
            {
                Debug.LogError("PlayerNetworkModel not found on " + gameObject.name);
                return;
            }
            if (_model.IsInitialized)
                Initialize();
            else
                _model.EventDispacher.OnInitialized += Initialize;
        }

        private void Initialize()
        {
            _model.EventDispacher.OnInitialized -= Initialize;

            _model.EventDispacher.OnPlayerNameChanged += OnPlayerNameChanged;
            _model.EventDispacher.OnCurrentArmedTypeChanged += OnCurrentArmedStateChanged;
            _model.EventDispacher.OnAimAtPositionChanged += OnAimAtPositionChanged;

            _model.Movement.LookRotationEuler = KCC.GetLookRotation();
            _animationController.Initialize(properties);
            _equipmentController.Initialize(_model);
            _weaponController.Initialize(transform, _model, properties);
            var events = Runner.GetComponent<NetworkEvents>();
            _playerInputCollector.Initialize(_hasInputAuthority, events);
            _movementController.Initialize(properties, KCC, _model);
            _damageController.Initialized(_model);
            _damageController.OnKill += OnKill;
            _weaponController.OnAimAtPositionChanged += OnAimAtPosition;
            _cameraController.OnLookRotationEulerChanged += OnLookRotationEulerChanged;


            if (_hasInputAuthority)//proxy in client
            {
                _cameraController.SetupCameraControl();
                _cameraController.SetCameraMode(CameraController.CameraMode.Follow);
                GameManager.Instance.OnControlPlayerInitialize(_model);
                _model.EventDispacher.PlayerControlChanged(true);
            }

            OnPlayerNameChanged(_model.NT_playerName);
        }

        private void OnKill(DamageInfo damageInfo)
        {
            KCC.SetPosition(SpawnedPosition);
            string msg = $"Player: {_model.PlayerConditions.NT_playerName} killed by PlayerID: {damageInfo.damageData.Source} ," +
           $" hit in {damageInfo.damagePart.Name}";

            //RPC_SendMessage(msg);
            RPC_RelayMessage(msg, Runner.LocalPlayer);
        }

        #region RPC_Test

        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority, HostMode = RpcHostMode.SourceIsHostPlayer)]
        public void RPC_SendMessage(string message, RpcInfo info = default)
        {
            RPC_RelayMessage(message, info.Source);
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All, HostMode = RpcHostMode.SourceIsServer)]
        public void RPC_RelayMessage(string message, PlayerRef messageSource)
        {
            //if (messageSource == Runner.LocalPlayer)
            //{
            //    message = $"You said: {message}";
            //}
            //else
            //{
            //    message = $"Some other player said: {message}";
            //}

            SimpleLogger.Log($"RPC_RelayMessage: {message}");
        }

        #endregion

        public override void Spawned()
        {
            _hasStateAuthority = HasStateAuthority;
            _hasInputAuthority = HasInputAuthority;

            if (_hasStateAuthority)//proxy in host
            {
                SpawnedPosition = KCC.Position;
            }
        }

        public override void FixedUpdateNetwork()
        {
            if (!_model.IsInitialized)
                return;
            if (GetInput(out NetworkInputData newInput))
            {
                NetworkButtons buttons = newInput.buttons;
                if (_hasStateAuthority)//排除掉client player,根據input計算出model值的部分
                {
                    _movementController.HandleMovementInput(newInput, Runner.DeltaTime);
                    _weaponController.HandleWeaponInput(Object.Id, newInput);
                }
                _movementController.HandleJumpInput(newInput);
                _model.PreviousButtons = buttons;
            }
            if (_hasStateAuthority)
            {
                if (KCC.Transform.position.y < -100)//reset position if player fall off the map
                    KCC.SetPosition(SpawnedPosition);
                _equipmentController.UpdateWeaponEquipment(Runner.DeltaTime);
                _damageController.UpdateHealth(Runner.DeltaTime);
            }
            _movementController.UpdateMovement();
        }

        public override void Render()
        {
            Vector3 velocityLocal = KCC.Transform.InverseTransformDirection(KCC.RealVelocity);
            //struct to hold animation parameters
            ref PlayerMovementState mSate = ref _model.Movement;

            AnimationParams @params = new AnimationParams()
            {
                LocalVelocity = velocityLocal,
                IsGrounded = KCC.IsGrounded,
                IsStrafe = mSate.IsStrafe,
                TurningSpeed = mSate.TurningSpeed,
                IsSprint = mSate.IsSprint,
                IsJump = mSate.IsJump
            };
            _animationController.SetAnimationParams(in @params);
        }

        private void LateUpdate()
        {
            //only update camera rotation if this player has input authority (the client who control this player)
            if (_hasInputAuthority)
            {
                //local result for smooth camera rotation
                var lookDelta = _playerInputCollector._lookDelta;
                var isMouse = _playerInputCollector.IsCurrentDeviceMouse;
                _cameraController.UpdateCameraRotation(lookDelta, isMouse, Time.deltaTime);
            }
        }

        private void Update()
        {
            if (_hasInputAuthority)
            {
                _weaponController.UpdateAimPosition(Time.deltaTime);
            }
        }

        private void OnLookRotationEulerChanged(Vector2 lookRotationEuler)
        {
            _playerInputCollector.CachedInputData.lookRotationEuler = lookRotationEuler;
        }

        private void OnAimAtPosition(Vector3 targetPos)
        {
            _playerInputCollector.CachedInputData.aimAtPosition = targetPos;
        }

        private void OnAimAtPositionChanged(Vector3 targetPos)
        {
            if (_model.NT_CurrentArmedState == ArmedType.Aiming)
            {
                _aimTarget.position = _model.NT_AimAtPosition;
            }
        }

        public void OnFootstep(AnimationEvent animationEvent)
        {
            _audioController.PlayFootstep(animationEvent, transform.position);
        }

        public void OnLand(AnimationEvent animationEvent)
        {
            _audioController.PlayLanding(animationEvent, transform.position);
        }

        private void OnPlayerNameChanged(string name)
        {
            if (_playNameText != null)
                _playNameText.text = name;
        }

        private void OnCurrentArmedStateChanged(ArmedType armedType)
        {
            if (_hasInputAuthority)
            {
                var mode = armedType == ArmedType.Aiming ? CameraController.CameraMode.Aiming : CameraController.CameraMode.Follow;
                _cameraController.SetCameraMode(mode);
            }
        }

        private void OnDestroy()
        {
            _cameraController.OnLookRotationEulerChanged -= OnLookRotationEulerChanged;
            _weaponController.OnAimAtPositionChanged -= OnAimAtPosition;
            _model.EventDispacher.OnPlayerNameChanged -= OnPlayerNameChanged;
            _model.EventDispacher.OnCurrentArmedTypeChanged -= OnCurrentArmedStateChanged;
            _model.EventDispacher.OnAimAtPositionChanged -= OnAimAtPositionChanged;
            _damageController.OnKill -= OnKill;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_hasStateAuthority && other.CompareTag("Weapon") && other.TryGetComponent(out IWeapon weapon))
            {
                _equipmentController.AddWeaponToInventory(weapon);
            }
        }
    }

}
