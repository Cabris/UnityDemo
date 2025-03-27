using Cinemachine;
using UnityEngine;

namespace UnityDemo
{
    public class CameraController : MonoBehaviour
    {
        [SerializeField]
        private Transform _cameraTarget;
        [SerializeField]
        private CinemachineVirtualCamera _virtualCamera;

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
        //private GameObject _mainCamera;

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
        }

        public void SetupCameraControl()
        {
            if (_virtualCamera == null)
                _virtualCamera = FindFirstObjectByType<CinemachineVirtualCamera>();
            if (_virtualCamera == null)
            {
                SimpleLogger.Log($"Player Spawned Error: link VirtualCamera Error _playerVirtualCamera = null");
            }
            if (_cameraTarget == null)
            {
                SimpleLogger.Log($"Player Spawned Error: link VirtualCamera Error _cameraTarget = null");
                return;
            }
            _virtualCamera.Follow = _cameraTarget;
        }

        public void UpdateCameraRotation(Vector2 lookInput, bool isMouseControl, float deltaTime, out Vector3 lookRotationEuler)
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
            lookRotationEuler = rotation.eulerAngles;
        }

    }
}