# Unity Integration

当前仓库根目录已经是 Unity 工程结构。第一阶段已完成 Unity 跑酷原型、Android Library 导出和 Android 主工程接入。

## 当前状态

- Unity 版本：`6000.4.5f1`。
- 已导出 `unityLibrary/`，Android 主工程通过 `:unityLibrary` 依赖 Unity 运行时。
- 跑酷主流程已改为 Unity-only，首页进入跑酷时直接启动 `com.unity3d.player.UnityPlayerGameActivity`。
- Android `minSdk` 已提升到 25，以匹配 Unity 导出的最低 API。
- `app:compileDebugKotlin` 和 `app:assembleDebug` 已通过。
- 导出脚本会在导出后自动移除 Unity Activity 的 launcher intent-filter，避免 APK 安装后出现第二个启动图标。
- 导出脚本当前同时生成 `arm64-v8a` 和 `x86_64`。`arm64-v8a` 面向真机，`x86_64` 用于本机 Android 模拟器验证。

## 当前接入方式

- Android 跑酷入口只启动 Unity，不再保留 Kotlin/OpenGL runner 回退。
- 如果 APK 内没有 Unity 运行时，会显示 Unity 未就绪说明并返回首页。
- 旧原生跑酷的 `app/src/main/java/com/example/mygame/engine/` 和 `Runner3DScreen` 已移除；后续跑酷玩法、地图、道具和手感都在 Unity 侧继续开发。

## 导出 Unity Android Library

### 自动导出

使用 Unity batchmode：

```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.4.5f1\Editor\Unity.exe" `
  -batchmode `
  -quit `
  -projectPath "C:\Users\dxh53\Desktop\mygame" `
  -executeMethod PenguinRun.Editor.UnityRunnerProjectSetup.ExportAndroidLibrary `
  -logFile "C:\Users\dxh53\Desktop\mygame\Logs\unity-export.log"
```

如果日志出现：

```text
Switching to AndroidPlayer is disabled
Error building player because build target was unsupported
```

说明当前 Unity 编辑器没有安装 Android Build Support。需要在 Unity Hub 为 `6000.4.5f1` 增加这些模块：

- Android Build Support
- Android SDK & NDK Tools
- OpenJDK

### 手动导出

1. 在 Unity 中打开本仓库根目录。
2. 菜单运行 `Penguin Run > Create Runner Scene`。
3. 菜单运行 `Penguin Run > Export Android Library`。
4. 导出会生成仓库根目录下的 `unityLibrary/`。
5. 主工程已包含下面这些接入配置；如果未来从干净分支重新接入，需要确认 `settings.gradle.kts` 包含：

```kotlin
include(":unityLibrary")
project(":unityLibrary").projectDir = file("unityLibrary/unityLibrary")
```

6. 确认 `app/build.gradle.kts` 包含：

```kotlin
implementation(project(":unityLibrary"))
```

7. `gradle.properties` 不再需要 `useUnityRunner` 开关；Unity 已是唯一跑酷运行时。

## Android -> Unity 参数

Android 启动 Unity 时会传这些 Intent extra：

- `com.example.mygame.extra.RUN_MODE`
- `com.example.mygame.extra.DAILY`
- `com.example.mygame.extra.NICKNAME`
- `com.example.mygame.extra.BEST_SCORE`
- `com.example.mygame.extra.DASH_LEVEL`
- `com.example.mygame.extra.TUAN_LEVEL`
- `com.example.mygame.extra.MAGNET_LEVEL`
- `com.example.mygame.extra.POLAR_LEVEL`
- `com.example.mygame.extra.SFX_ENABLED`
- `com.example.mygame.extra.HAPTIC_INTENSITY`

Unity 侧需要在入口脚本中读取 Android Intent，并在结算时通过 bridge 回传分数、距离、金币等数据。

Unity 结算回传使用 `Activity.setResult(RESULT_OK, intent)`，字段如下：

- `com.example.mygame.extra.RESULT_SCORE`
- `com.example.mygame.extra.RESULT_DISTANCE`
- `com.example.mygame.extra.RESULT_COINS`
- `com.example.mygame.extra.RESULT_SECONDS`

Android 收到结果后会更新最佳分、按金币增加鱼干，并向本地榜提交 `unity_runner` 或 `unity_daily` 记录。返回首页后会显示一张结算浮层，展示本局分数、距离、金币、存活时间和本地榜名次。

## Unity 玩法与角色表现

- 角色模型已从单一胶囊体拆成 `Assets/Scripts/Runtime/PenguinRun/PenguinCharacter.cs`，由身体、头部、肚皮、眼睛、喙、护目镜、围巾、翅膀和脚部组合成低多边形企鹅。
- 角色动画由代码驱动，当前包含待机挥手、跑步起伏、翅膀摆动、脚步摆动、跳跃张臂、滑铲压低、失败姿态、围巾摆动、护目镜轻微晃动。
- 道具表现已接到角色层：鱼干冲刺会改变身体色调并显示尾迹，团团护盾会显示脉冲护罩，极光磁针扩大金币吸附范围。
- 道具体系扩展为五类：鱼干冲刺、极光磁针、团团护盾、星光加分、极地滑翔。星光加分提升计分倍率，极地滑翔降低空中重力并提升跳跃表现。
- 玩法节奏从单点随机障碍升级为组合模式：金币线、单车道障碍、双车道障碍、安全车道奖励、道具奖励段混合生成，减少连续无解或奖励分布突兀的问题。
- 碰撞仍使用玩家实际 `position.x`，换道途中不会再按目标车道提前判定，护盾命中障碍时会消耗护盾并移除该障碍。
- Android 端曾出现运行时 primitive 材质变洋红的问题，已新增 `Assets/Scripts/Runtime/PenguinRun/RunnerVisuals.cs`，所有运行时生成物体统一使用显式 URP/Unlit/Standard fallback 材质。
- Android Player 若未包含 Physics/Collider 类，`GameObject.CreatePrimitive` 会持续输出 `SphereCollider/BoxCollider/CapsuleCollider doesn't exist`。当前 `RunnerVisuals.CreatePrimitive` 已改为只创建 `MeshFilter + MeshRenderer`，不再自动挂 Collider；项目碰撞继续使用跑酷脚本里的距离判定。
- Unity 地图已从单一赛道扩展为冰原跑道：包含冰面分段速度线、发光车道线、雪原边界、远山、月亮、极光、路侧雪堆和暖色路灯。
- 地图结构继续增强：加入检查点门架，障碍从单个 primitive 改为组合对象，高冰柱、滑铲门和道具拾取物都有更明确的轮廓和识别色。
- 金币收集新增短时连击计分，连续拾取会叠加 `comboBonusScore` 并显示连击反馈。

## 音频接入

- BGM 已替换为 `.ogg` 循环资源：大厅、剧情、跑酷分别使用独立曲目；Unity 跑酷内额外加入冰原风声 ambience。
- 原生 Android 的 `SoundManager` 继续负责首页、剧情和非 Unity UI 音频；Unity 跑酷内由 `Assets/Scripts/Runtime/PenguinRun/RunnerAudio.cs` 独立播放 BGM、风声和即时 SFX。
- Unity 跑酷 SFX 已覆盖跳跃、落地、滑铲、金币、冲刺、磁针、护盾、星光加分、滑翔、撞击、冰裂和检查点。
- 资源与授权记录见 `docs/AUDIO_CREDITS.md`。

## 验证记录

- 已在 `x86_64` 模拟器安装 debug APK，并验证首页入口能启动 `UnityPlayerGameActivity`。
- 曾出现 `libgame.so not found`，原因是模拟器主 ABI 为 `x86_64`，而 Unity Library 只导出了 `arm64-v8a`；已通过双 ABI 导出修复。
- Unity Activity 可进入前台，任务栈中 `resultTo=MainActivity` 正常。
- `Small_Phone` 模拟器多次出现 Vulkan / ADB 不稳定，已通过关闭 Vulkan 和 `angle_indirect` 缓解，但跑局过程仍建议优先真机验证。
- 当前 Android 编译、Unity 导出和 debug APK 打包均已通过；APK 同时包含 `arm64-v8a` 与 `x86_64` Unity native libraries。

## 迁移阶段

1. **阶段 1：Unity 原型并行**
   - Unity 内已有 `Assets/Scripts/Runtime/PenguinRun/PenguinRunnerGame.cs`。
   - 已跑通 Android 启动、输入、基础障碍、返回回退和 APK 打包。
   - 当前 Unity 入口已接到首页跑酷流程，旧 Kotlin/OpenGL 跑酷回退路径已移除。
2. **阶段 2：迁移核心手感**
   - 把现有 Kotlin `PlayerController` 的跳跃缓冲、滑铲、三车道判定迁移到 Unity。
   - Android 侧继续负责首页、存档、音频设置、排行榜。
   - Unity 侧已加入土狼时间、跳跃缓冲、快速下落、滑铲、真实 X 碰撞、车道高亮、操作反馈、系统返回结算。
   - Unity 侧已读取营地强化等级，并接入鱼干冲刺、极光磁针、团团护盾、星光加分、极地滑翔；冲刺/磁针/护盾/滑翔/加分时长由 Android 存档等级参与决定，极地直觉等级会提高道具生成概率。
3. **阶段 3：迁移玩法系统**
   - 迁移道具、障碍节奏、每日挑战 seed、结算数据回传。
4. **阶段 4：移除旧 OpenGL 跑酷**
   - `RunnerRenderer`、`RunnerGameView`、`PlayerController`、`PowerUpManager` 等旧原生 runner 渲染与控制路径已移除。
   - 后续阶段集中在 Unity 内继续完善地图结构、道具生态、音效反馈和真机性能。
