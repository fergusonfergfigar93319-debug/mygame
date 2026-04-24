# 开发协作指南

## 适用对象

这份文档面向第一次接手本项目的开发者。

## 先看哪些文件

建议按这个顺序阅读：

1. [README.md](C:/Users/dxh53/Desktop/mygame/README.md)
2. [GAME_DESIGN.md](C:/Users/dxh53/Desktop/mygame/GAME_DESIGN.md)
3. [docs/STORY.md](C:/Users/dxh53/Desktop/mygame/docs/STORY.md)
4. [docs/ARCHITECTURE.md](C:/Users/dxh53/Desktop/mygame/docs/ARCHITECTURE.md)
5. [SaveRepository.kt](C:/Users/dxh53/Desktop/mygame/app/src/main/java/com/example/mygame/data/SaveRepository.kt)
6. [GuguGagaGame.kt](C:/Users/dxh53/Desktop/mygame/app/src/main/java/com/example/mygame/game/GuguGagaGame.kt)

## 当前开发重点

### 已完成

- 第一关基础原型
- 团团支援系统雏形
- 鱼干冲刺 / 泡泡围巾

### 下一步建议

1. 第二关 `冰湖回音谷`
2. 伙伴能力扩展
3. 家园重建系统
4. 数据配置化

## 提交代码建议

### 新增玩法

- 优先新增到 `game` 包内
- 不要把复杂逻辑重新堆回 `MainActivity`

### 新增 UI

- 可复用 UI 放到 `GameUi.kt`
- 特定章节专用 UI 可以先建独立文件

### 新增剧情与关卡

- 建议把剧情文本和关卡参数逐步配置化
- 不要长期把所有文本、坐标、触发条件都写死在一个函数里

## 未来推荐拆分点

### 关卡数据

建议后续新增：

- `LevelOneData.kt`
- `LevelTwoData.kt`
- `LevelDefinitions.kt`

### 系统逻辑

建议后续新增：

- `CollisionEngine.kt`
- `EnemySystem.kt`
- `CompanionSystem.kt`
- `ItemSystem.kt`

### 存档与进度

- `SaveRepository.kt`（已接入：最高分、伙伴救援等；章节进度与关卡解锁可继续收敛到此）
- 建议后续按需补充：`ProgressState.kt` 或与关卡 id 对齐的数据类，便于在 UI 与仓库之间传递状态

## 设计一致性约定

- 新道具必须优先服务世界观，不要回退成常见平台游戏模板
- 新伙伴必须具备剧情价值和玩法价值
- 地图命名、敌人命名、道具命名保持企鹅世界观统一
- UI 文案尽量保持温暖、冒险、轻童话风格

## 测试建议

每次改动后至少验证：

1. 项目能否 `assembleDebug`
2. 第一关是否可以正常开始
3. 通关和失败状态是否还能触发
4. 道具状态是否会影响角色运动
5. 伙伴支援按钮是否存在状态错误
