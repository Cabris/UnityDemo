using Fusion;
using Fusion.Addons.SimpleKCC;
using System;
using UnityEngine;
using UnityEngine.Windows;
namespace UnityDemo
{
    public class MovementController
    {
        private SimpleKCC _KCC;
        private IPlayerNetworkModel _model;
        private PlayerControllerProperties _properties;
        private float jumpImpulse;
        private float _yawRotationSpeed = 0f;
        private float _pitchRotationSpeed = 0f;

        public void Initialize(PlayerControllerProperties properties, SimpleKCC kCC, IPlayerNetworkModel model)
        {
            _properties = properties;
            _KCC = kCC;
            _model = model;
        }

        public void HandleMovementInput(NetworkInputData input, float deltaTime)
        {
            if (_model==null)
            {
                return;
            }
            ref PlayerMovementState mSate = ref _model.Movement;
            NetworkButtons previousButtons = _model.PreviousButtons;
            NetworkButtons currentButtons = input.buttons;
            ArmedType currentArmedState = _model.WeaponState.NT_CurrentArmedState;
            float acceleration = _KCC.IsGrounded ? _properties.GroundAcceleration : _properties.AirAcceleration;
            float deceleration = _KCC.IsGrounded ? _properties.GroundDeceleration : _properties.AirDeceleration;

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
            float targetSpeed = mSate.IsSprint ? _properties.SprintSpeed : _properties.WalkSpeed;
            mSate.MotionSpeedMultiply = input.analogMovement ? input.moveDelta.magnitude : 1f;

            //Rotate player based on input
            // if there is a move input rotate player when the player is moving
            float turningSpeed = 0;
            if (input.moveDelta != Vector2.zero || currentArmedState == ArmedType.Aiming)//moving or aiming
            {
                Vector3 lookRotationnEuler = Vector3.zero;
                Vector3 currentLookRotationEuler = _KCC.GetLookRotation();

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
                    ref _yawRotationSpeed, _properties.RotationSmoothTime, Mathf.Infinity, deltaTime);
                float newPitch = Mathf.SmoothDampAngle(currentLookRotationEuler.x, lookRotationnEuler.x,
                    ref _pitchRotationSpeed, _properties.RotationSmoothTime, Mathf.Infinity, deltaTime);

                var rotationEuler = new Vector3(newPitch, newYaw, 0f);
                var stateLookRotationEuler = new Vector3(mSate.LookRotationEuler.x, mSate.LookRotationEuler.y, 0);

                if (stateLookRotationEuler != rotationEuler)
                {
                    //calculate turningSpeed in range[-1,1] by NT_lookRotationEuler and rotationEuler
                    Vector3 newLookDirection = Quaternion.Euler(rotationEuler) * Vector3.forward;
                    Vector3 oldLookDirection = Quaternion.Euler(stateLookRotationEuler) * Vector3.forward;

                    float angleDifference = Vector3.SignedAngle(oldLookDirection, newLookDirection, Vector3.up);
                    turningSpeed = Mathf.Clamp(angleDifference * _properties.turnSpeedMultiply / 180f, -1f, 1f);
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
            Vector3 currentVelocity = _KCC.RealVelocity;
            float currentHorizontalSpeed = new Vector3(currentVelocity.x, 0.0f, currentVelocity.z).magnitude;

            //multiplies the input direction by the look rotation _yaw
            //to give us the horizontal direction we need to move in world space
            Vector3 lookRotationnEulerHorizontal = new Vector3(0f, lookRotationEuler.y + Mathf.Atan2(inputDirection.x, inputDirection.y) * Mathf.Rad2Deg, 0f);
            Vector3 moveDirection = Vector3.zero;
            moveDirection = Quaternion.Euler(lookRotationnEulerHorizontal) * Vector3.forward;

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

        public void UpdateMovement()
        {
            if (_model == null)
            {
                return;
            }
            ref PlayerMovementState mSate = ref _model.Movement;
            // It feels better when the player falls quicker
            _KCC.SetGravity(_KCC.RealVelocity.y >= 0f ? _properties.UpGravity : _properties.DownGravity);
            _KCC.SetLookRotation(mSate.LookRotationEuler);
            _KCC.Move(mSate.MoveVelocity, jumpImpulse);

            if (mSate.IsJump && _KCC.IsGrounded)
            {
                mSate.IsJump = false;
            }
        }

        internal void HandleJumpInput(NetworkInputData newInput)
        {
            if (_model == null)
            {
                return;
            }
            var current = newInput.buttons;
            var previous = _model.PreviousButtons;
            ref PlayerMovementState mSate = ref _model.Movement;
            jumpImpulse = 0f;
            if (_KCC.IsGrounded && current.WasPressed(previous, PlayerInputButtons.Jump))//only on pressed 
            {
                jumpImpulse = _properties.JumpImpulse;
                mSate.IsJump = true;
                _properties.VerticalimpulseVelocity = jumpImpulse / _KCC.Rigidbody.mass;
            }
        }
    }
}