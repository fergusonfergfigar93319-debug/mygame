using System;
using System.Collections.Generic;
using UnityEngine;

namespace PenguinRun.Core
{
    /// <summary>
    /// 泛型对象池（VFX / 临时效果专用）。
    ///
    /// 池满时回收最旧的活跃对象——适合粒子、冲击环等短生命周期效果。
    /// 不适合仍被游戏逻辑引用的实体（障碍物、敌人等），迁移时注意区分。
    ///
    /// 用法：var pool = new GenericObjectPool&lt;MyEffect&gt;(prefab, parent, 32);
    /// </summary>
    public sealed class GenericObjectPool<T> where T : Component
    {
        private readonly Stack<T> available = new();
        private readonly List<T> active = new();
        private readonly T prefab;
        private readonly Transform parent;
        private readonly int maxCapacity;
        private readonly bool recycleOnFull; // true=回收最旧对象(VFX), false=拒绝分配

        public IReadOnlyList<T> Active => active;

        /// <param name="recycleOnFull">true=VFX模式(池满回收最旧); false=安全模式(池满不分配)</param>
        public GenericObjectPool(T prefab, Transform parent, int initialCapacity = 32, int maxCapacity = 256, bool recycleOnFull = true)
        {
            this.prefab = prefab;
            this.parent = parent;
            this.maxCapacity = maxCapacity;
            this.recycleOnFull = recycleOnFull;

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
            else if (recycleOnFull)
            {
                // VFX 模式：池耗尽时回收最旧的活跃对象
                obj = active[0];
                active.RemoveAt(0);
                obj.gameObject.SetActive(false);
            }
            else
            {
                // 安全模式：池满不分配，避免误回收仍被引用的实体
                Debug.LogWarning($"GenericObjectPool<{typeof(T).Name}>: pool exhausted ({maxCapacity}), refusing to rent.");
                return null;
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
    /// recycleOnFull=true → VFX 模式（池满回收最旧）；false → 安全模式（池满拒绝+告警）。
    /// </summary>
    public sealed class GameObjectPool
    {
        private readonly Stack<GameObject> available = new();
        private readonly List<GameObject> active = new();
        private readonly Func<GameObject> factory;
        private readonly Transform parent;
        private readonly int maxCapacity;
        private readonly bool recycleOnFull;

        public IReadOnlyList<GameObject> Active => active;

        public GameObjectPool(Func<GameObject> factory, Transform parent,
            int initialCapacity = 32, int maxCapacity = 256, bool recycleOnFull = true)
        {
            this.factory = factory;
            this.parent = parent;
            this.maxCapacity = maxCapacity;
            this.recycleOnFull = recycleOnFull;

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
            else if (recycleOnFull)
            {
                obj = active[0];
                active.RemoveAt(0);
                obj.SetActive(false);
            }
            else
            {
                Debug.LogWarning($"GameObjectPool: pool exhausted ({maxCapacity}), refusing to rent.");
                return null;
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
