using Fusion;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityDemo
{
    [Serializable]
    public class PlayerControllerProperties
    {
        [Header("Movement Setup")]
        public float WalkSpeed = 2f;
        public float RunSpeed = 5f;
        public float SprintSpeed = 5f;
        public float JumpImpulse = 280f;
        public float UpGravity = -25f;
        public float DownGravity = -27f;
        public float VerticalimpulseVelocity = 10;

        [Header("Movement Accelerations")]
        public float GroundAcceleration = 20f;
        public float GroundDeceleration = 25f;
        public float AirAcceleration = 25f;
        public float AirDeceleration = 1.3f;

        [Header("Movement Rotation")]
        [Tooltip("How fast the character turns to face movement direction")]
        [Range(0.0f, 0.3f)]
        public float RotationSmoothTime = 0.219f;
        public float turnSpeedMultiply = 50f;

        [Header("Weapon Aim")]
        public LayerMask _layerMask;
        public float _raycastDistance = 1000f;
        public float _aimSmoothFactor = 30f;
    }
}
