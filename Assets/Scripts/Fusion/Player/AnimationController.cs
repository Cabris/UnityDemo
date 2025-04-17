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
        private int _animIDIsJump;
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
            public bool IsJump;
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
            _animIDIsStrafe = Animator.StringToHash("IsStrafe");
            _animIDIsJump = Animator.StringToHash("IsJump");

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

        private void SetIsStrafe(bool isStrafe)
        {
            if (isInitialized)
            {
                _animator.SetBool(_animIDIsStrafe, isStrafe);
            }
        }

        private void SetIsJump(bool isJump)
        {
            if (!isInitialized)
                return;
            _animator.SetBool(_animIDIsJump, isJump);
        }

        internal void SetAnimationParams(in AnimationParams @params)
        {
            if (!isInitialized)
                return;

            Vector3 localVelocityNor = Vector3.zero;
            Vector3 scale = new Vector3(1, 1, 1);
            if (@params.IsSprint)
            {
                scale.x = scale.z = 1f / properties.SprintSpeed;//[0,1]
            }
            else//max speed is RunSpeed
            {
                scale.x = scale.z = sprintThreshold / properties.RunSpeed;//[0,sprintThreshold]
            }
            localVelocityNor = Vector3.Scale(@params.LocalVelocity, scale);
            localVelocityNor.y = NormalizeVerticalSpeed(@params.LocalVelocity.y, properties.VerticalimpulseVelocity,
            properties.UpGravity, properties.DownGravity);

            const float zeroThreshold = 0.05f;
            if (Mathf.Abs(localVelocityNor.x) < zeroThreshold)
                localVelocityNor.x = 0;
            if (Mathf.Abs(localVelocityNor.y) < zeroThreshold)
                localVelocityNor.y = 0;
            if (Mathf.Abs(localVelocityNor.z) < zeroThreshold)
                localVelocityNor.z = 0;

            SetForwardSpeed(localVelocityNor.z);
            SetLateralSpeed(localVelocityNor.x);
            SetVerticalSpeed(localVelocityNor.y);
            SetTurningSpeed(@params.TurningSpeed);
            SetIsJump(@params.IsJump);
            SetIsGrounded(@params.IsGrounded);
            SetIsStrafe(@params.IsStrafe);

            //Debug.Log($"@params.LocalVelocity: {@params.LocalVelocity},  properties.RunSpeed: {properties.RunSpeed}, localVelocityNor: {localVelocityNor}");
        }

        private float NormalizeVerticalSpeed(float v, float impulseVelocity, float gravityUp, float gravityDown)
        {
            // 計算初始速度
            float vMax = impulseVelocity;

            // 計算最高點位移
            float s = (vMax * vMax) / (2 * Mathf.Abs(gravityUp));

            // 計算下落時的最大速度
            float vMin = Mathf.Sqrt(2 * Mathf.Abs(gravityDown) * s);

            // 標準化公式
            return 2f * ((v - (-vMin)) / (vMax - (-vMin))) - 1f;
        }
    }
}