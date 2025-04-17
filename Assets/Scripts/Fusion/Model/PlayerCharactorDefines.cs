using Fusion;
using System;
using UnityEngine;
namespace UnityDemo
{
    [CreateAssetMenu(fileName = "PlayerCharactorDefines", menuName = "Scriptable Objects/PlayerCharactorDefines")]
    public class PlayerCharactorDefines : ScriptableObject
    {
        public PlayerCharactor[] _playerCharactors;

        [Serializable]
        public struct PlayerCharactor
        {
            public string name;
            public NetworkPrefabRef prefab;
        }
    }
}