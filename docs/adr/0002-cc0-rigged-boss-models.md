# 0002. Boss 视觉采用 CC0 骨骼模型 + Legacy Animation 程序化播放

## Status

Accepted（2026-07-20）

## Context

- 6 个 Boss 此前全部由运行时 `PrimitiveType`（Capsule/Cube/Sphere）拼装，剪影可读但特写与出场/击败演出撑不起长局高潮感（PLAYER_EXPERIENCE_AUDIT PX-012 视觉短板）。
- `CONTEXT.md`「范围之外」原条款：Unity Animator 状态机不使用，Boss 动画保留程序化实现，**除非有完整 rig 可用**。
- Quaternius「Ultimate Monsters」包（CC0 1.0）提供 50 只带完整骨骼与动画片段（idle/attack/hit/death 等）的低多边形怪物，风格与项目低模基调兼容，可映射全部 6 个主题 Boss（Yeti→冰霜雪王、Goleling_Evolved→雪松哨兵、Dragon_Evolved→极光长蛇、Ghost→雾堤守卫、Squidle→珊瑚海怪、Birb→雷云苍鹰）。
- 命中判定权威链 `CheckPatternHit → IsBossPatternHit → IsPatternDangerous` 是纯计时器 + 车道几何，任何动画方案都不得进入判定路径（领域规则）。

## Decision

- 我们引入 Quaternius CC0 rigged FBX 作为 6 个 Boss 的视觉本体，放置于 `Assets/Resources/PenguinRun/Models/Bosses/`，按 OpenAssetPack 规则登记 credits。
- 我们**不引入 Animator 状态机（Mecanim Controller）**：`BossModelImportProcessor` 在导入期将 rig 设为 Legacy，运行时由 `BossRigAnimator` 直接播放 `Animation` 片段——待机/移动循环、招式单发（melee/cast/slam 按招式映射）、受击、破绽眩晕、死亡。
- 攻击片段用**播放速度缩放**对齐 `BossAttackPhase` 的危险窗时序（Windup=起手、Strike=命中帧附近），动画仅表现层；根节点位移/前倾仍由 `BossBodyAnimator` 程序化驱动，`BossActiveZ`、预警带与全部命中判定不变。
- 模型缺失或加载失败时，`BossEncounter.BuildSilhouette` 回退到原程序化剪影，玩法零影响。

## Consequences

- 更容易：Boss 出场/受击/破绽/死亡有真实骨骼演出；后续替换/增补 Boss 只需放置新 FBX 并登记 credits；判定与表现解耦使动画替换零风险。
- 更难：动画片段命名依赖 Quaternius 命名习惯（`*|Idle`、`*|HeadButt` 等），换其他素材源需扩展 `BossRigAnimator.Bind` 的后缀映射；Legacy Animation 不支持状态混合树，复杂过渡只能靠 CrossFade。
- 明确不做：不建 Animator 状态机、不做 humanoid retarget、不让动画事件参与命中；`CONTEXT.md`「范围之外」条款已同步修订。
