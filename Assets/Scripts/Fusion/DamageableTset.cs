using Fusion;
using System;
using UnityEngine;
namespace UnityDemo
{
    namespace UnityDemo
    {
        public class DamageableTset : NetworkBehaviour, IDamageable
        {
            [SerializeField]
            [Networked, OnChangedRender(nameof(OnHPChangedRender))]
            float _hp { get; set; } = 100f; // 生命值

            [SerializeField][Networked] float _maxHp { get; set; } = 100f; // 最大生命值

            [SerializeField]
            float _recoverValue = 25f; // 每秒回復生命值
            [SerializeField]
            Color _deadColor = Color.red; // 死亡顏色
            [SerializeField]
            private Renderer _renderer;
            [SerializeField]
            TMPro.TextMeshProUGUI _hpText;
            Color _normalColor;
            bool _initialized = false; // 是否已經初始化

            [SerializeField]
            private ParticleSystem _hitEffectPrefab; // 受擊特效預製物件

            private void Start()
            {
                _normalColor = _renderer.material.color; // 獲取初始顏色
            }

            public override void Render()
            {
                if (!_initialized)
                {
                    if (_renderer)
                    {
                        OnHPChangedRender();
                        _initialized = true;
                    }
                }
            }

            public override void FixedUpdateNetwork()
            {
                base.FixedUpdateNetwork();
                if (_hp < _maxHp)
                {
                    _hp += Time.fixedDeltaTime * _recoverValue; // 每秒回復5點生命值
                }
            }

            // Start is called once before the first execution of Update after the MonoBehaviour is created
            public void ApplyDamage(DamageData damageData)
            {
                Fusion.NetworkId source = damageData.Source;
                if (!source.IsValid)
                {
                    Debug.LogError("DamageableTset: Source is null");
                    return;
                }
                if (_hp > 0)
                {
                    _hp -= damageData.DamageAmount;
                }
                else
                    _hp = 0;
                OnHPChangedRender();
                RPC_OnHitEffect(damageData.HitPosition, damageData.HitNormal, Runner.LocalPlayer);
            }

            private void OnHPChangedRender()
            {
                float hpPercent = _hp / _maxHp;//[0,1]
                _renderer.material.color = Color.Lerp(_deadColor, _normalColor, hpPercent);
                if (_hpText)
                    _hpText.text = $"{(int)_hp}/{(int)_maxHp}"; // 更新UI顯示
            }

            //RPC_OnHitEffect(damageData.HitPosition, damageData.HitNormal, Runner.LocalPlayer);
            [Rpc(RpcSources.StateAuthority, RpcTargets.All, HostMode = RpcHostMode.SourceIsServer)]
            public void RPC_OnHitEffect(Vector3 hitPosition, Vector3 hitNormal, PlayerRef source)
            {
                //Debug.LogError("DamageableTset: RPC_OnHitEffect");
                //TODO: 未來可以改成object pool
                var hitEffect = Instantiate(_hitEffectPrefab, hitPosition + hitNormal * 0.02f, Quaternion.LookRotation(hitNormal));
                var main = hitEffect.main;
                main.stopAction = ParticleSystemStopAction.Destroy;
            }
        }
    }
}