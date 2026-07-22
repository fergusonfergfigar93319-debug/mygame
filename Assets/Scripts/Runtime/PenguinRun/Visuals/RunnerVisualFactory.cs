using System.Collections.Generic;
using UnityEngine;

namespace PenguinRun
{
    /// <summary>Cached prefab attachment with deterministic ownership and explicit procedural fallback.</summary>
    public sealed class RunnerVisualFactory
    {
        public const string AttachmentName = "CustomVisual";

        private readonly Dictionary<string, GameObject> prefabCache = new();
        private readonly HashSet<string> missingLog = new();

        public bool TryAttachObstacle(Transform root, RunnerMapTheme theme, ObstacleKind kind) =>
            TryAttach(root, RunnerVisualAssetCatalog.ForObstacle(theme, kind));

        public bool TryAttachPickup(Transform root, PickupVisualId id) =>
            TryAttach(root, RunnerVisualAssetCatalog.ForPickup(id));

        public bool TryAttachEnvironment(Transform root, RunnerMapTheme theme, EnvironmentPropId id) =>
            TryAttach(root, RunnerVisualAssetCatalog.ForEnvironment(theme, id));

        public bool TryAttachThemeMechanic(Transform root, string apparatusPath)
        {
            var attached = TryAttach(root, new RunnerVisualDescriptor(
                $"PenguinRun/Prefabs/ThemeMechanics/{apparatusPath}",
                Vector3.zero,
                Vector3.zero,
                Vector3.one));
            if (attached)
            {
                // 机关表现层动画（摆动/开合/光束脉冲）；只动 CustomVisual，不碰判定
                var visual = root.Find(AttachmentName);
                if (visual != null)
                {
                    ThemeMechanicMotion.Configure(visual.gameObject, apparatusPath);
                }
            }

            return attached;
        }

        public void ClearAttachment(Transform root)
        {
            if (root == null) return;
            for (var index = root.childCount - 1; index >= 0; index--)
            {
                var child = root.GetChild(index);
                if (child.name != AttachmentName) continue;
                if (Application.isPlaying)
                {
                    child.SetParent(null, false);
                    Object.Destroy(child.gameObject);
                }
                else Object.DestroyImmediate(child.gameObject);
            }
        }

#if UNITY_EDITOR
        public bool TryAttachForValidation(Transform root, RunnerVisualDescriptor descriptor) =>
            TryAttach(root, descriptor);
#endif

        private bool TryAttach(Transform root, RunnerVisualDescriptor descriptor)
        {
            if (root == null || descriptor.IsEmpty) return false;
            ClearAttachment(root);

            var prefab = LoadPrefab(descriptor.ResourcePath);
            if (prefab == null)
            {
                LogMissingOnce(descriptor.ResourcePath);
                if (!string.IsNullOrWhiteSpace(descriptor.FallbackResourcePath))
                    prefab = LoadPrefab(descriptor.FallbackResourcePath);
            }
            if (prefab == null) return false;

            var instance = Object.Instantiate(prefab, root, false);
            instance.name = AttachmentName;
            instance.transform.localPosition = descriptor.LocalPosition;
            instance.transform.localRotation = Quaternion.Euler(descriptor.LocalEuler);
            instance.transform.localScale = descriptor.LocalScale;
            return true;
        }

        private GameObject LoadPrefab(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return null;
            if (prefabCache.TryGetValue(path, out var cached)) return cached;
            var loaded = Resources.Load<GameObject>(path);
            prefabCache[path] = loaded;
            return loaded;
        }

        private void LogMissingOnce(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !missingLog.Add(path)) return;
            Debug.LogWarning($"[RunnerVisualFactory] Missing custom visual '{path}'; using procedural fallback.");
        }
    }
}
