using Fusion;
using System;
using UnityEngine;

namespace UnityDemo
{
    public enum PlayerInputButtons
    {
        Jump=0,//¸õÅDÁä
        Attack=1,  // ¶}¤õ
        Sprint=2,  // ½Ä¨ë
    }

    [Serializable]
    public struct NetworkInputData : INetworkInput
    {
        public Vector2 moveDelta;
        public Vector2 lookDelta;
        public Vector3 lookRotationEuler;
        public NetworkButtons buttons;
        public NetworkBool analogMovement;
        public NetworkBool rotateTowardMovement;
        public void Reset()
        {
            moveDelta = lookDelta = Vector2.zero;
            analogMovement = false;
            lookRotationEuler= default;
            //jump = sprint = false;
            buttons.SetAllUp();
        }

        
    }
}