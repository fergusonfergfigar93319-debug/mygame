using System;
using System.Collections.Generic;
using UnityEngine;

namespace PenguinRun.Core
{
    /// <summary>
    /// 轻量服务定位器：集中管理跨系统共享服务，避免静态变量散落。
    /// 所有服务在场景 Awake 时注册，OnDestroy 时注销。
    /// </summary>
    public static class ServiceLocator
    {
        private static readonly Dictionary<Type, object> services = new();

        public static void Register<T>(T service) where T : class
        {
            var type = typeof(T);
            if (services.ContainsKey(type))
            {
                Debug.LogWarning($"ServiceLocator: {type.Name} already registered, replacing.");
            }
            services[type] = service;
        }

        public static T Get<T>() where T : class
        {
            var type = typeof(T);
            if (services.TryGetValue(type, out var service))
            {
                return service as T;
            }
            Debug.LogError($"ServiceLocator: {type.Name} not registered.");
            return null;
        }

        public static bool TryGet<T>(out T service) where T : class
        {
            var type = typeof(T);
            if (services.TryGetValue(type, out var obj))
            {
                service = obj as T;
                return service != null;
            }
            service = null;
            return false;
        }

        public static void Unregister<T>() where T : class
        {
            services.Remove(typeof(T));
        }

        public static void Clear()
        {
            services.Clear();
        }
    }
}
