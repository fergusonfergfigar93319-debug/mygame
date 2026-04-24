# 项目架构说明

## 目标

当前文档用于帮助新的开发者快速理解项目分层与代码职责。

## 当前目录结构

```text
app/src/main/java/com/example/mygame
├─ MainActivity.kt
├─ data
│  └─ SaveRepository.kt
├─ game
│  ├─ GuguGagaGame.kt
│  ├─ GameModels.kt
│  ├─ GameUi.kt
│  └─ level
│     ├─ GameLevel.kt
│     ├─ LevelCatalog.kt
│     ├─ LevelContent.kt
│     ├─ LevelOneData.kt
│     └─ LevelTwoData.kt
└─ ui/theme
   ├─ Color.kt
   ├─ Theme.kt
   └─ Type.kt
```

## 各文件职责

### `MainActivity.kt`

- Android 入口
- 只负责挂载 Compose 内容
- 尽量不放业务逻辑

### `data/SaveRepository.kt`

- 负责封装 SharedPreferences，统一保存最高分、伙伴解锁、**接续关卡**（`resume_level`：雪松村 / 冰湖）、**玩家昵称**与**匿名 `playerId`**（联网榜预留）等字段

### `game/level/`（关卡配置）

- `GameLevel.kt`：关卡枚举（雪松村废墟、冰湖回音谷）
- `LevelContent.kt`：`LevelContent` / `LevelPresentation`（几何数据 + 文案 + 场景配色）
- `LevelOneData.kt` / `LevelTwoData.kt`：各关 `build(worldWidth, worldHeight)` 产出关卡快照；第二关当前为可玩占位数据
- `LevelCatalog.kt`：按枚举分发到具体关卡数据

### `game/GameModels.kt`

- 存放关卡元素和运行时数据结构
- 例如平台、坑洞、敌人、奖励、关底目标

### `game/GuguGagaGame.kt`

- 存放主游戏循环
- 负责：
  - 玩家状态
  - 关卡初始化
  - 碰撞逻辑
  - 道具效果
  - 关卡通关/失败状态
  - 主要场景绘制

### `game/GameUi.kt`

- 存放可复用 UI 组件
- 例如：
  - HUD
  - 状态卡片
  - 章节预告
  - 长按按钮

## 推荐的后续扩展结构

当项目继续扩大时，建议继续细拆。（`data/SaveRepository.kt` 已存在于顶层 `data` 包，负责封装 SharedPreferences，统一保存最高分、伙伴解锁和后续章节进度；后续可把章节进度、关卡解锁等字段继续收敛到此类。）

```text
app/src/main/java/com/example/mygame
├─ data
│  ├─ SaveRepository.kt
│  └─ LevelRepository.kt
└─ game
   ├─ level
   │  ├─ LevelOneData.kt
   │  └─ LevelTwoData.kt
   ├─ logic
   │  ├─ CollisionEngine.kt
   │  ├─ EnemySystem.kt
   │  └─ ItemSystem.kt
   ├─ render
   │  ├─ BackgroundRenderer.kt
   │  ├─ CharacterRenderer.kt
   │  └─ EnemyRenderer.kt
   └─ ui
      ├─ Hud.kt
      ├─ Dialogs.kt
      └─ Controls.kt
```

## 目前存在的结构问题

虽然已经做了第一轮拆分，但 `GuguGagaGame.kt` 依然偏大，后续建议继续拆：

1. ~~把关卡数据抽到 `level` 包~~（已完成首版：`LevelOneData` / `LevelTwoData` + `LevelCatalog`）
2. 把碰撞与物理更新抽到 `logic` 包
3. 把 Canvas 绘制拆到 `render` 包
4. 把章节进度、当前关卡解锁状态等更多字段纳入 `SaveRepository`，避免散落在界面层

## 协作原则

- 新增玩法时，优先放进 `game` 子目录，不要回写到 `MainActivity`
- 纯数据配置优先和逻辑代码分离
- UI 组件尽量复用，不要在主循环文件里重复写卡片/按钮
- 关卡数据如果变多，优先配置化，不要把所有坐标都写死在一个函数里
