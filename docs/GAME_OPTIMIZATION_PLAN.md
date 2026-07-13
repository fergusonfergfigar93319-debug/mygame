# 咕咕嘎嘎优化方案与执行记录

> 状态：执行记录。本文包含早期 Android/Kotlin 原型阶段和后续 Unity 跑酷阶段的优化记录。当前设计入口见 [CURRENT_DESIGN.md](./CURRENT_DESIGN.md)；本文中的旧文件名和旧阶段建议用于追溯历史，不代表当前优先级。

本文用于给后续开发者快速理解：当前游戏已经做到哪里、接下来优先改什么、每一阶段为什么这样排。

## 设计参考结论

结合移动游戏、独立平台跳跃游戏与现代休闲游戏的常见做法，当前版本优先围绕六个方向优化：

- 短局循环：一次游玩要能在几十秒到数分钟内产生明确结果。
- 每日挑战：固定 seed、今日榜、可复现路线能提升回访动机。
- 渐进引导：先教核心操作，再逐步展示道具、伙伴、Boss 与营地成长。
- 强反馈：关键状态变化要有音效、粒子、镜头、HUD 或结算确认。
- 元成长：局内收集资源，局外做轻量升级，但不能压过操作本身。
- 低摩擦界面：模式入口、暂停、结算、排行榜、营地之间要能快速切换。

## 当前项目判断

项目已经具备可玩 Alpha 的主体结构：

- 主线关卡：雪松村废墟、冰湖回音谷、北境雾堤与高松鹅 Boss 链路已接入。
- 无尽模式：极夜漂流、今日挑战、固定 seed、排行榜和结算明细已经形成闭环。
- 局外系统：补给营地、鱼干累计、三项轻量升级已经落地。
- 表现层：BGM、音效、暂停层、设备倾斜、粒子、场景主题和图鉴正在形成统一风格。

主要风险：

- `GuguGagaGame.kt` 与 `EndlessMode.kt` 仍然偏大，后续改 Boss、机关、道具容易互相影响。
- 旧文件中仍有少量编码损坏的文案，影响测试、协作和审阅效率。
- 营地成长、今日挑战、排行榜之间还需要更明确的玩家目标提示。
- Boss 与第三关已经有骨架，但还需要更清楚的阶段提示、失败复盘和奖励闭环。

## 分阶段路线

### 第一阶段：可读性与体验闭环

目标：玩家打开游戏后，能清楚知道可以玩什么、得到什么、下一步做什么。

- 修复主界面、暂停层、营地、转场、默认昵称等可见乱码。
- 统一暂停层的音频开关、返回、继续按钮文案。
- 明确营地升级说明，让鱼干资源和局内收益形成闭环。
- 保持主线和无尽 BGM 自动切换，并保留暂停时的 BGM 暂停/恢复。

### 第二阶段：首局体验

目标：前 5 分钟不让玩家迷路。

- 第一关引导改为更短的分段提示，只在触发点出现。
- 第一次拿到鱼干、围巾、护盾、长跳靴时弹出一次性轻提示。
- 第一次通关后提示“团团加入”和“营地可升级”，把主线奖励接到局外成长。
- 首页根据存档状态突出一个推荐入口，例如继续主线、今日挑战、去营地升级。

### 第三阶段：无尽模式留存

目标：让极夜漂流成为每天愿意打开一次的模式。

- 今日挑战结算页展示“今日第几次尝试”和“距离今日最佳差多少”。
- 无尽 HUD 增加倍率档位进度和下档倒计时。
- 奖励段和危险段做更强的视觉区分，玩家提前读懂接下来的节奏。
- 排行榜筛选保留全部/今日，并增加“我的最好一局”高亮。

### 第四阶段：Boss 与章节高潮

目标：第三关从占位关卡变成真正的阶段高潮。

- 高松鹅 Boss 增加分阶段提示：登场、护盾、破盾、狂暴、击败。
- Boss 失败后显示复盘提示，例如需要鱼干冲刺或团团支援破盾。
- 击败 Boss 后更新图鉴和首页标题，形成明确剧情反馈。
- 第三关加入更多雾堤专属机关，让它和前两关有玩法差异。

### 第五阶段：工程拆分

目标：降低后续多人开发冲突。

- 把主线物理、碰撞、道具拾取从 `GuguGagaGame.kt` 拆成系统类。
- 把 Boss 绘制、Boss 碰撞、Boss 事件反馈从主循环继续下沉。
- 把无尽模式的世界拼接、实体裁剪、计分事件拆成更小的协作边界。
- 给 `SaveRepository` 增加结构化进度快照，减少 UI 层直接拼状态。

## 已执行内容

### 2026-07-13：长期成长闭环

- 新增长期成长存档版本 3 与单向、非破坏迁移；局内札记/技巧连段继续作为会话数据，营地、羁绊、发现与目标作为永久数据。
- 新增五处三级营地重建节点及前置关系，并接入实际使用的营地/商店/战绩三合一面板和大厅动态剪影；原有六项数值强化完整保留。
- 新增团团/Dror 羁绊等级、旅程经验来源和封顶同行帮助；收藏帮助进入结算鱼干倍率，条件援助使用确定性判定并由 `RunOutcomeRouter` 发放。
- 路线导演记录真实完成的事件 ID；图鉴新增 12 条路线手记、6 件主题遗物、未知线索与新发现已读状态。
- 新增 `GrowthTransactionInput/Result`，统一任务、旧剧情解锁、主题发现和羁绊更新；任务记录不再在旅程事务内部重复保存。
- 大厅建议统一为“可领取目标 → 新发现 → 可重建建筑 → 可购买强化 → 新旅程”，结算页显示最强永久成长反馈。
- 四阶段静态门禁、包清单解析、运行时 `.meta` 校验、占位符扫描、差异检查和 Unity Roslyn 编译通过。Unity Editor 批处理仍因本机许可证缺失返回 198，运行时布局、输入、动效与 Android 实机验证尚未完成。

### 2026-07-13：局内深度与主题 Boss

- 新增确定性路线事件目录与导演，覆盖六主题和六类事件；路线标记无 Collider，选择结果通过独立世界命令发奖。
- 新增四类局内强化、确定性三选一、6 次选择上限、3 层堆叠上限、暂停覆盖层和 12 秒自动选择兜底。
- 新增技巧评分与 HUD 摘要，真实接入碰撞、拾取、片段、路线和 Boss 反击事件；受伤断链。
- 新增六主题 Boss 特性与本局主题遗物标记，保持 `CheckPatternHit -> RunnerHitResolver.IsBossPatternHit -> IsPatternDangerous` 为唯一伤害权威，特效池容量继续为 48，碎片爆发上限继续为 6。
- 结算页新增路线、构筑、技巧和主题遗物高光；阶段二只传递结果，不新增永久成长写入。
- 静态门禁、包清单解析、差异检查、运行时 `.meta` 校验和 Unity Roslyn 编译通过。Unity Editor 批处理因本机许可证缺失返回 198，运行时与 Android 验证尚未完成。

- 修复 `SaveRepository` 中的默认昵称、注释与存档扩展入口，新增今日挑战尝试次数、今日最佳分、营地首次提示等状态。
- 首页新增动态推荐卡，根据主线进度、营地可升级、今日挑战和 Boss 进度提示下一步。
- 第一关通关后新增“团团归队，营地开放”的一次性提示，把剧情奖励接到营地成长。
- 营地首次访问会写入访问状态，后续可用于更细的新手任务。
- 今日挑战结算展示今日尝试次数、今日最佳刷新或差距。
- Boss 战失败提示改为更具体的破盾复盘，减少玩家不明原因死亡的挫败感。
- 无尽 HUD 修复乱码，新增倍率进度条、下档倒计时和封顶提示。
- 新增“极光磁针”道具：主线和无尽均可短时间吸附附近鱼干，增强收集爽感与路线收益。
- 生成并接入自有合成音频：主线/无尽 BGM、UI 选择、金币、道具、磁针和薄冰破裂音效，避免引入版权不明素材。
- 修复主线 HUD 与第二关剧情文案乱码，让关卡内容、状态提示和失败复盘保持统一中文表达。
- 营地成长扩展为四项升级，新增“磁针调校”，让极光磁针持续时间可通过局外鱼干资源提升。
- 当前工程已通过 `./gradlew.bat :app:compileDebugKotlin`。
- **Boss 战可读性与场景建模（2026-05）：** 程序化攻击与 `IsPatternDangerous` 同步（体前倾/缩放）；预警与跑道 FX 对齐 `PlayerTelegraphZ`；齐射投射物落点对准危险窗；雾堤假预警 decoy；冲锋改为安全车道判定；HUD 危险条；鱼干/滚球/旋刃视觉强化（程序化 + 现有 FBX）。
- **主菜单 Lobby 完整重构（2026-05）：** 顶栏改为紧凑玩家条 + 点击打开 `ProfileStatsPanel` 战绩 Sheet；活动信息迁入 `TopTicker` 单行条，移除遮挡吉祥物的 `ActivityBanner`；主 CTA 上方增加可点击 `DifficultyChip`（`SettingsOnly` / 开局前选难度）；`RunOutcomeRouter` 结算时 `IncrementTotalRuns`，`PlayerSave.EnsureTotalRunsSynced` 回填老存档；底栏 Tab 选中态填色强化。
- **跑道布景可读性（2026-05）：** `PlayableViewCorridor` + `SceneryLateralOffset` 随弯道对齐；天空云/光柱远场化，彩虹改为两侧细条；近场走廊内隐藏灯/云/光柱/远山；验收：16:9 **SkyFlight** 开局 10 s 中央可见三车道与前方障碍；侧向无大块实心遮挡。
- **Unity 存档 / UI / 排行榜闭环（2026-05）：** 「幸运起手令」开局消耗 pending 并给予 +1 生命（与 `ShopStore` / `RunnerGameController` / `WorldDirector` 接通）；移除主菜单与 `PanelHeader` 调试日志；删除未使用的 `SeenCampUnlockNotice`、`LeaderboardStore.GetBestEntry`/`GetAverageTotalScore`、`RunRecords.GetAll`/`Clear`；`DifficultyPreset.ScoreMultiplier` 在 `RunOutcomeRouter.Apply` 与结算得分相乘；排行榜增加「全部榜单 / 今日 challenge 桶」筛选、`TryGetPlayerPeakMetrics` +「我」峰值行高亮；图鉴沿用 5 条剧情进度条，新增 Boss 战绩区块（读 `PlayerSave.HasDefeatedBoss`）、解锁阈值集中到 `CodexUnlocks` 常量；极地直觉 UI 文案使用 `PolarPowerUpChancePerLevel = 0.025` 与 SegmentSpawner 对齐；任务面板领奖后刷新头部鱼干胶囊（`PanelHeader.RefreshCurrency`）。
- **Boss VFX 收口（2026-05）：** 修复 `BossPatternVfxPlayer.End()` 双重 Destroy 导致 FX 对象池失效；`SegmentSpawner` 补齐 5 处 `ObstacleKind` 标签；`EndPattern` 清零跨招震屏残留；SalvoOrb 动画去 O(N²)；雾堤/珊瑚 Boss 增加 `Head` 锚点使 `StrikeHeadPitch` 生效；6 招 Strike SFX 复用现有 clip + 音调/叠层差异化；Boss BGM 专属文件占位状态固化于 credits。
- **逻辑判定 / 反馈 / UI 三向优化（2026-05）：** `CollisionSystem` 读取 `ObstacleBehavior.IsCollidable`；冲刺接好 `ShatterWall.TryShatter`；`ObstacleColliderSpec.IsThunderFeatherDestructible` 替代尺寸启发式；`RunnerHitResolver` + `RunnerCombatTuning` 集中无敌与 Boss 闪避常量；`EmitRunFeedback` 统一闪屏；`PenguinCharacter` 受击 tint / 完美闪避环；危险/奖励片段 HUD 横幅；`RunnerHud` 倍率 tier 条与反馈胶囊分色；OpenUPM 安装 DOTween + `UiBuilder.WireClickFeedback` / 面板 `PlayPanelPopIn` / 结算 `DOScale`。

## 已执行：Boss 战与跑酷体验优化（Unity 版本）

已完整实施市面流行跑酷游戏 Boss 战设计参考，具体内容如下：

### 第一阶段：Boss 战打磨

- **阶段化节奏**：Boss 战现在遵循 `Intro -> Learn -> Pressure -> Vulnerable -> Defeat` 的节奏，每 4 招后强制进入破绽期，避免完全随机带来的不可预测性。
- **改进 HUD**：显示当前轮次、距离下次破绽还有几招、当前阶段已持续时间，让玩家能预判战斗节奏。
- **死亡复盘**：新增 `GetDeathAnalysisHint()` 方法，根据被什么招式击中给出具体的改进建议（如"震地脉冲需要滑铲，不是跳跃"）。
- **镜头特效**：Boss 危险攻击时轻微震动，击败时定格 0.25 秒 + 震动 0.5 秒，增强打击感。

### 第二阶段：6 个 Boss 明确身份

为每个 Boss 定义了特色定位：

| Boss | 定位 | 主机制 | 推荐反制 | 专属奖励 |
|------|------|--------|----------|----------|
| 冰霜雪王 | 均衡型 | 慢节奏重击，破绽期长 | 观察预警，把握跳跃时机 | 雪王之冠 |
| 雪松哨兵 | 地面型 | 路障/木桩主题 | 多用滑铲 | 哨兵之角 |
| 极光长蛇 | 机动型 | 招式预警短但有颜色轨迹 | 善用磁吸道具有规划路线 | 极光鳞片 |
| 雾堤守卫 | 诡诈型 | 真假预警，雾区遮挡 | 相信 HUD 提示而非视觉假象 | 雾核 |
| 珊瑚海怪 | 防御型 | 多点投射物，护盾反弹 | 拾取护盾在破绽期撞击 | 珊瑚碎片 |
| 雷云苍鹰 | 空战型 | 高机动俯冲和冲锋 | 拾取滑翔羽毛善用空中移动 | 苍鹰之羽 |

### 第三阶段：场景联动与刷新条件

- **Boss 刷新规则**：新增 `BossSpawnContext` 和 `BossSpawnCondition`，Boss 出现由当前场景 + 玩家进度 + 难度共同决定。
  - 冰霜雪王：首个教学 Boss，无条件出现
  - 雪松哨兵：雪松废墟场景，400m 后出现
  - 极光长蛇：极光磁场场景，600m 后且击败过 1 只 Boss
  - 雾堤守卫：雾堤场景，800m 后且击败过雪松哨兵
  - 珊瑚海怪：海洋珊瑚礁场景，500m 后
  - 雷云苍鹰：天空飞翔场景，1000m 后且击败过 3 只 Boss
- **主题切换约束**：Boss 活跃时不切图，切图后重算下一只可刷 Boss。
- **HUD 预告**：主面板显示当前场景可能遭遇的 Boss（如"\u2620 雪松哨兵"）。

### 第四阶段：前奏与奖励片段

- **Boss 前奏段**：在 Boss 出现前 100m 生成特殊片段：
  - 放置推荐反制道具
  - 教学金币线展示正确动作路线
  - 降低普通障碍密度
- **Boss 奖励段**：击败后生成鱼干大串、短暂安全冲刺道具、主题特色道具。

### 关键实现文件

- `Assets/Scripts/Runtime/PenguinRun/Systems/BossDefinitions.cs` - Boss 定义与刷新条件
- `Assets/Scripts/Runtime/PenguinRun/Systems/BossSystem.cs` - Boss 战斗逻辑与复盘
- `Assets/Scripts/Runtime/PenguinRun/Systems/BossEncounter.cs` - Boss 阶段节奏控制
- `Assets/Scripts/Runtime/PenguinRun/Systems/RunnerHud.cs` - HUD 显示
- `Assets/Scripts/Runtime/PenguinRun/Systems/SegmentSpawner.cs` - 前奏与奖励片段
- `Assets/Scripts/Runtime/PenguinRun/PenguinRunnerGame.cs` - 镜头特效
- `Assets/Scripts/Runtime/Game/Save/PlayerSave.cs` - 击败记录

## 下一批建议

下一批建议优先处理"道具教学 + 文件拆分"：

- 为鱼干、围巾、团团支援、长跳靴增加首次获得轻提示。
- 抽出 `StoryRunState` / `StoryCollisionSystem` / `StoryPickupSystem`，先减少主循环压力。
- 给排行榜页增加"我的最好一局"视觉高亮，把每日挑战目标进一步做实。
