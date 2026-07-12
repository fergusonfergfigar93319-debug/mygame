# 开发协作指南

## 适用对象

这份文档面向第一次接手本项目的开发者。

## 先看哪些文件

建议按这个顺序阅读：

1. [README.md](C:/Users/dxh53/Desktop/mygame/README.md)
2. [docs/CURRENT_DESIGN.md](C:/Users/dxh53/Desktop/mygame/docs/CURRENT_DESIGN.md)
3. [docs/STORY.md](C:/Users/dxh53/Desktop/mygame/docs/STORY.md)
4. [CONTEXT.md](C:/Users/dxh53/Desktop/mygame/CONTEXT.md)
5. [docs/UNITY_INTEGRATION.md](C:/Users/dxh53/Desktop/mygame/docs/UNITY_INTEGRATION.md)
6. [docs/adr/0001-lock-landscape-orientation.md](C:/Users/dxh53/Desktop/mygame/docs/adr/0001-lock-landscape-orientation.md)
7. [Assets/Scripts/Runtime/PenguinRun/PenguinRunnerGame.cs](C:/Users/dxh53/Desktop/mygame/Assets/Scripts/Runtime/PenguinRun/PenguinRunnerGame.cs)
8. [Assets/Scripts/Runtime/PenguinRun/Systems/RunnerSegmentCatalog.cs](C:/Users/dxh53/Desktop/mygame/Assets/Scripts/Runtime/PenguinRun/Systems/RunnerSegmentCatalog.cs)
9. [Assets/Scripts/Runtime/Game/UI/MainMenuBootstrap.cs](C:/Users/dxh53/Desktop/mygame/Assets/Scripts/Runtime/Game/UI/MainMenuBootstrap.cs)

`GAME_DESIGN.md` 是旧横版动作冒险历史稿，只作为世界观和命名参考。

## 当前开发重点

### 已完成

- Unity 3D 横屏跑酷主流程
- 自动奔跑、跳跃、下滑、换道和移动端手势输入
- 主题片段池、道具、Boss 前奏/战斗/奖励段
- 大厅、营地/商店、任务、图鉴、本地排行榜和结算闭环

### 下一步建议

1. 减轻首局教学和大厅信息量
2. 压缩 Boss 战 HUD，保护跑道中心和下半屏
3. 将移动端输入阈值改成响应式
4. 统一大厅 uGUI 与跑酷 IMGUI HUD 的视觉规范

## 提交代码建议

### 新增玩法

- 优先新增到 `Assets/Scripts/Runtime/PenguinRun/` 或 `Assets/Scripts/Runtime/Game/`
- 遵守 `CONTEXT.md` 的领域规则，例如新道具必须同步扩展枚举、拾取、视觉、音频和状态管理

### 新增 UI

- 大厅与局外系统优先使用 uGUI 和 `UiBuilder`
- 跑酷实时 HUD 当前使用 IMGUI，新增信息前先判断是否会遮挡跑道
- Boss 战期间只保留关键战斗信息，非关键统计应弱化或隐藏

### 新增剧情与关卡

- 剧情语气参考 `docs/STORY.md`
- 关卡节奏优先通过 `RunnerSegmentCatalog` 和相关生成系统扩展
- 新主题必须具备片段池、障碍调色板、Boss 刷新条件和 BGM/ambience 映射

## 未来推荐拆分点

- 将大厅 UI 中重复的卡片、按钮和 Sheet 样式继续收敛到 `UiBuilder` 或主题常量。
- 给跑酷 HUD 建立共享视觉规范，减少 uGUI 与 IMGUI 的样式漂移。
- 如果片段池继续扩大，考虑把代码驱动的片段定义迁移到 ScriptableObject 或数据表。
- 如果 Boss 招式继续增多，保持命中规则、VFX 和提示文案分层，避免视觉效果改变判定。

## 设计一致性约定

- 新道具必须优先服务世界观，不要回退成常见平台游戏模板
- 新伙伴必须具备剧情价值和玩法价值
- 地图命名、敌人命名、道具命名保持企鹅世界观统一
- UI 文案尽量保持温暖、冒险、轻童话风格
- 任何相机、HUD、输入或布局改动都必须按横屏 16:9 基准验证

## 测试建议

每次改动后至少验证：

1. Unity 场景是否能进入 `MainMenu` 和 `PenguinRunner`
2. 横屏 16:9 下三车道与前方障碍是否清楚
3. 跳跃、下滑、换道、暂停和返回结算是否正常
4. 道具、Boss 预警、破绽和奖励段是否能读懂
5. 结算、鱼干、任务、图鉴和排行榜是否更新
6. Android 构建或 Unity 导出是否仍通过
