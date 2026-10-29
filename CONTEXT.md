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
- **宽容语义：** 起身期间（约 0.16s）仍保持低姿态穿过资格；空中下滑 / 贴地预输入经 `slideBufferTimer`（默认 0.16s，急坠可延长）在落地帧衔接滑铲，并优先于过期跳跃缓冲。
- **统一消费方：** Boss 低空招式（`SweepLow` / `QuakePulse`）与障碍 `IsLow` 共用同一蹲姿盒语义（`PlayerMotor.IsLowPosture`，含起身宽容窗），不得只认 `SlideTimer>0`——否则同一帧姿态会出现「障碍判过、Boss 判死」。
- **勿混淆：** 跳跃净空判定仍走 `FeetClearanceAboveRoot`，与蹲姿盒相互独立。

### 完美动作窗口（PerfectActionWindow）

- **意指：** 玩家执行跳跃或滑铲后的短窗口（跳跃 0.32s、滑铲 0.50s——与滑铲时长 0.56s 对齐，空中变体 0.28s，见 `RunnerTuningConfig`），在此窗口内成功规避障碍记为完美闪避并加分。
- **执行点：** `PlayerMotor` 设置窗口（含预输入跳跃兑现与落地衔接滑铲两条补设路径）；`CollisionSystem` 在窗口内匹配 `LastCriticalAction` 与规避方式。

### 命中解析（RunnerHitResolver）

- **意指：** 障碍与 Boss 共用的一层防御/扣血判定，统一护盾、鱼弹、冰镜、树人护甲、珊瑚回弹、雷羽击破与无敌时长（`RunnerCombatTuning`）。Boss 招式命中同样按此顺序消耗防御层（护盾为消耗制并附护盾碎裂无敌——**障碍与 Boss 两条路径均附**，不再是 Boss 期间持续免疫）；真实扣血后由 `BossSystem.NotifyBossDamageTaken` 记录死亡复盘。
- **防御顺序陷阱：** 护盾在防御顺序中优先于珊瑚回弹，因此道具**不得**附赠护盾层（珊瑚回弹曾附赠 3.5s 护盾，导致回弹层永远轮不到消耗、道具名不副实）。
- **勿混淆：** Boss 招式闪避几何仍由 `RunnerHitResolver.IsBossPatternHit` 与 `BossSystem.CheckPatternHit` 协作；VFX 不改变命中规则。

### 移动敌人（MovingEnemy）

- **意指：** 注册在 `EnemyNpcState` 列表中、有独立运动轨迹的障碍单位。当前支持两种基本行为：`IsPatrol`（横向巡逻，正弦振荡）和斜向扑入（从屏幕外冲入）。
- **勿混淆：** Boss（有专属生命值/阶段/招式池）不属于 MovingEnemy。

### 道具（Pickup / PowerUp）

- **意指：** 玩家拾取后触发即时效果的场景物件。由 `PowerUpKind` 枚举标识，在 `PickupSystem` 中激活效果，在 `WorldDirector` 中管理持续时间/状态。
- **分层：** 通用道具适用全主题；主题道具仅在对应调色板片段中以更高概率出现。
- **雾灯（FogLantern）：** 激活期间除磁吸/慢动作外，雾起点/终点平滑外推（+45m/+70m，`FogLanternFogDriver`），提前照亮前方障碍——这是它区别于磁铁的核心语义，雾堤主题收益最明显。
- **勿混淆：** 金币（Coin）是另一类收集物，不视为道具。

### Boss

- **意指：** 在跑酷途中出现的大型对手，有独立生命值（`HitsRemaining`）、攻击模式池（`PatternPool`）和阶段流程（出场 → 学习 → 压迫 → 破绽 → 击败）。
- **相关：** Boss 前奏片段（`SpawnBossPrelude`）在 Boss 出现前 ~100m 生成预警和补给；Boss 奖励片段（`SpawnBossReward`）在击败后生成奖励。
- **预告一致性：** `BossSystem.NextBossDefinition` 首次访问时按真实上下文抽取并缓存（含第一场 Boss），`SpawnBoss` 消费同一定义——前奏预告谁、出场就是谁，不允许出场时重抽。
- **命中车道锁定：** `DiveHigh` 命中判定只读开战锁定的 `DiveLaneX`；Boss 根节点 x 会向中线回插，读 `WorldX` 会让命中带与预警圈车道相反。齐射（`RangedSalvo`）仅在弹道落地时刻附近（约 0.14s 冲击窗）判伤。

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

- **意指：** `ThemePresentationProfile` 中的光照字段组（`KeyLightColor` / `KeyLightIntensity` / `KeyLightEuler` / `RimLightColor` / `RimLightIntensity` / `AmbientLight` / `FogColor` / `FogStart` / `FogEnd`），决定每个主题的主光色温与朝向、边光配色与雾带景深。另含 `SkyAuroraColor`（天空盒极光带颜色，黑色 = 该主题天上没有极光带）与 `PostTemperature` / `PostSaturation`（`RunnerPostFx.ApplyThemeGrading` 消费的逐主题色调）。
- **天象归属（ThemeSkyDressing）：** 月亮 / 太阳 / 极光带按主题布置于 `ThemeSkyDressing`：极光图满编五条极光带、冰湖两条余晖、其余主题无极光带；天空主题为日盘（复用 "Moon" 命名继承不贴地/不淡出行为）、雾堤只有漫开的冷光晕。冰晶簇/雪堆只属于寒带主题，远山剪影配色随主题传入。
- **背景：** 此前六个主题共用同一套方向光与雾距，仅雾色不同，导致每张图的「光」完全一样。光向与色温是主题之间最强的区分手段。
- **执行点：** `PenguinRunnerGame.ApplyMapThemeVisuals` 消费档案并覆盖 `keyLight` / `rimFillLight`；`BuildScene` 只建默认值。主题切换时重新应用。
- **勿混淆：** Boss 战氛围（`RunnerPostFx.SetBossEmphasis`）是叠加在主题光照之上的临时后处理，不修改 `RenderSettings`。

### Boss 专属 BGM（BossBgmClip）

- **意指：** 进入 Boss 战时替换跑步 BGM 的独立音乐轨，由 `RunnerAudio.EnterBossMusic` 切入、`ExitBossMusic` 恢复。每个 Boss 通过 `BossDefinition.BgmClipName` 字段（本次内容包新增）映射至 `Resources/PenguinRun/` 下的 `.ogg` 文件。

### 纸张 UI 层（PaperUiLayer）

- **意指：** `Assets/Scripts/Runtime/Game/UI` 下的大厅与面板 UI，使用童话纸张色板（`PaperBase` / `InkPrimary` / `PineGreen` / `MistBlue` / `CoralAction` / `StitchBorder`）。
- **权威边界：** 该层**禁止**引用旧霓虹色板令牌（`UiTheme.AccentCyan` / `UiTheme.Surface*` / `UiTheme.TextPrimary` / `UiTheme.TextSecondary` / `UiTheme.TextTertiary` / `UiTheme.Success` 等），一律走 `UiBuilder` 别名。整套纸张改版就是靠别名层重映射完成的，漏掉一个别名（当年的 `AccentCyan`，以及后来的 `Success` / `TextTertiary`）就会让几十个调用点同时跑偏。由 `VerifyStorybookPalette.ps1` 断言。
- **勿混淆：** 局内 HUD（`RunnerHudCanvas`，`Runtime/PenguinRun/Systems`）画在深色 3D 场景上，继续使用旧令牌是正当的，不在本层范围内。
- **主操作语言：** 主 CTA 一律 `CoralAction` + 白字（与大厅底坞一致）；选中/激活态用 `AccentActive`（PineGreen）；次级按钮用 `MistBlue` + 墨色字。
- **相关陷阱：** `UiBuilder.AddOutline` 走 uGUI `Shadow`，描边 alpha 会乘以源顶点 alpha——底色全透明的「幽灵按钮」连描边一起消失。

### 美术回退链（ArtFallbackChain）

- **意指：** 大厅美术的多级回退（premium 资源 → storybook 资源 → `UiBuilder.CircleSprite` 占位圆）。
- **风险：** 整条链**静默**——资源缺失不抛异常、不打日志，画面上只是少了角色或多了个白圆。吉祥物就是这样消失了很久没被发现（`.meta` 缺 `TextureImporter:` 块 → 资源从未导入 → 一路退到占位圆）。
- **规则：** 会显示出来的美术必须是真资源；允许"缺席"（隐藏节点），**不允许**退化成占位圆或复用他人的立绘（伙伴退化成吉祥物会让大厅出现两个同样的角色）。由 `ProjectLobbyArtValidation` 断言。
- **相关：** 新增 PNG 资源必须确认 `.meta` 含 `TextureImporter:` 与 `textureType: 8`；只有 `fileFormatVersion` + `guid` 的 60 字节 meta 是残缺的。
- **现网主视觉：** premium 吉祥物、团团、Dror 与营地建筑图集已于 2026-07-28 落盘；大厅全景优先 `camp_panorama_image2`，次级纸张页（章节/手记/图鉴/商店/结算/死亡/战绩/暂停/加载）走 `Image2ArtCatalog` 或 `CampVisualAssetCatalog` 的 Image 2 路径。路径写在目录里仍不等于能加载——必须同时有完整 `TextureImporter` `.meta`。
- **勿混淆：** 伙伴缺失时返回 null（隐藏节点），不得回退成吉祥物立绘或占位圆。旧 premium 全景文件仍保留作资产，但运行时全景入口已切到 Image 2。

### UI 字形（UiGlyph）

- **意指：** UI 文本里出现的符号字符。项目 TMP 字体是**动态图集**，能否显示取决于源字体覆盖，而非当前图集内容。
- **规则：** 新增装饰性符号一律走 `UiIcons` Sprite，不要写裸 Unicode。已确认 ★(U+2605)、✓(U+2713) 可用；➞(U+279E)、❀(U+2740)、⌛(U+231B)、✨(U+2728) 在实机是豆腐块。
- **勿混淆：** `TMP_FontAsset.HasCharacter` 对动态字体只反映当前图集，不能用来判断「会不会变豆腐块」——据此写的门禁会把正常显示的 ★ 误报为缺失。

### 画质档位（RunnerQualityTier）

- **意指：** 按设备能力（内存 / 核心数 / 显存）选出的 Low / Medium / High 三档，以及每档对应的 `RunnerQualityBudget`（Bloom、阴影、粒子 Raycast、LOD Bias、骨骼权重、贴图 mip）。
- **权威边界：** 档位决策只在 `RunnerDeviceTier` 一处；`RunnerPostFx` 等消费方一律读 `RunnerQualityBudget.For(RunnerDeviceTier.Current)`，**不得**自行读 `QualitySettings.GetQualityLevel()`——项目里没有任何代码设置那个等级，读它等于读构建默认值，这正是上一版分档从未生效的原因。
- **可测性约束：** 档位决策**不得**被 `#if UNITY_ANDROID` 之类的平台宏编译掉，否则编辑器门禁无法覆盖。桌面/编辑器固定落在 High。
- **单调性：** 高档在任一杠杆上都不得比低档更省，由 `ProjectQualityTierValidation` 断言。
- **调用顺序：** `RunnerDeviceTier.Apply()` 必须早于 `RunnerPostFx.TryAttach`。
- **勿混淆：** `Application.targetFrameRate` 与 vSync 由 `ConfigureRuntimePerformance` 管理，不属于档位表。

### 局内反馈配方（RunFeedbackProfile）

- **意指：** 一次局内反馈事件（完美闪避 / 擦身 / 受伤 / 死亡 / Boss 击中 / 格挡 / 冲刺起手…）在各表现通道上的配方：闪屏颜色与时长、震屏幅度与时长、视野踢击度数、命中顿帧秒数、是否振动。纯数据表，`RunFeedbackProfile.For(kind)` 查询。
- **权威边界：** 判定层（`RunnerGameController` / `CollisionSystem`）只产出 `RunFeedbackKind`，不得再写死 `new Color(...)` 闪屏或直接调振动；`PenguinRunnerGame.HandleRunFeedback` 是唯一分发点。振动强度 = 配方强度 × 玩家设置 `HapticIntensity`，Android 走 `Vibrator.vibrate(ms)`，时长 18–70ms。
- **命中顿帧：** 受伤 / Boss 击中 0.14s、格挡 0.06s，模拟 dt × `HitStopTimeScale`（0.3）。**布景滚动必须乘同一个 `SimulationTimeScale`**，否则路面在动、障碍不动。输入在顿帧期间照常读取。
- **勿混淆：** Boss 击败的 `defeatFreezeTimer` 只放慢相机 dt，是演出而非顿帧；死亡定格阶段以 0.35 倍速推进相机与角色，属死亡序列。

### 全屏表现层（RunnerScreenOverlay）

- **意指：** 跑酷场景里所有铺满屏幕的表现：一次性闪屏、Boss 边缘红光预警、死亡红幕、开局 3-2-1 倒计时与主题切换幕布。保留式 uGUI，`sortingOrder` 200（HUD 170 之上、死亡卡 240 / 札记 260 之下），**不带 GraphicRaycaster**（闪屏不得吞掉暂停按钮触摸）。
- **背景：** 原先这些都在 `PenguinRunnerGame.OnGUI` 里用 IMGUI 画。IMGUI 每帧至少跑两遍（Layout + Repaint），闪屏计时器在绘制里递减，衰减速度随该帧 IMGUI 遍数漂移；倒计时字体、缩放与其余 uGUI 不一致。
- **规则：** `PenguinRunnerGame` 不得再出现 `OnGUI` / `GUI.` / `GUIStyle`；闪屏按 `Time.unscaledDeltaTime` 每帧只衰减一次；颜色写入按 `Color32` 量化门控，缩放/透明度按 1/100 门控。由 `VerifyRunFeelBatch.ps1` 断言。

### 擦身而过（NearMiss）

- **意指：** 未做跳/铲、仅靠车道位置以极小侧向间距（判定盒外 `NearMissBandX` = 0.55m 带内）掠过障碍，且没有靴底净空。记 `SkillAction.NearMiss`（基础 40 分，走技巧连段倍率）、高频短促气流音、淡青闪屏 + 视野微扩。
- **边界：** 只看本帧真实重叠带（`overlapsNow`），掉帧穿透补判不算；从上方跳过的属于完美跳跃路径，不重复计；每个障碍只计一次。
- **勿混淆：** 完美闪避要求在动作窗口内以跳/铲清过障碍；擦身是「换道贴过去」的奖励，两者互斥。

### 局内 HUD 霜面色板（RunnerHudFrostPalette）

- **意指：** 深色 3D 场景上的跑酷 HUD 使用一组独立令牌：`CardFill`（深海蓝 78% 底）/ `CardStroke`（冰雾描边）/ `TextSnow`（雪白主文）/ `TextFrost`（霜蓝辅文）/ `Reward`（暖金）/ `Danger`（珊瑚）。暂停面板主操作与大厅底坞同语言：珊瑚底 + 白字。
- **规则：** `RunnerHudCanvas` 不得引用 `UiTheme.AccentCyan` / `UiTheme.TextSecondary` / `UiTheme.PaperWarm`（此前同一张卡上三套色板混用）；`BossHudPanel` 破绽态用 `UiTheme.PineGreen`，不用霓虹 `Success`。生命心形 ≥ 40px。
- **勿混淆：** 这不是纸张 UI 层（`PaperUiLayer`），纸张层的别名门禁不覆盖本层；本层由 `VerifyRunFeelBatch.ps1` 单独断言。

### 主题听感档案（ThemeAudioProfile）

- **意指：** 每个 `RunnerMapTheme` 的听觉身份：循环环境音床（clip + 音调 + 音量缩放）、落地/受击共用的地面音色、金币音调性、Boss 击发音调缩放。与 `ThemePresentationProfile` 对称，同为纯表现层，不改任何判定。
- **背景：** 此前六张图共用一条 ambience 与同一套撞击音，音频上的主题差异只有 BGM 一层——BGM 一停或重复，六张图听起来完全一样。
- **执行点：** `PenguinRunnerGame.ApplyMapThemeVisuals` 同时切视觉与听感，避免画面已换、声音还停在上一张图；`RunnerAudio.ApplyTheme` 换床。
- **玩家设置边界：** 仅当环境音设置为 `PlayerSave.AmbienceThemed`（新默认值）时才随图切换；玩家显式选过风声/洞穴/赛博则保留其选择，此时只应用音效层。老存档未写过该键，因此无需 SaveSchema 迁移。
- **资源：** 由 `tools/generate_theme_audio.py` 合成（无第三方授权），每主题定种子，重建字节一致。
- **勿混淆：** Boss 专属 BGM（`BossBgmClip`）按 Boss 走，不按主题走；两者在 Boss 战期间同时生效。

### 片段池（SegmentPool）

- **意指：** 同一主题下所有 `RunnerSegmentDefinition` 的集合，由 `RunnerSegmentCatalog.GetPool(theme)` 返回，`SegmentWaveDirector` 从中随机选取下一个片段。

### 跑道可视走廊（PlayableViewCorridor）

- **意指：** 第三人称镜头与玩家之间的近场带内，程序化布景不得用大体块遮挡三车道与前方障碍预判；侧向装饰应落在护坡外（`|x| ≥ SceneryMinAbsX` = **8.5 m**，以代码常量为准），相机—玩家 Z 带内误入走廊的物体在 `AnimateScenery` 中淡出至低 alpha。
- **中景带：** 第二排主题模型填近场道具与远山剪影之间的空带，落在护坡外更远一侧（约 13–16 m）。只做视觉，必须剥掉碰撞，不得挤进走廊。
- **例外：** 贴在路面上的扁平细节（冰裂等）作为**路面瓦片的子物体**存在，随路滚动、随瓦片销毁，不注册进布景系统，也不受本走廊淡出约束。
- **执行点：** `PenguinRunnerGame` 常量 `ViewCorridorHalfWidth`（约 **5.2 m**）、`SceneryLateralOffset`（布景随弯道 `WorldCurveX` 偏移）、`ApplyViewCorridorFade`（走廊内隐藏或极低 alpha）；天空浮云（`sky_cloud`，不贴地）/光柱/侧向彩虹条、远山 `Distant Ice Ridge` 为高风险装饰。主题套件（珊瑚、废墟、拱门等）走开源网格，不得再用大方块堆进走廊。
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

### 主角实距读感（AvatarGameplayLegibility）

- **意指：** 主角在 `RunnerCameraTuning` 真实机位（behind 10.5m / height 6.5m / FOV 72°）下的**背面**可辨识度。玩家整局看到的就是这个角度，它与近距离预览（`ProjectPenguinPreviewCapture`，4m / 34°）是两个不同的验收面，后者看不出前者的问题。
- **权威边界：** `GugugagaStylized.shader` 的主光固定在**视角空间**（`mul(dir, (float3x3)UNITY_MATRIX_V)`），保证镜头所在的一侧总是受光侧。世界空间常量主光只能照亮跑酷背面与大厅正面中的一个，另一个会全部落到 `0.38` 的 wrap 下限。
- **门禁：** `ProjectAvatarGameplayViewCapture` 断言角色像素 `meanLuma ≥ 65`、`detailStdDev ≥ 18`。
- **勿混淆：** 顶点色是基色（`input.color * _Tint`），但提亮顶点色不能替代光照方向修正——受光侧不对时，再亮的基色也会被 wrap 下限压平。

### 表面贴片（SurfacePatch）

- **意指：** 贴在身体或头部曲面上、只提供颜色分区而非独立体积的网格（腹部白斑、面罩、胸兜）。
- **规则：** 必须经 `conform_inside()` 对微放大的宿主体做布尔求交，轮廓不得越过宿主轮廓；固定 y 偏移的扁椭球在宿主收窄处会穿出，侧视读作一块游离的浅色椭圆。多块贴片的叠压顺序由 **clip margin** 决定（当前胸兜 1.006 / 腹部 1.012 / 面罩 1.006），不靠各自往前顶。
- **连带约束：** 贴片贴合曲面后，原先按旧平面标定的面部装饰（眉毛、脸颊）会浮空，必须按宿主表面重新落座。

### 游玩内容（RunContent）

- **意指：** 局内机械内容：片段、障碍、道具、Boss 作为遭遇、路线选择作为玩法分叉。
- **勿混淆：** 剧情（Narrative）是章节承诺与高松鹅终局，当前阶段后置，见 `docs/adr/0005-defer-narrative-prioritize-run-feel.md`。

### 操作体验（RunFeel）

- **意指：** 输入、位移跟手、碰撞公平、反馈配方与镜头构图。换道跟手必须对帧率不敏感；局内收集物对玩家称「鱼干」。
- **相关：** 完美动作窗口、擦身而过、局内反馈配方、画面构图基准、手动冲刺。

### 手动冲刺（ManualDash）

- **意指：** 双击屏幕或 Shift/F 触发的短冲刺，与拾取道具「鱼干冲刺」分开计时。冷却 `ManualDashCooldownSeconds`（2.5s）；冷却中再次按下会缓冲到冷却结束再发出。
- **HUD：** 跑道左上常驻 `DashReady`：就绪为「可冲刺」，冷却中为「冲刺 1.2」。Boss 战期间仍可见，因为破绽反击走的就是这次冲刺。冷却不得再写入中央反馈条。
- **勿混淆：** Buff 条上的「冲刺 5.5s」是道具持续时间，不是手动冲刺冷却。

### 新手期（Onboarding Period）

- **意指：** 玩家尚未演示最小动作集时的首局保护期。四个能力位为换道、跳跃、滑铲、收集；全部演示过即毕业，另设 `TotalRuns >= 3` 保险丝强制毕业。
- **生效边界：** 「是否新手期」在开局 `RunnerSessionConfig.Snapshot()` 读一次，整局不变；能力位局内实时记录，结算时落盘。
- **期内行为：** 抑制路线分支、旅途札记升级、主题机关、世界事件与移动敌人；片段池按 `LaneWeave → JumpArc → SlideTunnel → RewardRun` 轮转并排除高难 pattern。第一次道具固定为鱼干冲刺（只展示加速这一种效果）。Boss **不**抑制。
- **勿混淆：** `Runner3DTutorialDone` 属于图鉴域，语义是「已完成第一次旅程」（解锁 `story_first_journey`），**不是**新手期毕业标志。
- **相关：** `docs/adr/0004-competence-driven-onboarding.md`、`OnboardingState`。

---

## 领域规则

- `RunnerSessionConfig.MapTheme` 是开局快照；局内主题权威是 `RunnerGameController.CurrentTheme`。新增玩法消费者不得继续读取开局主题。52 s 只累计有效 Running 模拟时间并产生单个 Pending 请求，Daily 不轮换；路线/机关/Boss 前奏与奖励尾部未释放时不得切图清场。
- 精通与契约按本局访问主题归属；A→B→A 复用 tracker，技巧契约只认该主题赚取的技巧分。多主题快照通过既有终局事务逐主题结算一次，不得在切图时写精通存档或重复结算其他成长。
- 本阶段新增实现必须服务游玩内容或操作体验；不为章节叙事、高松鹅终章战或局内换装新增系统（`docs/adr/0005-defer-narrative-prioritize-run-feel.md`）。
- 局内收集物对玩家称「鱼干」，反馈文案不得写成「金币」。
- 手动冲刺冷却只显示在 `DashReady`，不得用中央反馈条逐次播报剩余秒数。
- 每个主题必须同时具有：片段池定义、调色板障碍外观映射、Boss 刷新条件、BGM 映射、`ThemeAudioProfile` 听感档案（环境音床 + 地面音色，且不得与他主题重复）。
- 新障碍必须配套 `ObstacleColliderSpec` 规格；不允许未注册碰撞规格的障碍进入 `obstacles` 列表。动态障碍须实现 `ObstacleBehavior.IsCollidable` 并与视觉状态一致。
- 新道具必须在 `PowerUpKind`、`PickupSystem.ActivatePowerUp`、`WorldDirector`（如有持续状态）、`GetPowerUpColors`、`AddPowerUpEffects`、`PlayPowerUp` 共 6 处同步扩展，缺一处视为未完成。
- `Resources.Load` 路径区分大小写（部分平台）；模型/音频资源命名必须与代码字符串完全一致。
- CC-BY 素材在引入前必须先记录作者、来源链接、许可证和本地文件名到 credits 文件；未记录即不允许提交。
- 任何新增 UI/相机调参必须按横屏基准设计；引入会受 aspect 影响的逻辑前需先在 CONTEXT.md 与 ADR 中变更画面构图基准（`FramingReference`）。
- Boss 招式 FX 必须通过 `BossPatternFxPool` 租用；`BossPatternVfxPlayer.End()` 不得 `Destroy` 已交回池的对象。池容量上限 48，回收由 `pool.ReleaseAll` 统一处理。
- 新主题必须在 `ThemePresentationProfile` 中填全光照档案字段；不允许新主题复用他主题的主光参数（会退化回「只有雾色不同」）。
- 共享缓存材质的逐物件染色必须走 `RunnerVisuals.SetRendererColor`（MaterialPropertyBlock）；直接写 `sharedMaterial` 会让同批物件一起变色。
- 主角网格改动一律改 `tools/blender/build_gugugaga_avatar.py` 后重建，不得直接编辑 FBX；重建后必须依次跑 `RunnerAvatarAssetBuilder`（重建预制体 + LOD/材质校验）与 `ProjectAvatarGameplayViewCapture`（实距读感）。
- 新增贴在角色曲面上的颜色分区必须走 `conform_inside()`；未裁剪的贴片视为未完成（见「表面贴片」）。
- 每帧刷新的 HUD 文本必须按值变更门控，不得逐帧构造字符串；倒计时类文本以其显示粒度（如 0.1s）为比对单位。
- 新增局内反馈事件必须同时扩展 `RunFeedbackKind` 与 `RunFeedbackProfile.For`；判定层只报告种类，不得自带闪屏颜色或振动调用。
- 跑酷场景内任何铺满屏幕的表现一律挂到 `RunnerScreenOverlay`，不得新增 IMGUI 绘制。
- 难度得分倍率（`DifficultyPreset.ScoreMultiplier`）在结算中按乘法如实生效，不得用 `Max(base, …)` 之类写法把 <1 的倍率抹平——难度页承诺的取舍必须真实存在。
- `tools/` 下的门禁脚本一律纯 ASCII 注释（无 BOM 时 PowerShell 5.1 按 ANSI 读取，中文会导致误导性解析错误）。
- `VerifyFileSizeRatchet.ps1` 登记的超大文件只准缩小，不准增长。拆分落地时同步下调上限；上调上限必须在评审中说明理由，不得顺手改。
- 门禁脚本中的「禁止出现某写法」检查必须先剥掉注释再匹配，否则解释该规则的注释本身会让门禁失败。

---

## 范围之外

- 角色解锁/皮肤系统。
- 多人/排行榜网络功能。
- **按角色类型分治动画范式**（见 `docs/adr/0003-mecanim-for-protagonist-avatar.md`）：
  - 主角：允许 Mecanim Animator 状态机（`GugugagaBase.controller` + 运行时 `AnimatorOverrideController`），因为需要 `Speed01` / `LaneLean` / `VerticalSpeed` 参数混合。
  - Boss：继续禁止 Mecanim；播放走 Legacy `Animation` + `BossRigAnimator`（ADR-0002）。
  - 动画仍严禁进入命中判定路径；`applyRootMotion` 必须为 false。
- ScriptableObject 关卡编辑器（当前为代码驱动，短期内不迁移）。
