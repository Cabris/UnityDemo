using Fusion;
using UnityEngine;
namespace UnityDemo
{
    public class PlayerSpawner : NetworkBehaviour
    {
        public override void Spawned()
        {
            base.Spawned();
            PlayerSpawnData spawnData = new PlayerSpawnData();
            spawnData.CharactorID = GameManager.Instance.GetPlayerChararctorID();
            if (spawnData.CharactorID < 0)
            {
                Debug.LogError("PlayerSpawner Spawned, but CharactorID is invalid");
                return;
            }

            spawnData.SpawnPosition = transform.position;
            spawnData.PlayerRef = Runner.LocalPlayer;
            spawnData.Name = GameManager.Instance.GetPlayerName();//WIP
            spawnData.Name = "Player_" + Runner.LocalPlayer.PlayerId.ToString();
            spawnData.SpawnerID = Object.Id;
            Debug.Log($"PlayerSpawner::Spawned: PlayerId = {spawnData.PlayerRef.PlayerId}");
            if (HasInputAuthority)
                RPC_OnPlayerReadyToSpawner(spawnData);
        }

        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority, HostMode = RpcHostMode.SourceIsHostPlayer)]
        public void RPC_OnPlayerReadyToSpawner(PlayerSpawnData spawnData, RpcInfo info = default)
        {
            Debug.Log($"RPC_SpawnPlayer: PlayerId = {spawnData.PlayerRef.PlayerId}");
            GameManager.Instance.OnOnPlayerReadyToSpawner(spawnData);
        }
    }
}