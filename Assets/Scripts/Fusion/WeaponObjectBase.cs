using Fusion;
using System;
using UnityEngine;
namespace UnityDemo
{
    //TODO: ISP
    public interface IWeapon
    {
        Transform SelfTransform { get; }
        Transform HoldR { get; }
        Transform HoldL { get; }
        Transform RayCastFire { get; }
        NetworkBehaviour NetworkBehaviour { get; }
        ref NetworkWeaponStruct WeaponStructRef { get; }
        NetworkTransform NetworkTransform { get; }

        void AddToInventory();
        void HandleShootRequest(ShootRequestData request);
        void PlayEffects();
        void StopEffects();
    }


    //TODO: SRP: move VFX/Network to another classes
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
        public NetworkBehaviour NetworkBehaviour => this;
        [Networked] private ref NetworkWeaponStruct NT_WeaponStructRef => ref MakeRef<NetworkWeaponStruct>();
        [Networked, OnChangedRender(nameof(OnColliderEnableChangedRender))]
        NetworkBool NT_colliderEnabled { get; set; } // 用於控制碰撞器的啟用狀態

        //TODO: DIP: use scriptable object to define weapon behavior
        [SerializeField] StrategyRifle strategyRifle;
        IWeaponStrategy _strategy;

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

            //TODO:
            //未來_strategy可改成array來達成複數種行為
            //若request之後有擴充攻擊模式(射擊/進戰打擊/榴彈砲等基於同一把武器的不同行為)
            //可以透過切換不同的_strategy來達成
            //改為透過工廠 or ScriptableObject來DI
            if (_strategy == null)
            {
                _strategy = strategyRifle;
                _strategy.Initialize(this);
            }
        }

        private void OnColliderEnableChangedRender()
        {
            _collider.enabled = NT_colliderEnabled;
        }

        public void RemoveFromInventory()
        {
            NT_colliderEnabled = true;
        }

        public void AddToInventory()
        {
            Debug.Log($"AddToInventory: HasStateAuthority: {HasStateAuthority}, HasInputAuthority: {HasInputAuthority}");
            NT_colliderEnabled = false;
        }
        //called from Player::FixedUpdateNetwork::HandleWeaponAttack
        public void HandleShootRequest(ShootRequestData request)
        {
            if (!HasStateAuthority)
            {
                Debug.LogError($"WeaponObjectBase::Shoot: HasStateAuthority is false");
                return;
            }
            _strategy?.HandleRequest(request);
        }

        public override void FixedUpdateNetwork()
        {
            _strategy?.Update();
        }

        public void PlayEffects()
        {
            RPC_PlayEffect();
        }
        public void StopEffects()
        {
            RPC_StopEffect();
        }

        public ref NetworkWeaponStruct WeaponStructRef
        {
            get
            {
                return ref NT_WeaponStructRef;
            }
        }

        public Transform SelfTransform => transform;

        public NetworkTransform NetworkTransform => GetComponent<NetworkTransform>();

        [Rpc(RpcSources.StateAuthority, RpcTargets.All, HostMode = RpcHostMode.SourceIsServer)]
        public void RPC_PlayEffect()
        {
            _strategy?.PlayFireEffects();
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All, HostMode = RpcHostMode.SourceIsServer)]
        public void RPC_StopEffect()
        {
            _strategy?.StopFireEffects();
        }
    }

    [Serializable]
    public sealed class StrategyRifle : IWeaponStrategy
    {
        [SerializeField] ParticleSystem _muzzleFlash;
        [SerializeField] Animator _animator;

        private TickTimer _cooldown;
        private const float _cooldownTime = 0.15f; // 連射間隔
        const string _fireAniState = "Rifle_Fire";
        const string _staticAniState = "Static";
        private readonly RaycastHit[] _hits = new RaycastHit[3];

        private const float _maxDistance = 100f; // 射擊距離
        private const int _damageAmount = 10; // 射擊傷害
        private IWeapon _weapon = null;
        private NetworkBehaviour _networkBehaviour = null;
        private ShootRequestData? _request;

        public void Initialize(IWeapon weapon)
        {
            _weapon = weapon;
            _networkBehaviour = weapon.NetworkBehaviour;
            _request = null;
            _animator = _networkBehaviour.GetComponent<Animator>();
            _muzzleFlash.Stop();
            var main = _muzzleFlash.main;
            main.simulationSpeed = 5f;
        }

        public void HandleRequest(ShootRequestData request)
        {
            if (!_networkBehaviour)
                return;
            switch (request.FireType)
            {
                case FireInputType.Pressed:
                    {
                        // 按下射擊
                        _request = request;
                        _weapon.PlayEffects();
                        //Debug.Log($"StrategyRifle::Update: 按下射擊");
                    }
                    break;
                case FireInputType.Released:
                case FireInputType.Cancelled:
                    {
                        // 釋放射擊
                        _request = null; // 清除射擊請求
                        _weapon.StopEffects();
                        //Debug.Log($"StrategyRifle::Update: 釋放射擊");
                        return;
                    }
                case FireInputType.Hold:
                    {
                        _request = request;
                        //Debug.Log($"StrategyRifle::Update: 持續射擊");
                    }
                    break;
            }
        }

        public void StopFireEffects()
        {
            _animator.Play(_staticAniState, -1, 0.0f);
            _muzzleFlash.Stop();
        }

        public void PlayFireEffects()
        {
            _animator.Play(_fireAniState, -1, 0.0f);
            _muzzleFlash.Play();
        }

        public void Update()
        {
            if (_request == null)
                return;
            var shootData = _request.Value;

            if (!IsAvailable())
                return;

            var runner = _networkBehaviour.Runner;
            _cooldown = TickTimer.CreateFromSeconds(runner, _cooldownTime);

            LayerMask layerMask = shootData.LayerMaskValue;
            Vector3 aimAt = shootData.AimAtPosition;
            Vector3 origin = _weapon.RayCastFire.position;
            Vector3 direction = Vector3.Normalize(aimAt - origin);

            int hitCount = Physics.RaycastNonAlloc(origin, direction, _hits, _maxDistance, layerMask);

            for (int i = 0; i < hitCount; i++)
            {
                var hit = _hits[i];
                if (hit.collider.TryGetComponent(out IDamageable dmg))
                {
                    var damage = new DamageData();
                    damage.HitPosition = hit.point;
                    damage.HitNormal = hit.normal;
                    damage.DamageAmount = _damageAmount;
                    damage.Source = shootData.Requester;
                    dmg.ApplyDamage(damage);
                }
            }
        }

        public bool IsAvailable()
        {
            if (_networkBehaviour == null || !_networkBehaviour.isActiveAndEnabled)
                return false;
            var runner = _networkBehaviour.Runner;
            return _cooldown.ExpiredOrNotRunning(runner);
        }
    }

    public interface IWeaponStrategy
    {
        void Initialize(IWeapon weapon);
        void HandleRequest(ShootRequestData request);
        void Update();
        bool IsAvailable();
        void PlayFireEffects();
        void StopFireEffects();
    }
}
