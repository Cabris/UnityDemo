using Cinemachine;
using UnityEngine;

namespace UnityDemo
{
    public class CameraController : MonoBehaviour
    {
        public enum CameraMode
        {
            Follow,
            Aiming,
            CharactorSelect,
            Disable
        }

        [SerializeField] private Transform _cameraTarget;
        [SerializeField] private CinemachineVirtualCamera _followCamera;
        [SerializeField] private CinemachineVirtualCamera _aimingCamera;
        [SerializeField] private CinemachineVirtualCamera _charSelCamera;

        [Header("Camera Settings")]
        public float TopClamp = 70.0f;
        public float BottomClamp = -30.0f;
        public float CameraAngleOverride = 0.0f;
        public bool LockCameraPosition = false;
        [Range(0, 1)]
        public float _mouseRotationMultiply = 1.0f;
        [Range(0, 5)]
        public float _rotationMultiply = 1;

        private const float _threshold = 0.01f;
        private float _yaw, _pitch;

        public delegate void OnLookRotationEulerChanged(Vector2 lookRotationEuler);
        public OnLookRotationEulerChanged onLookRotationEulerChanged;

        private void Awake()
        {
            if (_cameraTarget == null)
            {
                Debug.LogError("Camera target not found on " + gameObject.name);
            }
            else
            {
                _yaw = _cameraTarget.rotation.eulerAngles.y;
                _pitch = _cameraTarget.rotation.eulerAngles.x;
            }
            _followCamera.enabled = false;
            _aimingCamera.enabled = false;
            _charSelCamera.enabled = false;
        }

        public void SetupCameraControl()
        {
            if (_followCamera == null || _aimingCamera == null)
            {
                SimpleLogger.Log($"SetupCameraControl Error: _followCamera = {_followCamera}, _aimingCamera = {_aimingCamera}");
                return;
            }
            if (_cameraTarget == null)
            {
                SimpleLogger.Log($"Player Spawned Error: link VirtualCamera Error _cameraTarget = null");
                return;
            }
            _aimingCamera.Follow = _cameraTarget;
            _followCamera.Follow = _cameraTarget;
        }

        public void SetCameraMode(CameraMode mode)
        {
            if (_followCamera == null || _aimingCamera == null || _charSelCamera == null)
            {
                SimpleLogger.Log($"Player Spawned Error: link VirtualCamera Error _playerVirtualCamera = null");
                return;
            }
            switch (mode)
            {
                case CameraMode.Follow:
                    _followCamera.enabled = true;
                    _aimingCamera.enabled = false;
                    _charSelCamera.enabled = false;
                    break;
                case CameraMode.Aiming:
                    _followCamera.enabled = false;
                    _aimingCamera.enabled = true;
                    _charSelCamera.enabled = false;
                    break;
                case CameraMode.CharactorSelect:
                    _followCamera.enabled = false;
                    _aimingCamera.enabled = false;
                    _charSelCamera.enabled = true;
                    break;
                case CameraMode.Disable:
                    _followCamera.enabled = false;
                    _aimingCamera.enabled = false;
                    _charSelCamera.enabled = false;
                    break;
            }
        }

        public void UpdateCameraRotation(Vector2 lookInput, bool isMouseControl, float deltaTime)
        {
            if (lookInput.sqrMagnitude >= _threshold && !LockCameraPosition)
            {
                float deltaTimeMultiplier = isMouseControl ? _mouseRotationMultiply : _rotationMultiply * deltaTime;

                _yaw += lookInput.x * deltaTimeMultiplier;
                _pitch += lookInput.y * deltaTimeMultiplier;
            }

            _pitch = Mathf.Clamp(_pitch, BottomClamp, TopClamp);
            Quaternion rotation = Quaternion.Euler(_pitch + CameraAngleOverride, _yaw, 0.0f);

            if (_cameraTarget != null)
                _cameraTarget.rotation = rotation;
            onLookRotationEulerChanged?.Invoke(rotation.eulerAngles);
        }

    }
}