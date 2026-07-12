using System.Collections.Generic;
using UnityEngine;

namespace PenguinRun
{
    /// <summary>
    /// 轻量粒子池：不依赖 Unity ParticleSystem，纯 GameObject 池化管理。
    /// 支持 256 个可复用粒子，提供速度/重力/透明度衰减/缩放衰减。
    /// </summary>
    internal sealed class ParticleEffectPool
    {
        private sealed class Particle
        {
            public GameObject Go;
            public ParticleSystem System;
            public Vector3 Velocity;
            public float Life;
            public float MaxLife;
            public float StartScale;
            public Color StartColor;
            public bool Alive;
            public bool UseGravity;
        }

        private readonly List<Particle> pool = new();
        private readonly List<Particle> active = new();
        private readonly Transform parent;
        private readonly float groundY;

        private static ParticleEffectPool instance;
        public static ParticleEffectPool Instance => instance;

        public ParticleEffectPool(Transform parent, float groundY, int capacity = 256)
        {
            this.parent = parent;
            this.groundY = groundY;
            instance = this;

            for (var i = 0; i < capacity; i++)
            {
                var go = new GameObject($"Particle System {i}", typeof(ParticleSystem));
                go.transform.SetParent(parent, true);
                go.SetActive(false);
                go.hideFlags = HideFlags.HideAndDontSave;
                var system = go.GetComponent<ParticleSystem>();
                var main = system.main;
                main.playOnAwake = false;
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                main.maxParticles = 256;
                var emission = system.emission;
                emission.enabled = false;

                pool.Add(new Particle
                {
                    Go = go,
                    System = system,
                    Alive = false,
                });
            }
        }

        /// <summary>在指定位置发射 count 个粒子。</summary>
        public void Emit(Vector3 position, Color color, int count = 5, float speed = 2f, float lifetime = 0.6f, bool useGravity = true)
        {
            for (var i = 0; i < count; i++)
            {
                var p = Rent();
                if (p == null) break;

                var angle = Random.value * Mathf.PI * 2f;
                var elevation = Random.value * Mathf.PI * 0.5f;
                var dir = new Vector3(
                    Mathf.Cos(angle) * Mathf.Cos(elevation),
                    Mathf.Sin(elevation),
                    Mathf.Sin(angle) * Mathf.Cos(elevation));

                p.Go.transform.position = position;
                p.Go.transform.localScale = Vector3.one * Random.Range(0.04f, 0.1f);
                p.Velocity = dir * speed * Random.Range(0.6f, 1f);
                p.Life = lifetime;
                p.MaxLife = lifetime;
                p.StartScale = p.Go.transform.localScale.x;
                p.StartColor = color;
                p.UseGravity = useGravity;
                p.Alive = true;
                p.Go.SetActive(true);
                EmitParticle(p);

                active.Add(p);
            }
        }

        /// <summary>在指定位置发射环形粒子波（适用于道具拾取等）。</summary>
        public void EmitRing(Vector3 position, Color color, int count = 12, float radius = 0.8f, float speed = 3f, float lifetime = 0.5f)
        {
            for (var i = 0; i < count; i++)
            {
                var p = Rent();
                if (p == null) break;

                var angle = (float)i / count * Mathf.PI * 2f;
                var dir = new Vector3(Mathf.Cos(angle), 0.15f, Mathf.Sin(angle)).normalized;

                p.Go.transform.position = position + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                p.Go.transform.localScale = Vector3.one * 0.05f;
                p.Velocity = dir * speed;
                p.Life = lifetime;
                p.MaxLife = lifetime;
                p.StartScale = 0.05f;
                p.StartColor = color;
                p.UseGravity = false;
                p.Alive = true;
                p.Go.SetActive(true);
                EmitParticle(p);

                active.Add(p);
            }
        }

        /// <summary>发射垂直向上飞散粒子（受伤、Boss 受击等）。</summary>
        public void EmitBurst(Vector3 position, Color color, int count = 15, float speed = 4f, float lifetime = 0.7f)
        {
            for (var i = 0; i < count; i++)
            {
                var p = Rent();
                if (p == null) break;

                var angle = Random.value * Mathf.PI * 2f;
                var elevation = Random.value * Mathf.PI * 0.4f + 0.2f;
                var dir = new Vector3(
                    Mathf.Cos(angle) * Mathf.Cos(elevation),
                    Mathf.Sin(elevation) + 0.3f,
                    Mathf.Sin(angle) * Mathf.Cos(elevation));

                p.Go.transform.position = position;
                p.Go.transform.localScale = Vector3.one * Random.Range(0.05f, 0.14f);
                p.Velocity = dir * speed * Random.Range(0.5f, 1.2f);
                p.Life = lifetime;
                p.MaxLife = lifetime;
                p.StartScale = p.Go.transform.localScale.x;
                p.StartColor = color;
                p.UseGravity = true;
                p.Alive = true;
                p.Go.SetActive(true);
                EmitParticle(p);

                active.Add(p);
            }
        }

        /// <summary>每帧更新：移动粒子、衰减、回收。</summary>
        public void Tick(float dt, float worldDistance)
        {
            const float gravity = -9.8f;

            for (var i = active.Count - 1; i >= 0; i--)
            {
                var p = active[i];
                if (!p.Alive) continue;

                p.Life -= dt;
                if (p.Life <= 0f)
                {
                    Return(p, i);
                    continue;
                }

                var t = 1f - p.Life / p.MaxLife; // 0→1

                // 运动
                p.Go.transform.position += p.Velocity * dt;
                if (p.UseGravity)
                {
                    p.Velocity.y += gravity * dt;
                }

                // 缩放衰减
                var scale = Mathf.Lerp(p.StartScale, 0.01f, t);
                p.Go.transform.localScale = Vector3.one * scale;

                // 低于地面则回收
                if (p.Go.transform.position.y < groundY - 0.5f)
                {
                    Return(p, i);
                }
            }
        }

        private Particle Rent()
        {
            for (var i = pool.Count - 1; i >= 0; i--)
            {
                if (!pool[i].Alive)
                {
                    return pool[i];
                }
            }
            return null; // 池耗尽
        }

        private static void EmitParticle(Particle p)
        {
            var emitParams = new ParticleSystem.EmitParams
            {
                position = p.Go.transform.position,
                velocity = p.Velocity,
                startColor = p.StartColor,
                startSize = p.StartScale,
                startLifetime = p.MaxLife,
            };
            p.System.Emit(emitParams, 1);
        }

        private void Return(Particle p, int activeIdx)
        {
            p.Alive = false;
            p.System.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            p.Go.SetActive(false);
            p.Go.transform.position = Vector3.zero;
            active.RemoveAt(activeIdx);
        }

        /// <summary>清空所有活跃粒子。</summary>
        public void ReleaseAll()
        {
            for (var i = active.Count - 1; i >= 0; i--)
            {
                active[i].Alive = false;
                active[i].System.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                active[i].Go.SetActive(false);
            }
            active.Clear();
        }
    }
}
