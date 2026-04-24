# 咕咕嘎嘎

`咕咕嘎嘎` 是一个 Android 单机横版动作冒险游戏项目。

当前原型已经包含：

- 主角企鹅 `咕咕嘎嘎`
- 第一关 `雪松村废墟`
- 敌人 `冰壳海豹` / `风雪乌鸦`
- 道具 `鱼干冲刺` / `泡泡围巾`
- 伙伴 `团团` 的首次救援与支援能力

## 开发文档

- [项目设计稿](C:/Users/dxh53/Desktop/mygame/GAME_DESIGN.md)
- [项目架构说明](C:/Users/dxh53/Desktop/mygame/docs/ARCHITECTURE.md)
- [背景剧情说明](C:/Users/dxh53/Desktop/mygame/docs/STORY.md)
- [开发协作指南](C:/Users/dxh53/Desktop/mygame/docs/DEVELOPMENT_GUIDE.md)

## 当前代码结构

- [MainActivity.kt](C:/Users/dxh53/Desktop/mygame/app/src/main/java/com/example/mygame/MainActivity.kt)
  仅负责承载 Compose 入口
- [GuguGagaGame.kt](C:/Users/dxh53/Desktop/mygame/app/src/main/java/com/example/mygame/game/GuguGagaGame.kt)
  主游戏循环、关卡数据与绘制逻辑
- [GameModels.kt](C:/Users/dxh53/Desktop/mygame/app/src/main/java/com/example/mygame/game/GameModels.kt)
  游戏数据模型与枚举
- [GameUi.kt](C:/Users/dxh53/Desktop/mygame/app/src/main/java/com/example/mygame/game/GameUi.kt)
  HUD、提示卡、按钮等通用 UI 组件
- [SaveRepository.kt](C:/Users/dxh53/Desktop/mygame/app/src/main/java/com/example/mygame/data/SaveRepository.kt)
  统一管理本地存档与进度读写
- 关卡配置目录 `game/level/`：`LevelOneData` / `LevelTwoData`（冰湖占位）、`LevelCatalog` 与 `LevelContent`（文案与配色）
- 极夜漂流（无尽）：`game/modes/EndlessMode.kt` + `EndlessBalanceConfig.kt`（权重/倍率/路程等调参）+ `game/level/EndlessSegment*.kt` + `EndlessScoreBook`；结算明细 `ui/endless/EndlessSettlementOverlay.kt`；本地榜 `data/LocalLeaderboardRepository.kt`（条目含 `playerId`）；入口见 `ui/GameRoot.kt`

## 后续建议

下一阶段建议优先做：

1. 第二关 `冰湖回音谷`
2. 伙伴系统扩展
3. 家园重建系统
4. 资源与章节切换结构
