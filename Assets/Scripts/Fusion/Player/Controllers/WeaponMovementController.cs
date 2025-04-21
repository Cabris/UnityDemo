using Fusion;
using UnityEngine;
namespace UnityDemo
{
    public class WeaponMovementController
    {
        private Transform _rootTF;
        private IPlayerNetworkModel _model;
        private PlayerControllerProperties _properties;

        private Vector3 _currentHitPos = default;
        public delegate void OnAimToPositionChangedEvent(Vector3 aimPosition);
        public OnAimToPositionChangedEvent OnAimAtPositionChanged;

        internal void Initialize(Transform root, IPlayerNetworkModel model, PlayerControllerProperties properties)
        {
            _rootTF = root;
            _model = model;
            _properties = properties;
        }


        //TODO: move to PlayerWeaponHandler
        internal void HandleWeaponInput(NetworkId owner, NetworkInputData newInput)
        {
            NetworkButtons previousButtons = _model.PreviousButtons;
            if (_model.WeaponState.NT_CurrentArmedState == ArmedType.Aiming)
            {
                _model.WeaponState.NT_AimAtPosition = newInput.aimAtPosition;
            }
            var curtentWeapon = _model.WeaponState.GetCurrentWeaponCached;
            if (newInput.buttons.WasPressed(previousButtons, PlayerInputButtons.Aim))
            {
                _model.WeaponState.NT_IsAiming = !_model.WeaponState.NT_IsAiming;
            }

            if (curtentWeapon == null || _model.WeaponState.NT_CurrentArmedState != ArmedType.Aiming)
            {
                return;
            }

            // Handle input for shooting
            ShootRequestData data = default;
            data.Requester = owner;
            data.AimAtPosition = _model.WeaponState.NT_AimAtPosition;
            data.LayerMaskValue = _properties._layerMask;

            if (newInput.buttons.WasPressed(previousButtons, PlayerInputButtons.Attack))
            {
                data.FireType = FireInputType.Pressed;
                curtentWeapon.HandleShootRequest(data);
            }
            if (newInput.buttons.WasReleased(previousButtons, PlayerInputButtons.Attack))
            {
                data.FireType = FireInputType.Released;
                curtentWeapon.HandleShootRequest(data);
            }
            if (newInput.buttons.IsSet(PlayerInputButtons.Attack))
            {
                data.FireType = FireInputType.Hold;
                curtentWeapon.HandleShootRequest(data);
            }
        }

        //called in Player::Update(), only when hasInputAuthority is true
        public void UpdateAimPosition(float deltaTime)
        {
            if (!TryGetCurrentWeapon(out IWeapon currentWeapon) || Camera.main == null || _model == null)
                return;

            Vector3 rayStart = currentWeapon.RayCastFire.position;
            var screenRay = Camera.main.ScreenPointToRay(new Vector2(Screen.width / 2, Screen.height / 2));

            Vector3 rayOrigin = screenRay.origin;
            Vector3 rayDir = screenRay.direction.normalized;
            Vector3 toRayStart = rayStart - rayOrigin;
            float projectedLength = Vector3.Dot(toRayStart, rayDir);
            Vector3 closestPoint = rayOrigin + rayDir * projectedLength;

            RaycastHit[] hits = new RaycastHit[10];
            int count = Physics.RaycastNonAlloc(closestPoint, rayDir, hits, _properties._raycastDistance, _properties._layerMask);
            RaycastHit nearestHit = default;
            float nearestDistance = float.MaxValue;
            if (count > 0)
            {
                for (int i = 0; i < count; i++)
                {
                    var hit = hits[i];
                    if (hit.transform == _rootTF || hit.transform.IsChildOf(_rootTF)) // is self
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
                hitPos = screenRay.origin + screenRay.direction * _properties._raycastDistance;
            }
            if (_currentHitPos == default)
                OnAimAtPositionChanged?.Invoke(hitPos);
            else
            {
                hitPos = Vector3.Lerp(_currentHitPos, hitPos, deltaTime * _properties._aimSmoothFactor);
                OnAimAtPositionChanged?.Invoke(hitPos);
            }
            _currentHitPos = hitPos;
        }

        private bool TryGetCurrentWeapon(out IWeapon weapon)
        {
            if (_model == null || !_model.IsInitialized)
            {
                weapon = null;
                return false;
            }
            weapon = _model.WeaponState.GetCurrentWeaponCached;
            if (weapon == null)
            {
                //Debug.LogWarning("No current weapon assigned");
                return false;
            }
            return true;
        }

    }
}