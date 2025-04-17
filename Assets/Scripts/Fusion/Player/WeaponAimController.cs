using UnityEngine;
namespace UnityDemo
{
    public class WeaponAimController : MonoBehaviour
    {
        [SerializeField] LayerMask _layerMask;
        [SerializeField] float _raycastDistance = 1000f;
        [SerializeField] float _aimSmoothFactor = 10f;

        [Header("Model")]
        [SerializeField] PlayerNetworkModel _model;

        private Vector3 _currentHitPos = default;
        public delegate void OnAimToPositionChangedEvent(Vector3 aimPosition);
        public OnAimToPositionChangedEvent OnAimAtPositionChanged;

        internal void Initialize(PlayerNetworkModel model)
        {
            _model = model;
        }

        //called in Player::Update(), only when hasInputAuthority is true
        public void UpdateAimPosition(float deltaTime)
        {
            if (!TryGetCurrentWeapon(out IWeapon currentWeapon) || Camera.main == null)
                return;

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

            Vector3 hitPos = default;
            if (nearestDistance < float.MaxValue)
            {
                hitPos = nearestHit.point;
            }
            else
            {
                hitPos = screenRay.origin + screenRay.direction * _raycastDistance;
            }
            if (_currentHitPos == default)
                OnAimAtPositionChanged?.Invoke(hitPos);
            else
            {
                hitPos = Vector3.Lerp(_currentHitPos, hitPos, deltaTime * _aimSmoothFactor);
                OnAimAtPositionChanged?.Invoke(hitPos);
            }
            _currentHitPos = hitPos;
        }

        public void BuildShootRequest(ref ShootRequestData request)
        {
            request.LayerMaskValue = _layerMask;
        }

        private bool TryGetCurrentWeapon(out IWeapon weapon)
        {
            if (_model == null || !_model.IsInitialized)
            {
                weapon = null;
                return false;
            }
            weapon = _model.GetCurrentWeaponCached;
            if (weapon == null)
            {
                //Debug.LogWarning("No current weapon assigned");
                return false;
            }
            return true;
        }

    }
}