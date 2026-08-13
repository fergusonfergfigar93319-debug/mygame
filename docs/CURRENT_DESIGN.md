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

击发同步约定：命中判定、Strike 特效、骨骼攻击收势与齐射落地共用 `PatternStrikeImpactTime`（危险窗起点）；招式时钟在本帧视觉更新前推进，避免「先扣血后出特效」。

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

1. Android 实机（骁龙 7 系）60fps 性能实测与 Post Processing 分档校准（含主角 Avatar 材质批次）。

已完成并移出列表：六主题音频差异（见 2026-07-26 节）、主角视觉瑕疵与跑酷实距可读性（见 2026-07-26 节）、减轻首局信息量（能力驱动新手期，见 2026-07-25 节）、移动端输入阈值响应式（`RunnerGestureThresholds.ForViewport`）、跑酷 HUD 迁 uGUI 并与大厅同一套 `UiTheme` 令牌（IMGUI `RunnerHud` 已裁撤）、Boss 战 HUD 压缩（Boss 态只保留生命/招式/反制提示）。

## 2026-08-05 CC0 场景增量与 Cube Pet 主角

用户选型落地：场景侧采用 Kenney Nature Kit 增量 + Platformer Kit 雪地/道具；主角采用 Kenney Cube Pets 企鹅（C3）。

- 新开源 FBX 精选入库 `Assets/Resources/PenguinRun/Models/`，六主题路边 `AddOpenNatureProp` 映射更新（冰湖雪松/雪块，雪松木桥营火，极光秋叶，雾堤菌石，珊瑚高棕榈，天空旗与木箱）。
- 局内主角优先 `CubePetPenguinCharacter`（Resources `Characters/CubePetPenguin/animal-penguin`），无完整跑酷片库故用程序化 bob/squash；失败时回退 Gugugaga Mecanim，再回退程序化可爱网格。
- itch/Google Drive 上的 Quaternius Stylized Nature、Ultimate Nature 全量与 KayKit Forest：自动化 `/file` 拉取返回 `invalid key`，Drive 单文件也被限流；已加 `tools/FetchItchCc0Pack.ps1` 供浏览器手动领取。Kenney 第二批 25 个 FBX 已补入库并刷新主题映射。
- **KayKit Forest Nature Pack 1.0 FREE** 已手动入库：18 个精选 `vendor_*` FBX + `kaykit_forest_texture.png`；`VendorCc0PackImporter` 收紧为只处理 KayKit/Quaternius zip（跳过 Kenney），并改为白名单精选。
- **Quaternius Ultimate Nature**（`vendor_un_*`）与 **Stylized Nature MegaKit Standard**（`Models/vendor_sn/` + 贴图）已手动入库；主题模型选型下沉到 `NaturePropCatalog`（含包级缩放与 KayKit 混用）；`AddOpenNatureProp` 改为 `RegisterSceneryProp` 跟弯道贴地；`VendorModelImportRepair` 补全 Quaternius stub meta（`useFileScale`）。
- 大厅双 Coral CTA：底坞「出发」降为导航项，主页不再默认高亮；章节卡「翻开旅程」保留唯一主 CTA。
- 路边验收：`ProjectOpenNaturePreviewCapture`（RenderTexture）按六主题拍 `Artifacts/OpenNaturePreviews/`，断言道具像素覆盖与明度；batchmode 可用，补 journey smoke 截不到帧的缺口。
- 冰湖过白修正（2026-08-06）：靛紫天空 + 琥珀边光/描边、灰蓝雪面、路边道具暖橙/青蓝染色；Cube Pet 程序化跑跳滑铲（stride 颠簸/侧倾/落地挤压）并暖化近白材质。
- 局内货币金币（2026-08-06）：Kenney `coin_gold`（1.15×）优先，否则分层薄金币；弱自发光 + 绕 Y 旋转；拾取缩放 1.05（避免叠成光团）；开局三串 +「散鱼」可拾取。经济语义仍为「鱼干」。
- Boss / 重叠（2026-08-06）：各难度首 Boss 与间隔大幅延后（普通约 2200m / 2100m）；Boss 临近 120m 停刷障碍并清净空；金币生成避开障碍车道（旁道重试或跳过）。
- 操作手感（2026-08-06）：滑铲预输入 0.16s；落地滑铲优先于过期跳跃缓冲；换道更跟手（LaneLerp 29）；斜滑取主轴；键盘 Shift/F 冲刺 + 冷却预输入；Esc 暂停。
- Boss 判定公平（2026-08-06）：收窄车道命中盒、加长前摇/缩短危险窗；齐射最多两道且仅落地瞬间判伤，跳跃或冲刺可躲；各难度 min 预警上调（普通约 0.75s）。
- Boss 击发同步（2026-08-06）：判定 / Strike VFX / 骨骼收势 / 齐射落地锁定同一击发时刻；身体前冲用 `StrikeImpact01` 峰值；弹道由招式 elapsed 驱动。
- 静态门禁：`tools/VerifyCubePetAndNatureProps.ps1`。

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

## 2026-07-26 大厅：重复卡片与失踪的吉祥物

两条缺陷都不是"渲染没写好"，而是**静默降级**——没有异常、没有日志，画面上只是少了东西。

### 1. 左右两张卡内容一字不差

`MainMenuBootstrap.BuildLobbyViewModel` 里：

```csharp
chapterTitle = kind == StartJourney ? "雪松遗迹" : recommendation.Title;   // ← 左卡就是它
chapterBody  = kind == StartJourney ? "..."      : recommendation.Body;
```

`StartJourney` 只是五种推荐类型之一，**其余四种全部回落到左卡的文案**。截图抓到的是 `RebuildCamp` 状态，所以两张卡都是「让营火更亮一些 / 星火篝火已经备齐修复材料」。

改为：「下一章」由**主题精通进度**驱动——进度顺序里第一张还没打满三星的图，显示其名称与一句话简介。这才是「下一章」这个标签应有的内容。新增 `RunnerMapThemeInfo`（展示名 + 简介 + 进度顺序），`PenguinRunnerGame.GetThemeDisplayName` 的独立副本一并合并——那份副本已经漂移了（它写「雾堤」，其余地方写「北境雾堤」）。

### 2. 吉祥物是两个白色圆块

根因是**资源从未被导入**：`mascot_gugugaga.png.meta` 与 `lobby_panorama.png.meta` 只有 60 字节（`fileFormatVersion` + `guid`，**没有 `TextureImporter:` 块**）。`Resources.Load` 拿不到，`StorybookArtCatalog.Mascot` 一路回退到 `CreateFallbackMascot()` = `UiBuilder.CircleSprite`，一个纯白圆。

那张 1.3MB 的立绘（帽子、护目镜、红围巾、挎包）**一直躺在仓库里**，只是从来没被加载过。已补回导入块并保留 GUID。

**第二个白圆是伙伴。** `CampVisualAssetCatalog` 声明了 `companion_tuantuan` / `companion_dror` / `mascot_gugugaga_premium` / `camp_panorama_premium` 四条路径，但 `tools/generate_premium_home_assets.py` 只产出 8 个 `camp_*` 文件——当时这四个资源从未被生成过。伙伴的回退链曾经是 `?? Mascot`，所以修好 meta 之后会变成「两只一模一样的企鹅」。代码先改为缺美术时返回 null；2026-07-28 已正式补入团团（盾牌与青绿色斗篷）、Dror（极光磁针与地图）两张透明伙伴立绘，并复用已验收的正式营地全景与主角立绘补齐 premium 稳定路径。

**新增门禁** `ProjectLobbyArtValidation`：premium 全景、主角与两位伙伴现在都必须可加载，且任何角色都不得解析为占位圆或重复主角。`VerifyPremiumStorybookHome.ps1` 同时锁住 12 张大厅素材的完整 `TextureImporter`，并要求四张主视觉按 Sprite 导入。

原问题阶段的验证记录：15 个静态门禁、全量 Roslyn 编译与 4 个 Unity 校验器通过。2026-07-28 的素材补齐批次改由加强后的 `VerifyPremiumStorybookHome.ps1` 和 `ProjectLobbyArtValidation` 覆盖四张 premium 主视觉。

**已完成：** 伙伴立绘（团团 / Dror）、premium 全景图与 premium 主角路径均已落盘；大厅不再依赖隐藏伙伴或旧图回退来保持画面正确。

## 2026-07-26 结算页与调色板收口

从三张实机截图（`Artifacts/PlayerExperienceAudit`）反查，发现的都是既有机制发现不了的视觉缺陷。

- **根因：一次有意的推迟，后续从未落地。** 整套改版是靠 `UiBuilder` 别名层重映射完成的（`SurfaceBase`→PaperBase、`Surface1`→PaperWarm、`WarmGold`→CoralAction 都改了），**唯独 `AccentCyan` 保持不变**，仍指向旧霓虹色板。这不是疏忽——当年的计划书（`docs/superpowers/plans/2026-07-13-storybook-ui-foundation.md:178`）明确写了「Keep `AccentCyan` … unchanged **until their consumers receive semantic replacements**」，而那个后续批次没有发生。推迟本身合理，缺的是把它记成待办的机制；现在由 `VerifyStorybookPalette.ps1` 兜住。这一处遗漏就是营地商店、图鉴、排行榜、结算页、死亡页所有「选中」「主操作」元素在米黄纸面上渲染成霓虹青的共同来源。已重映射为 `PineGreen` 并改名 `AccentActive`——一个叫 Cyan 的绿色正是这类 bug 复发的温床。取 PineGreen 而非 CoralAction 是因为后者已是 `WarmGold`，同色会让 `isBest ? WarmGold : AccentActive` 这类分支渲染成同一个颜色。
- **主按钮语言统一**：`CreatePrimaryButton` 改为大厅底坞同款（CoralAction + 白字 + 暖色描边/投影），玩家在大厅学到的「主操作长这样」在结算页与死亡页同样成立。`CreateSecondaryButton` 从深海军蓝实底改为雾蓝（墨色字 6.97:1）。不用全透明「幽灵」底：`AddOutline` 走 uGUI `Shadow`，会把描边 alpha 乘以源顶点 alpha，底色全透明时描边一起消失。
- **占位图标补齐**：结算页三个统计格用的是裸 Unicode（U+279E / U+2740 / U+231B）与横幅前缀 U+2728，都不在项目 TMP 字体图集里，实机是豆腐块加一个星号。新增 `tools/generate_ui_icons.py` 生成风格一致的旗帜（距离）与鱼干图标，配合已有的 `Timer`。
- **奖励横幅曾被按钮盖住**：56px 里有 50px 压在 CTA 之下——双倍鱼干券、Boss 速杀这些奖励**被告知在玩家看不见的地方**。且横幅把最多 8 段文案塞进一条 56px 里，「跑得越好、奖励越多，反而越读不清」。现重排为 统计 −237..−115 / 横幅 −329..−253 / 按钮 −433..−345，横幅限 4 段 + 省略号 + TMP 自动缩放兜底，文字改墨色（原浅金压浅黄仅约 1.2:1，现 10.2:1）。

**新增门禁**：
- `ProjectResultScreenCapture`（Unity）：用**满配奖励**的样本局构建结算页并断言下半区区块**两两不相交**，同时产出截图证据。用矩形相交而非像素比对——像素阈值会随配色字体漂移，「两个可见区块不许叠在一起」才是稳定契约。这个缺陷只在玩家真拿到奖励时出现，旅程 smoke 的样本局没有奖励，加上它在本机截不到帧，所以此前无从发现。
- `VerifyStorybookPalette.ps1`（静态）：纸张 UI 层禁止引用旧霓虹色板令牌。作用域限于 `Runtime/Game/UI`——局内 HUD（`RunnerHudCanvas`）画在深色 3D 场景上，继续用旧令牌是正当的。实测能挡回归。

验证：15 个静态门禁全过；全量 Roslyn 编译通过（173 源文件）；4 个 Unity 校验器通过；截图证据在 `Artifacts/ResultScreens`。

**明确待办：**

1. **大厅两张卡内容完全重复**：已在 `MainMenuBootstrap.BuildLobbyViewModel` 按主题区分手记/下一章文案（旧审计截图过时）。
2. **大厅吉祥物渲染成两个白色圆块**：已修（meta + premium 立绘路径）。
3. **UI 代码里还有约 30 处裸 Unicode 字形**（`CampShopPanel` / `CodexPanel` / `LeaderboardPanel` 等）。已知 ★ ✓ 渲染正常、➞ ❀ ⌛ ✨ 是豆腐块，逐个靠肉眼排查不可行。曾尝试写字形覆盖门禁但**已删除**：项目用动态字体图集，`TMP_FontAsset.HasCharacter` 查的是「当前图集有没有」而非「源字体有没有」，会把正常显示的 ★ 误报为缺失；且需要先剥离注释（`—`、`""`、`─` 大量出现在注释里）。要做对需要改查源字体文件覆盖。
4. 大厅两个橙色主 CTA：已处理——底坞「出发」降为导航项，主页不再默认高亮；章节「翻开旅程」保留唯一 Coral 主 CTA。

## 2026-07-26 代码健康度：止血而非大修

`GAME_OPTIMIZATION_PLAN` 里记的风险是「改 Boss、机关、道具容易互相影响」，根源是它们都落在同几个超大文件里。安全拆分需要运行时验证，而本环境暂时给不了（无 Android 设备；`ProjectPlayerJourneySmokeTest` 在 batchmode 下截不到帧）。因此本批次只做**可验证**的那一半。

- **一次纯搬运的拆分**：`SegmentSpawner.cs` 2645 → 2343 行，障碍外观构建（程序化 Mesh、开源模型挂载、调色板染色、配饰）移入 `SegmentSpawner.ObstacleVisuals.cs`。用 `partial` 而非抽独立类：成员、可见性、调用点全部不变，因此**不可能**改变行为。已用「拆分前后成员声明集合完全相同（133 = 133）」证明是纯搬运。这条缝选得不随意——它正是项目既有的权威边界（碰撞规格由 `ObstacleColliderSpec` 决定，与网格无关），拆分让这条边界在文件树上可见。
- **新增门禁** `VerifyFileSizeRatchet.ps1`：为六个超大文件记录行数上限（约当前值 +2.5%）。文件可以自由缩小，**不能**继续膨胀。余量足够修 bug，不足以塞进一个新功能；拆分落地时应同步下调上限，上调上限则是一个需要走评审的显式决定。

**明确未做（并说明原因）：** `BossEncounter`（1849 行）、`CampShopPanel`（1771 行）与 `PenguinRunnerGame`（2341 行）的职责下沉未做。这三处的拆分会改变调用关系与生命周期，仅靠编译通过不足以保证正确；应放到能跑实机或能跑通旅程 smoke 的批次里做。棘轮门禁保证在那之前它们不会继续变差。

## 2026-07-26 画质分档校准

处理待办 #2 中不依赖实机的那一半：分档机制本身。

- **根因：分档是死代码。** `RunnerPostFx` 读 `QualitySettings.GetQualityLevel()` 决定是否关 Bloom，但**全项目没有任何代码按设备能力设置过这个等级**——读到的永远是构建默认值，骁龙 4 系与 8 系拿到完全相同的画质。整段还被包在 `#if UNITY_ANDROID && !UNITY_EDITOR` 里，编辑器内既跑不到也测不了，所以「分档没生效」这件事没有任何机制能发现。
- **补上决策层** `RunnerQualityTier.cs`：按内存 / 核心数 / 显存三项分 Low / Medium / High，任意一项落在低端区间即降档（跑酷是持续 60fps 负载，偶发掉帧比整体画质低更伤手感）。`RunnerDeviceTier.Apply()` 把预算真正写进 QualitySettings。
- **档位表**（Bloom / 阴影 / 粒子 Raycast / LOD Bias / 骨骼权重 / 贴图 mip）：
  - Low：关 Bloom、关实时阴影、粒子 16、LOD 0.7、2 骨骼、贴图半分辨率
  - Medium：关 Bloom、硬阴影 32m、粒子 48、LOD 1.0、2 骨骼
  - High：Bloom fastMode、软阴影 45m、粒子 64、LOD 1.2、4 骨骼
- **可测性**：决策不再被 `#if` 编译掉，编辑器与真机走同一条路径。`ApplyMobileQualityBudget` 原本对非移动端直接 return，现在桌面固定落在 High，使编辑器截图门禁与高端机发布版一致。
- **原先分散的移动端预算**（`particleRaycastBudget = 64` 等硬编码值）收进档位表；`VerifyUiArtP0` 中对应断言已指向新位置，契约不变。

**新增门禁**：
- `ProjectQualityTierValidation`（Unity 批处理）：三档必须互不相同、按档**单调**（高档不得在任一杠杆上比低档更省），且逐档 `Apply()` 后回读 QualitySettings 确认预算真的写进去了。校验前后对 QualitySettings 做快照还原，不弄脏 `ProjectSettings`。
- `VerifyQualityTierCalibration.ps1`（静态）：档位表完整、决策来自 `SystemInfo`、运行时确实调用 `Apply()`；并**禁止**重新出现 `QualitySettings.GetQualityLevel()` 与 `#if UNITY_ANDROID` 这两个让旧分档失效的写法。实测能挡回归。

验证：13 个静态门禁全过；全量 Roslyn 编译通过（171 源文件）；`ProjectQualityTierValidation` 通过。

验证命令：

```powershell
powershell -ExecutionPolicy Bypass -File tools/VerifyQualityTierCalibration.ps1
& 'C:\Program Files\Unity\Hub\Editor\6000.4.5f1\Editor\Unity.exe' -batchmode -quit -projectPath . -executeMethod PenguinRun.Editor.ProjectQualityTierValidation.ValidateFromCommandLine -logFile _tmp/quality-tier.log
```

**明确待办（本批次不声称通过）：**

1. **实机 60fps 实测仍未做**（本机无连接 Android 设备）。分档阈值（3GB/4 核/1GB 显存与 6GB/8 核/2GB）是按机型分布定的**先验值**，必须用真机帧率数据回校。
2. 分辨率缩放未纳入档位表：它会与 `FramingReference`（`ApplyRunnerCameraViewport169` 读 `Screen.width/height`）纠缠，在无实机验证前不引入。
3. 主角材质批次数（Draw Call）实测未做。

## 2026-07-26 六主题音频差异化

此前六张主题图共用一条 ambience 与同一套撞击/拾取音，音频上的「主题差异」实际只有 BGM 一层——BGM 一停或重复，六张图听起来完全一样。

- **新增 `ThemeAudioProfile`**（与 `ThemePresentationProfile` 对称的纯表现层）：每主题定义环境音床（clip / 音调 / 音量缩放）、落地与受击共用的地面音色、金币调性、Boss 击发音调缩放。
- **自有合成音频** `tools/generate_theme_audio.py`：用噪声与振荡器合成 6 条 8 秒可循环环境音床 + 6 条地面撞击音，无第三方授权（沿用既有 "Generated Project SFX" 管线）。每主题定种子，重建字节一致。尾部交叉淡回头部保证无缝循环。
- **客观可分辨**：谱心 449Hz（雾堤，闷）/ 879（珊瑚）/ 1275（雪松）/ 2543（冰湖）/ 3189（天空）/ 4945（极光，嘶），相邻主题至少差 1.3 倍。
- **切换时机**：`ApplyMapThemeVisuals` 同处切视觉与听感，主题切换后不会出现画面已换、声音还停在上一张图。
- **叠加而非替换**：地面音色叠在既有 `land`/`slide`/`ice_crack` 之上，音效风格包（`alt_*`）与每招 Boss 音调差异都不受影响。冲刺提速叠在主题基准音调上，不再把音调拉回全局 1.0（否则雾堤的低沉床会在每次冲刺后被抹平）。
- **玩家设置**：新增「随图」选项并设为默认。老存档只有在玩家**显式**改过环境音时才写过该键，因此显式选择仍被保留，无需 SaveSchema 迁移。
- **移动端导入参数**：环境音床 `CompressedInMemory` + Vorbis q0.4（8 秒循环，内存与包体优先）；地面短音 `DecompressOnLoad` + PCM（<350ms，解码延迟比 13KB 更值得避免）。

**新增门禁** `VerifyThemeAudioDifferentiation.ps1`：六个主题必须各有完整档案、引用的 clip 必须存在于磁盘，且**不得有两个主题共用同一条床或同一地面音色**。已实测该门禁能挡住回归（人为把雪松的床改成冰湖的床即 FAIL）。

验证：12 个静态门禁全过；全量 Roslyn 编译通过（169 源文件）；Unity 重导入无错误，12 个 `.meta` 正常生成。

验证命令：

```powershell
python tools/generate_theme_audio.py
powershell -ExecutionPolicy Bypass -File tools/VerifyThemeAudioDifferentiation.ps1
```

**明确待办：** 实机听感（尤其手机外放下六张床的可分辨度）与音量平衡尚未在 Android 设备上验收。

## 2026-08-13 场景美术与交互体验提升

在同日修复批次之上的表现层提升，不改任何判定：

**地图场景（六主题辨识度）**

- **天象按主题布置（新文件 `ThemeSkyDressing`）：** 此前六图共用一轮月亮 + 五条极光带——天空白昼挂月亮、海洋礁湖飘极光。现在极光图独占满编极光带（冰湖留两条弱余晖），雪松是低位琥珀暮月，雾堤只剩被雾漫开的冷光晕，海洋是高位小冷月，天空换成日盘 + 暖光晕。天空盒极光带颜色随 `SkyAuroraColor` 走，非极光主题为黑（无带）。
- **雾与天际拉开三张夜图：** 冰湖雾偏靛紫且最远（52–172）、极光偏青绿且更短（42–148）、海洋深青更饱和（32–126）且地平压深；此前冰湖与极光雾距完全相同、50m 外不可分。
- **寒带套件不再全图共用：** 冰晶簇只属于冰湖/极光/雾堤，雪堆只属于冰湖/雪松/雾堤；远山剪影配色随主题传入（雪松暮紫 / 海洋礁蓝 / 天空云白），构建拆到新文件 `ThemeRidgeScenery`。
- **走廊合规与泄漏修复：** 旗帜从 ±5.65 移到护坡外 ±8.6；海草/贝壳/气泡从三车道上移到 |x|≥8.6（原先海草长在跑道上会被误读成障碍）；冰裂改为路面瓦片子物体（随路滚动、随瓦片销毁——此前不注册、切主题跨图残留且不随路动）；IceArch 地标从"一条 2.8m 冰裂"改为真正的双柱冰拱。
- **逐主题后处理色调：** `ThemePresentationProfile` 新增 `PostTemperature/PostSaturation`，`RunnerPostFx.ApplyThemeGrading` 在主题切换时覆盖（雪松 +16 暖、极光 -10 冷 +12 饱和、雾堤 -10 去饱和等）。
- **地形渐入提前：** 起伏 55–520m → 45–360m、弯道 140–860m → 110–560m（相机 yaw/bank 同步），海洋的"糙"与天空的"高"第一分钟内可读。

**页面交互（DOTween 已启用，本批全面接线）**

- 按钮按压反馈：全局改为 PointerDown 压缩 0.96 / PointerUp 回弹（`PressScaleFeedback`），替代 onClick 后置弹跳；补接底栏 Tab、营地热点、难度行、局内三选一卡。
- 面板过渡：`PlayPanelPopIn` 升级为缩放 + 淡入（OutBack）；新增 `PlayPanelPopOut` 退场（淡出+收缩，首帧封锁输入），`LobbyPanelRouter` 关闭不再瞬切；暂停面板弹出。
- 结算节拍：结算页与死亡卡分数 0→总分滚动（`PlayCountUp`，整数变化帧才写 text）；三张统计卡错峰入场；死亡卡补进场。
- HUD 离散跳动：鱼干增加、连击推进、生命增减（对应心形图标）、提示条换文案时各 punch 一次；分数每帧变化故不跳动。
- 列表入场：`CreateScrollListRow` 前 10 行按兄弟序号错峰淡入（只动 scale/alpha，不与 LayoutGroup 抢位置）。
- 加载页：提示文案每 2.2s 轮换（进跑酷=玩法贴士 / 回营地=氛围文案）；进度条显示值平滑推进。

**验证：** `CompileFullRoslyn`（185 源文件）与全部静态门禁 + `VerifyFileSizeRatchet` 通过（远山与天象拆出后 `PenguinRunnerGame.cs` 余量回到 11 行）。**明确待办：** 本机 Unity license 失效（licensing 初始化失败即中止），六主题实距截图、`ProjectAvatarGameplayViewCapture` 亮度断言与 Android 实机验收未能运行，属运行时待验证。

## 2026-08-13 判定公平性与页面回归修复

三路审查（核心判定 / 未提交批次 / UI 层）后的集中修复批次，全部为已确认缺陷，不引入新玩法：

- **Boss 公平性：** DiveHigh 命中改读开战锁定的 `DiveLaneX`（根节点 x 向中线回插导致命中带与预警反向）；俯冲期间 Boss 本体保持在锁定车道上方。齐射改为落地时刻 ±0.14s 冲击窗判伤（原 `IsSalvoImpactActive` 为恒真式）；齐射全局击发不再被 Strike 相位预占 `strikeFired` 短路。SweepLow/QuakePulse 躲避与障碍共用蹲姿盒 `IsLowPosture`（含起身宽容窗）。
- **防御契约：** 障碍路径护盾碎裂补上 `invulnAfterShieldBreak` 无敌（与 Boss 路径同条款，`ShieldGraceSeconds` 随之生效）；珊瑚回弹不再附赠护盾。
- **Boss 前奏：** `NextBossDefinition` 懒加载抽取并缓存、`SpawnBoss` 消费同一定义——第一场 Boss 也有前奏，且预告与出场必然一致；`Reset` 清空缓存。
- **操作手感：** 跳跃预输入落地帧先兑现再扣时（不再吞掉缓冲末帧）；空中急坠不再重复发 `PlayerSlid`（滑铲音只响一次）。
- **生成/道具：** Boss 清场先回收叠障金币再毁障碍（原顺序使重叠检测恒 false、金币残留净空带）；雾灯补上「雾距外推提前照亮障碍」的核心玩法（+45m/+70m 平滑插值）；`BossPatternFxPool` 改按 primitive 类型分桶，Release 路径彻底不 Destroy。
- **页面（纸张回归）：** 修复米黄底白字标题、排行榜旧深蓝底、领取/购买/签到 CTA 霓虹青、Tab 选中态误用 Coral、豆腐块 Unicode（➞/⌛ 等）；Snackbar/Settings 深色容器改纸面。
- **页面（性能）：** 结算庆祝星/金色光晕移入独立嵌套 Canvas，卡上脉冲限时收敛；营地篝火光晕/炊烟/角色呼吸各挂嵌套 Canvas（角色层带 GraphicRaycaster 保持可点击）；HUD 主题机关文本、共鸣条、Boss 速杀条、反馈文本、商店/任务倒计时全部按值变更门控。
- **小项：** CubePet 局末归位姿态基线（重开不误触落地挤压）；运行时金币实例剥除 FBX Collider；破绽反击文案补键盘 Shift/F。

## 2026-07-26 主角剪影与实距可读性

收口 2026-07-25 节遗留的三条视觉瑕疵，并找出「实距糊成一团」的真正根因。

- **根因（着色器，不是配色）：** `GugugagaStylized.shader` 的主光是世界空间常量 `(0.35, 0.8, 0.45)`，+Z 指向赛道深处。跑酷时玩家只看得到角色背面（法线约 -Z），点积恒为负，朝向镜头的每个像素都钉在 `0.38` 的 wrap 下限——围巾、护目镜、鳍肢被压成同一团暗色，只剩橙色脚掌可读；大厅吉祥物正面朝 -Z，是同一问题的镜像。**没有任何单一世界方向能同时照亮这两面**，故改为视角空间固定主光（`mul(dir, (float3x3)UNITY_MATRIX_V)`），镜头在哪一侧就照哪一侧。实测角色像素 `meanLuma` 53.8 → 76.0。
- **面片穿插：** 新增 `conform_inside()`，把腹部/面罩按微放大的宿主体做布尔求交，patch 轮廓不再越过身体轮廓（侧视那块「游离白椭圆」消失）。三块正面 patch 用 clip margin 分层（胸兜 1.006 / 腹部 1.012 / 面罩 1.006）决定叠压顺序，而不是各自往前顶。
- **护目镜：** 原本整组停在 y=-0.455 的竖直平面上，比该高度的额头前方约 0.12m，侧视整体浮空且镜片退化成线。改为绕枢轴整体后仰 50° 贴合颅顶斜面，镜片 y 半径 0.012 → 0.022 成穹面；头带半径 0.335 大于该高度颅骨截面（0.31/0.29），整圈脱离头部成一块平板，改为按颅骨截面推导的等高椭圆并共用头部 -3° 倾角。
- **翅膀：** 原为一个胖椭球 + 三个挂在肋侧 x=±0.39 的「羽毛」椭球（在身体轮廓外但不连着翅膀，读作贴在躯干的血块）。新增 `lofted_limb()` 单网格放样，六段截面连续收窄且 y 比 x 收得更快（扁鳍），无内部接缝。
- **其他：** 胸口 45° 立方体「游离菱形」移除，改为围巾褶上的居中铜扣（贴在真实存在的曲面上）；围巾尾原本伸到 y=0.88（躯干背面之外 0.53m）读作硬板，改为主要向下垂坠；脸颊/眉毛原本停在旧的平面面罩位置，面罩贴合颅骨后会浮空 0.09m，已按颅骨表面重新落座。
- **配色：** 顶点色即游戏内基色（着色器 `input.color * _Tint`）。Body/BodyAccent 拉开明度台阶使鳍肢在剪影中分离，Bronze/Lens/Scarf 提亮。
- **面数不升反降：** LOD0 36608 → 29616、LOD1 19160 → 15712、LOD2 7328 → 6792（布尔裁掉了 patch 的隐藏背面，放样比堆叠椭球便宜）。

**新增门禁** `ProjectAvatarGameplayViewCapture`：按 `RunnerCameraTuning` 真实机位（behind 10.5 / height 6.5 / FOV 72）拍三车道**背面**，同机位拍带/不带角色两张求差得到角色掩码，断言 `meanLuma ≥ 65` 与 `detailStdDev ≥ 18`。阈值经过校准——旧着色器的 53.8 会被卡住，即门禁真的能挡回归。近距离的 `ProjectPenguinPreviewCapture` 看不出此问题（4m / 34° 镜头 vs 玩家实际的 10.5m / 72°）。

验证：11 个静态门禁全过；全量 Roslyn 编译通过（168 源文件）；`ProjectRunnerAvatarValidation`（LOD 预算 + 材质≤3）通过；`ProjectPenguinPreviewCapture` 通过且七个姿势像素差全面上升（run 4.34→5.76、slide 11.22→14.90）；`ProjectAvatarGameplayViewCapture` 通过。

验证命令：

```powershell
& "$env:LOCALAPPDATA\Programs\Blender Foundation\blender-4.5.10-windows-x64\blender.exe" --background --factory-startup --python tools/blender/build_gugugaga_avatar.py
powershell -ExecutionPolicy Bypass -File tools/VerifyProtagonistAvatarRevamp.ps1
powershell -ExecutionPolicy Bypass -File tools/CompileFullRoslyn.ps1
& 'C:\Program Files\Unity\Hub\Editor\6000.4.5f1\Editor\Unity.exe' -batchmode -quit -projectPath . -executeMethod PenguinRun.Editor.RunnerAvatarAssetBuilder.BuildFromCommandLine -logFile _tmp/avatar-rebuild.log
& 'C:\Program Files\Unity\Hub\Editor\6000.4.5f1\Editor\Unity.exe' -batchmode -quit -projectPath . -executeMethod PenguinRun.Editor.ProjectAvatarGameplayViewCapture.CaptureFromCommandLine -logFile _tmp/avatar-gameplay-view.log
& 'C:\Program Files\Unity\Hub\Editor\6000.4.5f1\Editor\Unity.exe' -batchmode -quit -projectPath . -executeMethod PenguinRun.Editor.ProjectPenguinPreviewCapture.CaptureAllFromCommandLine -logFile _tmp/avatar-preview.log
```

**明确待办（本批次不声称通过）：**

1. Android 实机 60fps 与主角材质批次数实测（本机无连接设备）。
2. 腹部 patch 下缘布尔接缝处有一处轻微凹口，大厅近景可见、玩法视距不可见。
3. `ProjectPlayerJourneySmokeTest` 在本机 `-batchmode` 下 `ScreenCapture.CaptureScreenshot` 产不出截图（Stage timeout: Capture），本批次未能刷新 `Artifacts/PlayerExperienceAudit` 五张旅程证据；该目录内容已从 git 还原为上一批次版本。属环境/工具问题，与本次改动无关。

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
