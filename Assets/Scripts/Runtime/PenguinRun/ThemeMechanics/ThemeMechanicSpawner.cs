using System;
using UnityEngine;

namespace PenguinRun
{
    internal sealed class ThemeMechanicSpawner
    {
        public const int Capacity = 24;
        private const float SpawnLeadZ = 30f;
        private const float BeatSpacing = 6f;

        private readonly Transform[] roots = new Transform[Capacity];
        private readonly ThemeMechanicBeat[] pattern = new ThemeMechanicBeat[Capacity];
        private readonly RunnerVisualFactory visualFactory = new();
        private readonly float laneWidth;
        private readonly float groundY;

        private int patternCount;
        private int nextBeat;
        private float startDistance;

        public ThemeMechanicSpawner(Transform sceneRoot, float laneWidth, float groundY)
        {
            this.laneWidth = Mathf.Max(0.1f, laneWidth);
            this.groundY = groundY;
            for (var index = 0; index < roots.Length; index++)
            {
                var root = new GameObject($"ThemeMechanicProp_{index:00}");
                root.transform.SetParent(sceneRoot, false);
                root.SetActive(false);
                roots[index] = root.transform;
            }
        }

        public int ActiveCount { get; private set; }
        public bool CriticalAssetMissing { get; private set; }

        public void Begin(ReadOnlySpan<ThemeMechanicBeat> beats, float runDistance)
        {
            Clear();
            patternCount = Mathf.Min(beats.Length, Capacity);
            for (var index = 0; index < patternCount; index++)
            {
                pattern[index] = beats[index];
            }
            nextBeat = 0;
            startDistance = runDistance;
            CriticalAssetMissing = beats.Length > Capacity;
        }

        public void Tick(float activeSeconds, float runDistance)
        {
            while (nextBeat < patternCount && pattern[nextBeat].Seconds <= activeSeconds)
            {
                Activate(nextBeat, pattern[nextBeat], runDistance);
                nextBeat++;
            }
        }

        public void Clear()
        {
            for (var index = 0; index < roots.Length; index++)
            {
                visualFactory.ClearAttachment(roots[index]);
                roots[index].gameObject.SetActive(false);
                roots[index].localPosition = Vector3.zero;
                pattern[index] = default;
            }
            patternCount = 0;
            nextBeat = 0;
            startDistance = 0f;
            ActiveCount = 0;
            CriticalAssetMissing = false;
        }

        private void Activate(int index, ThemeMechanicBeat beat, float runDistance)
        {
            if (index < 0 || index >= roots.Length)
            {
                if (beat.Rewarding)
                {
                    CriticalAssetMissing = true;
                }
                return;
            }

            var root = roots[index];
            var z = SpawnLeadZ + beat.Seconds * BeatSpacing;
            root.position = new Vector3(
                beat.Lane.ToX(laneWidth),
                TrackTerrain.SurfaceY(z, runDistance + startDistance, groundY) + 0.06f,
                z);
            root.gameObject.SetActive(true);
            ActiveCount++;

            if (!visualFactory.TryAttachThemeMechanic(root, beat.ApparatusPath))
            {
                if (IsCriticalApparatus(beat.ApparatusPath))
                {
                    CriticalAssetMissing = true;
                }
                BuildFallbackMarker(root, beat);
            }
        }

        private static bool IsCriticalApparatus(string apparatusPath)
        {
            return apparatusPath == "ice/floe_bridge";
        }

        private static void BuildFallbackMarker(Transform root, ThemeMechanicBeat beat)
        {
            var attachment = new GameObject(RunnerVisualFactory.AttachmentName);
            attachment.transform.SetParent(root, false);
            var marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            marker.name = beat.ApparatusPath.Replace('/', '_');
            marker.transform.SetParent(attachment.transform, false);
            marker.transform.localPosition = Vector3.zero;
            marker.transform.localScale = new Vector3(1.05f, 0.025f, 1.05f);
            var collider = marker.GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = false;
                if (Application.isPlaying)
                {
                    UnityEngine.Object.Destroy(collider);
                }
                else
                {
                    UnityEngine.Object.DestroyImmediate(collider);
                }
            }
            marker.GetComponent<Renderer>().sharedMaterial = RunnerVisuals.CreateMaterial(
                beat.Rewarding ? new Color(0.2f, 0.95f, 1f, 0.86f) : new Color(1f, 0.82f, 0.18f, 0.86f),
                true);
        }
    }
}
