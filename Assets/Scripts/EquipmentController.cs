using System;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.InputSystem;
namespace UnityDemo
{
    [Serializable]
    public struct EquipmentPoint {
        public TwoBoneIKConstraint constraint;
        public Transform ikHand;
        public Transform targetHand;
    }


    public class EquipmentController : MonoBehaviour
    {
        [SerializeField]
        EquipmentPoint _leftHandEquipmentPoint, _rightHandEquipmentPoint;

        [SerializeField]
        Transform  _target;
        [SerializeField]
        LayerMask _layerMask;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
        }

        // Update is called once per frame
        void Update()
        {
            _rightHandEquipmentPoint.ikHand.position = _rightHandEquipmentPoint.targetHand.position;
            _rightHandEquipmentPoint.ikHand.rotation = _rightHandEquipmentPoint.targetHand.rotation;

            _leftHandEquipmentPoint.ikHand.position = _leftHandEquipmentPoint.targetHand.position;
            _leftHandEquipmentPoint.ikHand.rotation = _leftHandEquipmentPoint.targetHand.rotation;
        }

        private void FixedUpdate()
        {
            var ray = Camera.main.ScreenPointToRay(new Vector2(Screen.width / 2, Screen.height / 2));
            Debug.DrawRay(ray.origin, ray.direction * 100, Color.red);
            if (Physics.Raycast(ray, out var hit, 100000, _layerMask))
            {
                //Debug.Log(hit.collider.gameObject.name);
                _target.position = hit.point;
            }
        }

        [SerializeField]
        bool isAttack= false;

        public void OnAttack(InputValue value)
        {
            isAttack = value.isPressed;
        }
    }
}