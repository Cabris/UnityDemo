using Fusion;
using UnityEngine;
using UnityEngine.InputSystem;

namespace UnityDemo
{
    public sealed class PlayerInputCollector : MonoBehaviour
    {

        [Header("Movement Settings")]
        public bool analogMovement;

        [Header("Mouse Cursor Settings")]
        public bool cursorLocked = true;
        public bool cursorInputForLook = true;
        [Header("InputDatas")]
        [SerializeField] private NetworkInputData _cachedInputData;
        [SerializeField] public Vector2 _lookDelta = Vector2.zero;
        [Header("InputDatas")]
        [SerializeField]
        private InputActionAsset inputActions;
        private NetworkEvents _networkEvents;
        public ref NetworkInputData CachedInputData => ref _cachedInputData;

        private PlayerInput _playerInput;

        public void OnMove(InputValue value)
        {
            _cachedInputData.moveDelta = value.Get<Vector2>();
            //Debug.Log($"OnMove: {_cachedInputData.moveDelta}");
        }

        public void OnStrafe(InputValue value)
        {
            _cachedInputData.buttons.Set(PlayerInputButtons.Strafe, value.isPressed);
        }

        public void OnLook(InputValue value)
        {
            if (cursorInputForLook)
            {
                _lookDelta = value.Get<Vector2>();
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

        public void OnAim(InputValue value)
        {
            _cachedInputData.buttons.Set(PlayerInputButtons.Aim, value.isPressed);
        }

        public void OnSprint(InputValue value)
        {
            _cachedInputData.buttons.Set(PlayerInputButtons.Sprint, value.isPressed);
        }

        public void OnDrop(InputValue value)
        {
            _cachedInputData.buttons.Set(PlayerInputButtons.Drop, value.isPressed);

        }

        public void Initialize(bool hasInputAuthority, NetworkEvents networkEvents)
        {
            if (hasInputAuthority)
            {
                _playerInput = gameObject.AddComponent<PlayerInput>();
                _playerInput.enabled = true;
                _playerInput.actions = inputActions;
                //_playerInput.neverAutoSwitchControlSchemes = true;
                //_playerInput.defaultControlScheme = "Any";
                _playerInput.defaultActionMap = "Player";
                _playerInput.notificationBehavior = PlayerNotifications.SendMessages;

                // Register to Fusion input poll callback
                _networkEvents = networkEvents;
                _networkEvents.OnInput.AddListener(OnInput);
            }
            else
            {
                return;
            }
        }

        private void OnDestroy()
        {
            if (_networkEvents == null)
                return;
            _networkEvents.OnInput.RemoveListener(OnInput);
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
    }
}