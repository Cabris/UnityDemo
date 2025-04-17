using UnityEngine;
using System.Collections.Generic;
using System;
[CreateAssetMenu(fileName = "BoneSnapshot", menuName = "Scriptable Objects/BoneSnapshot")]
public class BoneSnapshot : ScriptableObject
{
    [Serializable]
    public struct BoneData
    {
        public string name;
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;
    }

    [SerializeField]
    public List<BoneData> boneDatas;

    public bool TryFinedBoneData(string name, out BoneData boneData)
    {
        foreach (var data in boneDatas)
        {
            if (data.name == name)
            {
                boneData = data;
                return true;
            }
        }
        boneData = default;
        return false;
    }

}
