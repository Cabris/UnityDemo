using UnityEngine;

public class BoneRecoder : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    public BoneSnapshot _boneSnapshot;
    public Transform _root;

    [ContextMenu("TakeSnapshot")]
    public void TakeSnapshot()
    {
        if (!_boneSnapshot || !_root)
            return;

        _boneSnapshot.boneDatas.Clear();
        StoreBoneData(_root);
    }

    private void StoreBoneData(Transform bone)
    {
        BoneSnapshot.BoneData data = new BoneSnapshot.BoneData
        {
            position = bone.localPosition,
            rotation = bone.localRotation,
            scale = bone.localScale,
            name = bone.name
        };
        _boneSnapshot.boneDatas.Add(data);

        foreach (Transform child in bone)
        {
            StoreBoneData(child);
        }
    }

    private void RestoreBoneData(Transform bone)
    {
        if (_boneSnapshot.TryFinedBoneData(bone.name, out BoneSnapshot.BoneData data))
        {
            bone.localPosition = data.position;
            bone.localRotation = data.rotation;
            bone.localScale = data.scale;
        }
        else
        {
            Debug.LogWarning($"Bone data not found for {bone.name}");
        }

        foreach (Transform child in bone)
        {
            RestoreBoneData(child);
        }
    }

    [ContextMenu("SetFromSnapshot")]
    public void SetFromSnapshot()
    {
        if (!_boneSnapshot || !_root)
            return;

        RestoreBoneData(_root);
    }

}
