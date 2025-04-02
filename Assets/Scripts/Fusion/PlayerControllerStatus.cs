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

        private void OnPlayerNameChanged()
        {
            onPlayerNameChanged?.Invoke();
        }
    }

}
