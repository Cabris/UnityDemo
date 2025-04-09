using Fusion;
using System;
using UniGLTF;
using UnityEngine;
namespace UnityDemo
{
    public class WeaponObjectBase : NetworkBehaviour, IWeapon
    {
        [SerializeField]
        string _name;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        [SerializeField] Transform _holdR, _holdL, _rayCastFire;
        [SerializeField] Collider _collider;
        public Transform HoldR => _holdR;
        public Transform HoldL => _holdL;
        public Transform RayCastFire => _rayCastFire;

        [Networked] public ref NetworkWeaponStruct NT_WeaponStructRef => ref MakeRef<NetworkWeaponStruct>();
        [Networked, OnChangedRender(nameof(OnColliderEnableChangedRender))] NetworkBool NT_colliderEnabled { get; set; } // 用於控制碰撞器的啟用狀態
        //[Networked]public NetworkWeaponStruct NT_networkWeaponStruct { get; set; }

        private void Awake()
        {
            _collider = GetComponent<Collider>();
        }

        public override void Spawned()
        {
            base.Spawned();
            NT_WeaponStructRef.WeaponId = Object.Id; // 設定武器的 ID 為當前物件的 NetworkId
            NT_WeaponStructRef.Name = _name;
            NT_colliderEnabled = _collider.enabled;
        }
        private void OnColliderEnableChangedRender()
        {
            _collider.enabled = NT_colliderEnabled;
        }

        internal void RemoveFromInventory()
        {
            NT_colliderEnabled = true;
        }

        internal void AddToInventory()
        {
            Debug.Log($"AddToInventory: HasStateAuthority: {HasStateAuthority}, HasInputAuthority: {HasInputAuthority}");
            NT_colliderEnabled = false;
        }
    }

    public class WeaponUtility
    {
        public static bool TryGetWeaponObjFromRef(NetworkRunner runner, NetworkWeaponStruct weaponRef, out WeaponObjectBase weapon)
        {
            weapon = null;

            if (!weaponRef.IsValid)
                return false;

            if (runner.TryFindObject(weaponRef.WeaponId, out var obj) && obj.TryGetBehaviour(out weapon))
                return true;

            return false;
        }

    }

    public interface IWeapon
    {
        Transform HoldR { get; }
        Transform HoldL { get; }
        Transform RayCastFire { get; }

    }
}
