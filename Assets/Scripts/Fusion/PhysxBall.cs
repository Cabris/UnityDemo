using Fusion;
using UnityEngine;
namespace UnityDemo
{

    public class PhysxBall : NetworkBehaviour
    {
        [Networked] private TickTimer life { get; set; }

        public void Init(Vector3 linearVelocity)
        {
            life = TickTimer.CreateFromSeconds(Runner, 5.0f);
            GetComponent<Rigidbody>().linearVelocity = linearVelocity;
        }

        public override void FixedUpdateNetwork()
        {
            if (life.Expired(Runner))
                Runner.Despawn(Object);
        }
    }
}