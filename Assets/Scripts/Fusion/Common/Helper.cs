using Fusion;
using System.Collections.Generic;
using UnityEngine;

namespace UnityDemo
{

    public class WeaponUtility
    {
        public static bool TryGetWeaponObjFromRef(NetworkRunner runner, NetworkWeaponStruct weaponRef, out WeaponObjectBase weapon)
        {
            weapon = null;
            if (!weaponRef.IsValid)
                return false;
            if (runner.TryFindObject(weaponRef.WeaponId, out var obj) && obj.TryGetBehaviour(out weapon))
                return true;
            return false;
        }
    }


    public class Helper
    {
        public static GameObject SearchByTag(GameObject root, string target_tag)
        {
            Queue<Transform> queue = new Queue<Transform>();
            queue.Enqueue(root.transform);

            while (queue.Count > 0)
            {
                Transform current = queue.Dequeue();
                if (current.CompareTag(target_tag))
                {
                    return current.gameObject;
                }

                foreach (Transform child in current)
                {
                    queue.Enqueue(child);
                }
            }

            return null;
        }
    }
}