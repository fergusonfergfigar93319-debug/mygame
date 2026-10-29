# 企鹅快跑

`企鹅快跑` 是一款以咕咕嘎嘎为主角的 Android 单机跑酷小游戏。新的产品方向以“自动奔跑、跳跃/下滑、道具收集、每日挑战、排行榜复玩”为核心，保留企鹅伙伴、高松鹅追击和冰雪世界观。

## 当前方向

- 主入口转向跑酷玩法：默认进入自动奔跑的“极夜快跑”模式。
- 核心操作收敛为：跳跃、下滑、伙伴支援、道具时机。
- 关卡从传统横版平台堆叠，逐步改为片段化跑道、障碍组合和难度节奏。
- 保留图鉴、营地、今日挑战、本地排行榜、背景音乐和反馈入口。

## 工程

Unity **6000.4.5f1**，横屏 3D。玩法代码在 `Assets/Scripts/Runtime/PenguinRun/`，局外系统与 UI 在 `Assets/Scripts/Runtime/Game/`。

## 设计文档

- [游戏开发设计总纲 GDD](./docs/GDD.md) — 按系统汇总的数值、缺口与路线图
- [当前设计基准](./docs/CURRENT_DESIGN.md)
- [术语与领域规则](./CONTEXT.md) — **改动前先读**
- [验证体系](./docs/VERIFICATION.md) — 门禁、校验器与资产生成器
- [开发协作指南](./docs/DEVELOPMENT_GUIDE.md)
- [背景剧情说明](./docs/STORY.md)
- [页面设计与美术优化方案](./docs/UI_ART_REVAMP_PLAN.md)
- [Unity 集成说明](./docs/UNITY_INTEGRATION.md)
- [跑酷重构记录](./docs/PENGUIN_RUN_RESTRUCTURE.md)
- [优化执行记录](./docs/GAME_OPTIMIZATION_PLAN.md)（含早期 Android/Kotlin 阶段，仅供追溯）
- [旧 Android 架构历史稿](./docs/ARCHITECTURE.md)

## 构建与验证

```powershell
# 编译检查（不启动 Unity）
powershell -ExecutionPolicy Bypass -File tools/CompileFullRoslyn.ps1

# 全部静态门禁
Get-ChildItem tools/Verify*.ps1 | ForEach-Object { & powershell -ExecutionPolicy Bypass -File $_.FullName }

# Android 测试包
powershell -ExecutionPolicy Bypass -File tools/BuildLatestAndroidApk.ps1
```

完整的门禁、校验器与资产生成器清单见 [docs/VERIFICATION.md](./docs/VERIFICATION.md)。

> 早期 Android/Kotlin 原型的 `./gradlew.bat` 构建方式已随工程转向 Unity 而失效，根目录不再有 Gradle 工程。
