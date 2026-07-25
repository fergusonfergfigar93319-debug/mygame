# 0004. 能力驱动的首局新手期

## Status

Accepted（2026-07-25）

## Context

- `docs/CURRENT_DESIGN.md`「首局体验」要求只教最小动作集（换道/跳/铲/收集），并延后 Boss、复杂道具、图鉴与营地深层系统。
- 代码里只有散落的一次性钩子（滑铲横梁提示、Boss 招式引导、按主题机关教学），没有中心化的新手期概念；片段池从 0 米起无难度过滤，路线分支（220m）、主题机关（100m）、旅途札记升级与世界事件（500m）都会在首局出现。
- `Runner3DTutorialDone` 名似教学完成，实际是图鉴域「已完成第一次旅程」，不能复用。
- 可选路径包括：固定局数、严格第一局、距离驱动、主动教学编排；能力驱动与纯抑制是本轮取舍。

## Decision

- 引入能力驱动的「新手期」：四个持久化能力位（换道/跳/铲/收集）全部演示过即毕业，另设 `TotalRuns >= 3` 保险丝强制毕业。
- 「是否新手期」在 `RunnerSessionConfig.Snapshot()` 开局读一次，整局不变；能力位局内实时记录，结算时一次性落盘。
- 新手期内只做抑制与片段轮转，不做主动教学序列编排：关掉路线分支、旅途札记升级、主题机关、世界事件、移动敌人；`SegmentWaveDirector` 按 `LaneWeave → JumpArc → SlideTunnel → RewardRun` 确定性轮转，并排除 `HazardRhythm` / `PowerUpTrial` / `SplitGate` / `EnemyAmbush`。
- Boss 不抑制：普通难度首 Boss 在 1050m，且 `snow_king` 已有 `IsTutorialBoss` 引导路径。
- 提示层统一到能力位；废弃 `RunnerSlideGateTutorialDone` 的写入路径（存档迁移可读）；动作标签仅在新手期显示。

## Consequences

- 更容易：首局信息量受控且可测；片段覆盖有保证，不会因纯随机抽不到滑铲而永远不毕业。
- 更难：项目多了一组存档位与开局快照契约；新增局内系统必须显式决定是否尊重 `IsOnboarding`。
- 明确不做：不在本 ADR 范围内做主动教学序列器；不推迟 Boss；不把 `Runner3DTutorialDone` 改义为新手期毕业。
