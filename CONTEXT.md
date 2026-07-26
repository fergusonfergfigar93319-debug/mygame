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
- **动态碰撞开关：** 挂载 `ObstacleBehavior` 的障碍通过 `IsCollidable` 控制；`CollisionSystem` 在 AABB 通过后必须读取该标志。已接入生成链的动态障碍：`TimedGate`（定时开合能量门，500m 后出现）、`ShatterWall`（冲刺可碎宽墙，350m 后按概率升级自 WideWall，带裂纹光带提示）、`MovingBarrier`（横向巡移浮障，750m 后出现；移动障碍根节点保证视觉与判定一致）。
- **可破坏标记：** `ObstacleColliderSpec.IsThunderFeatherDestructible` 标记雷羽可粉碎的小型可跳障碍；`ShatterWall` 由冲刺（`DashTimer > 0`）触发 `TryShatter()`，与雷羽路径分离。
- **勿混淆：** 移动敌人注册在 `EnemyNpcState` 中管理轨迹；Boss 战障碍由 `SpawnPaused` 暂停片段生成。

### 蹲姿判定盒（CrouchHitbox）

- **意指：** 玩家判定身高 `PlayerMotor.BodyHeight`：站立 1.56、滑铲立即压至 0.86、滑铲结束后按 1.2/s 起身。`IsLow` 障碍以 `ObstacleColliderSpec.PassUnderMaxHeight`（默认 1.05）判定可否从下方穿过——判定身高低于该值即穿过，替代早期「SlideTimer>0 布尔」判定。
- **宽容语义：** 起身期间（约 0.16s）仍保持低姿态穿过资格；空中下滑落地帧自动衔接滑铲（`queuedSlideOnLand`）；贴地小幅腾空时按下滑会缓冲到落地帧执行。
- **勿混淆：** 跳跃净空判定仍走 `FeetClearanceAboveRoot`，与蹲姿盒相互独立。

### 完美动作窗口（PerfectActionWindow）

- **意指：** 玩家执行跳跃或滑铲后的短窗口（跳跃 0.32s、滑铲 0.50s——与滑铲时长 0.56s 对齐，空中变体 0.28s，见 `RunnerTuningConfig`），在此窗口内成功规避障碍记为完美闪避并加分。
- **执行点：** `PlayerMotor` 设置窗口（含预输入跳跃兑现与落地衔接滑铲两条补设路径）；`CollisionSystem` 在窗口内匹配 `LastCriticalAction` 与规避方式。

### 命中解析（RunnerHitResolver）

- **意指：** 障碍与 Boss 共用的一层防御/扣血判定，统一护盾、鱼弹、冰镜、树人护甲、珊瑚回弹、雷羽击破与无敌时长（`RunnerCombatTuning`）。Boss 招式命中同样按此顺序消耗防御层（护盾为消耗制并附护盾碎裂无敌，不再是 Boss 期间持续免疫）；真实扣血后由 `BossSystem.NotifyBossDamageTaken` 记录死亡复盘。
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

### Boss 骨骼动画（BossRigAnimator）

- **意指：** rigged Boss 模型（`Resources/PenguinRun/Models/Bosses/`，Quaternius CC0）的 Legacy Animation 播放器：待机/移动循环、招式单发片段（melee/cast/slam 按招式映射）、受击、眩晕（破绽）、死亡。攻击片段用播放速度缩放对齐 `BossAttackPhase` 危险窗时序。
- **权威边界：** 动画纯表现层——命中判定只读 `IsPatternDangerous` 计时器与车道几何，不读动画帧。模型缺失时 `BossEncounter.BuildSilhouette` 回退程序化剪影，玩法不受影响。
- **相关：** `docs/adr/0002-cc0-rigged-boss-models.md`、`BossModelImportProcessor`（导入期设 Legacy rig）。

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

### Boss 残血压迫（BossHealthAggression）

- **意指：** Boss 每损失一管血，招式之间的**攻击间隔**收紧一档（满血 ×1.0 / 二段 ×0.88 / 残血 ×0.76，见 `BossEncounter.HealthAggressionMultiplier`）。三段血此前节奏完全相同，压迫感不随战斗推进。
- **公平边界：** 只压缩攻击**间隔**。前摇（`ApplySignatureWindup`）与预警窗口（`patternDangerDelay` / `patternActiveWindow`）是玩家的反应预算，**不得**随血量缩短——缩短它们会把难度变成不公平而非更有压迫感。
- **可读信号：** 残血阶段（`HitsRemaining == 1`）Boss 本体在 `UpdateBodyTint` 中持续余烬脉动（暖橙），与破绽期的绿色脉动明确区分；进入该阶段时另给一次文字提示。
- **相关：** Boss 招式阶段（BossAttackPhase）、Boss 战 HUD 面板（BossHudPanel）。

### Boss 战 HUD 面板（BossHudPanel）

- **意指：** Boss 战期间替换常规资源面板的专属 HUD（`BossHudPanel`），承载四类信息：分段血条（段数恒等于 `BossSystem.BossMaxHits`）、破绽倒数节拍点、招式名与反击提示、速杀奖励窗口条。
- **设计取舍：** 血条为**离散分段**而非连续 fill——跑酷中玩家只有余光，离散段位比连续长度好读。速杀条只在窗口内可见，过期即隐藏，不给玩家留已失效的信息。
- **数据来源：** 只读 `BossSystem` 的公开快照（`CurrentBossPatternsUntilVulnerable`、`CurrentBossSpeedRewardTimeLeft` 等），**不参与任何命中判定**。
- **勿混淆：** 常规 HUD（`RunnerHudCanvas.normalStatsRoot`）与本面板互斥显示，由 `SetBossMode` 切换。

### 主题光照档案（ThemeLightingProfile）

- **意指：** `ThemePresentationProfile` 中的光照字段组（`KeyLightColor` / `KeyLightIntensity` / `KeyLightEuler` / `RimLightColor` / `RimLightIntensity` / `AmbientLight` / `FogColor` / `FogStart` / `FogEnd`），决定每个主题的主光色温与朝向、边光配色与雾带景深。
- **背景：** 此前六个主题共用同一套方向光与雾距，仅雾色不同，导致每张图的「光」完全一样。光向与色温是主题之间最强的区分手段。
- **执行点：** `PenguinRunnerGame.ApplyMapThemeVisuals` 消费档案并覆盖 `keyLight` / `rimFillLight`；`BuildScene` 只建默认值。主题切换时重新应用。
- **勿混淆：** Boss 战氛围（`RunnerPostFx.SetBossEmphasis`）是叠加在主题光照之上的临时后处理，不修改 `RenderSettings`。

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
  - 跑酷 HUD（`RunnerHudCanvas`，保留式 uGUI）：`UiBuilder.CreateCanvas` + `Screen.safeArea` 适配（`ApplySafeArea`），生命/资源/Boss 面板等均为数据驱动的持久节点，仅在值变化时刷新；IMGUI 版 `RunnerHud` 已裁撤。
  - 主菜单等 uGUI：`UiBuilder.CreateCanvas` 的 `CanvasScaler.referenceResolution` 为 **1920×1080**（横屏参考）。
- **勿混淆：** 玩家"握姿"（左/右横屏）允许由系统在两种横屏方向之间旋转，构图基准不变。

### 大厅界面（LobbySurface）

- **意指：** 主菜单（`MainMenu`）中的界面分工：中央保留吉祥物与开始游戏等核心操作；营地、任务、排行、图鉴、设置等系统以 **底栏 Tab** 为主要入口，避免与中央区叠放多块重复入口卡片。
- **紧凑玩家条（CompactProfile）：** 顶栏左侧窄条（头像 + 昵称 + 等级 + `›`），点击打开 **我的战绩 Sheet**（`ProfileStatsPanel`），完整统计不在主屏展开。
- **顶栏 Ticker（TopTicker）：** 玩家条下方单行动态文案（签到 / 任务 / 营地 / 今日挑战 / 赛季进度），替代原 Hero 区活动横幅，避免遮挡吉祥物。
- **难度芯片（DifficultyChip）：** 主 CTA 上方可点击胶囊，可单独修改难度（`SettingsOnly` 弹层）；开始极夜快跑或今日挑战时仍经同一难度选择弹层再开局。
- **今日建议：** 仅保留左侧单张战略卡，由 `MainMenuBootstrap.RefreshStrategyCard` 按优先级提示下一步（签到、任务、营地等），与底栏职责区分清晰。
- **相关：** 画面构图基准（FramingReference）、我的战绩 Sheet（`ProfileStatsPanel`）。

### 装饰层 Canvas 隔离（AmbientCanvasIsolation）

- **意指：** 每帧持续演化的**纯装饰**元素（星点闪烁、极光漂移、雪花飘落）必须挂在自己的**嵌套 Canvas** 上，与交互 UI 分离。
- **原因：** 改 `Image.color` 或 `RectTransform.anchoredPosition` 会把 Graphic 标脏，触发所在 Canvas 的合批重建。装饰与交互同处一个 Canvas 时，十几个星点就会让整张界面每帧重建一次。
- **执行点：** 大厅 `MainMenuBootstrap.CreateAmbientLayer`（`sortingOrder = -1`，压在交互层下）；营地 `LobbyCampScene.CreateSnowMotes` 的 `SnowLayer`（`sortingOrder = 1`，浮在场景上）。两者均 `overrideSorting = true`，且层内 Image 一律 `raycastTarget = false`。
- **勿混淆：** 一次性交互动效（按钮弹跳、面板进场）走 `TweenExtensions`，不需要独立 Canvas；本规则只针对**持续循环**的装饰。
- **相关：** 画面构图基准（FramingReference）。

### 开源素材包（OpenAssetPack）

- **意指：** 通过 CC0 或 CC-BY 授权引入项目的第三方资源（模型、贴图、音乐、音效）。
  - 模型/贴图目录：`Assets/Resources/PenguinRun/Models/`、`Assets/Resources/PenguinRun/Textures/`
  - 音频目录：`Assets/Resources/PenguinRun/`
  - 署名清单：`OPEN_SOURCE_VISUAL_ASSETS_CREDITS.txt`、`OPEN_SOURCE_AUDIO_CREDITS.txt`（随包发布）

### 主角表现层（PlayerAvatarPresentation）

- **意指：** 玩法与主角视觉之间的边界。`RunnerGameController` 每帧产出只读 `RunnerAvatarState`（跑/落地/滑铲/结束、速度比、换道倾角、竖直速度、buff 位标志）；`IPlayerCharacterPresentation` 消费该快照并驱动动画、表情、二次运动与角色特效。
- **默认实现：** `PlayerAvatarPresenter` 加载 `Resources/PenguinRun/Characters/Gugugaga/GugugagaAvatar`，用 Mecanim（`GugugagaBase.controller` + `AnimatorOverrideController`）播放 clip；`Animator.applyRootMotion` 必须为 false。
- **回退：** 定义缺失或实例化失败且 `fallbackToProcedural` 为真时，回退到程序化 `PenguinCharacter.CreateCute`；玩法判定不变。
- **预算：** LOD0/1/2 三角面分别 ≤50000 / ≤22000 / ≤8000；单贴图边长 ≤2048；预制体唯一材质数 ≤3。
- **勿混淆：** Boss 骨骼播放走 Legacy `Animation` + `BossRigAnimator`（见 ADR-0002），不属于本表现层。
- **相关：** `docs/adr/0003-mecanim-for-protagonist-avatar.md`。

### 新手期（Onboarding Period）

- **意指：** 玩家尚未演示最小动作集时的首局保护期。四个能力位为换道、跳跃、滑铲、收集；全部演示过即毕业，另设 `TotalRuns >= 3` 保险丝强制毕业。
- **生效边界：** 「是否新手期」在开局 `RunnerSessionConfig.Snapshot()` 读一次，整局不变；能力位局内实时记录，结算时落盘。
- **期内行为：** 抑制路线分支、旅途札记升级、主题机关、世界事件与移动敌人；片段池按 `LaneWeave → JumpArc → SlideTunnel → RewardRun` 轮转并排除高难 pattern。Boss **不**抑制。
- **勿混淆：** `Runner3DTutorialDone` 属于图鉴域，语义是「已完成第一次旅程」（解锁 `story_first_journey`），**不是**新手期毕业标志。
- **相关：** `docs/adr/0004-competence-driven-onboarding.md`、`OnboardingState`。

---

## 领域规则

- 每个主题必须同时具有：片段池定义、调色板障碍外观映射、Boss 刷新条件、BGM 映射。
- 新障碍必须配套 `ObstacleColliderSpec` 规格；不允许未注册碰撞规格的障碍进入 `obstacles` 列表。动态障碍须实现 `ObstacleBehavior.IsCollidable` 并与视觉状态一致。
- 新道具必须在 `PowerUpKind`、`PickupSystem.ActivatePowerUp`、`WorldDirector`（如有持续状态）、`GetPowerUpColors`、`AddPowerUpEffects`、`PlayPowerUp` 共 6 处同步扩展，缺一处视为未完成。
- `Resources.Load` 路径区分大小写（部分平台）；模型/音频资源命名必须与代码字符串完全一致。
- CC-BY 素材在引入前必须先记录作者、来源链接、许可证和本地文件名到 credits 文件；未记录即不允许提交。
- 任何新增 UI/相机调参必须按横屏基准设计；引入会受 aspect 影响的逻辑前需先在 CONTEXT.md 与 ADR 中变更画面构图基准（`FramingReference`）。
- Boss 招式 FX 必须通过 `BossPatternFxPool` 租用；`BossPatternVfxPlayer.End()` 不得 `Destroy` 已交回池的对象。池容量上限 48，回收由 `pool.ReleaseAll` 统一处理。
- 新主题必须在 `ThemePresentationProfile` 中填全光照档案字段；不允许新主题复用他主题的主光参数（会退化回「只有雾色不同」）。
- 共享缓存材质的逐物件染色必须走 `RunnerVisuals.SetRendererColor`（MaterialPropertyBlock）；直接写 `sharedMaterial` 会让同批物件一起变色。
- 每帧刷新的 HUD 文本必须按值变更门控，不得逐帧构造字符串；倒计时类文本以其显示粒度（如 0.1s）为比对单位。
- `tools/` 下的门禁脚本一律纯 ASCII 注释（无 BOM 时 PowerShell 5.1 按 ANSI 读取，中文会导致误导性解析错误）。

---

## 范围之外

- 角色解锁/皮肤系统。
- 多人/排行榜网络功能。
- **按角色类型分治动画范式**（见 `docs/adr/0003-mecanim-for-protagonist-avatar.md`）：
  - 主角：允许 Mecanim Animator 状态机（`GugugagaBase.controller` + 运行时 `AnimatorOverrideController`），因为需要 `Speed01` / `LaneLean` / `VerticalSpeed` 参数混合。
  - Boss：继续禁止 Mecanim；播放走 Legacy `Animation` + `BossRigAnimator`（ADR-0002）。
  - 动画仍严禁进入命中判定路径；`applyRootMotion` 必须为 false。
- ScriptableObject 关卡编辑器（当前为代码驱动，短期内不迁移）。
