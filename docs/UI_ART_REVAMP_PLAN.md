# 企鹅快跑 · 页面设计与美术效果优化方案

> 状态：P0 与“童话营地”视觉基础已完成静态交付（2026-07-13）；玩法深度与长期成长阶段继续执行。本文是「页面美化 + 美术提升 + 功能结构完善」的总入口。
>
> **2026-07-26 补记：** 下方 A1 里「迁移 `UiBuilder` 现有 AccentCyan …」这一项直到 2026-07-26 才真正完成。当年的实施计划把它标为「保持不变，直到消费方拿到语义替代」，而那个后续批次没有发生，导致纸张改版后所有主/选中操作元素仍以霓虹青渲染在米黄纸面上。现已重映射为 `PineGreen` 并改名 `AccentActive`，由 `VerifyStorybookPalette.ps1` 锁住。详见 [CURRENT_DESIGN.md](./CURRENT_DESIGN.md) 的「2026-07-26 结算页与调色板收口」。

## 2026-07-13 阶段一交付记录

- 已完成：童话语义设计令牌、正式大厅全景、透明吉祥物、公共故事书组件。
- 已完成：大厅背景/故事主卡/伙伴角/底坞/面板路由拆分，入口类从 2416 行降至约 1524 行。
- 已完成：主要功能面板章节化；跑酷 HUD 迁移到 `RunnerHudCanvas`；旧 `RunnerHud.cs` 删除。
- 已完成：死亡与完成结算统一数据模型；Loading 从 IMGUI 迁移到保留式 uGUI。
- 验证：P0 与阶段一 PowerShell 静态门禁通过；Unity 批处理受本机许可证返回码 198 阻断。
- 后续：按独立计划继续路线事件、局内构筑、技巧评分、六主题 Boss 深化、营地重建、伙伴羁绊与图鉴发现。
>
> 执行时以本文、[CURRENT_DESIGN.md](./CURRENT_DESIGN.md) 与 `CONTEXT.md` 领域规则为准；任何与横屏构图基准（FramingReference）冲突的改动须先修订 ADR。

---

## 一、现状诊断摘要

### 1.1 UI 层（大厅 + 面板 + HUD）

| # | 问题 | 证据 | 影响 |
|---|------|------|------|
| 1 | **uGUI 与 IMGUI 双栈割裂**：大厅/面板用 uGUI 代码生成；跑酷 HUD、死亡屏、Loading 过渡用 IMGUI | `RunnerHud.cs`（IMGUI + 1px 纹理画矩形）、`PenguinRunnerGame.cs` 死亡屏 `GUI.Label` 裸排、`SceneTransitionManager.cs` | 局内外体验断层，组件与主题无法共享 |
| 2 | **无 TextMeshPro**：全项目使用内置 `LegacyRuntime.ttf` | `UiBuilder.CreateText`、`RunnerHud` 字体加载 | 中文排版粗糙、高分屏发虚、无字重层次 |
| 3 | **几乎没有 UI 美术资源**：图标全靠 Unicode 字符（✦ ☰ ★ ⚙ ♥），吉祥物是程序色块拼的企鹅，圆角是 32×32 白图 9-slice | `MainMenuBootstrap.DrawPenguinMascot`、底栏 Tab；`Resources` 下 UI 贴图仅 6 张图鉴 icon | 界面像「工程占位」，缺乏品牌识别度 |
| 4 | **设计 token 分叉**：`UiBuilder` 有颜色/间距体系，但 `RunnerHud`、`MissionPanel` 各自复制或偏离（如 Tab 激活色 0.75 vs 0.85）；字号无统一阶梯（18–110 随手写） | `UiBuilder.cs` 12–66 行 vs `MissionPanel.cs` 27–39 行 vs `RunnerHud.cs` 43–53 行 | 视觉不一致，改主题要改多处 |
| 5 | **God Class**：`MainMenuBootstrap` 2400+ 行；`CampShopPanel` 1687 行；`CampPanel`/`ShopPanel` 是未被引用的旧实现 | `MainMenuBootstrap.cs`、`CampShopPanel.cs` | 维护成本高，改 UI 容易牵连玩法逻辑 |
| 6 | **死亡屏与结算双重标准**：局内死亡是裸 IMGUI 文字，回大厅才有精美 `RunResultOverlay` | `PenguinRunnerGame.cs` 368–387 行 | 失败瞬间（情绪峰值）体验最差 |
| 7 | **动效体系混乱**：DOTween 只用于点击弹跳/面板弹入/相机震动；其余靠 9 个手写 MonoBehaviour Tween 与 Coroutine | `TweenExtensions.cs`、`MainMenuBootstrap` 内嵌 `PulseTween/FloatTween/...` | 缓动曲线不统一，性能与维护差 |

### 1.2 局内美术（场景 + 角色 + 特效）

| # | 问题 | 证据 | 影响 |
|---|------|------|------|
| 1 | **天空 = 纯色清屏**：无 Skybox，月亮是两个 Sphere，远山是旋转 Cube 拼的 | `PenguinRunnerGame.cs` 707–708 行 `CameraClearFlags.SolidColor`；`AddMountainGroup` | 画面最「空」的部分，第一眼廉价感的主要来源 |
| 2 | **材质大量纯色、贴图只铺跑道**：障碍/布景多为无贴图 Standard/Unlit；运行时 `renderer.material` 逐个实例化导致无法合批 | `RunnerVisuals.CreateMaterial`、`SegmentSpawner.TintModel` | 画面平、性能差 |
| 3 | **粒子 = 小 Cube**：`ParticleEffectPool` 用 256 个 Cube 手写运动；Boss FX 也是半透明几何体 | `ParticleEffectPool.cs` 41–47 行 | 拾取/着陆/Boss 打击缺乏爽感 |
| 4 | **无后处理**：无 Bloom/Color Grading/Vignette；边缘发光条、水晶、极光带没有辉光 | 全项目 0 个 `.shader`，无 PostProcessing 引用 | 六主题氛围差异只能靠雾色硬撑 |
| 5 | **六主题读感差异不足**：Cedar/Mist 共用 `stone_1`/`tree_simple`，IceLake/Aurora 高障共用 `rock_largeA`；credits 里规划的珊瑚/云/水晶/雾主题模型仍是 PLACEHOLDER | `SegmentSpawner` 1157–1187 行、`OPEN_SOURCE_VISUAL_ASSETS_CREDITS.txt` | 与 CURRENT_DESIGN「为六个主题补足更明显的视觉差异」目标不符 |
| 6 | **光照单薄**：唯一一盏方向光，Android 默认 Medium 档 shadowDistance=20（阴影基本不可见），路灯是假光 Sphere | `BuildScene` 718–721 行、`QualitySettings.asset` | 场景缺乏体积感 |
| 7 | **角色与 Boss 全程序化**：企鹅是程序 Mesh（无贴图无骨骼），6 个 Boss 是 Capsule/Cube 拼装 | `CutePenguinMesh.cs`、`BossEncounter.BuildSilhouette` | 剪影可用，但特写与出场演出撑不起高潮感 |

**总体判断**：玩法系统与主题框架已经比较完整，视觉停留在「程序化原型」阶段。优化应该走「保留程序化架构、系统性替换表现层」的路线，而不是推翻重写玩法代码。

---

## 二、设计目标与原则

1. **一眼有品牌感**：极夜冰原 + 童话企鹅的暖冷对比风格，30 秒内让玩家感觉「这是一款完成度高的游戏」。
2. **局内外风格统一**：大厅、面板、跑酷 HUD、死亡结算、Loading 使用同一套设计 token（颜色/字号/圆角/动效曲线）。
3. **跑道可读性不退让**：所有美术强化不得违反 `CONTEXT.md` 的 PlayableViewCorridor（可视走廊）与 FramingReference（横屏 16:9）规则；HUD 中心和下半屏留给赛道。
4. **移动端性能优先**：目标 Android 中端机 60fps；材质合批、粒子上限、Bloom 强度都以此为约束。
5. **素材合规**：新增第三方素材一律 CC0/CC-BY，先记 credits 再入库（领域规则）。
6. **渐进式落地**：每个阶段结束都是可发布状态，不搞长期分支。

---

## 三、方案总览

分四条工作线（A–D），按三个阶段（P0/P1/P2）交付：

```
A 设计系统统一     A1 token 集中 → A2 TMP + 字号阶梯 → A3 图标/图集
B 界面美化与重构   B1 HUD 迁 uGUI → B2 死亡结算统一 → B3 大厅升级 → B4 面板整合
C 局内美术提升     C1 天空远景 → C2 材质贴图化 → C3 粒子升级 → C4 主题差异化 → C5 光照后处理
D 功能结构完善     D1 MainMenuBootstrap 拆分 → D2 动效统一 → D3 冗余清理
```

---

## 四、工作线详细设计

### A. 设计系统统一（基础，先行）

#### A1 设计 token 集中（`UiTheme` 静态类）

新建 `Assets/Scripts/Runtime/Game/UI/UiTheme.cs`，把颜色、间距、圆角、字号、动效时长全部收口：

- **颜色**：迁移 `UiBuilder` 现有 AccentCyan / WarmGold / Success / Warning / Danger / Surface 系 / Text 系 / Border 系；补充每个地图主题的「主题强调色」6 组（供 HUD 段落 Banner、结算背景随主题染色）。
- **字号阶梯**（横屏 1920×1080 基准）：`Display 72 / H1 48 / H2 36 / Body 28 / Caption 22 / Micro 18`，禁止面板内再写裸数字。
- **间距**：沿用 8px grid（Spacing1–6），补 `RadiusS 8 / RadiusM 14 / RadiusL 22`。
- **动效常量**：`DurFast 0.15s / DurBase 0.25s / DurSlow 0.4s`，统一 Ease（OutQuad 进场、OutBack 弹入）。
- `MissionPanel`、`RunnerHud`（迁移后）的私有色常量全部删除，改引 `UiTheme`。

#### A2 引入 TextMeshPro + 中文字体

- 引入 TMP 包（Unity 内置 `com.unity.textmeshpro`）。
- 选一款可商用中文字体（候选：**思源黑体 SC**（SIL OFL）或 **阿里巴巴普惠体**），生成两套 SDF 字集：
  - `Font_UI_SDF`：常用汉字 3500 + ASCII + 游戏专有词（从所有 UI 文案扫描字符集）。
  - `Font_Display_SDF`：标题字重（Bold），用于大厅标题/结算大数字。
- `UiBuilder.CreateText` 切换为 `TextMeshProUGUI`；保留旧签名做适配层，各面板逐步替换。
- 数字滚动（分数/鱼干）统一用 TMP + tween 数字插值。

#### A3 UI 图标与图集

- 用 **Kenney Game Icons / UI Pack**（CC0，与现有 CodexIcons 同源）补齐一套图标：底栏 5 Tab（营地/任务/排行/图鉴/设置）、货币鱼干、生命、返回、暂停、音量、难度、Boss 骷髅等约 24 枚。
- 打成一张 UI SpriteAtlas；建立 `UiIcons` 注册表（图标名 → Sprite），替换所有 Unicode 字符图标。
- 圆角/发光/卡片底图改为 4 张精制 9-slice PNG（普通卡、高亮卡、按钮、Chip），替换程序生成的 32×32 白图。
- 按领域规则登记 credits。

**验收标准（A 线）**：全项目 grep 无 `LegacyRuntime.ttf`；面板代码无裸字号/裸颜色；底栏与 HUD 无 Unicode 图标。

---

### B. 界面美化与结构重构

#### B1 跑酷 HUD 迁移 uGUI（最大单项）

把 `RunnerHud`（IMGUI）重写为 uGUI Canvas（`RunnerHudCanvas`），保持现有信息架构与 `RunnerHudLayout` 的 Safe Area 逻辑：

- **布局不变、皮肤升级**：左上主卡片（分数/生命/三栏 Chip）、右上返回、顶部 Boss 面板、左下成就栏、全屏反馈胶囊。
- 生命值用心形图标 Sprite + 受击时 DOPunchScale；分数用 TMP 数字滚动；新纪录金色脉冲保留。
- **Boss 面板减负**（呼应 CURRENT_DESIGN 后续优先级 #2）：默认只显示「名称 + 分段 HP 条 + 危险条」，招式提示与破绽进度合并为一行 ticker；速杀奖励移到破绽期才浮现。
- 低血量红边脉冲、段落 Banner（危险/奖励）改为 uGUI 全屏 Image + 主题强调色。
- 更新维度：HUD 每帧只改数据驱动的 Text/Image，禁止每帧重建；沿用现有「仅在值变化时刷新」思路。
- 迁移完成后删除 IMGUI 版 `RunnerHud` 绘制代码，`OnGUI` 仅保留编辑器调试开关。

#### B2 死亡/结算体验统一

- 局内死亡屏废弃 IMGUI 裸文字，改为局内 uGUI 结算卡：定格 → 暗角渐入 → 结算卡弹入（得分/距离/鱼干/最佳对比/死因复盘 `GetDeathAnalysisHint()`）→「再来一局 / 回营地」双按钮。
- **短链路重开**：「再来一局」直接重置跑酷场景，不回大厅（呼应重构文档「失败后短链路重开」）。
- `SceneTransitionManager` Loading 改为 uGUI：品牌 logo + 企鹅奔跑小动画（Sprite 序列帧或程序 tween）+ 主题提示文案。

#### B3 大厅视觉升级

- **吉祥物**：程序色块企鹅升级为精绘立绘（2–3 张 PNG：常态/挥手/开跑预备），带呼吸浮动与点击互动反馈；或复用局内 3D 企鹅用 RenderTexture 展示（二选一，推荐立绘，成本低且可控）。
- **背景**：保留极光/星点/雪花程序化层，底部叠一张远景冰原剪影横图（视差 2 层），替代纯色渐变。
- **主 CTA**：加图标 + 渐变底 + 微呼吸动画，视觉权重明确高于难度芯片和今日挑战。
- 顶栏/底栏应用 A3 图标与新卡片底图；Tab 选中态加下滑指示条动画。

#### B4 面板整合与一致化

- 删除未被引用的 `CampPanel.cs`、`ShopPanel.cs`（确认 `MainMenuBootstrap` 仅打开 `CampShopPanel` 后移除）。
- `MissionPanel` 配色回归 `UiTheme`；任务卡增加类型图标与领奖粒子反馈。
- `SettingsPanel` Toggle knob 位置加 tween；分段选择条加滑动指示。
- 所有面板进出场统一走 `PlayPanelPopIn` / 新增 `PlayPanelFadeOut`。

**验收标准（B 线）**：局内死亡到重开 ≤ 2 次点击；HUD 与大厅同一套字体/颜色/圆角；16:9 下 Boss 战时中心与下半屏无 HUD 遮挡。

---

### C. 局内美术提升

#### C1 天空与远景（性价比最高，先做）

- **渐变 Skybox**：写一个 2-color（地平线色/天顶色）+ 星点/极光噪声的程序化 Skybox Shader（约 60 行），六主题各配一组渐变参数，接入 `ApplyMapThemeVisuals` 随主题切换插值过渡。
- 远山从旋转 Cube 换为 2–3 层低面山脉 Mesh（或 Billboard 长条剪影），颜色随雾色渐隐，保持在可视走廊外。
- 月亮/太阳改为带辉光贴图的 Quad Billboard；极光带保留脉动逻辑但换成柔边长条贴图。

#### C2 材质体系与合批

- `RunnerVisuals.CreateMaterial` 改为**共享材质缓存**：按（颜色量化 + 是否透明 + 贴图）键值缓存，运行时改色一律走 `MaterialPropertyBlock`，消除逐实例 `renderer.material`。
- 贴图从「只铺跑道」扩展到障碍与布景：现有 4 张表面贴图（冰/石/金属/布）+ 新增 4 张（雪地/木纹/珊瑚/云绒，CC0 无缝贴图），全部加 tiling。
- 跑道边缘发光条、水晶等发光体统一改用 Emission 材质（为 C5 Bloom 做准备）。

#### C3 粒子系统升级

- 保留 `ParticleEffectPool` 的池化接口，内部实现换成 **Unity ParticleSystem** 预制的 8 种效果：金币拾取（星尘）、着陆（雪雾）、滑铲（雪浪）、受击（碎冰）、完美闪避（光环）、Boss 打击（冲击波+碎片）、世界事件（暴雪/流星/极光雨）、结算庆祝（彩带）。
- Boss `BossPatternFxPool` 保留租用/归还规则（领域规则：池上限 48、`End()` 不 Destroy），内部 Cube 换为 ParticleSystem + 少量发光几何体组合。
- 企鹅奔跑加持续尾部雪尘 Trail（低开销 ParticleSystem，走廊内不遮挡）。

#### C4 六主题读感差异化

落地 credits 中规划的主题专属资源，消除跨主题模型复用：

| 主题 | 跑道/地面 | 专属障碍模型 | 天空渐变 | 强调色 |
|------|-----------|--------------|----------|--------|
| 冰湖回音谷 | 冰面贴图 + 高反光条 | 冰晶柱、冰拱 | 深蓝→青 | 冰青 |
| 雪松废墟 | 石板 + 积雪 | 断木桩、残垣石块 | 暮紫→暖橙 | 松绿 |
| 极光磁场 | 雪地 + 发光纹路 | 发光水晶簇、磁石 | 墨绿→紫红极光 | 极光紫 |
| 北境雾堤 | 湿石 + 雾贴地 | 雾柱、风灯石垛 | 灰蓝→灰白 | 雾灰蓝 |
| 海洋珊瑚礁 | 沙地 + 波光 | 珊瑚枝、贝壳、气泡柱 | 碧青→深海蓝 | 珊瑚橙 |
| 天空飞翔 | 云面 + 虹桥 | 云团、浮岛、风车 | 晴空→金橙 | 天蓝 |

- 模型优先从 Kenney / Quaternius（CC0）补齐珊瑚、云、水晶、雾柱四组；找不到的用 `ObstacleMeshBuilder` 扩展程序化形体（补齐 `BuildRoundedBox` 的真实倒角）。
- 每主题的雾色/环境光/天空渐变/强调色在 `UiTheme` 与 `ApplyMapThemeVisuals` 中同源配置，主题切换时 3 秒插值过渡（现有逻辑保留）。
- 保持 `ObstacleColliderSpec` 不变——**只换外观，不动碰撞**（领域规则）。

#### C5 光照、阴影与后处理

- 主方向光按主题配色温与角度；新增一盏低强度反向补光（冷色 Rim），提升角色与障碍立体感。
- Android 质量档：shadowDistance 20 → 45，开启 Soft Shadow（中端机实测帧率后定档）；桌面档不变。
- 引入 **Post Processing Stack v2**（Built-in 兼容，不迁 URP）：Bloom（阈值高、强度轻，只让 Emission 发光体辉光）+ 轻度 Color Grading（每主题一组 LUT 倾向）+ Vignette（跑酷时极轻，死亡瞬间加深）。
- URP 迁移**不在本方案范围**（改动面过大），作为未来独立 ADR 评估。

**验收标准（C 线）**：六主题开局截图并排肉眼可辨识主题；Android 中端机（骁龙 7 系）跑酷 60fps、SetPass Call 较现状下降；Boss 打击瞬间有冲击波粒子 + 辉光 + 震屏三重反馈。

---

### D. 功能结构完善

#### D1 `MainMenuBootstrap` 拆分

按职责拆为 5 个类（均在 `Game/UI/Lobby/` 下）：

- `LobbyBackground`：背景大气层（极光/星点/雪花/视差远景）。
- `LobbyTopBar`：紧凑玩家条 + 货币胶囊 + Ticker。
- `LobbyHero`：吉祥物、主 CTA、难度芯片、今日挑战。
- `LobbyDock`：底栏 Tab 与选中态。
- `LobbyPanelRouter`：面板打开/关闭/互斥与弹入动效（现 `OpenPanel/ClosePanel`）。

`MainMenuBootstrap` 保留为装配入口（目标 < 400 行）。拆分是纯移动代码，不改行为。

#### D2 动效层统一

- 删除 `MainMenuBootstrap` 内嵌的 9 个手写 Tween MonoBehaviour，统一迁到 `TweenExtensions`（DOTween 实现 + `#if !DOTWEEN` 回退保留）。
- `Snackbar` 的 Coroutine 动画改 DOTween 序列。
- 建立动效规范：进场 OutBack、退场 InQuad、强调 Punch、循环呼吸 Sine，时长引用 `UiTheme` 常量。

#### D3 冗余与死代码清理

- 删除 `CampPanel.cs` / `ShopPanel.cs`（确认无引用后）。
- `RunnerHud` IMGUI 实现、`PenguinRunnerGame` 死亡屏 IMGUI、`SceneTransitionManager` IMGUI 在对应 uGUI 版验收后删除。
- 清理后运行一次全量编译 + 试玩冒烟（大厅 → 开局 → 死亡 → 重开 → 回大厅 → 五个面板各开一次）。

---

## 五、分阶段执行路线

### P0 · 基础与止损（先做，约 1–2 周工作量）

1. A1 设计 token 集中（`UiTheme`）。
2. A2 TMP + 中文字体接入，`UiBuilder` 切 TMP。
3. C1 渐变 Skybox + 远景剪影（画面第一眼提升）。
4. C2 材质共享缓存 + MaterialPropertyBlock（性能止损）。
5. B2 局内死亡结算卡（情绪峰值体验修复）。

**P0 出口**：打开游戏第一眼（大厅字体/天空）与死亡瞬间明显变好；帧率不降反升。

### P1 · 核心体验统一（约 2–3 周）

1. A3 图标图集 + 卡片底图。
2. B1 跑酷 HUD 迁 uGUI + Boss 面板减负。
3. C3 粒子系统升级（8 种 ParticleSystem 效果）。
4. B3 大厅视觉升级（吉祥物立绘、CTA、底栏图标）。
5. D1 `MainMenuBootstrap` 拆分（与 B3 同批做，避免二次改动）。

**P1 出口**：局内外同一套设计语言；关键反馈（拾取/着陆/受击/Boss 打击）有粒子爽感。

### P2 · 主题深化与打磨（约 2–3 周）

1. C4 六主题差异化（模型落地 + 主题配置表）。
2. C5 光照 + Post Processing（Bloom / LUT / Vignette）。
3. B4 面板整合一致化 + D2 动效统一 + D3 冗余清理。
4. Loading 过渡动画（B2 尾项）。

**P2 出口**：六主题截图可直接当宣传图；代码层无 IMGUI 主路径、无双份面板实现。

---

## 六、风险与约束

| 风险 | 缓解 |
|------|------|
| HUD 迁 uGUI 引入每帧 GC / Canvas 重建卡顿 | 静态与动态元素分 Canvas；文本只在值变化时 set；上线前 Profiler 对比 IMGUI 版 |
| Bloom / Soft Shadow 拖慢中端机 | 全部效果做质量分档开关，Android Medium 档默认关 Bloom 半分辨率开 |
| 新贴图/模型破坏可视走廊 | 每个新布景过 `ApplyViewCorridorFade` 验收；SkyFlight 开局 10s 三车道可读性回归测试（沿用既有验收法） |
| TMP 中文字集缺字 | 构建脚本扫描全部 UI 文案生成字符表；运行时 fallback 字体兜底 |
| CC-BY 素材合规 | 沿用领域规则：先写 credits 再入库，CI/自查清单核对 |
| 拆分 `MainMenuBootstrap` 引入回归 | 纯移动不改逻辑；拆分前后手工冒烟清单（附录 D3）逐项过 |
| Boss FX 池规则被粒子替换破坏 | 保持 `BossPatternFxPool` 租用/归还接口与 48 上限不变，仅替换池内对象类型 |

---

## 七、涉及文件清单（预估）

**新增**

- `Assets/Scripts/Runtime/Game/UI/UiTheme.cs`（token）
- `Assets/Scripts/Runtime/Game/UI/UiIcons.cs`（图标注册表）
- `Assets/Scripts/Runtime/Game/UI/Lobby/`（5 个拆分类）
- `Assets/Scripts/Runtime/PenguinRun/Systems/RunnerHudCanvas.cs`（uGUI HUD）
- `Assets/Shaders/GradientSkybox.shader`
- `Assets/Resources/PenguinRun/UI/`（图标图集、卡片 9-slice、吉祥物立绘、远景剪影）
- `Assets/Resources/PenguinRun/Fonts/`（TMP SDF 字体资产）
- `Assets/Resources/PenguinRun/Particles/`（8 个 ParticleSystem prefab）
- 主题贴图/模型若干（记入 `OPEN_SOURCE_VISUAL_ASSETS_CREDITS.txt`）

**重点修改**

- `UiBuilder.cs`（TMP 化、底图 Sprite 化）
- `MainMenuBootstrap.cs`（拆分装配）
- `RunnerHud.cs` → 迁移后裁撤
- `PenguinRunnerGame.cs`（Skybox/灯光/死亡结算/后处理接入）
- `RunnerVisuals.cs`（材质缓存 + MPB）
- `ParticleEffectPool.cs`（内部实现替换）
- `SegmentSpawner.cs` / `ObstacleMeshBuilder.cs`（主题外观映射）
- `MissionPanel.cs` / `SettingsPanel.cs` / `CampShopPanel.cs`（token 回归 + 微交互）
- `SceneTransitionManager.cs`（uGUI Loading）
- `ProjectSettings/QualitySettings.asset`（Android 档位）

**删除（验收后）**

- `CampPanel.cs`、`ShopPanel.cs`
- `RunnerHud` IMGUI 绘制、死亡屏 IMGUI、Loading IMGUI
- `MainMenuBootstrap` 内嵌手写 Tween MonoBehaviour

---

## 八、文档同步

- 本文列入 `README.md` 设计文档目录与 `docs/CURRENT_DESIGN.md` 文档分工。
- P2 若确需评估 URP 迁移，另立 ADR（`docs/adr/0002-*.md`），不在本方案内实施。
- 执行过程中新增术语（如 UiTheme、RunnerHudCanvas）按惯例补入 `CONTEXT.md` 词汇表。
