using Fusion;
using System;
using UnityEngine;
namespace UnityDemo
{
    public struct DamageData
    {
        public int DamageAmount;
        public NetworkId Source;
        public Vector3 HitPosition;
        internal Vector3 HitNormal;
    }

    public interface IDamageable
    {
        void ApplyDamage(DamageData damageData);
    }

    public interface IDamageManager
    {
        void HandleDamage(DamageInfo damageInfo);
    }

    [Serializable]
    public struct DamagePart
    {
        public string Name;
        public float DamageFactor;
    }

    public struct DamageInfo
    {
        public DamagePart damagePart;
        public DamageData damageData;
    }

    public class DamageReceiver : MonoBehaviour, IDamageable
    {
        [SerializeField]
        DamagePart damagePart;
        private IDamageManager _damageManager;

        private void Start()
        {
            _damageManager=GetComponentInParent<IDamageManager>();
        }

        public void ApplyDamage(DamageData damageData)
        {
            if (_damageManager == null)
                return;
            DamageInfo damageInfo = default;
            damageInfo.damageData = damageData;
            damageInfo.damagePart = damagePart;
            _damageManager.HandleDamage(damageInfo);
        }
    }

}