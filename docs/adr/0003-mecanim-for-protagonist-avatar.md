# 0003. 主角采用 Mecanim，Boss 保持 Legacy Animation

## Status

Accepted（2026-07-25）

## Context

- ADR-0002 为 6 个 Boss 引入 CC0 rigged 模型时，把 `CONTEXT.md`「范围之外」从旧条款「不使用 Animator 状态机，**除非有完整 rig 可用**」收成了无条件禁止 Mecanim。
- 主角 Gugugaga 改造需要连续的跑/换道/跳/铲混合（`Speed01`、`LaneLean`、`VerticalSpeed`）以及 Hurt/Celebrate/交互触发；ADR-0002 的 Consequences 已承认「Legacy Animation 不支持状态混合树」。
- Boss 招式仍是离散单发片段，用 Legacy `Animation` + `BossRigAnimator` 的 `CrossFade` 与播放速度缩放即可对齐 `BossAttackPhase`。
- 命中判定权威链（障碍与 Boss）仍是纯计时器 + 车道几何，任何动画方案都不得进入判定路径。

## Decision

- 主角表现层使用 Mecanim：`GugugagaBase.controller` + 运行时 `AnimatorOverrideController`，由 `RunnerAnimationDriver` 驱动参数与触发器。
- Boss 继续走 Legacy `Animation` + `BossRigAnimator`，不建 Animator 状态机（ADR-0002 不变）。
- AnyState 过渡必须 `canTransitionToSelf = false`，避免每帧重进把姿势钉在片段开头。
- `Animator.applyRootMotion` 必须为 false；位移与碰撞仍由 `PlayerMotor` / `CollisionSystem` 权威。

## Consequences

- 更容易：主角获得参数驱动的混合与可读的局内姿态；后续换装/换 clip 只需改 `RunnerAnimationSet` 覆盖。
- 更难：项目并存两套动画范式；新增可动画角色必须显式选边（主角族 Mecanim / Boss 族 Legacy）。
- 明确不做：不把 Boss 迁到 Mecanim；不让动画事件或 root motion 参与命中；不在本 ADR 范围内做皮肤商店或多人换装。
