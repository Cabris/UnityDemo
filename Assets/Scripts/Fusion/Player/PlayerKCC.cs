using Fusion;
using UnityEngine;
using Fusion.Addons.SimpleKCC;
using static UnityDemo.AnimationController;
using System.Collections.Generic;

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
        public WeaponAimController _aimController;

        [SerializeField]
        private PlayerControllerProperties properties = new PlayerControllerProperties();
        [SerializeField]
        private PlayerNetworkModel _model;

        [SerializeField] private Transform _aimTarget;
        [SerializeField] private TMPro.TextMeshProUGUI _playNameText;

        private float jumpImpulse;
        private float _yawRotationSpeed = 0f;
        private float _pitchRotationSpeed = 0f;
        private bool _hasStateAuthority;
        private bool _hasInputAuthority;
        private Vector3 SpawnedPosition = default;
        private Queue<IWeapon> _toBeEquipWeapon = new Queue<IWeapon>();

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
                _model.OnInitialized += Initialize;
        }

        private void Initialize()
        {
            Debug.Log($"PlayerKCC Initialize: NT_playerName: {_model.NT_playerName}," +
            $" _hasInputAuthority:{_hasInputAuthority}, _hasStateAuthority: {_hasStateAuthority}");
            _model.OnInitialized -= Initialize;

            _model._eventDispacher.OnPlayerNameChanged += OnPlayerNameChanged;
            _model._eventDispacher.OnCurrentArmedTypeChanged += OnCurrentArmedStateChanged;
            _model.Movement.LookRotationEuler = KCC.GetLookRotation();
            _animationController.Initialize(properties);
            _equipmentController.Initialize(_model);
            _aimController.Initialize(_model);
            _aimController.OnAimAtPositionChanged += OnAimAtPosition;
            _cameraController.onLookRotationEulerChanged += OnLookRotationEulerChanged;
            OnPlayerNameChanged(_model.NT_playerName);

            if (_hasInputAuthority)//proxy in client
            {
                _cameraController.SetupCameraControl();
                _cameraController.SetCameraMode(CameraController.CameraMode.Follow);
                GameManager.Instance.OnControlPlayerInitialize(_model);
                _model.OnPlayerHasControl();
            }
        }

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
            ref PlayerMovementState mSate = ref _model.Movement;

            if (GetInput(out NetworkInputData newInput))
            {
                NetworkButtons buttons = newInput.buttons;
                if (_hasStateAuthority)//排除掉client player,根據input計算出model值的部分
                {
                    HandleMovementInput(ref mSate, newInput, _model.PreviousButtons, Runner.DeltaTime);
                    if (_model.NT_CurrentArmedState == ArmedType.Aiming)
                    {
                        _model.AimAtPosition = newInput.aimAtPosition;
                    }
                    HandleWeaponAttack(newInput, _model.PreviousButtons);
                }
                HandleJumpInput(ref mSate, newInput.buttons, _model.PreviousButtons);
                _model.PreviousButtons = buttons;
            }
            if (_hasStateAuthority)
            {
                if (KCC.Transform.position.y < -100)//reset position if player fall off the map
                    KCC.SetPosition(SpawnedPosition);
                _equipmentController.UpdateEquipmentState(Runner.DeltaTime);

                if (_toBeEquipWeapon.Count > 0)
                {
                    var weapon = _toBeEquipWeapon.Dequeue();
                    _equipmentController.EquipWeapon(weapon);
                }

            }

            // It feels better when the player falls quicker
            KCC.SetGravity(KCC.RealVelocity.y >= 0f ? properties.UpGravity : properties.DownGravity);
            KCC.SetLookRotation(mSate.LookRotationEuler);
            KCC.Move(mSate.MoveVelocity, jumpImpulse);

            if (mSate.IsJump && KCC.IsGrounded)
            {
                mSate.IsJump = false;
            }
        }

        //TODO: move to PlayerWeaponHandler
        private void HandleWeaponAttack(NetworkInputData newInput, in NetworkButtons previousButtons)
        {
            var curtentWeapon = _model.GetCurrentWeaponCached;
            if (newInput.buttons.WasPressed(previousButtons, PlayerInputButtons.Aim))
            {
                _model.IsAiming = !_model.IsAiming;
            }

            if (curtentWeapon == null || _model.NT_CurrentArmedState != ArmedType.Aiming)
            {

                return;
            }

            // Handle input for shooting
            ShootRequestData data = default;
            data.Requester = Object.Id;
            data.AimAtPosition = _model.AimAtPosition;
            _aimController.BuildShootRequest(ref data);
            if (newInput.buttons.WasPressed(previousButtons, PlayerInputButtons.Attack))
            {
                data.FireType = FireInputType.Pressed;
                curtentWeapon.HandleShootRequest(data);
            }
            if (newInput.buttons.WasReleased(previousButtons, PlayerInputButtons.Attack))
            {
                data.FireType = FireInputType.Released;
                curtentWeapon.HandleShootRequest(data);
            }
            if (newInput.buttons.IsSet(PlayerInputButtons.Attack))
            {
                data.FireType = FireInputType.Hold;
                curtentWeapon.HandleShootRequest(data);
            }
        }

        //TODO: move to PlayerMovementHandler
        //use input to set Networked properties
        private void HandleMovementInput(ref PlayerMovementState mSate, in NetworkInputData input,
        in NetworkButtons previousButtons, float deltaTime)
        {
            NetworkButtons currentButtons = input.buttons;
            float acceleration = KCC.IsGrounded ? properties.GroundAcceleration : properties.AirAcceleration;
            float deceleration = KCC.IsGrounded ? properties.GroundDeceleration : properties.AirDeceleration;

            // normalise input direction
            Vector2 inputDirection = new Vector2(input.moveDelta.x, input.moveDelta.y).normalized;
            Vector3 lookRotationEuler = input.lookRotationEuler;

            //sprint
            mSate.IsSprint = currentButtons.IsSet(PlayerInputButtons.Sprint);

            //strafe
            if (currentButtons.WasPressed(previousButtons, PlayerInputButtons.Strafe))
            {
                mSate.IsStrafe = !mSate.IsStrafe;
            }

            // Update movement speed
            float targetSpeed = mSate.IsSprint ? properties.SprintSpeed : properties.WalkSpeed;
            mSate.MotionSpeedMultiply = input.analogMovement ? input.moveDelta.magnitude : 1f;

            //Rotate player based on input
            // if there is a move input rotate player when the player is moving
            float turningSpeed = 0;
            if (input.moveDelta != Vector2.zero || _model.NT_CurrentArmedState == ArmedType.Aiming)//moving or aiming
            {
                Vector3 lookRotationnEuler = Vector3.zero;
                Vector3 currentLookRotationEuler = KCC.GetLookRotation();

                if (mSate.IsStrafe)
                {
                    lookRotationnEuler = new Vector3(lookRotationEuler.x, lookRotationEuler.y, lookRotationEuler.z);
                }
                else//rotate to camera+input direction
                {
                    lookRotationnEuler = new Vector3(
                        lookRotationEuler.x,
                        lookRotationEuler.y + Mathf.Atan2(inputDirection.x, inputDirection.y) * Mathf.Rad2Deg,
                        lookRotationEuler.z);
                }

                float newYaw = Mathf.SmoothDampAngle(currentLookRotationEuler.y, lookRotationnEuler.y,
                    ref _yawRotationSpeed, properties.RotationSmoothTime, Mathf.Infinity, deltaTime);
                float newPitch = Mathf.SmoothDampAngle(currentLookRotationEuler.x, lookRotationnEuler.x,
                    ref _pitchRotationSpeed, properties.RotationSmoothTime, Mathf.Infinity, deltaTime);

                var rotationEuler = new Vector3(newPitch, newYaw, 0f);
                var stateLookRotationEuler = new Vector3(mSate.LookRotationEuler.x, mSate.LookRotationEuler.y, 0);

                if (stateLookRotationEuler != rotationEuler)
                {
                    //calculate turningSpeed in range[-1,1] by NT_lookRotationEuler and rotationEuler
                    Vector3 newLookDirection = Quaternion.Euler(rotationEuler) * Vector3.forward;
                    Vector3 oldLookDirection = Quaternion.Euler(stateLookRotationEuler) * Vector3.forward;

                    float angleDifference = Vector3.SignedAngle(oldLookDirection, newLookDirection, Vector3.up);
                    turningSpeed = Mathf.Clamp(angleDifference * properties.turnSpeedMultiply / 180f, -1f, 1f);
                    mSate.LookRotationEuler = rotationEuler;
                    mSate.TurningSpeed = turningSpeed;
                }
                mSate.TurningSpeed = turningSpeed;
            }
            else
            {
                mSate.TurningSpeed = Mathf.Lerp(mSate.TurningSpeed, turningSpeed, 0.5f);
            }

            //Move
            Vector3 currentVelocity = KCC.RealVelocity;
            float currentHorizontalSpeed = new Vector3(currentVelocity.x, 0.0f, currentVelocity.z).magnitude;

            //multiplies the input direction by the look rotation _yaw
            //to give us the horizontal direction we need to move in world space
            Vector3 lookRotationnEulerHorizontal = new Vector3(0f, lookRotationEuler.y + Mathf.Atan2(inputDirection.x, inputDirection.y) * Mathf.Rad2Deg, 0f);
            Vector3 moveDirection = Vector3.zero;
            moveDirection = Quaternion.Euler(lookRotationnEulerHorizontal) * Vector3.forward;

            Debug.DrawRay(transform.position, moveDirection, Color.green);
            Debug.DrawRay(transform.position, currentVelocity.normalized, Color.red);

            float speedOffset = 0.1f;
            Vector3 tempVelocity = currentVelocity;
            // accelerate or decelerate to target speed
            if (Mathf.Abs(targetSpeed - currentHorizontalSpeed) > speedOffset)
            {
                // creates curved result rather than a linear one giving a more organic speed change
                tempVelocity = Vector3.Lerp(currentVelocity,
                    moveDirection * targetSpeed, deltaTime * acceleration);
            }
            else
            {
                tempVelocity = moveDirection * targetSpeed;
            }

            if (inputDirection == Vector2.zero)
            {
                targetSpeed = 0;
                tempVelocity = Vector3.Lerp(currentVelocity,
                    moveDirection * targetSpeed, deltaTime * deceleration);
            }
            tempVelocity.y = 0;
            const float moveSpeedThreshold = 0.01f;
            if (Mathf.Abs(tempVelocity.x) < moveSpeedThreshold)
                tempVelocity.x = 0;
            if (Mathf.Abs(tempVelocity.z) < moveSpeedThreshold)
                tempVelocity.z = 0;
            mSate.MoveVelocity = tempVelocity;
        }

        private void HandleJumpInput(ref PlayerMovementState mSate, in NetworkButtons current, in NetworkButtons previous)
        {
            jumpImpulse = 0f;
            if (KCC.IsGrounded && current.WasPressed(previous, PlayerInputButtons.Jump))//only on pressed 
            {
                jumpImpulse = properties.JumpImpulse;
                mSate.IsJump = true;
                properties.VerticalimpulseVelocity = jumpImpulse / KCC.Rigidbody.mass;
            }
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
                _aimController.UpdateAimPosition(Time.deltaTime);
            }

            if (_model.NT_CurrentArmedState == ArmedType.Aiming)
            {
                _aimTarget.position = _model.AimAtPosition;
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
            _cameraController.onLookRotationEulerChanged -= OnLookRotationEulerChanged;
            _aimController.OnAimAtPositionChanged -= OnAimAtPosition;
            _model._eventDispacher.OnPlayerNameChanged -= OnPlayerNameChanged;
            _model._eventDispacher.OnCurrentArmedTypeChanged -= OnCurrentArmedStateChanged;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_hasStateAuthority && other.CompareTag("Weapon") && other.TryGetComponent(out IWeapon weapon))
            {
                _toBeEquipWeapon.Enqueue(weapon);
            }
        }
    }

}
