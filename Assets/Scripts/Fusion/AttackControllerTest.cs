using Fusion;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
namespace UnityDemo
{
    public class AttackControllerTest : NetworkBehaviour
    {
        [Networked] private TickTimer _attackDelay { get; set; }
        [SerializeField] private Ball _prefabBall;
        [SerializeField] private PhysxBall _prefabPhysxBall;
        [SerializeField] private float _delay = 0.1f;
        [Networked] public bool spawnedProjectile { get; set; }
        private ChangeDetector _changeDetector;

        [Networked]
        [Capacity(4)] // Sets the fixed capacity of the collection
        NetworkArray<int> NetArray { get; }
        // Optional initialization
        = MakeInitializer(new int[] { 0, 1, 2, 3 });

        [Networked]
        [Capacity(4)] // Sets the fixed capacity of the collection
        [UnitySerializeField] // Show this private property in the inspector.
        private NetworkLinkedList<NetworkString<_32>> NetList { get; }
          = MakeInitializer(new NetworkString<_32>[] { "Zero", "One", "Two", "Four" });

        [Networked, Capacity(4)]
        NetworkDictionary<NetworkString<_32>, NetworkString<_32>> NetDict { get; }
        //Optional initialization
        = MakeInitializer(new Dictionary<NetworkString<_32>, NetworkString<_32>> {
            { "k0", "v0" }, { "k1", "v1" } });


        public struct NetworkStructExample : INetworkStruct
        {
            public int IntField;
        }

        [Networked]
        public ref NetworkStructExample NetworkedStructRef => ref MakeRef<NetworkStructExample>();


        public override void Spawned()
        {
            _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);
            /*
            for (int i = 0; i < NetArray.Length; ++i)
            {
                Debug.Log($"NetArray: {i}: '{NetArray[i]}''");
            }


            NetDict.Clear();
            NetDict.Add("k0", "Zero");
            NetDict.Add("k2", "Two");
            NetDict.Add("k2", "Two_2");
            NetDict.Set("k3", "THREE");
            foreach (KeyValuePair<NetworkString<_32>, NetworkString<_32>> item in NetDict)
            {
                Debug.Log($"NetDict: {item.Key}: '{item.Value}'");
            }



            // Remove the second entry, leaving one open capacity.
            NetList.Remove("One");
            // Find an entry by value
            NetList.Set(NetList.IndexOf("Two"), "TWO");
            // Add a new entry. In memory it backfills the now open memory position.
            NetList.Add("Five");
             
            // The indexed order however remains in sequence,
            // so only the changed memory position is dirty and networked.
            Debug.Log($"List {NetList.Count}/{NetList.Capacity}: " +
              $"0:'{NetList[0]}' 1:'{NetList[1]}' 2:'{NetList[2]} 3:'{NetList[3]}'");
            */
        }

        public override void FixedUpdateNetwork()
        {
            if (GetInput(out NetworkInputData data))
            {
                UpdateAttackInputs(data.buttons);
            }
        }

        private void UpdateAttackInputs(in NetworkButtons buttons)
        {
            if (HasStateAuthority)//only player in host can spawn ball
            {
                //always on pressing
                if (buttons.IsSet(PlayerInputButtons.Attack) && _attackDelay.ExpiredOrNotRunning(Runner))
                {
                    _attackDelay = TickTimer.CreateFromSeconds(Runner, _delay);
                    Runner.Spawn(_prefabBall,
                    transform.position + transform.forward, Quaternion.LookRotation(transform.forward),
                    Object.InputAuthority, OnBeforeBallSpawned);

                    void OnBeforeBallSpawned(NetworkRunner runner, NetworkObject obj)
                    {
                        if (obj.TryGetComponent(out Ball ball))
                        {
                            ball.Init();
                        }
                    }

                    _attackDelay = TickTimer.CreateFromSeconds(Runner, _delay);
                    Runner.Spawn(_prefabPhysxBall,
                      transform.position + transform.forward + transform.up,
                      Quaternion.LookRotation(transform.forward),
                      Object.InputAuthority,
                      (runner, o) =>
                      {
                          o.GetComponent<PhysxBall>().Init(10 * transform.forward);
                      });

                    spawnedProjectile = !spawnedProjectile;
                }
            }

        }

        public override void Render()
        {
            foreach (var change in _changeDetector.DetectChanges(this))
            {
                switch (change)
                {
                    case nameof(spawnedProjectile):
                        //_material.color = Color.white;
                        SimpleLogger.Log($"Change detected: spawnedProjectile: {spawnedProjectile}");
                        //spawnedProjectile = !spawnedProjectile;

                        if (HasInputAuthority)
                        {
                            //RPC_SendMessage("Hey Mate!");
                        }

                        break;
                }
            }
        }


        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority, HostMode = RpcHostMode.SourceIsHostPlayer)]
        public void RPC_SendMessage(string message, RpcInfo info = default)
        {
            SimpleLogger.Log($"RPC_SendMessage: HasInputAuthority = {HasInputAuthority}," +
               $" HasStateAuthority = {HasStateAuthority}, message: {message}");
            RPC_RelayMessage(message, info.Source);
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All, HostMode = RpcHostMode.SourceIsServer)]
        public void RPC_RelayMessage(string message, PlayerRef messageSource)
        {
            if (messageSource == Runner.LocalPlayer)
            {
                message = $"You said: {message}";
            }
            else
            {
                message = $"Some other player said: {message}";
            }

            SimpleLogger.Log($"RPC_RelayMessage: HasInputAuthority = {HasInputAuthority}," +
                $" HasStateAuthority = {HasStateAuthority}, message: {message}");

            //_messages.text += message;
        }
    }
}