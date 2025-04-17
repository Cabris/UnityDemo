using System;
using UnityEngine;
namespace UnityDemo
{
    [Serializable]
    public sealed class WeaponEquipmentContext
    {
        public float _unequipThresholdSpeed;
        public float _exitTimeUnhold = 2f;
        public float _exitTimeAiming = 0.1f;
        public float _exitTimeUnAim = 2f;

        [HideInInspector] public float _moveSpeed;
        [HideInInspector] public float _stateDuration;
        [HideInInspector] public bool _isAiming;
        public delegate void OnStateChange(IArmedState state);
        public OnStateChange OnStateEnter, OnStateExit;
    }

    public interface IArmedState
    {
        ArmedType Type { get; }
        void Enter(WeaponEquipmentContext context);
        void Exit(WeaponEquipmentContext context);

        ArmedType? TryGetNextState(WeaponEquipmentContext context);
    }

    public abstract class WeaponStateBase : IArmedState
    {
        public abstract ArmedType Type { get; }
        public void Enter(WeaponEquipmentContext context)
        {
            context._stateDuration = 0;
            context.OnStateEnter?.Invoke(this);
        }
        public void Exit(WeaponEquipmentContext context)
        {
            context.OnStateExit?.Invoke(this);
        }
        public abstract ArmedType? TryGetNextState(WeaponEquipmentContext context);
    }

    public sealed class UnarmedState : WeaponStateBase
    {
        public override ArmedType Type => ArmedType.Unarmed;

        public override ArmedType? TryGetNextState(WeaponEquipmentContext context)
        {
            if (context._isAiming)
                return ArmedType.Holding;
            return null;
        }
    }

    public sealed class HoldingState : WeaponStateBase
    {
        public override ArmedType Type => ArmedType.Holding;

        public override ArmedType? TryGetNextState(WeaponEquipmentContext context)
        {
            if (!context._isAiming && context._stateDuration >= context._exitTimeUnhold)
                return ArmedType.Unarmed;
            if (context._isAiming && context._stateDuration >= context._exitTimeAiming)
                return ArmedType.Aiming;
            return null;
        }
    }

    public sealed class AimingState : WeaponStateBase
    {
        public override ArmedType Type => ArmedType.Aiming;

        public override ArmedType? TryGetNextState(WeaponEquipmentContext context)
        {
            if (context._stateDuration > context._exitTimeAiming && !context._isAiming)
                return ArmedType.Holding;
            return null;
        }
    }
}
