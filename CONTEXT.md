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

- **意指：** 场景内需要玩家通过跳跃、滑铲或换道规避的静态或动态物件。碰撞由 `ObstacleColliderSpec` 定义（轴对齐 X/Z 半宽 + 脚底净空 `FeetClearanceAboveRoot`）；`IsLow == true` 时滑铲可穿过。
- **规格与规避：**
  - `LowSlideGate`（滑铲门 / SlideGate）— **滑铲**；跳跃净空不足
  - `WideSlideOnly`（宽低横梁 / WideSlideGate）— **滑铲**或走第三道
  - `MediumBarrier` — **跳跃或滑铲**
  - `SmallJumpable` — **跳跃**
  - `SmallFlying` — **跳跃**（悬空）
  - `WideWall` — **换道**
  - `TallSpike` — **换道**
  - `Rolling` — **换道或跳跃**（滚球前后判定较紧）
  - `PatrolEnemy` / `SwoopEnemy` — **换道或跳跃**（滑铲无效）
- **动态碰撞开关：** 挂载 `ObstacleBehavior` 的障碍（如 `TimedGate` 开门、`ShatterWall` 已碎）通过 `IsCollidable` 控制；`CollisionSystem` 在 AABB 通过后必须读取该标志。
- **可破坏标记：** `ObstacleColliderSpec.IsThunderFeatherDestructible` 标记雷羽可粉碎的小型可跳障碍；`ShatterWall` 由冲刺（`DashTimer > 0`）触发 `TryShatter()`，与雷羽路径分离。
- **勿混淆：** 移动敌人注册在 `EnemyNpcState` 中管理轨迹；Boss 战障碍由 `SpawnPaused` 暂停片段生成。

### 完美动作窗口（PerfectActionWindow）

- **意指：** 玩家执行跳跃或滑铲后的短窗口（默认 0.28–0.32 s，见 `RunnerTuningConfig`），在此窗口内成功规避障碍记为完美闪避并加分。
- **执行点：** `PlayerMotor` 设置窗口；`CollisionSystem` 在窗口内匹配 `LastCriticalAction` 与规避方式。

### 命中解析（RunnerHitResolver）

- **意指：** 障碍与 Boss 共用的一层防御/扣血判定，统一护盾、鱼弹、冰镜、树人护甲、珊瑚回弹、雷羽击破与无敌时长（`RunnerCombatTuning`）。
- **勿混淆：** Boss 招式闪避几何仍由 `RunnerHitResolver.IsBossPatternHit` 与 `BossSystem.CheckPatternHit` 协作；VFX 不改变命中规则。

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

### Boss 预警带（BossTelegraphZone）

- **意指：** Boss 战期间地面预警圈与跑道特效统一对齐的 Z 平面，固定为 `BossSystem.PlayerTelegraphZ`（玩家前方约 5.5m），与玩家站位（z≈0）和 Boss 站位（`BossActiveZ`）之间的可读带。
- **相关：** Boss 招式（BossPattern）、雾堤假预警（DeceptiveTelegraph）。

### 雾堤假预警（DeceptiveTelegraph）

- **意指：** `BossArchetype.Deceptive`（雾堤守卫）在真实车道预警之外额外生成的灰色 decoy 圈；危险窗口开始前淡出，仅真实预警在 `IsPatternDangerous` 时变红脉冲。
- **勿混淆：** 真实预警与 `CheckPatternHit` 车道一致；decoy 不参与命中。

### Boss 招式（BossPattern）

- **意指：** Boss 战期间选取的单次攻击动作，决定危险区域、预警显示和玩家所需的规避操作（跳/铲/换道/护盾/冲刺）。
- **现有值：** `SweepLow`（横扫低空）、`DiveHigh`（高空俯冲）、`ChargeAcross`（全场冲锋）、`RangedSalvo`（远程齐射）、`CenterBeam`（中央光束）、`QuakePulse`（震地脉冲）。

### Boss 招式阶段（BossAttackPhase）

- **意指：** 单次招式的视觉节拍：`Windup`（前摇蓄力）→ `Telegraph`（地面预警渐强）→ `Strike`（击发瞬间，与 `IsPatternDangerous` 对齐）→ `Recovery`（收招淡出）。由 `BossPatternVfxPlayer` 驱动，**不改变** `CheckPatternHit` 命中规则。
- **勿混淆：** Boss 招式前摇（`ApplySignatureWindup` 的体型缩放）属于起手表现；`BossAttackPhase` 覆盖整段招式时间轴。

### Boss 击发动效（BossStrikeVfx）

- **意指：** 进入 `BossAttackPhase.Strike` 首帧（或齐射弹道落地时）在 **Boss 预警带**（`PlayerTelegraphZ`）触发的冲击环、碎片散射与配套震屏/音效，由 `BossPatternVfxProfile` 配置。
- **相关：** `BossPatternVfxPlayer`、`RunnerAudio.PlayBossPatternStrike`。

### Boss 专属 BGM（BossBgmClip）

- **意指：** 进入 Boss 战时替换跑步 BGM 的独立音乐轨，由 `RunnerAudio.EnterBossMusic` 切入、`ExitBossMusic` 恢复。每个 Boss 通过 `BossDefinition.BgmClipName` 字段（本次内容包新增）映射至 `Resources/PenguinRun/` 下的 `.ogg` 文件。

### 片段池（SegmentPool）

- **意指：** 同一主题下所有 `RunnerSegmentDefinition` 的集合，由 `RunnerSegmentCatalog.GetPool(theme)` 返回，`SegmentWaveDirector` 从中随机选取下一个片段。

### 跑道可视走廊（PlayableViewCorridor）

- **意指：** 第三人称镜头与玩家之间的近场带内，程序化布景不得用大体块遮挡三车道与前方障碍预判；侧向装饰应落在护坡外（`|x| ≥ SceneryMinAbsX`，约 **7.4 m**），相机—玩家 Z 带内误入走廊的物体在 `AnimateScenery` 中淡出至低 alpha。
- **执行点：** `PenguinRunnerGame` 常量 `ViewCorridorHalfWidth`（约 **5.2 m**）、`SceneryLateralOffset`（布景随弯道 `WorldCurveX` 偏移）、`ApplyViewCorridorFade`（走廊内隐藏或极低 alpha）；天空浮云/光柱/侧向彩虹条、远山 `Distant Ice Ridge` 为高风险装饰。
- **勿混淆：** 玩法障碍与宽墙（`SegmentSpawner`）属碰撞实体，不在本走廊规则内。

### 画面构图基准（FramingReference）

- **意指：** 跑酷玩法与主相机构图以横屏 **16:9** 为设计基准；相机垂直 FOV 以 `RunnerCameraTuning.fieldOfView` 默认 **72°** 为准。竖屏视为不受支持的破坏性构图。
- **执行点：**
  - `ProjectSettings/ProjectSettings.asset` 中 `defaultScreenOrientation=3 (LandscapeLeft)`，且禁止任意 Portrait 自动旋转。
  - `PenguinRunnerGame.EnsureLandscape()` 与 `MainMenuBootstrap.EnsureLandscape()` 在场景入口运行时再校正一次。
  - `PenguinRunnerGame` 在每帧 `LateUpdate` 中调用 `ApplyRunnerCameraViewport169()`，将 `MainCamera.pixelRect` 铺满当前窗口（`Screen.width` × `Screen.height`），避免 Game 视图 Free Aspect 或超宽屏两侧出现未渲染条带。
  - 跑酷 **IMGUI HUD**（`RunnerHud`）：`Screen.height / 1080` 作为缩放因子、`GUI.matrix` 平移至 `Screen.safeArea` 左上角，逻辑宽/高在 `RunnerHudLayout` 中排版；边缘「角上返回/结算」触控带与 `RunnerHudLayoutScale` 同步（基准 260×180 逻辑像素 × 缩放）。
  - 主菜单等 uGUI：`UiBuilder.CreateCanvas` 的 `CanvasScaler.referenceResolution` 为 **1920×1080**（横屏参考）。
- **勿混淆：** 玩家"握姿"（左/右横屏）允许由系统在两种横屏方向之间旋转，构图基准不变。

### 大厅界面（LobbySurface）

- **意指：** 主菜单（`MainMenu`）中的界面分工：中央保留吉祥物与开始游戏等核心操作；营地、任务、排行、图鉴、设置等系统以 **底栏 Tab** 为主要入口，避免与中央区叠放多块重复入口卡片。
- **紧凑玩家条（CompactProfile）：** 顶栏左侧窄条（头像 + 昵称 + 等级 + `›`），点击打开 **我的战绩 Sheet**（`ProfileStatsPanel`），完整统计不在主屏展开。
- **顶栏 Ticker（TopTicker）：** 玩家条下方单行动态文案（签到 / 任务 / 营地 / 今日挑战 / 赛季进度），替代原 Hero 区活动横幅，避免遮挡吉祥物。
- **难度芯片（DifficultyChip）：** 主 CTA 上方可点击胶囊，可单独修改难度（`SettingsOnly` 弹层）；开始极夜快跑或今日挑战时仍经同一难度选择弹层再开局。
- **今日建议：** 仅保留左侧单张战略卡，由 `MainMenuBootstrap.RefreshStrategyCard` 按优先级提示下一步（签到、任务、营地等），与底栏职责区分清晰。
- **相关：** 画面构图基准（FramingReference）、我的战绩 Sheet（`ProfileStatsPanel`）。

### 开源素材包（OpenAssetPack）

- **意指：** 通过 CC0 或 CC-BY 授权引入项目的第三方资源（模型、贴图、音乐、音效）。
  - 模型/贴图目录：`Assets/Resources/PenguinRun/Models/`、`Assets/Resources/PenguinRun/Textures/`
  - 音频目录：`Assets/Resources/PenguinRun/`
  - 署名清单：`OPEN_SOURCE_VISUAL_ASSETS_CREDITS.txt`、`OPEN_SOURCE_AUDIO_CREDITS.txt`（随包发布）

---

## 领域规则

- 每个主题必须同时具有：片段池定义、调色板障碍外观映射、Boss 刷新条件、BGM 映射。
- 新障碍必须配套 `ObstacleColliderSpec` 规格；不允许未注册碰撞规格的障碍进入 `obstacles` 列表。动态障碍须实现 `ObstacleBehavior.IsCollidable` 并与视觉状态一致。
- 新道具必须在 `PowerUpKind`、`PickupSystem.ActivatePowerUp`、`WorldDirector`（如有持续状态）、`GetPowerUpColors`、`AddPowerUpEffects`、`PlayPowerUp` 共 6 处同步扩展，缺一处视为未完成。
- `Resources.Load` 路径区分大小写（部分平台）；模型/音频资源命名必须与代码字符串完全一致。
- CC-BY 素材在引入前必须先记录作者、来源链接、许可证和本地文件名到 credits 文件；未记录即不允许提交。
- 任何新增 UI/相机调参必须按横屏基准设计；引入会受 aspect 影响的逻辑前需先在 CONTEXT.md 与 ADR 中变更画面构图基准（`FramingReference`）。
- Boss 招式 FX 必须通过 `BossPatternFxPool` 租用；`BossPatternVfxPlayer.End()` 不得 `Destroy` 已交回池的对象。池容量上限 48，回收由 `pool.ReleaseAll` 统一处理。

---

## 范围之外

- 角色解锁/皮肤系统。
- 多人/排行榜网络功能。
- Unity Animator 状态机（Boss 动画保留程序化实现，除非有完整 rig 可用）。
- ScriptableObject 关卡编辑器（当前为代码驱动，短期内不迁移）。
