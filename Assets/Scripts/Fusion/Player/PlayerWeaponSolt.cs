using Fusion;
using System.Collections.Generic;
using UnityEngine;
namespace UnityDemo
{
    public class PlayerWeaponSolt : NetworkBehaviour
    {
        const int _maxWeaponCount = 2; // 最大武器數量
        private Queue<IWeapon> _toBeAddedWeapon = new Queue<IWeapon>();
        private IWeapon[] weaponObjectBases = new IWeapon[_maxWeaponCount];
        private int _currentWeaponIndex = 0; // 當前武器索引

        public void AddWeapon(WeaponObjectBase weapon)
        {
            if (_currentWeaponIndex >= _maxWeaponCount)
            {
                Debug.LogError($"PlayerWeaponSolt: AddWeapon: Max weapon count reached: {_maxWeaponCount}");
                return;
            }
            _toBeAddedWeapon.Enqueue(weapon);
        }

        public override void FixedUpdateNetwork()
        {
            if (_toBeAddedWeapon.Count > 0)
            {
                var weapon = _toBeAddedWeapon.Dequeue();
                var ntf = weapon.NetworkTransform;
                if (ntf)
                {
                    weapon.SelfTransform.SetParent(transform, true);
                    weapon.SelfTransform.localScale = Vector3.one;
                    weapon.SelfTransform.localRotation = Quaternion.identity;
                    weapon.SelfTransform.localPosition = Vector3.zero;
                    Debug.Log($"PlayerWeaponSolt: AddWeapon: Solt position: {transform.position}");
                    Debug.Log($"PlayerWeaponSolt: AddWeapon: weapon position: {weapon.SelfTransform.position}");
                }
                weaponObjectBases[_currentWeaponIndex] = weapon;
                _currentWeaponIndex++;
            }

            //fix NetworkTransform sync bug, WHY!?
            for (int i = 0; i < _currentWeaponIndex; i++)
            {
                var weapon = weaponObjectBases[i];
                if (weapon != null)
                {
                    weapon.SelfTransform.localScale = Vector3.one;
                    weapon.SelfTransform.localRotation = Quaternion.identity;
                    weapon.SelfTransform.localPosition = Vector3.zero;
                }
            }
        }
    }
}