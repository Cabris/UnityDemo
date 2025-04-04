using Fusion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace UnityDemo
{
    [Serializable]
    public class PlayerControllerStatus
    {
        public Action onPlayerNameChanged;

        [Networked] public Vector3 NT_moveVelocity { get; set; }

        [Networked] public float NT_motionSpeedMultiply { get; set; }

        [Networked] public Vector3 NT_lookRotationEuler { get; set; }

        [Networked] public float NT_turningSpeed { get; set; }

        [Networked] public NetworkButtons NT_previousButtons { get; set; }

        [Networked] public NetworkBool NT_isStrafe { get; set; }

        [Networked] public NetworkBool NT_isSprint { get; set; }

        [Networked, OnChangedRender(nameof(OnPlayerNameChanged))]
        public string NT_playerName { get; set; }

        public bool IsJump=false;

        private void OnPlayerNameChanged()
        {
            onPlayerNameChanged?.Invoke();
        }
    }


    [Serializable]
    public class PlayerControllerProperties
    {
        [Header("Movement Setup")]
        public float WalkSpeed = 2f;
        public float RunSpeed = 5f;
        public float SprintSpeed = 5f;
        public float JumpImpulse = 10f;
        public float UpGravity = -25f;
        public float DownGravity = -40f;
        public float VerticalimpulseVelocity = 10;


        [Header("Movement Accelerations")]
        public float GroundAcceleration = 55f;
        public float GroundDeceleration = 25f;
        public float AirAcceleration = 25f;
        public float AirDeceleration = 1.3f;

        [Header("Movement Rotation")]
        [Tooltip("How fast the character turns to face movement direction")]
        [Range(0.0f, 0.3f)]
        public float RotationSmoothTime = 0.12f;
        public float turnSpeedMultiply = 50f;
    }

}
