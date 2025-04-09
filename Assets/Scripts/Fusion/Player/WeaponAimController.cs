using Fusion;
using System;
using UnityEngine;
using UniVRM10;
namespace UnityDemo
{
    public class WeaponAimController : MonoBehaviour
    {
        [SerializeField]
        Transform _aimLookAt;
        [SerializeField]
        LayerMask _layerMask;
        [SerializeField]
        float _raycastDistance = 1000f;

        [Header("Model")]
        [SerializeField] PlayerNetworkModel _model;

        internal void Initialize(PlayerNetworkModel model)
        {
            _model = model;
        }

        public void UpdateAimPosition()
        {
            if (!_model || !_model.IsInitialized)
            {
                return;
            }

            ref var equip = ref _model.Equipment;
            var currentWeapon = _model.GetCurrentWeaponCached;

            if (currentWeapon == null || _model.CurrentArmedState != ArmedType.Aiming)
            {
                //Debug.LogWarning("No current weapon assigned");
                return;
            }
            Vector3 rayStart = currentWeapon.RayCastFire.position;
            var screenRay = Camera.main.ScreenPointToRay(new Vector2(Screen.width / 2, Screen.height / 2));

            Vector3 rayOrigin = screenRay.origin;
            Vector3 rayDir = screenRay.direction.normalized;
            Vector3 toRayStart = rayStart - rayOrigin;
            float projectedLength = Vector3.Dot(toRayStart, rayDir);
            Vector3 closestPoint = rayOrigin + rayDir * projectedLength;

            RaycastHit[] hits = new RaycastHit[10];
            int count = Physics.RaycastNonAlloc(closestPoint, rayDir, hits, _raycastDistance, _layerMask);
            RaycastHit nearestHit = default;
            float nearestDistance = float.MaxValue;
            if (count > 0)
            {
                for (int i = 0; i < count; i++)
                {
                    var hit = hits[i];
                    if (hit.transform == transform || hit.transform.IsChildOf(transform)) // is self
                        continue;
                    // find the closest hit
                    float distance = Vector3.Distance(rayStart, hit.point);
                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        nearestHit = hit;
                    }
                }
            }

            if (nearestDistance < float.MaxValue)
            {
                _aimLookAt.position = nearestHit.point;
               // Debug.Log($"Hit: {nearestHit.transform.name}");
            }
            else
            {
                _aimLookAt.position = screenRay.origin + screenRay.direction * _raycastDistance;
            }

            // 傳送打中物件資訊給 Host（其實是自己）
            // RPC_ApplyHit(hit.transform.GetComponent<NetworkObject>());

        }


    }
}