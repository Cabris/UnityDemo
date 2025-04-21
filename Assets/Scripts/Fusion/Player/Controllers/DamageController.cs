using System;
using UnityEngine;
namespace UnityDemo
{
    public class DamageController : MonoBehaviour, IDamageManager
    {
        private IPlayerNetworkModel _model;
        private float _hpTemp;
        public event Action<DamageInfo> OnKill;
        private CircularArrayQueue<DamageInfo> damageInfos = new CircularArrayQueue<DamageInfo>(30);

        public void Initialized(IPlayerNetworkModel model)
        {
            _model = model;
            _hpTemp = _model.PlayerConditions.NT_playerHP;
        }

        public void UpdateHealth(float deltaTime)
        {
            if (_model == null || !_model.HasStateAuthority)
                return;
            //for test
            if (_hpTemp < _model.PlayerConditions.NT_playerHPMax)
                _hpTemp += 10 * deltaTime;

            bool hasDamage = false;
            DamageInfo damageInfo = default;
            if (damageInfos.Count > 0)
            {
                damageInfo = damageInfos.Dequeue();
                var damageData = damageInfo.damageData;
                var damagePart = damageInfo.damagePart;
                float dmgA = damageData.DamageAmount * damagePart.DamageFactor;
                _hpTemp -= dmgA;
                hasDamage = true;
            }

            int newHP = Math.Clamp((int)_hpTemp, 0, _model.PlayerConditions.NT_playerHPMax);
            if (newHP != _model.PlayerConditions.NT_playerHP)
            {
                _model.PlayerConditions.NT_playerHP = newHP;
                _hpTemp = newHP;
            }
            if (_model.PlayerConditions.NT_playerHP == 0 && hasDamage)
            {
                OnKill?.Invoke(damageInfo);
            }
        }

        public void HandleDamage(DamageInfo damageInfo)
        {
            if (!_model.HasStateAuthority)
                return;
            damageInfos.Enqueue(damageInfo);
        }


    }
}