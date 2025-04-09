using System;
using UnityEngine;
using UnityEngine.Animations.Rigging;
namespace UnityDemo
{
    public class EquipmentController : MonoBehaviour
    {

        [Header("Rig Layers")]
        [SerializeField] Rig _AimPoseLayer;//Animation Rigging for aiming
        [SerializeField] Rig _HoldWeaponLayer;//Animation Rigging for holding weapon
        [SerializeField] Rig _HandPoseLayer;//Animation Rigging constraint hand to weapon

        [Header("Hand Positions")]
        [SerializeField] Transform _HandIK_R;//Right hand IK position
        [SerializeField] Transform _HandIK_L;//Left hand IK position

        [Header("Properties")]
        [SerializeField] float _unequipThresholdSpeed = 3f;
        [SerializeField] Transform _activeWeaponPivot, _disableWeapons;

        [Header("Model")]
        [SerializeField] private PlayerNetworkModel _model;

        float _stateDuration = 0f;

        public void Initialize(PlayerNetworkModel model)
        {
            _model = model;
            _model.OnCurrentArmedStateChanged += OnCurrentArmedStateChanged;
            TransitionToState(_model.CurrentArmedState);
            UpdateRigLayers(_model.CurrentArmedState);
        }

        private void OnDestroy()
        {
            _model.OnCurrentArmedStateChanged -= OnCurrentArmedStateChanged;
        }

        private void OnCurrentArmedStateChanged(ArmedType armedType)
        {
            UpdateRigLayers(_model.CurrentArmedState);
        }

        private void LateUpdate()
        {
            var currentWeapon = _model.GetCurrentWeaponCached;
            if (currentWeapon && _HandPoseLayer.weight > 0)
            {
                _HandIK_R.position = currentWeapon.HoldR.position;
                _HandIK_R.rotation = currentWeapon.HoldR.rotation;

                _HandIK_L.position = currentWeapon.HoldL.position;
                _HandIK_L.rotation = currentWeapon.HoldL.rotation;
            }
        }

        //only for StateAuthority
        public void UpdateEquipmentState(float deltaTime)
        {
            if (!_model || !_model.IsInitialized)
                return;

            var moveVelocity = _model.Movement.MoveVelocity;
            var currentUseWeapon = _model.Equipment.CurrentUseWeapon;
            bool isPressAttack = _model.PreviousButtons.IsSet(PlayerInputButtons.Attack);
            float horizontalSpeedSqr = MathF.Pow(moveVelocity.x, 2) + MathF.Pow(moveVelocity.z, 2);
            float _speedSqr = _unequipThresholdSpeed * _unequipThresholdSpeed;

            if (horizontalSpeedSqr > _speedSqr || !currentUseWeapon.IsValid)
            {
                TransitionToState(ArmedType.Unequip);
                return;
            }

            switch (_model.CurrentArmedState)
            {
                case ArmedType.Unequip:
                    {
                        if (isPressAttack)//go to Holding state
                        {
                            TransitionToState(ArmedType.Holding);
                        }
                    }
                    break;

                case ArmedType.Holding:
                    {
                        float exitTimeUnhold = 2f;
                        float exitTimeAiming = 0.5f;
                        if (!isPressAttack && _stateDuration >= exitTimeUnhold)
                            TransitionToState(ArmedType.Unequip);
                        if (isPressAttack && _stateDuration >= exitTimeAiming)
                            TransitionToState(ArmedType.Aiming);
                    }
                    break;

                case ArmedType.Aiming:
                    {
                        float exitTimeUnAim = 2f;
                        if (!isPressAttack && _stateDuration >= exitTimeUnAim)
                            TransitionToState(ArmedType.Holding);
                    }
                    break;
            }
            _stateDuration += deltaTime;
        }

        public void TransitionToState(ArmedType newState)
        {
            _stateDuration = 0;
            _model.CurrentArmedState = newState;
            ref PlayerMovementState mSate = ref _model.Movement;
            mSate.IsStrafe = _model.CurrentArmedState == ArmedType.Aiming ? true : false;
        }

        private void UpdateRigLayers(ArmedType state, bool immediate = false)
        {
            if (!_AimPoseLayer || !_HoldWeaponLayer || !_HandPoseLayer)
            {
                Debug.LogError("Rig layers not assigned");
                return;
            }

            switch (state)
            {
                case ArmedType.Unequip:
                    {
                        _AimPoseLayer.weight = _HoldWeaponLayer.weight = _HandPoseLayer.weight = 0f;
                    }
                    break;
                case ArmedType.Holding:
                    {
                        _HoldWeaponLayer.weight = 1f;
                        _AimPoseLayer.weight = 0f;
                        _HandPoseLayer.weight = 1f;
                    }
                    break;
                case ArmedType.Aiming:
                    {
                        _HoldWeaponLayer.weight = 0f;
                        _AimPoseLayer.weight = 1f;
                        _HandPoseLayer.weight = 1f;
                    }
                    break;
            }
        }

        //only hasStateAuthority
        public void EquipWeapon(ref NetworkWeaponStruct weaponStructRef)
        {
            if (!_model || !_model.IsInitialized)
            {
                return;
            }
            ref var equip = ref _model.Equipment;

            bool hasSameWeaponID = equip.ContainsSameWeaponID(weaponStructRef);
            if (hasSameWeaponID)
            {
                Debug.LogError($"WeaponId {weaponStructRef.WeaponId} already exists in inventory");
                return;
            }
            bool hasSameWeaponName = equip.ContainsSameWeaponName(weaponStructRef);
            if (hasSameWeaponName)
            {
                Debug.LogError($"WeaponName {weaponStructRef.Name} already exists in inventory");
                return;
            }

            int emptySolt = equip.FindEmptySolt();
            if (emptySolt == -1)
            {
                Debug.LogError("No empty slot in inventory");
                return;
            }
            equip.Weapons.Set(emptySolt, weaponStructRef);
            SwitchWeapon(weaponStructRef);

            if (WeaponUtility.TryGetWeaponObjFromRef(_model.Runner, weaponStructRef, out WeaponObjectBase weapon))
            {
                weapon.AddToInventory();
            }
        }

        //only hasStateAuthority
        public void DropWeapon(ref NetworkWeaponStruct weaponStructRef)
        {
            if (!_model || !_model.IsInitialized)
            {
                return;
            }

            ref var equip = ref _model.Equipment;

            int solt = -1;
            for (int i = 0; i < equip.Weapons.Length; i++)
            {
                if (equip.Weapons[i] == weaponStructRef)
                {
                    equip.Weapons.Set(i, default);// clear the slot
                    solt = i;
                    break;
                }
            }
            if (solt == -1)
            {
                Debug.LogError($"Weapon {weaponStructRef.Name} not found in inventory");
                return;
            }
            int newSolt = -1;
            for (int i = 0; i < equip.Weapons.Length; i++)
            {
                newSolt = (solt + i) & equip.Weapons.Length;
                var weap = equip.Weapons.Get(newSolt);
                if (weap.IsValid)
                {
                    NetworkWeaponStruct networkWeaponStruct = equip.Weapons.Get(newSolt);
                    SwitchWeapon(networkWeaponStruct);
                    break;
                }
            }
            if (newSolt == -1)
                Debug.LogError($"No weapon found in inventory to switch to after dropping {weaponStructRef.Name}");

            if (WeaponUtility.TryGetWeaponObjFromRef(_model.Runner, weaponStructRef, out WeaponObjectBase weapon))
            {
                weapon.RemoveFromInventory();
            }
        }

        private void SwitchWeapon(NetworkWeaponStruct weaponStruct)
        {
            if (!_model || !_model.IsInitialized)
            {
                return;
            }

            ref var equip = ref _model.Equipment;

            if (weaponStruct == equip.CurrentUseWeapon)
                return;
            if (WeaponUtility.TryGetWeaponObjFromRef(_model.Runner, equip.CurrentUseWeapon, out WeaponObjectBase curtWeapon))
                OnWeaponUnswitched(curtWeapon);

            if (WeaponUtility.TryGetWeaponObjFromRef(_model.Runner, weaponStruct, out WeaponObjectBase weapon))
                OnWeaponSwitched(weapon);
            else
            {
                Debug.LogError($"Weapon {weaponStruct.Name} not found");
                return;
            }
            equip.CurrentUseWeapon = weaponStruct;
        }

        private void OnWeaponSwitched(WeaponObjectBase weapon)
        {
            weapon.transform.SetParent(_activeWeaponPivot, false);
            weapon.transform.localPosition = Vector3.zero;
            weapon.transform.localRotation = Quaternion.identity;
        }

        private void OnWeaponUnswitched(WeaponObjectBase weapon)
        {
            weapon.transform.SetParent(_disableWeapons, false);
            weapon.transform.localScale = Vector3.zero;
        }

    }
}