# 企鹅快跑

`企鹅快跑` 是一款以咕咕嘎嘎为主角的 Android 单机跑酷小游戏。新的产品方向以“自动奔跑、跳跃/下滑、道具收集、每日挑战、排行榜复玩”为核心，保留企鹅伙伴、高松鹅追击和冰雪世界观。

## 当前方向

- 主入口转向跑酷玩法：默认进入自动奔跑的“极夜快跑”模式。
- 核心操作收敛为：跳跃、下滑、伙伴支援、道具时机。
- 关卡从传统横版平台堆叠，逐步改为片段化跑道、障碍组合和难度节奏。
- 保留图鉴、营地、今日挑战、本地排行榜、背景音乐和反馈入口。

## 设计文档

- [当前设计基准](./docs/CURRENT_DESIGN.md)
- [跑酷重构记录](./docs/PENGUIN_RUN_RESTRUCTURE.md)
- [术语与领域规则](./CONTEXT.md)
- [背景剧情说明](./docs/STORY.md)
- [优化执行方案](./docs/GAME_OPTIMIZATION_PLAN.md)
- [页面设计与美术优化方案](./docs/UI_ART_REVAMP_PLAN.md)
- [Unity 集成说明](./docs/UNITY_INTEGRATION.md)
- [旧 Android 架构历史稿](./docs/ARCHITECTURE.md)
- [开发协作指南](./docs/DEVELOPMENT_GUIDE.md)

## 构建

```powershell
./gradlew.bat :app:compileDebugKotlin
./gradlew.bat :app:assembleDebug
```
