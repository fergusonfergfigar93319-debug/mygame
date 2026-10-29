# 开发协作指南

## 适用对象

这份文档面向第一次接手本项目的开发者。

## 先看哪些文件

建议按这个顺序阅读：

1. [README.md](../README.md)
2. [docs/CURRENT_DESIGN.md](CURRENT_DESIGN.md)
3. [docs/GDD.md](GDD.md) — 设计总纲：各系统数值、已知缺口与路线图
4. [CONTEXT.md](../CONTEXT.md) — 术语、领域规则与工程约束，**动手前必读**
5. [docs/VERIFICATION.md](VERIFICATION.md) — 怎么验证你的改动
6. [docs/STORY.md](STORY.md)
7. [docs/UNITY_INTEGRATION.md](UNITY_INTEGRATION.md)
8. [docs/adr/](adr/) — 五份架构决策记录（横屏锁定、Boss 动画范式、主角 Mecanim、新手期、剧情后置）
9. [Assets/Scripts/Runtime/PenguinRun/PenguinRunnerGame.cs](../Assets/Scripts/Runtime/PenguinRun/PenguinRunnerGame.cs)
10. [Assets/Scripts/Runtime/PenguinRun/Systems/RunnerSegmentCatalog.cs](../Assets/Scripts/Runtime/PenguinRun/Systems/RunnerSegmentCatalog.cs)
11. [Assets/Scripts/Runtime/Game/UI/MainMenuBootstrap.cs](../Assets/Scripts/Runtime/Game/UI/MainMenuBootstrap.cs)

`GAME_DESIGN.md` 是旧横版动作冒险历史稿，只作为世界观和命名参考。`docs/STORY.md` 的章节结构按 ADR-0005 后置，不要拿它排本阶段玩法。

## 当前开发重点

### 已完成

- Unity 3D 横屏跑酷主流程
- 自动奔跑、跳跃、下滑、换道和移动端手势输入（阈值按视口响应式）
- 主题片段池、道具、Boss 前奏/战斗/奖励段
- 大厅、营地/商店、任务、图鉴、本地排行榜和结算闭环
- 六主题各自的光照档案与听感档案
- 能力驱动的首局新手期（ADR-0004）
- 跑酷 HUD 已全部迁到保留式 uGUI，与大厅共用 `UiTheme` 令牌
- 2026-09-21：换道跟手、镜头与构图对齐、新手期第一次道具、片段横幅

### 下一步建议

1. 继续打磨局内片段节奏、道具可读性与操作跟手（ADR-0005）
2. **Android 实机 60fps 实测**与画质分档阈值回校（分档阈值目前是先验值，未经真机数据验证）
3. UI 代码里仍有约 30 处裸 Unicode 字形，部分在实机是豆腐块（见 CONTEXT 的「UI 字形」）
4. `BossEncounter` / `CampShopPanel` / `PenguinRunnerGame` 的职责下沉（有行数棘轮兜底，不会继续恶化）
5. 剧情与章节结构后置，不在本阶段展开

## 提交代码建议

### 新增玩法

- 优先新增到 `Assets/Scripts/Runtime/PenguinRun/` 或 `Assets/Scripts/Runtime/Game/`
- 遵守 `CONTEXT.md` 的领域规则，例如新道具必须同步扩展枚举、拾取、视觉、音频和状态管理

### 新增 UI

- 大厅与局外系统使用 uGUI 和 `UiBuilder`；跑酷 HUD 是保留式 uGUI（`RunnerHudCanvas`），IMGUI 版本已裁撤
- 颜色一律走 `UiBuilder` 别名，**不要**直接引用 `UiTheme` 的旧霓虹令牌（见 CONTEXT 的「纸张 UI 层」）
- 装饰性符号走 `UiIcons` Sprite，不要写裸 Unicode——项目字体是动态图集，缺字会变豆腐块
- 每帧演化的纯装饰元素必须挂在独立的嵌套 Canvas 上（见 CONTEXT 的「装饰层 Canvas 隔离」）
- 新增信息前先判断是否会遮挡跑道；Boss 战期间只保留关键战斗信息

### 新增剧情与关卡

- 剧情语气参考 `docs/STORY.md`
- 关卡节奏优先通过 `RunnerSegmentCatalog` 和相关生成系统扩展
- 新主题必须具备片段池、障碍调色板、Boss 刷新条件、BGM 映射，以及 `ThemeAudioProfile` 听感档案（且不得与他主题重复）

### 新增/修改资产

- 主角网格改 `tools/blender/build_gugugaga_avatar.py` 后重建，**不要**直接编辑 FBX
- 音频、UI 图标、营地图集同样由生成器产出，改生成器而不是改产物
- 新增 PNG 后确认 `.meta` 含 `TextureImporter:` 块——只有 `fileFormatVersion` + `guid` 的残缺 meta 会让资源永远加载不到，而回退链是静默的
- 完整清单与命令见 [VERIFICATION.md](VERIFICATION.md)

## 未来推荐拆分点

- `BossEncounter`、`CampShopPanel`、`PenguinRunnerGame` 按职责下沉（`VerifyFileSizeRatchet.ps1` 保证它们不会继续膨胀）。
- 将大厅 UI 中重复的卡片、按钮和 Sheet 样式继续收敛到 `UiBuilder` 或主题常量。
- 如果片段池继续扩大，考虑把代码驱动的片段定义迁移到 ScriptableObject 或数据表。
- 如果 Boss 招式继续增多，保持命中规则、VFX 和提示文案分层，避免视觉效果改变判定。

## 设计一致性约定

- 新道具必须优先服务世界观，不要回退成常见平台游戏模板
- 新伙伴必须具备剧情价值和玩法价值
- 地图命名、敌人命名、道具命名保持企鹅世界观统一
- UI 文案尽量保持温暖、冒险、轻童话风格
- 任何相机、HUD、输入或布局改动都必须按横屏 16:9 基准验证

## 验证

**机器能验的先让机器验** —— 完整清单、命令与本机已知限制见 [VERIFICATION.md](VERIFICATION.md)。最低要求是全量编译 + 全部静态门禁通过。

每批改动落地时应**新增**锁住该批次契约的门禁；门禁只增不减。写门禁时注意两点：

- 阈值要经过校准，必须能真的挡住回归。写完后人为把缺陷改回去跑一遍，确认它会 FAIL。
- 宁可不要门禁，也不要留一个会误报的门禁——误报的门禁只会被关掉或被无视。

机器验不了、需要人工过一遍的：

1. Unity 场景能否进入 `MainMenu` 和 `PenguinRunner`
2. 横屏 16:9 下三车道与前方障碍是否清楚
3. 跳跃、下滑、换道、暂停和返回结算的手感
4. 道具、Boss 预警、破绽和奖励段是否能读懂
5. 结算、鱼干、任务、图鉴和排行榜是否更新
6. **Android 实机帧率与手感**（本机无设备，一律标记为待验证，不得声称通过）
