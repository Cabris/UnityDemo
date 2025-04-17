using Fusion;
using System;
using UnityEngine;
namespace UnityDemo
{
    public interface IPlayerNetworkModel
    {
        bool IsInitialized { get; }
        ModelEventDispacher EventDispacher { get; }
    }

    public class PlayerNetworkModel : NetworkBehaviour, IPlayerNetworkModel
    {
        /// <summary>
        /// Networked properties
        /// </summary>
        [Networked] public ref PlayerMovementState Movement => ref MakeRef<PlayerMovementState>();
        [Networked] public ref PlayerEquipmentNetworkState Equipment => ref MakeRef<PlayerEquipmentNetworkState>();
        [Networked, OnChangedRender(nameof(OnAimAtPositionChangedRender))] public Vector3 AimAtPosition { get; set; }
        [Networked, OnChangedRender(nameof(OnIsAimingChangedRender))] public NetworkBool IsAiming { get; set; } = false; // 是否瞄準中
        [Networked, OnChangedRender(nameof(OnCurrentArmedStateChangedRender))] public ArmedType NT_CurrentArmedState { get; set; } = ArmedType.Unarmed; // 初始狀態為未裝備
        [Networked, OnChangedRender(nameof(OnPlayerNameChangedRender))] public string NT_playerName { get; set; } = "Player"; // 玩家名稱
        [Networked, OnChangedRender(nameof(OnPlayerHPChangedRender))] public int NT_playerHP { get; set; } = 10;// 玩家血量
        [Networked] public int NT_playerHPMax { get; set; }
        [Networked] public NetworkButtons PreviousButtons { get; set; }

        public event Action OnInitialized;
        public bool IsInitialized { get; private set; } = false;

        private IWeapon _currentWeaponCache = null;
        public IWeapon GetCurrentWeaponCached
        {
            get
            {
                //need update cache?
                if (_currentWeaponCache == null || _currentWeaponCache.WeaponStructRef != Equipment.CurrentUseWeapon)
                {
                    if (WeaponUtility.TryGetWeaponObjFromRef(Runner, Equipment.CurrentUseWeapon, out var weapon))
                        _currentWeaponCache = weapon;
                    else
                    {
                        Debug.LogError($"GetCurrentWeaponCached: Weapon not found in Runner: {Equipment.CurrentUseWeapon.WeaponId}");
                    }
                }
                return _currentWeaponCache;
            }
        }

        public ModelEventDispacher EventDispacher => _eventDispacher;

        public readonly ModelEventDispacher _eventDispacher = new ModelEventDispacher();

        const int MAX_PLAYER_HP = 100;

        public override void Spawned()
        {
            base.Spawned();
            Debug.Log("PlayerNetworkState Initialized");

            if (HasStateAuthority)
            {
                Movement.IsStrafe = false;
                Movement.IsSprint = false;
                Movement.TurningSpeed = 0;
                NT_CurrentArmedState = ArmedType.Unarmed;
                NT_playerHPMax = MAX_PLAYER_HP;
                NT_playerHP = MAX_PLAYER_HP;
                OnPlayerHPChangedRender();
            }
            IsInitialized = true;
            OnInitialized?.Invoke(); // 通知其他系統
        }

        private void OnPlayerNameChangedRender()
        {
            _eventDispacher.PlayerNameChanged(this, NT_playerName);
        }

        private void OnCurrentArmedStateChangedRender()
        {
            _eventDispacher.CurrentArmedTypeChanged(this, NT_CurrentArmedState);
        }

        private void OnIsAimingChangedRender()
        {
            _eventDispacher.IsAimingChanged(this, IsAiming);
        }

        private void OnAimAtPositionChangedRender()
        {
            _eventDispacher.AimAtPositionChanged(this, AimAtPosition);
        }

        private void OnPlayerHPChangedRender()
        {
            _eventDispacher.PlayerHPChanged(this, (float)NT_playerHP / NT_playerHPMax);
        }

        public void OnPlayerHasControl()
        {
            _eventDispacher.PlayerControlChanged(true);
        }

        public void OnPlayerLossControl()
        {
            _eventDispacher.PlayerControlChanged(false);
        }
    }

}
