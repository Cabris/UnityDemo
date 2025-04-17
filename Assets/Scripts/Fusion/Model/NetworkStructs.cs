using Fusion;
using System;
using UnityEngine;

namespace UnityDemo
{
    public enum PlayerInputButtons
    {
        Jump = 0,//跳躍鍵
        Sprint = 2,  // 衝刺
        Strafe = 3,  // 側移
        Drop = 4,   // 丟棄
        Aim = 5,  // 瞄準
        Attack = 6,  // 開火
    }

    [Serializable]
    public struct NetworkInputData : INetworkInput
    {
        public Vector2 moveDelta;
        public Vector2 lookRotationEuler;
        public Vector3 aimAtPosition;
        public NetworkButtons buttons;
        public NetworkBool analogMovement;
        public void Reset()
        {
            moveDelta = Vector2.zero;
            analogMovement = false;
            lookRotationEuler = Vector2.zero;
            //jump = sprint = false;
            buttons.SetAllUp();
        }
    }

    [Serializable]
    public struct PlayerMovementState : INetworkStruct
    {
        public Vector3 MoveVelocity;
        public Vector2 LookRotationEuler;//Pitch,Yaw in degree
        public float TurningSpeed;//[-1,1]
        public float MotionSpeedMultiply;//[0,1]

        public NetworkBool IsStrafe;
        public NetworkBool IsSprint;
        public NetworkBool IsJump;
    }

    [Serializable]
    public struct NetworkWeaponStruct : INetworkStruct
    {
        public NetworkId WeaponId; // 來自 WeaponObjectBase 的 NetworkObjectId
        public bool IsValid => WeaponId.IsValid;
        public NetworkString<_16> Name;

        public override bool Equals(object obj)
        {
            if (obj is NetworkWeaponStruct other)
            {
                return WeaponId == other.WeaponId;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return WeaponId.GetHashCode();
        }

        public static bool operator ==(NetworkWeaponStruct left, NetworkWeaponStruct right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(NetworkWeaponStruct left, NetworkWeaponStruct right)
        {
            return !(left == right);
        }
    }

    [Serializable]
    public struct PlayerEquipmentNetworkState : INetworkStruct
    {
        const int MAX_WEAPON_COUNT = 5;
        public NetworkWeaponStruct CurrentUseWeapon;// 當前使用的武器
        [Networked, Capacity(MAX_WEAPON_COUNT)]
        public NetworkArray<NetworkWeaponStruct> Weapons => default;// 武器欄

        public bool ContainsSameWeaponID(NetworkWeaponStruct weaponStruct)
        {
            for (int i = 0; i < Weapons.Length; i++)
            {
                var weap = Weapons[i];
                if (weap == weaponStruct)
                {
                    return true;
                }
            }
            return false;
        }

        public bool ContainsSameWeaponName(NetworkWeaponStruct weaponStruct)
        {
            for (int i = 0; i < Weapons.Length; i++)
            {
                var weap = Weapons[i];
                if (weap.Name == weaponStruct.Name)
                {
                    return true;
                }
            }
            return false;
        }

        public int FindEmptySolt()
        {
            for (int i = 0; i < Weapons.Length; i++)
            {
                var weap = Weapons[i];
                if (!weap.IsValid)
                {
                    return i;
                }
            }
            return -1;
        }

    }
    public enum ArmedType
    {
        Undefined = -1,
        Unarmed = 0,
        Holding = 1,
        Aiming = 2,
    }

    public enum FireInputType
    {
        Cancelled = -1, // 取消
        Pressed = 0,   // 按下瞬間（如手槍）
        Released = 1,   // 放開瞬間（如蓄力弓、噴火器）
        Hold = 2,   // 持續按下（如手槍）
    }

    public struct ShootRequestData : INetworkStruct
    {
        public FireInputType FireType;
        public int LayerMaskValue;
        public NetworkId Requester;//who make the request
        public Vector3 AimAtPosition;
    }
}