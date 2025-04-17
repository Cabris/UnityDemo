using Fusion;
using System;
using UnityEngine;
namespace UnityDemo
{
    public class ModelEventDispacher
    {
        public event Action<string> OnPlayerNameChanged;
        public event Action<ArmedType> OnCurrentArmedTypeChanged;
        public event Action<bool> OnIsAimingChanged;
        public event Action<Vector3> OnAimAtPositionChanged;
        public event Action<float> OnPlayerHPChanged;
        public event Action<bool> OnPlayerControlChanged;
        internal void PlayerNameChanged(IPlayerNetworkModel model, string playerName)
        {
            OnPlayerNameChanged?.Invoke(playerName);
        }

        internal void CurrentArmedTypeChanged(IPlayerNetworkModel model, ArmedType armedType)
        {
            OnCurrentArmedTypeChanged?.Invoke(armedType);
        }

        internal void IsAimingChanged(IPlayerNetworkModel model, bool isAiming)
        {
            OnIsAimingChanged?.Invoke(isAiming);
        }

        internal void AimAtPositionChanged(IPlayerNetworkModel model, Vector3 aimAtPosition)
        {
            OnAimAtPositionChanged?.Invoke(aimAtPosition);
        }

        internal void PlayerHPChanged(IPlayerNetworkModel model, float nT_playerHPPersent)
        {
            OnPlayerHPChanged?.Invoke(nT_playerHPPersent);
        }

        internal void PlayerControlChanged(bool hasControl)
        {
            OnPlayerControlChanged?.Invoke(hasControl);
        }
    }

}