# 咕咕嘎嘎

`咕咕嘎嘎` 是一款 Android 单机横版动作冒险游戏。主角咕咕嘎嘎在家园被高松鹅毁掉后，踏上寻找伙伴、修复队伍、追击高松鹅的旅程。

## 当前内容

- 主线模式：雪松村废墟、冰湖回音谷、北境雾堤和高松鹅 Boss 链路。
- 无尽模式：极夜漂流、今日挑战、固定 seed、结算明细和本地排行榜。
- 伙伴与成长：团团支援、Dror 跟随、补给营地、鱼干累计和三项轻量升级。
- 体验表现：背景音乐、局内音效、暂停层、转场、场景主题、粒子反馈和设备倾斜。
- 图鉴系统：角色、伙伴状态、章节事件和能力说明。

## 文档入口

- [项目设计文档](./GAME_DESIGN.md)
- [优化执行方案](./docs/GAME_OPTIMIZATION_PLAN.md)
- [项目结构说明](./docs/ARCHITECTURE.md)
- [背景剧情说明](./docs/STORY.md)
- [开发协作指南](./docs/DEVELOPMENT_GUIDE.md)

## 代码结构

- `app/src/main/java/com/example/mygame/MainActivity.kt`：Android 入口，创建音频管理器并挂载 Compose。
- `app/src/main/java/com/example/mygame/ui/GameRoot.kt`：主页、主线、无尽、今日挑战、排行榜、图鉴和营地的路由。
- `app/src/main/java/com/example/mygame/game/GuguGagaGame.kt`：主线玩法循环、碰撞、道具、Boss 和绘制入口。
- `app/src/main/java/com/example/mygame/game/modes/EndlessMode.kt`：极夜漂流和今日挑战。
- `app/src/main/java/com/example/mygame/game/level/`：主线关卡数据、无尽片段和场景主题。
- `app/src/main/java/com/example/mygame/data/SaveRepository.kt`：本地存档、伙伴解锁、音频开关和营地升级。
- `app/src/main/java/com/example/mygame/data/LocalLeaderboardRepository.kt`：本地排行榜和今日挑战分桶。
- `app/src/main/java/com/example/mygame/ui/common/`：共享界面组件、暂停层、玩法 HUD 和控制面板。
- `app/src/main/java/com/example/mygame/ui/home/`：主页、图鉴、营地和大厅雪效。
- `app/src/main/java/com/example/mygame/audio/SoundManager.kt`：音效和循环背景音乐。

## 构建

```powershell
./gradlew.bat :app:compileDebugKotlin
./gradlew.bat :app:assembleDebug
```

当前建议优先继续推进：首局体验、首页推荐入口、Boss 复盘提示、第三关专属机制，以及 `GuguGagaGame.kt` / `EndlessMode.kt` 的系统拆分。
