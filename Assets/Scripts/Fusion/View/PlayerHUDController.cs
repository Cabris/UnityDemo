using System;
using UnityEngine;
using UnityEngine.UI;
namespace UnityDemo
{
    public class PlayerHUDController : MonoBehaviour
    {
        [SerializeField] Image _crosshair;
        [SerializeField] Slider _healthBar;
        [SerializeField] CanvasGroup _controlsCG;
        IPlayerNetworkModel _model;

        //dependency injection
        public void SetModel(IPlayerNetworkModel model)
        {
            if (!model.IsInitialized)
                return;

            if (_model != null)
            {
                UnsubscribeFromModelEvents();
                _model = null;
            }
            _model = model;
            var dispacher = _model.EventDispacher;
            if (dispacher != null)
            {
                dispacher.OnPlayerHPChanged += OnPlayerHPChanged;
                dispacher.OnAimAtPositionChanged += OnAimAtPositionChanged;
                dispacher.OnIsAimingChanged += OnIsAimingChanged;
                dispacher.OnPlayerControlChanged += OnPlayerControlChanged;
            }
            OnPlayerHPChanged(_model.PlayerConditions.GetPlayerHPPercent());
        }

        private void Start()
        {
            OnIsAimingChanged(false);
            OnPlayerControlChanged(false);
        }

        private void OnAimAtPositionChanged(Vector3 aimAtPosition)
        {
            // Update the crosshair position based on the aimAtPosition
            if (_crosshair != null)
                _crosshair.transform.position = Camera.main.WorldToScreenPoint(aimAtPosition);
        }

        private void OnIsAimingChanged(bool isAiming)
        {
            if (_crosshair != null)
            {
                _crosshair.GetComponent<CanvasGroup>().alpha = isAiming ? 1 : 0; // Set alpha to 1 if aiming, otherwise 0
            }
        }

        private void OnPlayerHPChanged(float persent)
        {
            if (_healthBar != null)
                _healthBar.value = persent;
        }

        private void OnPlayerControlChanged(bool hasControl)
        {
            if (_healthBar != null)
            {
                _healthBar.GetComponent<CanvasGroup>().alpha = hasControl ? 1 : 0; // Set alpha to 1 if has control, otherwise 0
            }

            if (_controlsCG != null)
            {
                _controlsCG.alpha = hasControl ? 1 : 0; // Set alpha to 1 if has control, otherwise 0
                _controlsCG.interactable = hasControl;
                _controlsCG.blocksRaycasts = hasControl;
            }
        }

        private void OnDestroy()
        {
            UnsubscribeFromModelEvents();
        }

        private void UnsubscribeFromModelEvents()
        {
            if (_model != null)
            {
                var dispacher = _model.EventDispacher;
                dispacher.OnPlayerHPChanged -= OnPlayerHPChanged;
                dispacher.OnAimAtPositionChanged -= OnAimAtPositionChanged;
                dispacher.OnIsAimingChanged -= OnIsAimingChanged;
                dispacher.OnPlayerControlChanged -= OnPlayerControlChanged;
            }
        }

    }
}