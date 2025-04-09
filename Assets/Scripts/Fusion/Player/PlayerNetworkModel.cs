using Fusion;
using System;
using UnityEngine;
namespace UnityDemo
{
    public class PlayerNetworkModel : NetworkBehaviour
    {
        /// <summary>
        /// Networked properties
        /// </summary>
        [Networked] public ref PlayerMovementState Movement => ref MakeRef<PlayerMovementState>();
        [Networked] public ref PlayerEquipmentNetworkState Equipment => ref MakeRef<PlayerEquipmentNetworkState>();

        [Networked] public NetworkButtons PreviousButtons { get; set; }
        [Networked, OnChangedRender(nameof(OnCurrentArmedStateChangedRender))]
        public ArmedType CurrentArmedState { get; set; } = ArmedType.Undefined; // 初始狀態為未裝備

        [Networked, OnChangedRender(nameof(OnPlayerNameChangedRender))]
        public string NT_playerName { get; set; }

        public event Action OnInitialized;
        public event Action<string> OnPlayerNameChanged;
        public event Action<ArmedType> OnCurrentArmedStateChanged;
        public bool IsInitialized { get; private set; }

        private WeaponObjectBase _currentWeaponCache = null;
        public WeaponObjectBase GetCurrentWeaponCached
        {
            get
            {
                //need update cache?
                if (_currentWeaponCache == null || _currentWeaponCache.NT_WeaponStructRef != Equipment.CurrentUseWeapon)
                {
                    if (WeaponUtility.TryGetWeaponObjFromRef(Runner, Equipment.CurrentUseWeapon, out var weapon))
                        _currentWeaponCache = weapon;
                    else
                    {
                        //Debug.LogError($"GetCurrentWeaponCached: Weapon not found in Runner: {Equipment.CurrentUseWeapon.WeaponId}");
                    }

                }
                return _currentWeaponCache;
            }
        }

        public override void Spawned()
        {
            base.Spawned();
            Debug.Log("PlayerNetworkState Initialized");

            if (HasStateAuthority)
            {
                Movement.IsStrafe = false;
                Movement.IsSprint = false;
                Movement.TurningSpeed = 0;
                CurrentArmedState = ArmedType.Unequip;
            }
            IsInitialized = true;
            OnInitialized?.Invoke(); // 通知其他系統
        }

        private void OnPlayerNameChangedRender()
        {
            OnPlayerNameChanged?.Invoke(NT_playerName);
        }

        private void OnCurrentArmedStateChangedRender()
        {
            OnCurrentArmedStateChanged?.Invoke(CurrentArmedState);
        }
    }

    public abstract class PlayerNetworkController : NetworkBehaviour
    {
        [SerializeField] protected PlayerNetworkModel _model;

        public override void Spawned()
        {
            base.Spawned();
            if (_model == null)
                _model = GetComponent<PlayerNetworkModel>();
            if (_model == null)
            {
                Debug.LogError("PlayerNetworkModel not found on " + gameObject.name);
                return;
            }
            if (_model.IsInitialized)
            {
                Initialize();
            }
            else
            {
                _model.OnInitialized += Initialize;
            }
        }

        protected bool IsInitialized => _model != null && _model.IsInitialized;

        protected void Initialize()
        {
            Debug.Log("WeaponController Init 開始運作");
            // 取消註冊，避免重複呼叫
            _model.OnInitialized -= Initialize;
            // 開始初始化武器功能
            // 可安全存取 _state 的內容
        }
    }
}
