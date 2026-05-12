# 企鹅快跑（PenguinRun）

一款 Unity 3D 无尽跑酷游戏：主角咕咕嘎嘎在极夜冰原上自动向前冲刺，玩家通过跳跃、下滑和道具时机躲避高松鹅布下的障碍，并在途中挑战各地图主题的 Boss。

---

## 词汇表

### 地图主题（MapTheme）

- **意指：** 一段完整的视觉/关卡风格切片，决定跑道外观、片段池、障碍调色板和优先 Boss。
- **现有值：** `IceLakeEcho`（冰湖回音谷）、`CedarRuins`（雪松废墟）、`AuroraField`（极光磁场）、`MistDike`（北境雾堤）、`OceanReef`（海洋珊瑚礁）、`SkyFlight`（天空飞翔）。
- **勿混淆：** 调色板（`SegmentObstaclePalette`）是主题的子集，仅控制单个片段内的障碍外观。

### 片段（Segment）

- **意指：** 一批逻辑上连续的"波次"，共享同一组生成参数（障碍密度、道具偏向、模式类型）。在 `RunnerSegmentCatalog` 中定义，由 `SegmentWaveDirector` 逐波消费。
- **相关：** 波次（Wave）。

### 波次（Wave）

- **意指：** 每次 `SegmentWaveDirector.ConsumeWave()` 的单次调用，返回一组 `SegmentSpawnModifiers`，驱动 `SegmentSpawner` 生成一批障碍/道具/敌人。
- **勿混淆：** 片段（Segment）是 N 个波次的集合；波次是最小生成节拍。

### 调色板（ObstaclePalette）

- **意指：** `SegmentObstaclePalette` 枚举值，决定在当前片段内使用哪套障碍外观（低模模型、材质着色）。每个主题有其惯用调色板，但片段可以独立覆盖。
- **现有值：** `IceLake`、`CedarWood`、`AuroraGlow`、`MistFog`、`OceanCoral`、`SkyCloud`。

### 模式（PatternKind）

- **意指：** `SegmentPatternKind` 枚举值，决定片段内波次的空间排列逻辑（例如 `LaneWeave` 车道交织、`SlideTunnel` 低矮下滑通道、`RewardRun` 高密度奖励金币、`JumpArc` 跳跃弧线等）。
- **相关：** `HazardRhythm`（危险节奏段，高障碍密度）、`EnemyAmbush`（敌人伏击段）、`PowerUpTrial`（道具展示段）——本次内容包新增。

### 障碍（Obstacle）

- **意指：** 场景内需要玩家通过跳跃或下滑规避的静态或动态物件。分为可跳跃（`SmallJumpable`）、宽墙（`WideWall`）、低滑行门（`LowSlideGate`）、滚动物（`Rolling`）和敌人（`Enemy`）五种碰撞规格。
- **勿混淆：** 敌人（Enemy）是移动的障碍，仍复用 `ObstacleColliderSpec.Enemy` 规格，但在 `EnemyNpcState` 列表中额外管理运动行为。

### 移动敌人（MovingEnemy）

- **意指：** 注册在 `EnemyNpcState` 列表中、有独立运动轨迹的障碍单位。当前支持两种基本行为：`IsPatrol`（横向巡逻，正弦振荡）和斜向扑入（从屏幕外冲入）。
- **勿混淆：** Boss（有专属生命值/阶段/招式池）不属于 MovingEnemy。

### 道具（Pickup / PowerUp）

- **意指：** 玩家拾取后触发即时效果的场景物件。由 `PowerUpKind` 枚举标识，在 `PickupSystem` 中激活效果，在 `WorldDirector` 中管理持续时间/状态。
- **分层：** 通用道具适用全主题；主题道具仅在对应调色板片段中以更高概率出现。
- **勿混淆：** 金币（Coin）是另一类收集物，不视为道具。

### Boss

- **意指：** 在跑酷途中出现的大型对手，有独立生命值（`HitsRemaining`）、攻击模式池（`PatternPool`）和阶段流程（出场 → 学习 → 压迫 → 破绽 → 击败）。
- **相关：** Boss 前奏片段（`SpawnBossPrelude`）在 Boss 出现前 ~100m 生成预警和补给；Boss 奖励片段（`SpawnBossReward`）在击败后生成奖励。

### Boss 出场动画（BossIntro）

- **意指：** Boss 在 `Spawning` 阶段从触发到进入 `Active` 前的专属登场演出，包含位移轨迹、姿态缓动、体型变化与主题预警特效。
- **勿混淆：** Boss 招式前摇（`ApplySignatureWindup`）属于攻击起手，不属于出场动画。
- **相关：** 每个 `BossSilhouette` 对应独立出场风格，以强化 Boss 识别度与首帧可读性。

### Boss 招式（BossPattern）

- **意指：** Boss 战期间选取的单次攻击动作，决定危险区域、预警显示和玩家所需的规避操作（跳/铲/换道/护盾/冲刺）。
- **现有值：** `SweepLow`（横扫低空）、`DiveHigh`（高空俯冲）、`ChargeAcross`（全场冲锋）、`RangedSalvo`（远程齐射）、`CenterBeam`（中央光束）、`QuakePulse`（震地脉冲）。

### Boss 专属 BGM（BossBgmClip）

- **意指：** 进入 Boss 战时替换跑步 BGM 的独立音乐轨，由 `RunnerAudio.EnterBossMusic` 切入、`ExitBossMusic` 恢复。每个 Boss 通过 `BossDefinition.BgmClipName` 字段（本次内容包新增）映射至 `Resources/PenguinRun/` 下的 `.ogg` 文件。

### 片段池（SegmentPool）

- **意指：** 同一主题下所有 `RunnerSegmentDefinition` 的集合，由 `RunnerSegmentCatalog.GetPool(theme)` 返回，`SegmentWaveDirector` 从中随机选取下一个片段。

### 画面构图基准（FramingReference）

- **意指：** 跑酷玩法/相机/HUD/输入像素阈值统一以"横屏 16:9 附近的宽高比 + 垂直 FOV 76°"为基准设计；竖屏视为不受支持的破坏性构图。
- **执行点：**
  - `ProjectSettings/ProjectSettings.asset` 中 `defaultScreenOrientation=3 (LandscapeLeft)`，且禁止任意 Portrait 自动旋转。
  - `PenguinRunnerGame.EnsureLandscape()` 与 `MainMenuBootstrap.EnsureLandscape()` 在场景入口运行时再校正一次。
- **勿混淆：** 玩家"握姿"（左/右横屏）允许由系统在两种横屏方向之间旋转，构图基准不变。

### 开源素材包（OpenAssetPack）

- **意指：** 通过 CC0 或 CC-BY 授权引入项目的第三方资源（模型、贴图、音乐、音效）。
  - 模型/贴图目录：`Assets/Resources/PenguinRun/Models/`、`Assets/Resources/PenguinRun/Textures/`
  - 音频目录：`Assets/Resources/PenguinRun/`
  - 署名清单：`OPEN_SOURCE_VISUAL_ASSETS_CREDITS.txt`、`OPEN_SOURCE_AUDIO_CREDITS.txt`（随包发布）

---

## 领域规则

- 每个主题必须同时具有：片段池定义、调色板障碍外观映射、Boss 刷新条件、BGM 映射。
- 新障碍必须配套 `ObstacleColliderSpec` 规格；不允许未注册碰撞规格的障碍进入 `obstacles` 列表。
- 新道具必须在 `PowerUpKind`、`PickupSystem.ActivatePowerUp`、`WorldDirector`（如有持续状态）、`GetPowerUpColors`、`AddPowerUpEffects`、`PlayPowerUp` 共 6 处同步扩展，缺一处视为未完成。
- `Resources.Load` 路径区分大小写（部分平台）；模型/音频资源命名必须与代码字符串完全一致。
- CC-BY 素材在引入前必须先记录作者、来源链接、许可证和本地文件名到 credits 文件；未记录即不允许提交。
- 任何新增 UI/相机调参必须按横屏基准设计；引入会受 aspect 影响的逻辑前需先在 CONTEXT.md 与 ADR 中变更画面构图基准（`FramingReference`）。

---

## 范围之外

- 角色解锁/皮肤系统。
- 多人/排行榜网络功能。
- Unity Animator 状态机（Boss 动画保留程序化实现，除非有完整 rig 可用）。
- ScriptableObject 关卡编辑器（当前为代码驱动，短期内不迁移）。
