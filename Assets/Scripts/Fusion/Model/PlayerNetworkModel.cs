using Fusion;
using System;
using UnityEngine;
namespace UnityDemo
{
    public interface IPlayerNetworkModel
    {
        bool IsInitialized { get; }
        ModelEventDispacher EventDispacher { get; }
        ref PlayerMovementState Movement { get; }
        ref PlayerEquipmentNetworkState Equipment { get; }
        IWeaponState WeaponState { get; }
        IPlayerConditions PlayerConditions { get; }
        bool HasInputAuthority { get; }
        bool HasStateAuthority { get; }
        NetworkButtons PreviousButtons { get; }
    }
    public interface IWeaponState
    {
        Vector3 NT_AimAtPosition { get; set; }
        NetworkBool NT_IsAiming { get; set; }
        ArmedType NT_CurrentArmedState { get; set; }
        IWeapon GetCurrentWeaponCached { get; }
    }

    public interface IPlayerConditions
    {
        string NT_playerName { get; set; }
        int NT_playerHP { get; set; }
        int NT_playerHPMax { get; set; }
        float GetPlayerHPPercent();
    }


    public class PlayerNetworkModel : NetworkBehaviour, IPlayerNetworkModel, IWeaponState, IPlayerConditions
    {
        /// <summary>
        /// Networked properties
        /// </summary>
        [Networked] public ref PlayerMovementState Movement => ref MakeRef<PlayerMovementState>();
        [Networked] public ref PlayerEquipmentNetworkState Equipment => ref MakeRef<PlayerEquipmentNetworkState>();
        [Networked, OnChangedRender(nameof(OnAimAtPositionChangedRender))] public Vector3 NT_AimAtPosition { get; set; }
        [Networked, OnChangedRender(nameof(OnIsAimingChangedRender))] public NetworkBool NT_IsAiming { get; set; } = false; // 是否瞄準中
        [Networked, OnChangedRender(nameof(OnCurrentArmedStateChangedRender))] public ArmedType NT_CurrentArmedState { get; set; } = ArmedType.Unarmed; // 初始狀態為未裝備
        [Networked, OnChangedRender(nameof(OnPlayerNameChangedRender))] public string NT_playerName { get; set; } = "Player"; // 玩家名稱
        [Networked, OnChangedRender(nameof(OnPlayerHPChangedRender))] public int NT_playerHP { get; set; } = 10;// 玩家血量
        [Networked] public int NT_playerHPMax { get; set; }
        [Networked] public NetworkButtons PreviousButtons { get; set; }

        public bool IsInitialized { get; private set; } = false;

        private IWeapon _currentWeaponCache = null;
        public IWeapon GetCurrentWeaponCached
        {
            get
            {
                //need update cache?
                if (_currentWeaponCache == null || _currentWeaponCache.WeaponStructRef != Equipment.CurrentUseWeapon)
                {
                    if (WeaponUtility.TryGetWeaponObjFromRef(Equipment.CurrentUseWeapon, out var weapon))
                        _currentWeaponCache = weapon;
                    else
                    {
                        // Debug.LogError($"GetCurrentWeaponCached: Weapon not found in Runner: {Equipment.CurrentUseWeapon.WeaponId}");
                    }
                }
                return _currentWeaponCache;
            }
        }

        public ModelEventDispacher EventDispacher => _eventDispacher;

        public IWeaponState WeaponState => this;

        public IPlayerConditions PlayerConditions => this;

        private readonly ModelEventDispacher _eventDispacher = new ModelEventDispacher();

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
            EventDispacher.Initialized(this);
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
            _eventDispacher.IsAimingChanged(this, NT_IsAiming);
        }

        private void OnAimAtPositionChangedRender()
        {
            _eventDispacher.AimAtPositionChanged(this, NT_AimAtPosition);
        }

        private void OnPlayerHPChangedRender()
        {
            _eventDispacher.PlayerHPChanged(this, GetPlayerHPPercent());
        }

        public float GetPlayerHPPercent()
        {
            return (float)NT_playerHP / NT_playerHPMax;
        }

    }

}
