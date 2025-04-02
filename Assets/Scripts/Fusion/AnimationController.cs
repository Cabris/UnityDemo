using Fusion.Addons.SimpleKCC;
using System;
using UnityEngine;
namespace UnityDemo
{
    public class AnimationController : MonoBehaviour
    {
        [SerializeField]
        private Animator _animator;
        private PlayerControllerProperties properties;
        private int _animIDForwardSpeed;
        private int _animIDLateralSpeed;
        private int _animIDIsGrounded;
        private int _animIDVerticalSpeed;
        private int _animIDTurningSpeed;
        private int _animIDIsEquipWeapon;
        private int _animIDIsStrafe;
        private bool isInitialized = false;
        private readonly float sprintThreshold = 2f / 3f;
        public struct AnimationParams
        {
            public Vector3 LocalVelocity;
            public float TurningSpeed;
            public bool IsGrounded;
            public bool IsStrafe;
            public bool IsSprint;
        }

        public void Initialize(PlayerControllerProperties properties)
        {
            this.properties = properties;
            if (this.properties != null && _animator != null)
                isInitialized = true;
        }

        private void Awake()
        {
            _animIDForwardSpeed = Animator.StringToHash("ForwardSpeed");
            _animIDLateralSpeed = Animator.StringToHash("LateralSpeed");
            _animIDIsGrounded = Animator.StringToHash("IsGrounded");
            _animIDVerticalSpeed = Animator.StringToHash("VerticalSpeed");
            _animIDTurningSpeed = Animator.StringToHash("TurningSpeed");
            _animIDIsEquipWeapon = Animator.StringToHash("IsEquipWeapon");
            _animIDIsStrafe = Animator.StringToHash("IsStrafe");

            if (_animator == null && !TryGetComponent<Animator>(out _animator))
            {
                Debug.LogError("Animator component not found on " + gameObject.name);
            }
        }


        private void SetForwardSpeed(float forwardSpeed)
        {
            if (isInitialized)
            {
                ;
                _animator.SetFloat(_animIDForwardSpeed, forwardSpeed);
            }
        }

        private void SetLateralSpeed(float lateralSpeed)
        {
            if (isInitialized)
            {
                _animator.SetFloat(_animIDLateralSpeed, lateralSpeed);
            }
        }
        private void SetVerticalSpeed(float verticalSpeed)
        {
            if (isInitialized)
            {
                _animator.SetFloat(_animIDVerticalSpeed, verticalSpeed);
            }
        }

        private void SetIsGrounded(bool isGrounded)
        {
            if (isInitialized)
            {
                _animator.SetBool(_animIDIsGrounded, isGrounded);
            }
        }

        private void SetTurningSpeed(float turningSpeed)
        {
            if (isInitialized)
            {
                _animator.SetFloat(_animIDTurningSpeed, turningSpeed);
            }
        }

        public void SetIsEquipWeapon(bool isEquipWeapon)
        {
            if (isInitialized)
            {
                _animator.SetBool(_animIDIsEquipWeapon, isEquipWeapon);
            }
        }

        private void SetIsStrafe(bool isStrafe)
        {
            if (isInitialized)
            {
                _animator.SetBool(_animIDIsStrafe, isStrafe);
            }
        }

        internal void SetAnimationParams(in AnimationParams @params)
        {
            if (!isInitialized)
                return;

            SetIsGrounded(@params.IsGrounded);
            SetIsStrafe(@params.IsStrafe);
            Vector3 localVelocityNor = Vector3.zero;

            if (@params.IsSprint)
            {
                localVelocityNor = @params.LocalVelocity / properties.SprintSpeed;//[0,1]
            }
            else//max speed is RunSpeed
            {
                localVelocityNor = @params.LocalVelocity * sprintThreshold / properties.RunSpeed;//[0,sprintThreshold]
            }

            SetForwardSpeed(localVelocityNor.z);
            SetLateralSpeed(localVelocityNor.x);
            float localVerticalSpeedNor = localVelocityNor.y;
            SetVerticalSpeed(localVerticalSpeedNor);
            SetTurningSpeed(@params.TurningSpeed);

            //Debug.Log($"@params.LocalVelocity: {@params.LocalVelocity},  properties.RunSpeed: {properties.RunSpeed}, localVelocityNor: {localVelocityNor}");
        }
    }
}