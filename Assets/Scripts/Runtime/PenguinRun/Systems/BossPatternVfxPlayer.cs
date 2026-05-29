using System.Collections.Generic;
using UnityEngine;

namespace PenguinRun
{
  internal sealed class BossPatternVfxPlayer
  {
    private readonly BossPatternFxPool pool = new();
    private readonly List<GameObject> persistentFx = new();
    private readonly List<GameObject> bossAttachedFx = new();

    private BossEncounter host;
    private BossPattern pattern;
    private BossPatternVfxProfile profile;
    private BossAttackPhase phase = BossAttackPhase.None;
    private BossAttackPhase previousPhase = BossAttackPhase.None;
    private bool strikeFired;
    private float patternDuration;
    private int patternContextLane;
    private int patternContextSafeLane = int.MinValue;
    private System.Action<BossPattern, BossPatternVfxProfile> onStrike;

    public BossAttackPhase Phase => phase;

    public void SetStrikeCallback(System.Action<BossPattern, BossPatternVfxProfile> callback) =>
      onStrike = callback;

    public void Begin(
      BossEncounter encounter,
      BossPattern p,
      float duration,
      int contextLane = 0,
      int safeLaneIdx = int.MinValue)
    {
      End();
      host = encounter;
      pattern = p;
      profile = BossPatternVfxProfile.Get(p);
      patternDuration = duration;
      patternContextLane = contextLane;
      patternContextSafeLane = safeLaneIdx;
      phase = BossAttackPhase.Windup;
      previousPhase = BossAttackPhase.None;
      strikeFired = false;

      if (host == null || p == BossPattern.None || p == BossPattern.Vulnerable) return;

      switch (p)
      {
        case BossPattern.SweepLow:
          BuildSweepWindup(safeLaneIdx);
          break;
        case BossPattern.DiveHigh:
          BuildDiveWindup(contextLane);
          break;
        case BossPattern.ChargeAcross:
          BuildChargeWindup();
          break;
        case BossPattern.RangedSalvo:
          BuildSalvoWindup();
          break;
        case BossPattern.CenterBeam:
          BuildBeamWindup();
          break;
        case BossPattern.QuakePulse:
          BuildQuakeWindup();
          break;
      }
    }

    public void Tick(float dt)
    {
      if (host == null || pattern == BossPattern.None || pattern == BossPattern.Vulnerable) return;

      phase = ResolvePhase();
      if (phase != previousPhase)
      {
        OnPhaseEntered(phase);
        previousPhase = phase;
      }

      AnimatePersistentFx(dt);
    }

    public void End()
    {
      pool.ReleaseAll();
      persistentFx.Clear();
      bossAttachedFx.Clear();
      host = null;
      pattern = BossPattern.None;
      phase = BossAttackPhase.None;
      previousPhase = BossAttackPhase.None;
      strikeFired = false;
    }

    public void OnSalvoProjectileImpact(float laneX)
    {
      if (host == null || pattern != BossPattern.RangedSalvo) return;
      SpawnImpactRing(laneX, BossSystem.PlayerTelegraphZ, 0.55f, new Color(1f, 0.55f, 0.2f, 0.72f));
      SpawnShardBurst(laneX, BossSystem.PlayerTelegraphZ, 5, 0.85f);
      onStrike?.Invoke(BossPattern.RangedSalvo, profile);
    }

    private BossAttackPhase ResolvePhase()
    {
      if (host.PatternTimer <= 0f) return BossAttackPhase.Recovery;
      var elapsed = host.GetPatternElapsed();
      if (elapsed < host.PatternDangerDelay * 0.45f) return BossAttackPhase.Windup;
      if (elapsed < host.PatternDangerDelay) return BossAttackPhase.Telegraph;
      if (host.IsPatternDangerous) return BossAttackPhase.Strike;
      return BossAttackPhase.Recovery;
    }

    private void OnPhaseEntered(BossAttackPhase newPhase)
    {
      switch (newPhase)
      {
        case BossAttackPhase.Strike when !strikeFired:
          strikeFired = true;
          FireStrikeVfx();
          onStrike?.Invoke(pattern, profile);
          break;
        case BossAttackPhase.Recovery:
          FadePersistentFx();
          break;
      }
    }

    private void FireStrikeVfx()
    {
      if (host == null || profile == null) return;
      var def = host.Definition;
      var theme = def.VfxColorMultiplier;

      switch (pattern)
      {
        case BossPattern.SweepLow:
          SpawnSweepStrike(patternContextSafeLane);
          break;
        case BossPattern.DiveHigh:
          SpawnDiveStrike(patternContextLane);
          break;
        case BossPattern.ChargeAcross:
          SpawnChargeStrike();
          break;
        case BossPattern.CenterBeam:
          SpawnBeamStrike();
          break;
        case BossPattern.QuakePulse:
          SpawnQuakeStrike();
          break;
        case BossPattern.RangedSalvo:
          break;
      }

      var burstColor = new Color(
        def.GlowColor.r * theme,
        def.GlowColor.g * theme,
        def.GlowColor.b * theme,
        0.65f);
      SpawnShardBurst(host.WorldX, BossSystem.PlayerTelegraphZ - 0.4f, 6, profile.ImpactBurstScale * 0.6f, burstColor);
		
		  // 击发瞬间：Boss 周身冲击波扩散环
		  SpawnShockwavePulse(def.GlowColor, profile.ImpactBurstScale);
		}

		/// <summary>击发瞬间 Boss 周身冲击波扩散环</summary>
		private void SpawnShockwavePulse(Color baseColor, float scaleMul)
		{
		  if (host?.Root == null) return;
		  var pos = host.Root.transform.position;
		  for (var i = 0; i < 3; i++)
		  {
			var radius = 1.8f + i * 2.2f;
			var alpha = 0.55f - i * 0.15f;
			var ring = RentWorldFx($"StrikePulse{i}", PrimitiveType.Cylinder,
			  new Vector3(pos.x, pos.y + 0.25f, pos.z),
			  new Vector3(radius, 0.06f, radius),
			  new Color(baseColor.r, baseColor.g, baseColor.b, alpha), 0.1f);
			if (ring != null) persistentFx.Add(ring);
		  }
		}

    // ── Windup builders ─────────────────────────────────────

    private void BuildSweepWindup(int safeLaneIdx)
    {
      var glow = HostGlow(0.46f);
      var slashL = RentBossFx("SweepSlashL", PrimitiveType.Cube,
        new Vector3(-0.85f, 0.72f, -0.55f), new Vector3(0.2f, 0.08f, 1.2f), glow);
      var slashR = RentBossFx("SweepSlashR", PrimitiveType.Cube,
        new Vector3(0.85f, 0.72f, -0.55f), new Vector3(0.2f, 0.08f, 1.2f), glow);
      if (slashL != null) bossAttachedFx.Add(slashL);
      if (slashR != null) bossAttachedFx.Add(slashR);

      for (var lane = -1; lane <= 1; lane++)
      {
        if (lane == safeLaneIdx) continue;
        AddLaneStrip(lane, new Color(1f, 0.38f, 0.15f, 0.68f), 3.6f, "SweepStrip");
      }
    }

    private void BuildDiveWindup(int laneIdx)
    {
      var laneX = laneIdx * host.LaneWidthInternal;
      var z = BossSystem.PlayerTelegraphZ;
      var y = TrackY(z);
      var inner = RentWorldFx("DiveInner", PrimitiveType.Cylinder,
        new Vector3(laneX, y, z), new Vector3(0.85f, 0.06f, 0.85f),
        new Color(1f, 0.45f, 0.18f, 0.72f));
      if (inner != null) persistentFx.Add(inner);

      var outer = RentWorldFx("DiveOuter", PrimitiveType.Cylinder,
        new Vector3(laneX, y + 0.02f, z), new Vector3(1.35f, 0.045f, 1.35f),
        new Color(1f, 0.35f, 0.12f, 0.58f));
      if (outer != null) persistentFx.Add(outer);

      var cone = RentBossFx("DiveBurstCone", PrimitiveType.Capsule,
        new Vector3(0f, 1.35f, -0.35f), new Vector3(0.28f, 1.1f, 0.28f), HostGlow(0.42f));
      if (cone != null)
      {
        cone.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        bossAttachedFx.Add(cone);
      }
    }

    private void BuildChargeWindup()
    {
      var z = BossSystem.PlayerTelegraphZ;
      var y = TrackY(z);
      var bar = RentWorldFx("ChargeWall", PrimitiveType.Cube,
        new Vector3(0f, y, z),
        new Vector3(host.LaneWidthInternal * 3.35f, 0.16f, 1.65f),
        new Color(host.Definition.GlowColor.r, host.Definition.GlowColor.g * 0.85f, host.Definition.GlowColor.b, 0.52f));
      if (bar != null) persistentFx.Add(bar);

      for (var i = 0; i < 4; i++)
      {
        var az = z + 0.35f + i * 0.42f;
        var arrow = RentWorldFx($"ChargeArrow{i}", PrimitiveType.Cube,
          new Vector3(0f, y + 0.12f + i * 0.05f, az),
          new Vector3(host.LaneWidthInternal * 2.4f - i * 0.35f, 0.1f, 0.22f),
          new Color(1f, 0.85f, 0.35f, 0.58f));
        if (arrow != null) persistentFx.Add(arrow);
      }

      for (var i = 0; i < 3; i++)
      {
        var laneX = (i - 1) * host.LaneWidthInternal;
        var trail = RentBossFx($"ChargeTrailBoss{i}", PrimitiveType.Cylinder,
          new Vector3(laneX, 0.06f, -1.2f), new Vector3(0.55f, 0.035f, 0.55f), HostGlow(0.36f));
        if (trail != null) bossAttachedFx.Add(trail);
      }
    }

    private void BuildSalvoWindup()
    {
      var ring = RentBossFx("SalvoGlyphRing", PrimitiveType.Cylinder,
        new Vector3(0f, 0.3f, 0f), new Vector3(1.5f, 0.03f, 1.5f),
        new Color(1f, 0.72f, 0.35f, 0.45f));
      if (ring != null) bossAttachedFx.Add(ring);

      for (var i = 0; i < 3; i++)
      {
        var ang = i * 120f * Mathf.Deg2Rad;
        var orb = RentBossFx($"SalvoOrb{i}", PrimitiveType.Sphere,
          new Vector3(Mathf.Cos(ang) * 0.9f, 1.5f, Mathf.Sin(ang) * 0.9f),
          Vector3.one * 0.24f, new Color(1f, 0.78f, 0.4f, 0.72f));
        if (orb != null) bossAttachedFx.Add(orb);
      }

      foreach (var lx in host.SalvoLaneX)
      {
        var dot = RentWorldFx("SalvoMarker", PrimitiveType.Cylinder,
          new Vector3(lx, TrackY(BossSystem.PlayerTelegraphZ) + 0.055f, BossSystem.PlayerTelegraphZ),
          new Vector3(0.42f, 0.05f, 0.42f), new Color(1f, 0.62f, 0.22f, 0.72f));
        if (dot != null) persistentFx.Add(dot);
      }
    }

    private void BuildBeamWindup()
    {
      var bossPos = host.Root.transform.position;
      var midZ = BossSystem.PlayerTelegraphZ + 1.2f;
      var beam = RentWorldFx("CenterBeam", PrimitiveType.Cylinder,
        new Vector3(0f, bossPos.y + 1.35f, midZ),
        new Vector3(0.42f, 5.5f, 0.42f),
        new Color(host.Definition.GlowColor.r, host.Definition.GlowColor.g * 0.85f, 1f, 0.32f));
      if (beam != null)
      {
        beam.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        persistentFx.Add(beam);
      }

      AddLaneStrip(0, new Color(1f, 0.25f, 0.72f, 0.58f), 8f, "BeamLane");

      var core = RentBossFx("BeamCoreFx", PrimitiveType.Sphere,
        new Vector3(0f, 1.35f, -0.15f), Vector3.one * 0.32f,
        new Color(1f, 0.55f, 0.9f, 0.45f));
      if (core != null) bossAttachedFx.Add(core);
    }

    private void BuildQuakeWindup()
    {
      for (var lane = -1; lane <= 1; lane++)
      {
        var laneX = lane * host.LaneWidthInternal;
        var ring = RentWorldFx($"QuakeRing_{lane}", PrimitiveType.Cylinder,
          new Vector3(laneX, TrackY(BossSystem.PlayerTelegraphZ), BossSystem.PlayerTelegraphZ + lane * 0.12f),
          new Vector3(1.15f, 0.055f, 1.15f), new Color(1f, 0.72f, 0.22f, 0.68f));
        if (ring != null) persistentFx.Add(ring);
      }

      var bossRing = RentBossFx("QuakeRingA", PrimitiveType.Cylinder,
        new Vector3(0f, 0.06f, 0f), new Vector3(1.6f, 0.035f, 1.6f),
        new Color(1f, 0.8f, 0.28f, 0.38f));
      if (bossRing != null) bossAttachedFx.Add(bossRing);
    }

    // ── Strike bursts ───────────────────────────────────────

    private void SpawnSweepStrike(int safeLaneIdx)
    {
      for (var lane = -1; lane <= 1; lane++)
      {
        if (lane == safeLaneIdx) continue;
        var lx = lane * host.LaneWidthInternal;
        var z = BossSystem.PlayerTelegraphZ;
        SpawnImpactRing(lx, z, 1.1f, new Color(1f, 0.35f, 0.12f, 0.75f));
        SpawnShardBurst(lx, z, 8, 1.1f);
      }
    }

    private void SpawnDiveStrike(int laneIdx)
    {
      var lx = laneIdx * host.LaneWidthInternal;
      var z = BossSystem.PlayerTelegraphZ;
      SpawnImpactRing(lx, z, 1.35f, new Color(1f, 0.45f, 0.18f, 0.78f));
      SpawnShardBurst(lx, z, 10, 1.2f);

      var streak = RentWorldFx("DiveStreak", PrimitiveType.Capsule,
        new Vector3(lx, TrackY(z) + 1.8f, z + 1.5f),
        new Vector3(0.35f, 2.4f, 0.35f), HostGlow(0.55f));
      if (streak != null)
      {
        streak.transform.rotation = Quaternion.Euler(75f, 0f, 0f);
        persistentFx.Add(streak);
      }
    }

    private void SpawnChargeStrike()
    {
      var z = BossSystem.PlayerTelegraphZ;
      for (var lane = -1; lane <= 1; lane++)
      {
        SpawnShardBurst(lane * host.LaneWidthInternal, z, 4, 0.9f);
      }

      foreach (var fx in persistentFx)
      {
        if (fx != null && fx.name.Contains("ChargeWall"))
        {
          fx.transform.position += new Vector3(0f, 0f, -1.5f);
        }
      }
    }

    private void SpawnBeamStrike()
    {
      var z = BossSystem.PlayerTelegraphZ;
      SpawnImpactRing(0f, z, 0.9f, new Color(1f, 0.3f, 0.75f, 0.8f));
      SpawnShardBurst(0f, z, 12, 1.15f, new Color(1f, 0.5f, 0.95f, 0.7f));

      foreach (var fx in bossAttachedFx)
      {
        if (fx != null && fx.name.Contains("BeamCoreFx"))
        {
          fx.transform.localScale = Vector3.one * 0.55f;
        }
      }
    }

    private void SpawnQuakeStrike()
    {
      for (var lane = -1; lane <= 1; lane++)
      {
        var lx = lane * host.LaneWidthInternal;
        var z = BossSystem.PlayerTelegraphZ;
        SpawnImpactRing(lx, z, 1.6f, new Color(1f, 0.78f, 0.22f, 0.72f));
        SpawnShardBurst(lx, z, 6, 1.25f, new Color(1f, 0.85f, 0.35f, 0.65f));
      }
    }

    // ── Animation ───────────────────────────────────────────

    private void AnimatePersistentFx(float dt)
    {
      var life01 = host.PatternDuration > 0.01f
        ? Mathf.Clamp01(host.PatternTimer / host.PatternDuration)
        : 0f;
      var strike01 = phase == BossAttackPhase.Strike ? host.DangerWindowProgress01 : 0f;

      foreach (var fx in persistentFx)
      {
        if (fx == null) continue;
        var marker = fx.GetComponent<BossWorldFxMarker>();
        SyncWorldFxToTrack(fx, marker);

        if (marker != null)
        {
          var pulse = 1f + Mathf.Sin(Time.time * 28f) * 0.1f;
          fx.transform.localScale = marker.BaseScale * pulse;
        }

        if (pattern == BossPattern.ChargeAcross && fx.name.Contains("ChargeArrow"))
        {
          fx.transform.position += new Vector3(0f, 0f, -2.8f * dt);
        }

        if (pattern == BossPattern.CenterBeam && fx.name.Contains("CenterBeam"))
        {
          var sy = 5.5f + strike01 * 2.2f;
          fx.transform.localScale = new Vector3(0.42f + strike01 * 0.12f, sy, 0.42f);
        }

        if (pattern == BossPattern.QuakePulse && fx.name.Contains("QuakeRing"))
        {
          var expand = 1f + strike01 * 1.4f;
          fx.transform.localScale = marker != null ? marker.BaseScale * expand : fx.transform.localScale * expand;
          fx.transform.Rotate(Vector3.up, 200f * dt, Space.World);
        }

        var rend = fx.GetComponent<Renderer>();
        if (rend != null)
        {
          var c = RunnerVisuals.GetColor(rend.material);
          if (phase == BossAttackPhase.Recovery)
          {
            c.a = Mathf.Lerp(0.15f, c.a, life01);
          }
          else if (phase == BossAttackPhase.Strike)
          {
            c.a = Mathf.Min(1f, c.a + strike01 * 0.22f);
          }
          else
          {
            c.a = Mathf.Max(c.a, 0.72f);
          }

          RunnerVisuals.SetColor(rend.material, c);
        }
      }

      for (var i = 0; i < bossAttachedFx.Count; i++)
      {
        var fx = bossAttachedFx[i];
        if (fx == null) continue;
        if (pattern == BossPattern.SweepLow && fx.name.Contains("Slash"))
        {
          var dir = fx.name.Contains("SlashL") ? 1f : -1f;
          var sweep = Mathf.Sin(Time.time * 26f + dir) * 0.5f;
          fx.transform.localPosition = new Vector3(dir * (0.72f + sweep), 0.75f, -0.65f);
          fx.transform.localRotation = Quaternion.Euler(0f, 0f, dir * (42f + sweep * 32f));
        }

        if (pattern == BossPattern.RangedSalvo && fx.name.Contains("SalvoOrb"))
        {
          var ang = Time.time * 2.8f + i * 2.1f;
          fx.transform.localPosition = new Vector3(Mathf.Cos(ang) * 0.9f, 1.45f + Mathf.Sin(Time.time * 5f) * 0.08f, Mathf.Sin(ang) * 0.9f);
        }

        if (pattern == BossPattern.RangedSalvo && fx.name.Contains("GlyphRing"))
        {
          fx.transform.Rotate(Vector3.up, 190f * dt, Space.Self);
        }
      }
    }

    private void FadePersistentFx()
    {
      foreach (var fx in persistentFx)
      {
        if (fx == null) continue;
        var rend = fx.GetComponent<Renderer>();
        if (rend != null)
        {
          var c = RunnerVisuals.GetColor(rend.material);
          c.a *= 0.85f;
          RunnerVisuals.SetColor(rend.material, c);
        }
      }
    }

    // ── Helpers ─────────────────────────────────────────────

    private void AddLaneStrip(int lane, Color color, float lengthZ, string prefix)
    {
      var lx = lane * host.LaneWidthInternal;
      var z = BossSystem.PlayerTelegraphZ;
      var strip = RentWorldFx($"{prefix}_{lane}", PrimitiveType.Cube,
        new Vector3(lx, TrackY(z) + 0.1f, z),
        new Vector3(host.LaneWidthInternal * 0.95f, 0.16f, lengthZ), color, 0.1f);
      if (strip != null) persistentFx.Add(strip);
    }

    private void SpawnImpactRing(float x, float z, float radius, Color color)
    {
      var ring = RentWorldFx("ImpactRing", PrimitiveType.Cylinder,
        new Vector3(x, TrackY(z) + 0.08f, z),
        new Vector3(radius, 0.08f, radius), color, 0.08f);
      if (ring != null) persistentFx.Add(ring);
    }

    private void SpawnShardBurst(float x, float z, int count, float scaleMul, Color? overrideColor = null)
    {
      var col = overrideColor ?? HostGlow(0.7f);
      for (var i = 0; i < count; i++)
      {
        var ang = i * (360f / count) * Mathf.Deg2Rad;
        var dist = 0.35f + (i % 3) * 0.18f;
        var shard = RentWorldFx("Shard", PrimitiveType.Cube,
          new Vector3(x + Mathf.Cos(ang) * dist, TrackY(z) + 0.2f + (i % 2) * 0.15f, z + Mathf.Sin(ang) * dist * 0.5f),
          Vector3.one * (0.12f + (i % 2) * 0.06f) * scaleMul * profile.ImpactBurstScale,
          col);
        if (shard == null) continue;
        var m = shard.GetComponent<BossWorldFxMarker>() ?? shard.AddComponent<BossWorldFxMarker>();
        m.BaseScale = shard.transform.localScale;
        m.SurfaceYOffset = 0.12f;
        persistentFx.Add(shard);
      }
    }

    private Color HostGlow(float alpha) =>
      new Color(host.Definition.GlowColor.r, host.Definition.GlowColor.g, host.Definition.GlowColor.b, alpha);

    private float TrackY(float z) => host.TelegraphSurfaceY(z);

    private void SyncWorldFxToTrack(GameObject fx, BossWorldFxMarker marker)
    {
      if (fx == null || host == null) return;
      var offset = marker != null ? marker.SurfaceYOffset : 0.09f;
      var p = fx.transform.position;
      p.y = host.TelegraphSurfaceY(p.z) + offset;
      fx.transform.position = p;
    }

    private Transform FxParent => host.FxSceneParentInternal;

    private GameObject RentWorldFx(string name, PrimitiveType type, Vector3 pos, Vector3 scale, Color color, float surfaceYOffset = 0.09f)
    {
      var go = pool.Rent(type, name, pos, scale, color, true, FxParent);
      if (go != null)
      {
        var m = go.GetComponent<BossWorldFxMarker>();
        if (m == null) m = go.AddComponent<BossWorldFxMarker>();
        m.BaseScale = scale;
        m.SurfaceYOffset = surfaceYOffset;
      }

      return go;
    }

    private GameObject RentBossFx(string name, PrimitiveType type, Vector3 localPos, Vector3 scale, Color color)
    {
      var go = pool.Rent(type, name, Vector3.zero, scale, color, true, host.BodyTransform);
      if (go != null)
      {
        go.transform.localPosition = localPos;
        go.transform.localRotation = Quaternion.identity;
      }

      return go;
    }
  }
}
