using Fusion;
using System;
using Unity.VisualScripting;
using UnityEngine;

namespace UnityDemo
{
    [Serializable]
    public struct PlayerMovementState : INetworkStruct
    {
        public Vector3 MoveVelocity;
        public Vector3 LookRotationEuler;
        public float TurningSpeed;
        public float MotionSpeedMultiply;

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
        Unequip = 0,
        Holding = 1,
        Aiming = 2,
    }
}