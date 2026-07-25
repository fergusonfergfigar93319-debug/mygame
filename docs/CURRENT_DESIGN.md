# 企鹅快跑当前设计基准

本文是当前版本的设计入口。后续新增玩法、UI、相机、关卡片段和 Boss 内容时，以本文、`CONTEXT.md` 和相关 ADR 为准。

## 2026-07-13 童话营地视觉基础

阶段一已经完成代码落地：

- 大厅改为“童话营地”构图，使用正式营地全景、透明企鹅吉祥物、故事主卡、伙伴角和五入口底坞。
- `MainMenuBootstrap` 只保留场景编排、存档读取和命令入口；背景、故事卡、伙伴、底坞和面板路由已拆为独立组件。
- 任务、图鉴、营地商店和设置使用纸张页面、章节标题、墨色正文和语义状态色。
- 局内统计 HUD 已从 IMGUI 迁移到保留式 uGUI；普通状态保护中心走廊，Boss 状态只保留生命、招式和反制提示。
- 死亡与完成结算共享 `JourneyResultViewModel`，并保留直接重开与返回营地两条路径。
- 场景 Loading 使用持久 uGUI 页面、吉祥物、主题提示和真实异步进度。

当前验证状态：`VerifyUiArtP0.ps1` 与 `VerifyStorybookUiPhase1.ps1` 静态门禁通过。Unity 6.0.4 批处理仍因本机无有效 Editor license（返回码 198）在导入/编译前退出，因此尚未声称完成 Unity 编译、运行时截图或 Android 实机验证。

## 2026-07-13 局内深度与主题 Boss

阶段二已完成代码落地：

- 每局按主题、每日模式和历史场次生成确定性的路线事件；首个事件不早于 220m，相邻事件至少间隔 280m。
- 路线选择提供明确的安心/冒险标签与无碰撞视觉标记；Boss、前奏和主题切换保持更高调度优先级。
- 路线完成后按第 1、3、5…次节奏出现三选一旅途札记；每局最多选择 6 次，同一强化最多 3 层，12 秒无操作时自动选择。
- 技巧分记录完美闪避、连续换道、空中收集、无伤片段、冒险路线和 Boss 反击；连段窗口为 2.5 秒、倍率上限 3.0，受伤会明确断链。
- HUD 在普通跑酷时显示路线、最多三项构筑摘要和技巧连段；Boss 战继续占据最高视觉优先级并隐藏次要信息。
- 六个主题 Boss 分别拥有冰裂回声、雪松根卫、极光换极、雾影分身、珊瑚回弹和风暴升流特性。特性只挂在既有阶段切换与攻击时刻，雾影仅为视觉对象。
- 结算携带路线次数、冒险次数、技巧分、峰值连段、强化 ID 只读副本和主题遗物；这些字段在阶段二不写入永久成长。

当前验证状态：`VerifyUiArtP0.ps1`、`VerifyStorybookUiPhase1.ps1`、`VerifyRunDepthPhase2.ps1` 均通过；`Packages/manifest.json` 可解析，`git diff --check`、运行时资源 `.meta` 完整性检查和 Unity Roslyn `Assembly-CSharp` 编译通过。Unity 6.0.4 批处理日志仍因本机没有有效 Editor license（返回码 198）在导入/运行前退出，因此路线实玩、三选一交互、六 Boss 特性、死亡重试和 Android 实机仍标记为运行时待验证。

## 2026-07-13 长期成长闭环

阶段三已经完成代码落地：

- 新增显式存档版本 3 与非破坏迁移。旧玩家的营地访问、伙伴解锁、剧情条目和 Boss 记录会被转换为新成长层的初始状态；局内临时强化仍不写入永久存档。
- 营地新增星火篝火、雪橇工坊、伙伴小屋、图书帐篷和云端瞭望台五处重建节点，每处三级，具备鱼干成本、前置建筑、故事说明和功能解锁。它们作为结构成长与既有六项数值强化并存。
- 真正活动的“营地/商店/战绩”三合一面板已显示重建区；购买后会刷新大厅剪影，五处建筑按等级逐步点亮且不拦截输入。
- 团团与 Dror 各有 1–5 级羁绊。路线完成、冒险选择、技巧分、每日挑战与主题遗物贡献经验；等级主要解锁对话和同行标签。团团的收藏帮助上限 8%，Dror 的条件援助上限 10%，均通过结算奖励权威消费，不新增碰撞或伤害通道。
- 路线事件现在携带本局真实完成的事件 ID 到结算。图鉴新增 12 条路线手记与 6 件主题遗物，按分组显示；未知条目提供具体线索，新发现保持标记直到玩家打开图鉴。
- `GrowthTransaction` 统一更新每日/成就目标、旧剧情图鉴、新主题发现和伙伴羁绊。旅程结果携带只读成长快照，`RunOutcomeRouter` 在记录写完后只保留一个显式 `PlayerSave.Flush`。
- 大厅只显示一个上下文建议，固定优先级为“可领取目标 → 新发现 → 可重建建筑 → 可购买强化 → 新旅程”；按钮使用枚举路由，不依赖文案字符串判断。
- 结算页同时保留路线、构筑、技巧与遗物高光，并补充本局最重要的永久成长变化。

永久数据包括存档版本、五处建筑等级、伙伴羁绊经验、发现/已读集合、任务进度与既有营地强化；会话数据包括本局路线选择、局内札记、技巧连段和 Boss 临时状态。标准写入顺序为“分数与资源结算 → 排行提交 → 统一成长事务 → 总局数/历史记录 → 单次存档刷新 → 结果展示”。

当前验证状态：`VerifyUiArtP0.ps1`、`VerifyStorybookUiPhase1.ps1`、`VerifyRunDepthPhase2.ps1`、`VerifyLongTermGrowthPhase3.ps1` 全部通过；包清单解析、运行时 `.meta` 配对、占位符扫描、`git diff --check` 和包含阶段二/三新增源文件的 Unity Roslyn `Assembly-CSharp` 编译通过。Unity 6.0.4 批处理日志 `_tmp/phase3-unity-batch.log` 仍报告无有效 Editor license，并以返回码 198 在导入/运行前退出；因此营地购买交互、羁绊实际结算、图鉴解锁动画、各分辨率布局和 Android 实机仍是运行时待验证项。

## 当前定位

`企鹅快跑` 是一款横屏 3D 单机跑酷游戏。玩家操控咕咕嘎嘎在极夜冰原与多个主题赛道中自动向前奔跑，通过跳跃、下滑、换道和道具时机躲避障碍，收集鱼干，并在长局中挑战主题 Boss。

旧横版动作冒险方向已经转为历史稿。它保留世界观、角色关系和道具命名参考，不再作为玩法结构依据。

## 设计目标

- 短局可玩：玩家进入游戏后几秒内能开始跑，几十秒内能得到明确结果。
- 读图清楚：三车道、障碍类型、Boss 预警和奖励路线必须在横屏 16:9 下优先可读。
- 操作收敛：核心输入只围绕跳跃、下滑、换道、开始/暂停和少量道具触发。
- 反馈及时：收集、完美闪避、受伤、Boss 破绽、主题切换和结算都要有清楚的视觉或音频反馈。
- 局外轻量成长：营地、商店、图鉴、任务和排行榜增强复玩，但不能盖过跑酷本身。

## 核心循环

1. 玩家在大厅查看今日建议、难度和资源状态。
2. 点击极夜快跑或今日挑战，并确认难度。
3. 角色自动奔跑，玩家通过手势或键盘处理障碍。
4. 跑道按主题片段池生成障碍、奖励、道具和敌人。
5. 到达条件后进入 Boss 前奏、Boss 战和奖励片段。
6. 失败或主动返回后进入结算，鱼干、分数、Boss 记录和任务进度写入存档。
7. 玩家回到大厅，进入营地升级、商店、图鉴、任务或排行榜。

## 首局体验

首局只应该教会玩家最小动作集：

- 点击开始。
- 左右滑动换道。
- 上滑跳跃。
- 下滑滑铲。
- 收集鱼干。
- 第一次道具只展示一种明确效果。

Boss、复杂道具组合、图鉴解释和营地深层升级应延后出现，避免首局信息过载。

## 局内跑酷

### 视角与构图

- 跑酷玩法以横屏 16:9 为基准。
- 竖屏不是支持目标，移动端通过 ProjectSettings 与运行时代码锁定横屏。
- 镜头、HUD、车道宽度、Boss 预警和装饰布景都必须保护跑道可读性。
- 三车道前方的可视走廊内不能出现大块非玩法装饰遮挡。

### 输入

- 移动端：上滑跳跃、下滑滑铲、左右滑换道、点击开始、双击冲刺、双指暂停。
- 编辑器/桌面：方向键或 WASD 换道/动作，空格跳跃，P 暂停。
- 输入阈值未来应按屏幕尺寸或 DPI 响应式调整，避免不同设备手感漂移。

### 障碍与片段

跑道通过 `RunnerSegmentCatalog` 定义主题片段池。片段负责描述一段时间内的障碍密度、奖励偏向、道具概率和主要模式。

现有片段模式包括：

- `Mixed`：普通混合段。
- `LaneWeave`：换道交织。
- `JumpArc`：跳跃弧线。
- `SlideTunnel`：低矮滑铲通道。
- `RewardRun`：奖励鱼干线。
- `SplitGate`：安全路线选择。
- `HazardRhythm`：高压危险节奏。
- `EnemyAmbush`：敌人伏击。
- `PowerUpTrial`：道具展示。

新增障碍必须配套 `ObstacleColliderSpec`，并明确推荐规避方式：跳跃、滑铲、换道或道具反制。

## Boss 设计

Boss 是长局高潮，不是随机大障碍。每个 Boss 需要同时具备：

- 主题归属。
- 清楚剪影和颜色识别。
- 招式池。
- 预警节奏。
- 推荐反制。
- 破绽窗口。
- 击败奖励或图鉴记录。

Boss 招式视觉节拍为：

1. `Windup`：前摇蓄力。
2. `Telegraph`：地面或跑道预警。
3. `Strike`：危险窗口，与命中判定对齐。
4. `Recovery`：收招和淡出。

Boss 活跃时，HUD 应优先显示生命、当前招式、破绽进度和必要提示；非关键统计应弱化，避免遮挡跑道。

## 大厅设计

大厅的职责是让玩家快速知道“现在该玩什么”。当前结构为：

- 顶栏：玩家昵称、等级、资源和动态 Ticker。
- 中央：吉祥物、主 CTA、今日挑战和难度芯片。
- 左侧：单张今日建议卡。
- 底栏：营地/任务/排行/图鉴/设置等系统入口。
- 面板：使用全屏覆盖或 Sheet，不在主屏堆叠多个重复入口卡。

大厅不做营销页，也不展开复杂说明。系统说明放入面板、图鉴或一次性提示。

## 局外系统

局外系统服务复玩动机：

- 营地：消耗鱼干强化局内收益或容错。
- 商店：兑换道具、装扮或补给。
- 任务：提供每日和长期目标。
- 排行榜：记录本地最佳、今日挑战和个人峰值。
- 图鉴：记录伙伴、Boss、剧情残页和击败成就。

这些系统不应强迫玩家在首局前理解。大厅只给出一个清楚的下一步建议。

## 视觉与音频方向

- 主色调是极夜冰原、极光、雪松、雾堤、海礁和天空主题的组合，不应长期只停留在单一蓝绿色。
- 每个主题需要有独立的障碍材质、道具偏向、Boss 和 BGM/ambience。
- 关键反馈优先使用短促、可识别的音效和粒子，避免持续遮挡画面。
- UI 可以有童话感和温暖感，但跑酷 HUD 要克制，中心和下半屏优先留给赛道。

## 当前文档分工

- `docs/CURRENT_DESIGN.md`：当前设计入口。
- `CONTEXT.md`：术语、领域规则和工程约束。
- `docs/STORY.md`：世界观、角色和剧情语气。
- `docs/PENGUIN_RUN_RESTRUCTURE.md`：从旧玩法转向跑酷的历史记录和已执行内容。
- `GAME_DESIGN.md`：旧横版动作冒险历史稿，仅保留世界观参考。
- `docs/adr/0001-lock-landscape-orientation.md`：横屏构图决策。
- `docs/adr/0002-cc0-rigged-boss-models.md`：Boss 视觉采用 CC0 骨骼模型 + Legacy Animation 程序化播放的决策。
- `docs/adr/0003-mecanim-for-protagonist-avatar.md`：主角 Mecanim / Boss Legacy 动画范式分治。
- `docs/adr/0004-competence-driven-onboarding.md`：能力驱动首局新手期（抑制噪音 + 片段轮转）。
- `docs/UI_ART_REVAMP_PLAN.md`：页面设计与美术效果优化方案（UI 统一、局内美术、功能结构重构的分阶段路线）。

## 后续优先级

1. 为六个主题补足更明显的音频差异（视觉差异已在 2026-07-20 批次落地）。
2. Android 实机（骁龙 7 系）60fps 性能实测与 Post Processing 分档校准（含主角 Avatar 材质批次）。
3. 主角视觉瑕疵（见 2026-07-25 节待办）：胸口游离菱形面片、侧视翅膀/护目镜几何、跑酷实距可读性。

已完成并移出列表：减轻首局信息量（能力驱动新手期，见 2026-07-25 节）、移动端输入阈值响应式（`RunnerGestureThresholds.ForViewport`）、跑酷 HUD 迁 uGUI 并与大厅同一套 `UiTheme` 令牌（IMGUI `RunnerHud` 已裁撤）、Boss 战 HUD 压缩（Boss 态只保留生命/招式/反制提示）。

## 2026-07-20 建模/特效/判定/页面全面优化

本批次四条线完成代码与资产落地：

- **判定手感**：Boss 护盾改消耗制并统一走 `RunnerHitResolver`（Boss 侧补齐冰镜/树甲/珊瑚防御层）；滑铲改蹲姿判定盒（`PlayerMotor.BodyHeight` + `PassUnderMaxHeight`）；空中下滑落地自动衔接滑铲；预输入跳跃补完美窗与音效；`WideWall`/`WideSlideOnly` 半宽收窄至 1.05/1.02；完美滑铲窗口对齐滑铲时长；`TimedGate`/`ShatterWall`/`MovingBarrier` 动态障碍接入生成链。
- **建模**：6 个 Boss 换用 Quaternius CC0 rigged 模型（Legacy Animation + `BossRigAnimator` 程序化调度，命中判定不读动画；缺模型时回退程序化剪影）；14 种缺失道具补齐双 LOD 预制体；六主题障碍差异化；企鹅双层网格清理并补死亡演出。
- **特效**：Boss FX 池粒子化（保持租用/归还接口与 48 上限）；Telegraph 专属进入特效与 6 Boss 差异化 Strike；道具拾取粒子、磁铁吸币飞线（纯视觉）、激活态角色特效与 HUD buff 条补全；月亮/极光 Billboard 化；`MaterialPropertyBlock` 合批；Post Processing（Bloom/Vignette，质量分档，Android 中档默认降级）。
- **页面**：`UiIcons` 图标注册表（Kenney CC0 + 自绘心形/时钟）替代 ♥★◷← Unicode 字符；面板层全面 TMP（`UiBuilder.CreateText` 即 TMP）；删除 `CampPanel`/`ShopPanel` 死代码；`MainMenuBootstrap` 内嵌 Tween 迁出（存活的 3 个移至 `LobbyAmbientTweens`，9 个死类删除，文件 1598 → 约 1200 行）。

修正记录（2026-07-21）：首版雕塑预制体的 LOD 阈值（0.60/0.22/空档 0.03）按"占屏大半"标定，跑酷视距下道具/障碍/机关模型全部落在空渲染档而隐形，画面只剩代码侧光球。已把两个 PrefabBuilder 改为 LOD0≥2.5% 屏高、0.6% 以下剔除并禁止空渲染档，批量重建 80 个预制体（62 障碍/道具/环境 + 18 机关）；`VerifyVisualJudgmentRevamp.ps1` 新增 LOD 阈值与空档断言防回归。

修正记录（2026-07-22）：障碍/环境仍按 2.5%/0.6% 剔除；**道具**因体量更小，远距 LOD1 仍会被裁掉而只剩 `Pickup Core` 光球与贴地环。道具改为 neverCull（LOD1 阈值 0.0001）、挂载缩放 1.85×、挂自定义模型时隐藏核心光球并缩小识别环；鱼干/气泡盾剪影加粗。

当前验证状态：九个静态门禁（含新增 `VerifyVisualJudgmentRevamp.ps1`）全部通过；全量 Roslyn `Assembly-CSharp` 编译通过（148 源文件）；Unity 6000.4.5f1 批处理 `ProjectUiLayoutRegressionTest` 通过；headed `ProjectPlayerJourneySmokeTest` 大厅→难度→跑酷→暂停→结算旅程通过并产出五张非空白截图（`Artifacts/PlayerExperienceAudit`，跑酷截图确认心形图标行、月亮辉光 Billboard、极光带与渐变天空正常渲染）；Android 测试包 `Builds/Android/PenguinRun-Latest-Test-v1.2.0-5.apk`（95.15 MB，APK Signature Scheme 验证通过，SHA-256 `1DF971A40FBC566DEA2E7A8C79FEA6FC4E2F20DA5C672DF6BD78B92F13D8FDE3`）。Android 实机帧率与手感验收仍为待办（本机无连接设备）。

## 2026-07-16 主题地图机关与精通玩法

阶段四已完成：六张地图现在拥有独立的可操作机关、标志性事件、Story/Endless/Daily 策略、主题精通、8 秒共鸣与自动旅途契约；同时新增 18 个细致双 LOD 机关预制体和 6 张主题预览。确定性、生命周期、取消清理、15 分钟耐久、旧资产、UI 布局及 Android v2 签名构建均已验证。完整规则、性能数据与 APK 信息见 [`THEME_MECHANICS_PHASE4_DELIVERY.md`](THEME_MECHANICS_PHASE4_DELIVERY.md)。

## 2026-07-25 主角 Avatar 改造着陆

本批次把程序化企鹅替换为数据驱动的 rigged Gugugaga 表现层，并收口动画未生效的回归空洞：

- **边界：** `IPlayerCharacterPresentation` + 只读 `RunnerAvatarState`；`PlayerAvatarPresenter` 为默认实现，失败时回退 `PenguinCharacter.CreateCute`。玩法判定（`PlayerMotor` / `CollisionSystem` / `RunnerHitResolver`）未改。
- **动画：** 主角走 Mecanim（`GugugagaBase.controller` + `AnimatorOverrideController`），Boss 仍走 Legacy（ADR-0003）。`RunnerAnimationDriver` 在换 controller 后 `Rebind()`，并兼容 `Armature|Clip` 形式的 override 键。
- **关键修复：** AnyState 过渡曾默认 `canTransitionToSelf=true`，每帧重进 Slide/Defeat/Jump 把 `normalizedTime` 钉在 ≈0.09，预览与局内都像绑定姿势。已在 builder 与现网 controller 设为 `false`。
- **验证：** `ProjectPenguinPreviewCapture` 对七个动作姿势做同机位 Idle 像素差断言（阈值 `MinPoseMeanAbsDiff=2.2`，实测最小 run≈4.34）；`VerifyProtagonistAvatarRevamp.ps1` 锁住该门禁与 `Rebind` 契约。Defeat 包围盒高度由 ~597 降到 ~438，滑铲/庆祝等姿势与 Idle 可区分。

验证命令：

```powershell
powershell -ExecutionPolicy Bypass -File tools/VerifyProtagonistAvatarRevamp.ps1
powershell -ExecutionPolicy Bypass -File tools/CompileFullRoslyn.ps1
# 有头批处理（不要加 -nographics，否则 Camera.Render 像素差恒为 0）
& 'C:\Program Files\Unity\Hub\Editor\6000.4.5f1\Editor\Unity.exe' -batchmode -quit -projectPath . -executeMethod PenguinRun.Editor.ProjectRunnerAvatarValidation.ValidateFromCommandLine -logFile _tmp/avatar-validation.log
& 'C:\Program Files\Unity\Hub\Editor\6000.4.5f1\Editor\Unity.exe' -batchmode -quit -projectPath . -executeMethod PenguinRun.Editor.ProjectPenguinPreviewCapture.CaptureAllFromCommandLine -logFile _tmp/avatar-preview.log
```

**明确待办（本批次不声称通过）：**

1. Android 实机 60fps 与主角材质批次数实测（本机无连接设备）。
2. 视觉瑕疵下一轮：所有视角胸口游离棕色菱形面片；`side.png` 翅膀呈惨白椭圆并与身体穿插、护目镜镜片拉扁；跑酷实距下红围巾与护目镜糊进深色身体，仅橙色脚掌清晰可读。

## 2026-07-25 首局新手期闸门

本批次落地能力驱动的首局保护（ADR-0004），对齐「首局体验」最小动作集：

- **概念：** `OnboardingState` 四个能力位（换道/跳/铲/收集）+ `TotalRuns >= 3` 保险丝；`RunnerSessionConfig.IsOnboarding` 开局快照，整局不变。
- **抑制：** 新手期内关闭路线分支、旅途札记升级、主题机关、世界事件、移动敌人；Boss 不抑制。
- **轮转：** `SegmentWaveDirector` 按 `LaneWeave → JumpArc → SlideTunnel → RewardRun` 确定性轮转，排除高难 pattern；冰湖/雾堤池补齐缺失的 `JumpArc`。
- **提示：** 废弃滑铲横梁旧 flag 的写入路径，四个能力位统一驱动首次提示；动作标签仅新手期显示。
- **迁移：** `SaveSchema` v4：老玩家 `TotalRuns >= 3` 直接毕业；`RunnerSlideGateTutorialDone` 迁入滑铲能力位。
- **验证：** `tools/VerifyFirstRunOnboarding.ps1` + `ProjectOnboardingValidation`（1200m 抑制、轮转覆盖、毕业/保险丝/迁移）。

验证命令：

```powershell
powershell -ExecutionPolicy Bypass -File tools/VerifyFirstRunOnboarding.ps1
powershell -ExecutionPolicy Bypass -File tools/CompileFullRoslyn.ps1
& 'C:\Program Files\Unity\Hub\Editor\6000.4.5f1\Editor\Unity.exe' -batchmode -quit -projectPath . -executeMethod PenguinRun.Editor.ProjectOnboardingValidation.ValidateFromCommandLine -logFile _tmp/onboarding-validation.log
```
