using ExitGames.Client.Photon.StructWrapping;
using Fusion;
using StarterAssets;
using System;
using UnityEngine;
using UnityEngine.InputSystem;
namespace UnityDemo
{
#if ENABLE_INPUT_SYSTEM
    [RequireComponent(typeof(PlayerInput))]
#endif
    //[RequireComponent(typeof(StarterAssetsInputs))]
    public sealed class PlayerInputCollector : NetworkBehaviour
    {
        //[SerializeField]
        //private StarterAssetsInputs _localInputs;
        [Header("Movement Settings")]
        public bool analogMovement;

        [Header("Mouse Cursor Settings")]
        public bool cursorLocked = true;
        public bool cursorInputForLook = true;

        [SerializeField]
        private NetworkInputData _cachedInputData;

        public ref NetworkInputData CachedInputData => ref _cachedInputData;

#if ENABLE_INPUT_SYSTEM
        private PlayerInput _playerInput;

        public void OnMove(InputValue value)
        {
            _cachedInputData.moveDelta = value.Get<Vector2>();
        }

        public void OnStrafe(InputValue value)
        {
            _cachedInputData.buttons.Set(PlayerInputButtons.Strafe, value.isPressed);
        }


        public void OnLook(InputValue value)
        {
            if (cursorInputForLook)
            {
                _cachedInputData.lookDelta = value.Get<Vector2>();
            }
        }

        public void OnJump(InputValue value)
        {
            _cachedInputData.buttons.Set(PlayerInputButtons.Jump, value.isPressed);
        }

        public void OnAttack(InputValue value)
        {
            _cachedInputData.buttons.Set(PlayerInputButtons.Attack, value.isPressed);
        }

        public void OnSprint(InputValue value)
        {
            _cachedInputData.buttons.Set(PlayerInputButtons.Sprint, value.isPressed);
        }
#endif
        private void Awake()
        {
            if (!TryGetComponent(out _playerInput))
            {
                SimpleLogger.Log("PlayerInputCollector Error: _playerInput = null");
            }
        }

        public override void Spawned()
        {
            if (HasInputAuthority == false)
                return;
            // Register to Fusion input poll callback
            var networkEvents = Runner.GetComponent<NetworkEvents>();
            networkEvents.OnInput.AddListener(OnInput);
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            if (runner == null)
                return;
            var networkEvents = runner.GetComponent<NetworkEvents>();
            if (networkEvents != null)
            {
                networkEvents.OnInput.RemoveListener(OnInput);
            }
        }

        private void OnInput(NetworkRunner runner, NetworkInput input)
        {
            input.Set(_cachedInputData);
        }

        public bool IsCurrentDeviceMouse
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return _playerInput.currentControlScheme == "Keyboard&Mouse";
#else
				return false;
#endif
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            SetCursorState(cursorLocked);
        }

        private void SetCursorState(bool newState)
        {
            Cursor.lockState = newState ? CursorLockMode.Locked : CursorLockMode.None;
        }

        public NetworkInputData LocalInputs { get => _cachedInputData; }
    }
}