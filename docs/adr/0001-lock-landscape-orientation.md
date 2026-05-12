# ADR 0001：移动端锁定横屏

**日期：** 2026-05-12  
**状态：** 已采纳

---

## 背景

企鹅快跑（PenguinRun）的相机、HUD 与输入系统自开发起均按横屏（16:9 附近宽高比）调优：

- 相机：垂直 FOV 76°，三车道在横屏下水平 FOV ≈ 108°，跑道宽阔，前方可读距离足够。
- 竖屏（9:18，aspect ≈ 0.44）下同一 76° 垂直 FOV 对应水平 FOV 仅 ≈ 38°，三车道挤入极窄视口，天空占比过大，企鹅与障碍比例失调，与 PC 编辑器展示效果差异极大。
- HUD（`RunnerHud.cs`）的面板宽度、Boss 血条、反馈卡片等均以横屏宽度为基准排布，竖屏下会出现无法预测的布局拥挤。
- 真机测试（红魔 9 Pro）证实了竖屏体验严重劣化，以 PC 编辑器（横屏 1920×1080）展示效果为对齐基准。

## 决定

**移动端（Android / iOS）只支持横屏（LandscapeLeft / LandscapeRight），完全禁用竖屏（Portrait / PortraitUpsideDown）。**

允许左/右两种横屏方向之间的系统自动旋转，以照顾左/右手握持习惯，构图基准不变。

## 备选方案

**方案 B：按 `Camera.aspect` 动态调整相机 FOV 与 HUD 布局**  
工作量显著，需对 `RunnerCameraTuning`、`RunnerHud`、`MainMenuBootstrap` 所有 UI 尺寸、`RunnerInput` swipe 阈值进行全量响应式重构；维护成本长期偏高。推迟，留作未来 ADR。

**方案 C：提供独立竖屏 UI 套件**  
工作量最大，需维护两套完整画面布局；当前团队规模不支持。暂不考虑。

## 执行范围

| 位置 | 改动 |
|---|---|
| `ProjectSettings/ProjectSettings.asset` | `defaultScreenOrientation: 3`（LandscapeLeft）；`allowedAutorotateToPortrait: 0`；`allowedAutorotateToPortraitUpsideDown: 0` |
| `Assets/Scripts/Runtime/PenguinRun/PenguinRunnerGame.cs` | `BuildScene()` 入口调用 `EnsureLandscape()` 运行时双重校正 |
| `Assets/Scripts/Runtime/Game/UI/MainMenuBootstrap.cs` | `Awake()` 入口调用 `EnsureLandscape()` |
| `CONTEXT.md` | 新增术语 `画面构图基准（FramingReference）`；领域规则补充横屏约束 |

## 后果

- 失去竖屏单手握持的用户体验，若玩家以竖屏启动游戏，系统将强制旋为横屏。
- 游戏视觉、操作体验与 PC 编辑器预览一致；无需维护多套相机/HUD 参数。
- 未来若需支持竖屏，须先修订本 ADR 并新建方案 B 或 C 的实施 ADR。
