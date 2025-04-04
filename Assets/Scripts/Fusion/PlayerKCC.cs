using Fusion;
using UnityEngine;
using Fusion.Addons.SimpleKCC;
using System;
using UnityEngine.UIElements;
using NUnit.Framework.Internal;
using Unity.VisualScripting;
using static UnityDemo.AnimationController;

namespace UnityDemo
{
    [RequireComponent(typeof(SimpleKCC))]
    public class PlayerKCC : NetworkBehaviour
    {
        [Header("References")]
        public SimpleKCC KCC;
        public CameraController _cameraController;
        public AnimationController _animationController;
        public AudioController _audioController;
        public PlayerInputCollector _playerInputCollector;

        [SerializeField]
        private PlayerControllerProperties properties = new PlayerControllerProperties();

        private PlayerControllerStatus status = new PlayerControllerStatus();

        public PlayerControllerStatus Status { get => status; }
        private float jumpImpulse;
        private float _yawRotationSpeed = 0f;
        private float _pitchRotationSpeed = 0f;
        private Vector3 SpawnedPosition = default;
        public TMPro.TextMeshProUGUI _playNameText;

        private void Awake()
        {
            status.onPlayerNameChanged += OnPlayerNameChanged;
        }

        private void Start()
        {
            _animationController.Initialize(properties);
            _cameraController.onLookRotationEulerChanged += OnLookRotationEulerChanged;
        }

        private void OnLookRotationEulerChanged(Vector3 lookRotationEuler)
        {
            //apply to network input
            _playerInputCollector.CachedInputData.lookRotationEuler = lookRotationEuler;
            _playerInputCollector.CachedInputData.rotateTowardMovement = false;
        }

        public override void Spawned()
        {
            if (Object.HasInputAuthority)//proxy in client
            {
                _cameraController.SetupCameraControl();
            }
            if (HasStateAuthority)//proxy in host
            {
                SpawnedPosition = KCC.Position;
                InitializePlayerStatus();
            }
            OnPlayerNameChanged();
        }

        private void InitializePlayerStatus()
        {
            status.NT_lookRotationEuler = KCC.GetLookRotation();
            status.NT_isStrafe = false;
            status.NT_isSprint = false;
            status.NT_playerName = "Default_" + Runner.LocalPlayer.PlayerId.ToString();
            status.NT_turningSpeed = 0;
        }

        public override void FixedUpdateNetwork()
        {
            if (GetInput(out NetworkInputData data))
            {
                NetworkButtons buttons = data.buttons;
                if (HasStateAuthority)
                {   //only handle movement input if this player has state authority
                    //(the host who control this player or player proxies on host that receive input from client)
                    HandleMovementInput(data, status.NT_previousButtons, Runner.DeltaTime);
                }
                //jump need to be handled in both host and client, because it is depend on KCC.IsGrounded,
                //which is not networked so it is not sync between host and client
                HandleJumpInput(data.buttons, status.NT_previousButtons);
                status.NT_previousButtons = buttons;
            }

            // It feels better when the player falls quicker
            KCC.SetGravity(KCC.RealVelocity.y >= 0f ? properties.UpGravity : properties.DownGravity);
            KCC.SetLookRotation(status.NT_lookRotationEuler);
            KCC.Move(status.NT_moveVelocity, jumpImpulse);
            //reset position if player fall off the map
            if (HasStateAuthority && KCC.Transform.position.y < -100)
                KCC.SetPosition(SpawnedPosition);

            if (status.IsJump && KCC.IsGrounded)
            {
                status.IsJump = false;
            }
        }

        //use input to set Networked properties
        private void HandleMovementInput(in NetworkInputData input,
        in NetworkButtons previousButtons, float deltaTime)
        {
            NetworkButtons currentButtons = input.buttons;
            float acceleration = KCC.IsGrounded ? properties.GroundAcceleration : properties.AirAcceleration;
            float deceleration = KCC.IsGrounded ? properties.GroundDeceleration : properties.AirDeceleration;

            // normalise input direction
            Vector2 inputDirection = new Vector2(input.moveDelta.x, input.moveDelta.y).normalized;
            Vector3 lookRotationEuler = input.lookRotationEuler;

            //sprint
            status.NT_isSprint = currentButtons.IsSet(PlayerInputButtons.Sprint);

            //strafe
            if (currentButtons.WasPressed(previousButtons, PlayerInputButtons.Strafe))
            {
                status.NT_isStrafe = !status.NT_isStrafe;
            }

            // Update movement speed
            float targetSpeed = status.NT_isSprint ? properties.SprintSpeed : properties.WalkSpeed;
            status.NT_motionSpeedMultiply = input.analogMovement ? input.moveDelta.magnitude : 1f;

            float turningSpeed = 0;
            //Rotate player based on input
            // note: Vector2's != operator uses approximation so is not floating point error prone, and is cheaper than magnitude
            // if there is a move input rotate player when the player is moving
            if (input.moveDelta != Vector2.zero)//moving
            {
                Vector3 lookRotationnEuler = Vector3.zero;
                Vector3 currentLookRotationEuler = KCC.GetLookRotation();

                if (status.NT_isStrafe)
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

                Vector3 rotationEuler = new Vector3(newPitch, newYaw, 0f);
                if (status.NT_lookRotationEuler != rotationEuler)
                {
                    //calculate turningSpeed in range[-1,1] by status.NT_lookRotationEuler and rotationEuler
                    Vector3 newLookDirection = Quaternion.Euler(rotationEuler) * Vector3.forward;
                    Vector3 oldLookDirection = Quaternion.Euler(status.NT_lookRotationEuler) * Vector3.forward;

                    float angleDifference = Vector3.SignedAngle(oldLookDirection, newLookDirection, Vector3.up);
                    turningSpeed = Mathf.Clamp(angleDifference * properties.turnSpeedMultiply / 180f, -1f, 1f);

                    status.NT_lookRotationEuler = rotationEuler;
                    status.NT_turningSpeed = turningSpeed;
                }
                status.NT_turningSpeed = turningSpeed;
            }
            else
            {
                status.NT_turningSpeed = Mathf.Lerp(status.NT_turningSpeed, turningSpeed, 0.5f);
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
            status.NT_moveVelocity = tempVelocity;
        }

        private void HandleJumpInput(in NetworkButtons current, in NetworkButtons previous)
        {
            jumpImpulse = 0f;
            if (KCC.IsGrounded && current.WasPressed(previous, PlayerInputButtons.Jump))//only on pressed 
            {
                jumpImpulse = properties.JumpImpulse;
                status.IsJump = true;
                properties.VerticalimpulseVelocity = jumpImpulse / KCC.Rigidbody.mass;
            }
        }

        void LateUpdate()
        {
            //only update camera rotation if this player has input authority (the client who control this player)
            if (HasInputAuthority)
            {
                //local result for smooth camera rotation
                _cameraController.UpdateCameraRotation(
                    _playerInputCollector.LocalInputs.lookDelta,
                    _playerInputCollector.IsCurrentDeviceMouse,
                    Time.deltaTime);
            }
        }

        //[SerializeField]
        //Vector3 KCC_VelocityLocal;

        //[SerializeField]
        //float KCC_speed;

        //[SerializeField]
        //bool isStrafe;

        //[SerializeField]
        //bool isSprint;

        //[SerializeField]
        //float turningSpeed;

        public override void Render()
        {
            Vector3 velocityLocal = KCC.Transform.InverseTransformDirection(KCC.RealVelocity);

            AnimationParams @params = new AnimationParams()
            {
                LocalVelocity = velocityLocal,
                IsGrounded = KCC.IsGrounded,
                IsStrafe = status.NT_isStrafe,
                TurningSpeed = status.NT_turningSpeed,
                IsSprint = status.NT_isSprint,
                IsJump = status.IsJump

            };
            _animationController.SetAnimationParams(in @params);

            //KCC_VelocityLocal = @params.LocalVelocity;
            //isStrafe = @params.IsStrafe;
            //turningSpeed = @params.TurningSpeed;
            //KCC_speed = KCC_VelocityLocal.magnitude;
            //isSprint = @params.IsSprint;
        }

        public void OnFootstep(AnimationEvent animationEvent)
        {
            _audioController.PlayFootstep(animationEvent, transform.position);
        }

        //trigger from animation, play land sound
        public void OnLand(AnimationEvent animationEvent)
        {
            _audioController.PlayLanding(animationEvent, transform.position);
        }
        private void OnPlayerNameChanged()
        {
            if (_playNameText != null)
                _playNameText.text = status.NT_playerName;
        }

        private void OnDestroy()
        {
            _cameraController.onLookRotationEulerChanged -= OnLookRotationEulerChanged;
            status.onPlayerNameChanged -= OnPlayerNameChanged;
        }
    }

}
