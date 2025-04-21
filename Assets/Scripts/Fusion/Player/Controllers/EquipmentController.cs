using Fusion;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UIElements;
using static Unity.Collections.Unicode;
namespace UnityDemo
{
    public class EquipmentController : MonoBehaviour
    {

        [Header("Rig Layers")]
        [SerializeField] Rig _AimPoseLayer;//Animation Rigging for aiming
        [SerializeField] Rig _HoldWeaponLayer;//Animation Rigging for holding weapon
        [SerializeField] Rig _HandPoseLayer;//Animation Rigging constraint hand to weapon
        [SerializeField] MultiParentConstraint _ActiveWeaponPivotParent;

        [Header("Properties")]
        [SerializeField] WeaponEquipmentContext _context = new WeaponEquipmentContext();

        [Header("Weapon Solts")]
        [SerializeField] PlayerWeaponSolt _activeWeaponPivot, _disableWeapons;

        [Header("Hand Positions")]
        [SerializeField] Transform _HandIK_R;//Right hand IK position
        [SerializeField] Transform _HandIK_L;//Left hand IK position

        private IPlayerNetworkModel _model;
        private Dictionary<ArmedType, IArmedState> _armedStates;
        private IArmedState _currentState;
        private Queue<IWeapon> _toBeEquipWeapon = new Queue<IWeapon>();

        [SerializeField]
        ArmedType DEBUG_OVERRIDE_ArmedType = ArmedType.Undefined;

        public void Initialize(IPlayerNetworkModel model)
        {
            _model = model;
            _model.EventDispacher.OnCurrentArmedTypeChanged += OnCurrentArmedTypeChanged;
            _context.OnStateEnter += OnStateEnter;
            _context.OnStateExit += OnStateExit;
            _armedStates = new Dictionary<ArmedType, IArmedState>
            {
                { ArmedType.Unarmed, new UnarmedState() },
                { ArmedType.Holding, new HoldingState() },
                { ArmedType.Aiming, new AimingState() },
            };

            _currentState = _armedStates[_model.WeaponState.NT_CurrentArmedState];
            _currentState.Enter(_context);
            UpdateRigLayers(_model.WeaponState.NT_CurrentArmedState);
        }

        private void OnDestroy()
        {
            _context.OnStateEnter -= OnStateEnter;
            _context.OnStateExit -= OnStateExit;
            if (_model != null)
                _model.EventDispacher.OnCurrentArmedTypeChanged -= OnCurrentArmedTypeChanged;
        }

        private void OnCurrentArmedTypeChanged(ArmedType armedType)
        {
            UpdateRigLayers(_model.WeaponState.NT_CurrentArmedState);
        }

        private void OnStateEnter(IArmedState state)
        {
            _model.WeaponState.NT_CurrentArmedState = state.Type;
            ref PlayerMovementState mSate = ref _model.Movement;
            mSate.IsStrafe = _model.WeaponState.NT_CurrentArmedState == ArmedType.Aiming ? true : false;
            Debug.Log($"OnStateEnter: {_model.WeaponState.NT_CurrentArmedState}, IsStrafe: {mSate.IsStrafe}");
        }

        private void OnStateExit(IArmedState state) { }

        private void LateUpdate()
        {
            IWeapon currentWeapon = _model.WeaponState.GetCurrentWeaponCached;
            if (currentWeapon != null && _HandPoseLayer.weight > 0)
            {
                _HandIK_R.position = currentWeapon.HoldR.position;
                _HandIK_R.rotation = currentWeapon.HoldR.rotation;

                _HandIK_L.position = currentWeapon.HoldL.position;
                _HandIK_L.rotation = currentWeapon.HoldL.rotation;
            }
        }

        //only for StateAuthority
        private void UpdateEquipmentState(float deltaTime)
        {
            if (_model == null || !_model.IsInitialized || _armedStates == null)
                return;

            var moveV = _model.Movement.MoveVelocity;
            var currentUseWeapon = _model.Equipment.CurrentUseWeapon;

            //_context._moveSpeed = Mathf.Sqrt(moveV.x * moveV.x + moveV.z * moveV.z);
            _context._stateDuration += deltaTime;
            _context._isAiming = _model.WeaponState.NT_IsAiming;

            if (DEBUG_OVERRIDE_ArmedType != ArmedType.Undefined)
            {
                if (_currentState.Type != DEBUG_OVERRIDE_ArmedType)
                {
                    _currentState = _armedStates[DEBUG_OVERRIDE_ArmedType];
                    _currentState.Enter(_context);
                }
                return;
            }


            if (_model.Movement.IsSprint || !currentUseWeapon.IsValid)
            {
                if (_currentState != _armedStates[ArmedType.Unarmed])
                {
                    _currentState = _armedStates[ArmedType.Unarmed];
                    _currentState.Enter(_context);
                }
                return;
            }

            var next = _currentState.TryGetNextState(_context);
            if (next.HasValue && next.Value != _currentState.Type)
            {
                _currentState.Exit(_context);
                _currentState = _armedStates[next.Value];
                _currentState.Enter(_context);
            }
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
                case ArmedType.Unarmed:
                    {
                        _AimPoseLayer.weight = _HoldWeaponLayer.weight = _HandPoseLayer.weight = 0f;
                        _ActiveWeaponPivotParent.data.sourceObjects.SetWeight(0, 0f);
                        _ActiveWeaponPivotParent.data.sourceObjects.SetWeight(1, 0f);
                        _ActiveWeaponPivotParent.data.sourceObjects.SetWeight(2, 0f);
                    }
                    break;
                case ArmedType.Holding:
                    {
                        _HoldWeaponLayer.weight = 1f;
                        _AimPoseLayer.weight = 0f;
                        _HandPoseLayer.weight = 1f;
                        _ActiveWeaponPivotParent.data.sourceObjects.SetWeight(0, 0f);
                        _ActiveWeaponPivotParent.data.sourceObjects.SetWeight(1, 0f);
                        _ActiveWeaponPivotParent.data.sourceObjects.SetWeight(2, 0f);
                    }
                    break;
                case ArmedType.Aiming:
                    {
                        _HoldWeaponLayer.weight = 0f;
                        _AimPoseLayer.weight = 1f;
                        _HandPoseLayer.weight = 1f;
                        _ActiveWeaponPivotParent.data.sourceObjects.SetWeight(0, 0f);
                        _ActiveWeaponPivotParent.data.sourceObjects.SetWeight(1, 0f);
                        _ActiveWeaponPivotParent.data.sourceObjects.SetWeight(2, 1f);
                    }
                    break;
            }
            var src = _ActiveWeaponPivotParent.data.sourceObjects;
            for (int i = 0; i <= (int)ArmedType.Aiming; i++)
            {
                src.SetWeight(i, i == (int)state ? 1f : 0f);
            }
            _ActiveWeaponPivotParent.data.sourceObjects = src;
        }

        //only hasStateAuthority
        private void EquipWeapon(IWeapon weaponObj)
        {
            ref NetworkWeaponStruct weaponStructRef = ref weaponObj.WeaponStructRef;

            if (_model == null || !_model.IsInitialized || !weaponStructRef.IsValid)
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
                Debug.Log($"WeaponName {weaponStructRef.Name} already exists in inventory");
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
            weaponObj.AddToInventory();
        }

        //only hasStateAuthority
        public void DropWeapon(ref NetworkWeaponStruct weaponStructRef)
        {
            if (_model == null || !_model.IsInitialized)
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

            if (WeaponUtility.TryGetWeaponObjFromRef(weaponStructRef, out WeaponObjectBase weapon))
            {
                weapon.RemoveFromInventory();
            }
        }

        private void SwitchWeapon(NetworkWeaponStruct weaponStruct)
        {
            if (_model == null || !_model.IsInitialized)
            {
                return;
            }

            ref var equip = ref _model.Equipment;

            if (weaponStruct == equip.CurrentUseWeapon)
                return;
            if (WeaponUtility.TryGetWeaponObjFromRef(equip.CurrentUseWeapon, out WeaponObjectBase curtWeapon))
                OnWeaponUnswitched(curtWeapon);

            if (WeaponUtility.TryGetWeaponObjFromRef(weaponStruct, out WeaponObjectBase weapon))
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
            _activeWeaponPivot.AddWeapon(weapon);
        }

        private void OnWeaponUnswitched(WeaponObjectBase weapon)
        {
            _disableWeapons.AddWeapon(weapon);
        }

        internal void UpdateWeaponEquipment(float deltaTime)
        {
            if (_toBeEquipWeapon.Count > 0)
            {
                var weapon = _toBeEquipWeapon.Dequeue();
                EquipWeapon(weapon);
            }
            UpdateEquipmentState(deltaTime);
        }

        internal void AddWeaponToInventory(IWeapon weapon)
        {
            _toBeEquipWeapon.Enqueue(weapon);
        }
    }
}