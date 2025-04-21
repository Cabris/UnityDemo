using Fusion;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityDemo
{

    public class WeaponUtility
    {
        public static bool TryGetWeaponObjFromRef(NetworkWeaponStruct weaponRef, out WeaponObjectBase weapon)
        {
            var runner = GameManager.Instance.NetworkRunner;
            if (runner == null)
            {
                weapon = null;
                return false;
            }
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



    public class CircularArrayQueue<T>
    {
        private T[] arr;
        private int front, size;
        private int capacity;

        // Constructor to initialize the queue
        public CircularArrayQueue(int c)
        {
            arr = new T[c];
            capacity = c;
            size = 0;
            front = 0;
        }

        // Get the front element
        public T GetFront()
        {
            if (size == 0)
                throw new InvalidOperationException("Queue is empty");
            return arr[front];
        }

        // Get the rear element
        public T GetRear()
        {
            if (size == 0)
                throw new InvalidOperationException("Queue is empty");
            int rear = (front + size - 1) % capacity;
            return arr[rear];
        }

        // Insert an element at the rear
        public void Enqueue(T x)
        {
            if (size == capacity)
                throw new InvalidOperationException("Queue is full");
            int rear = (front + size) % capacity;
            arr[rear] = x;
            size++;
        }

        // Remove an element from the front
        public T Dequeue()
        {
            if (size == 0)
                throw new InvalidOperationException("Queue is empty");
            T res = arr[front];
            front = (front + 1) % capacity;
            size--;
            return res;
        }

        // Optional: check if empty or full
        public bool IsEmpty() => size == 0;
        public bool IsFull() => size == capacity;
        public int Count => size;
    }


}