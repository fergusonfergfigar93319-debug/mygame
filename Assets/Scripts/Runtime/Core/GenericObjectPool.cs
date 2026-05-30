using System;
using System.Collections.Generic;
using UnityEngine;

namespace PenguinRun.Core
{
    /// <summary>
    /// 泛型对象池：T 必须是 Component。统一替代手动 List 管理。
    /// 用法：var pool = new GenericObjectPool<MyEffect>(prefab, parent, 32);
    ///       var obj = pool.Rent(); pool.Return(obj);
    /// </summary>
    public sealed class GenericObjectPool<T> where T : Component
    {
        private readonly Stack<T> available = new();
        private readonly List<T> active = new();
        private readonly T prefab;
        private readonly Transform parent;
        private readonly int maxCapacity;

        public IReadOnlyList<T> Active => active;

        public GenericObjectPool(T prefab, Transform parent, int initialCapacity = 32, int maxCapacity = 256)
        {
            this.prefab = prefab;
            this.parent = parent;
            this.maxCapacity = maxCapacity;

            for (var i = 0; i < initialCapacity; i++)
            {
                var obj = UnityEngine.Object.Instantiate(prefab, parent);
                obj.gameObject.SetActive(false);
                available.Push(obj);
            }
        }

        public T Rent()
        {
            T obj;
            if (available.Count > 0)
            {
                obj = available.Pop();
            }
            else if (active.Count < maxCapacity)
            {
                obj = UnityEngine.Object.Instantiate(prefab, parent);
            }
            else
            {
                // 池耗尽：回收最旧的活跃对象
                obj = active[0];
                active.RemoveAt(0);
                obj.gameObject.SetActive(false);
            }

            obj.gameObject.SetActive(true);
            active.Add(obj);
            return obj;
        }

        public void Return(T obj)
        {
            if (obj == null) return;
            obj.gameObject.SetActive(false);
            obj.transform.SetParent(parent, false);
            active.Remove(obj);
            if (available.Count < maxCapacity)
            {
                available.Push(obj);
            }
            else
            {
                UnityEngine.Object.Destroy(obj.gameObject);
            }
        }

        public void ReleaseAll()
        {
            foreach (var obj in active)
            {
                if (obj != null)
                {
                    obj.gameObject.SetActive(false);
                    available.Push(obj);
                }
            }
            active.Clear();
        }

        public void Clear()
        {
            ReleaseAll();
            while (available.Count > 0)
            {
                var obj = available.Pop();
                if (obj != null) UnityEngine.Object.Destroy(obj.gameObject);
            }
        }
    }

    /// <summary>
    /// 纯 GameObject 对象池（无 Component 约束）。
    /// </summary>
    public sealed class GameObjectPool
    {
        private readonly Stack<GameObject> available = new();
        private readonly List<GameObject> active = new();
        private readonly Func<GameObject> factory;
        private readonly Transform parent;
        private readonly int maxCapacity;

        public IReadOnlyList<GameObject> Active => active;

        public GameObjectPool(Func<GameObject> factory, Transform parent, int initialCapacity = 32, int maxCapacity = 256)
        {
            this.factory = factory;
            this.parent = parent;
            this.maxCapacity = maxCapacity;

            for (var i = 0; i < initialCapacity; i++)
            {
                var obj = factory();
                obj.transform.SetParent(parent, false);
                obj.SetActive(false);
                available.Push(obj);
            }
        }

        public GameObject Rent()
        {
            GameObject obj;
            if (available.Count > 0)
            {
                obj = available.Pop();
            }
            else if (active.Count < maxCapacity)
            {
                obj = factory();
                obj.transform.SetParent(parent, false);
            }
            else
            {
                obj = active[0];
                active.RemoveAt(0);
                obj.SetActive(false);
            }

            obj.SetActive(true);
            active.Add(obj);
            return obj;
        }

        public void Return(GameObject obj)
        {
            if (obj == null) return;
            obj.SetActive(false);
            obj.transform.SetParent(parent, false);
            active.Remove(obj);
            if (available.Count < maxCapacity)
                available.Push(obj);
            else
                UnityEngine.Object.Destroy(obj);
        }

        public void ReleaseAll()
        {
            foreach (var obj in active)
            {
                if (obj != null)
                {
                    obj.SetActive(false);
                    available.Push(obj);
                }
            }
            active.Clear();
        }
    }
}
