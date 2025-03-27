using Fusion;
using UnityEngine;
using Fusion.Addons.SimpleKCC;
using System;
using UnityEngine.UIElements;

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

        [Header("Movement Setup")]
        public float WalkSpeed = 2f;
        public float SprintSpeed = 5f;
        public float JumpImpulse = 10f;
        public float UpGravity = -25f;
        public float DownGravity = -40f;
        public float FallingSpeedThreshold = -10;

        [Header("Movement Accelerations")]
        public float GroundAcceleration = 55f;
        public float GroundDeceleration = 25f;
        public float AirAcceleration = 25f;
        public float AirDeceleration = 1.3f;

        [Header("Movement Rotation")]
        [Tooltip("How fast the character turns to face movement direction")]
        [Range(0.0f, 0.3f)]
        public float RotationSmoothTime = 0.12f;

        [SerializeField]
        bool _isRotateTowardMovement = true;

        [Networked] private bool NT_isRotateTowardMovement { get; set; }

        /// <param name="_moveVelocity">
        /// velocity in world space to apply to KCC
        /// </param>
        [Networked] private Vector3 NT_moveVelocity { get; set; }

        /// <param name="_motionSpeedMultiply">
        /// motion speed multiply for animation and movement
        /// </param>
        [Networked] private float NT_motionSpeedMultiply { get; set; }

        /// <param name="_aniVelocity">
        /// motion velocity multiply for animation
        /// </param>
        [Networked] private Vector2 NT_aniVelocity { get; set; }

        /// <param name="_lookRotation">
        /// look rotation in world space 
        /// </param>
        [Networked] private Vector3 NT_lookRotationEuler { get; set; }

        private float jumpImpulse { get; set; }

        /// <param name="_isJumping">
        /// is this player currently jumping
        /// </param>
        [Networked] private NetworkBool NT_aniIsJumping { get; set; }

        [Networked] private NetworkButtons NT_previousButtons { get; set; }

        private float _yawRotationSpeed = 0f;
        private float _pitchRotationSpeed = 0f;
        private Vector3 SpawnedPosition = default;

        public override void Spawned()
        {
            if (Object.HasInputAuthority)//proxy in client
            {
                _cameraController.SetupCameraControl();
            }
            if (HasStateAuthority)//proxy in host
            {
                SpawnedPosition = KCC.Position;

                NT_lookRotationEuler = KCC.GetLookRotation();
            }
        }

        public override void FixedUpdateNetwork()
        {
            if (GetInput(out NetworkInputData data))
            {
                NetworkButtons buttons = data.buttons;
                ProcessInput(data, NT_previousButtons, Runner.DeltaTime);
                NT_previousButtons = buttons;
            }
            // It feels better when the player falls quicker
            KCC.SetGravity(KCC.RealVelocity.y >= 0f ? UpGravity : DownGravity);
            KCC.SetLookRotation(NT_lookRotationEuler);
            KCC.Move(NT_moveVelocity, jumpImpulse);

            if (KCC.IsGrounded)
            {
                // Stop jumping
                NT_aniIsJumping = false;
            }

            //reset position if player fall off the map
            if (HasStateAuthority && KCC.Transform.position.y < -100)
                KCC.SetPosition(SpawnedPosition);

            if (NT_isRotateTowardMovement != _isRotateTowardMovement)
            {
                if (HasStateAuthority)
                {
                    NT_isRotateTowardMovement = _isRotateTowardMovement;
                }
                else if (!HasInputAuthority)
                {
                    _isRotateTowardMovement = NT_isRotateTowardMovement;
                }
            }

        }

        private void ProcessInput(in NetworkInputData input,
        in NetworkButtons previousButtons, float deltaTime)
        {
            NetworkButtons currentButtons = input.buttons;
            // normalise input direction
            Vector2 inputDirection = new Vector2(input.moveDelta.x, input.moveDelta.y).normalized;

            // Update movement speed
            float targetSpeed = currentButtons.IsSet(PlayerInputButtons.Sprint) ? SprintSpeed : WalkSpeed;
            float acceleration = KCC.IsGrounded ? GroundAcceleration : AirAcceleration;

            if (inputDirection == Vector2.zero)
                targetSpeed = 0.0f;

            Vector3 currentVelocity = KCC.RealVelocity;

            //players current horizontal velocity
            float currentHorizontalSpeed = new Vector3(
                currentVelocity.x, 0.0f,
                currentVelocity.z).magnitude;

            float speedOffset = 0.1f;
            NT_motionSpeedMultiply = input.analogMovement ? input.moveDelta.magnitude : 1f;

            Vector3 newInputLookRotationEuler = input.lookRotationEuler;

            //multiplies the input direction by the look rotation _yaw
            //to give us the horizontal direction we need to move in world space
            Vector3 lookRotationnEulerHorizontal = new Vector3(0f, newInputLookRotationEuler.y, 0f);
            Vector3 targetMoveDirectionHorizontal =
                Quaternion.Euler(lookRotationnEulerHorizontal) * new Vector3(inputDirection.x, 0f, inputDirection.y);
            //Debug.DrawRay(transform.position, targetMoveDirectionHorizontal, Color.green);

            // accelerate or decelerate to target speed
            if (Mathf.Abs(targetSpeed - currentHorizontalSpeed) > speedOffset)
            {
                // creates curved result rather than a linear one giving a more organic speed change
                NT_moveVelocity = Vector3.Lerp(currentVelocity, targetMoveDirectionHorizontal * targetSpeed * NT_motionSpeedMultiply, deltaTime * acceleration);
            }
            else
            {
                NT_moveVelocity = targetMoveDirectionHorizontal * targetSpeed * NT_motionSpeedMultiply;
            }

            Vector2 targetAniVelocity = Vector2.zero;

            // note: Vector2's != operator uses approximation so is not floating point error prone, and is cheaper than magnitude
            // if there is a move input rotate player when the player is moving
            if (input.moveDelta != Vector2.zero)//moving
            {
                Vector3 targetLookRotationEuler = default;
                Vector3 lookRotationnEuler = new Vector3(newInputLookRotationEuler.x, newInputLookRotationEuler.y, newInputLookRotationEuler.z);

                if (NT_isRotateTowardMovement)//player will rotate toward the direction it is moving
                {
                    float angleYaw = Mathf.Atan2(inputDirection.x, inputDirection.y) * Mathf.Rad2Deg;
                    targetLookRotationEuler = lookRotationnEuler += new Vector3(0, angleYaw, 0);

                    //alway play forward animation
                    targetAniVelocity.Set(0, Mathf.Abs(inputDirection.magnitude));
                }
                else//player will rotate toward the direction the camera is facing
                {
                    targetLookRotationEuler = lookRotationnEuler;
                    //play animation based on input direction
                    targetAniVelocity.Set(inputDirection.x, inputDirection.y);
                }

                Vector3 currentLookRotationEuler = KCC.GetLookRotation();

                float newYaw = Mathf.SmoothDampAngle(currentLookRotationEuler.y, targetLookRotationEuler.y,
                    ref _yawRotationSpeed, RotationSmoothTime, Mathf.Infinity, deltaTime);
                float newPitch = Mathf.SmoothDampAngle(currentLookRotationEuler.x, targetLookRotationEuler.x,
                    ref _pitchRotationSpeed, RotationSmoothTime, Mathf.Infinity, deltaTime);

                Vector3 newLookRotationEuler = new Vector3(newPitch, newYaw, 0f);
                if (NT_lookRotationEuler != newLookRotationEuler)
                    NT_lookRotationEuler = new Vector3(newPitch, newYaw, 0f);

                if (!currentButtons.IsSet(PlayerInputButtons.Sprint))
                {
                    float clampValue = 0.7f;
                    targetAniVelocity.x = Mathf.Clamp(targetAniVelocity.x, -clampValue, clampValue);
                    targetAniVelocity.y = Mathf.Clamp(targetAniVelocity.y, -clampValue, clampValue);
                }
            }

            float aniVelocityOffset = 0.0001f;
            if ((targetAniVelocity - NT_aniVelocity).sqrMagnitude > aniVelocityOffset)
                //smooth animation velocity
                NT_aniVelocity = Vector2.Lerp(NT_aniVelocity, targetAniVelocity, deltaTime * 10);
            else
                NT_aniVelocity = targetAniVelocity;

            jumpImpulse = 0f;
            //only on pressed 
            if (KCC.IsGrounded && currentButtons.WasPressed(previousButtons, PlayerInputButtons.Jump))
            {
                jumpImpulse = JumpImpulse;
                NT_aniIsJumping = true;
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
                    Time.deltaTime, out Vector3 lookRotationEuler);
                //apply to network input
                _playerInputCollector.CachedInputData.lookRotationEuler = lookRotationEuler;
                _playerInputCollector.CachedInputData.rotateTowardMovement = _isRotateTowardMovement;
            }
        }

        public override void Render()
        {
            _animationController.UpdateMovementAnimation(NT_aniVelocity, NT_motionSpeedMultiply);
            _animationController.SetGrounded(KCC.IsGrounded);
            _animationController.SetIsJump(NT_aniIsJumping);
            _animationController.SetFalling(KCC.RealVelocity.y < FallingSpeedThreshold);

            Span<int> numbers = stackalloc int[3];
            int[] a = new int[3];
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
    }
}
