# 验证体系

本项目靠一套门禁（gate）保持质量，而不是靠人工回归。每批改动落地时会**新增**锁住该批次契约的门禁，因此门禁数量只增不减。

改完代码至少跑「快速回合」；碰了美术、音频、UI 或性能相关的部分，再跑对应的 Unity 校验器。

---

## 快速回合

### 2026-10-01 M2a/M2b fresh evidence

| 检查 | 结果 |
| --- | --- |
| `TestGameplayFairness.ps1` | 21 组通过 |
| `TestRunnerInput.ps1` | 18 组通过 |
| `TestThemeContinuity.ps1` | 7 组通过，含有效计时、Daily、路线主题序列和世界事件冻结 |
| `TestThemeRouteEncounters.ps1` | 88 组通过 |
| `TestThemeMasterySession.ps1` | 7 组通过，含 200 个晚进入契约种子/主题组合及结算去重 |
| `CompileFullRoslyn.ps1` | 205 个源文件通过 |
| `Verify*.ps1` | 24 个中 23 个通过；唯一失败是继承的 `BossEncounter.cs` 1947/1900 行限制 |
| Unity `ProjectThemeContinuityValidation` | 未运行：Editor license 返回 198 |

独立 harness 只证明生产纯逻辑和编译边界，不替代 Unity 场景、遮罩、对象池视觉或 Android 设备证据。新增真实 controller/spawner 校验入口为 `ProjectThemeContinuityValidation`，许可证可用后执行。

本次 Unity 日志：`_tmp/theme-continuity-unity-2026-10-01.log`；错误为无有效 Editor license，返回 198，校验器未开始执行。编辑器菜单为 Tools > Penguin Run > Validation > Theme Continuity (Play Mode)。命令行入口是 `PenguinRun.Editor.ProjectThemeContinuityValidation.ValidateFromCommandLine`，由入口进入 Play Mode 并自行退出，启动参数不要加 `-quit`。校验器要求已有精通 schema，避免测试触发首次存档迁移；测试结束恢复随机状态、地形档案并卸载隔离场景。

独立规格审查与质量审查已完成。质量审查发现池满时路线鱼干归属可能漏记，已改为登记实际生成实例，并增加饱和池后的路线取消验证入口（已编译，受同一许可问题影响尚未运行）。主文件拆分后的上限已下调至 controller 1060、spawner 2210，未提高既有 BossEncounter 上限。

```powershell
# 1) 全量编译（不进 Unity，最快的语法/引用检查）
powershell -ExecutionPolicy Bypass -File tools/CompileFullRoslyn.ps1

# 2) 全部静态门禁
Get-ChildItem tools/Verify*.ps1 | ForEach-Object {
    & powershell -ExecutionPolicy Bypass -File $_.FullName
    if ($LASTEXITCODE -ne 0) { Write-Host "FAIL $($_.Name)" }
}
```

---

## 静态门禁（`tools/Verify*.ps1`）

纯文本检查，不启动 Unity，秒级完成。

| 脚本 | 守的是什么 |
| --- | --- |
| `VerifyUiArtP0.ps1` | UI/美术 P0 契约：图标注册表、道具视觉档案、移动端画质预算入口 |
| `VerifyStorybookUiPhase1.ps1` | 童话 UI 阶段一：纸张页面、章节标题、墨色正文结构 |
| `VerifyPremiumStorybookHome.ps1` | 童话营地大厅的资产与结构 |
| `VerifyStorybookPalette.ps1` | 纸张 UI 层不得引用旧霓虹色板（含 `Success` / `TextTertiary`）；主/次按钮语言；大厅美术 `.meta` 必须含导入块 |
| `VerifyImage2Art.ps1` | Image 2 七张插画的 Sprite 导入、目录路径，以及大厅/面板/死亡/暂停/战绩消费点 |
| `VerifyRunDepthPhase2.ps1` | 局内深度阶段二：路线事件、旅途札记、技巧分 |
| `VerifyLongTermGrowthPhase3.ps1` | 长期成长阶段三：营地重建、伙伴羁绊、图鉴发现 |
| `VerifyThemeMechanicsPhase4.ps1` | 主题地图机关与精通阶段四 |
| `VerifyThemeAudioDifferentiation.ps1` | 六个主题各有完整且**互不重复**的听感档案 |
| `VerifyFirstRunOnboarding.ps1` | 能力驱动的首局新手期（ADR-0004） |
| `VerifyPlayerExperienceAudit.ps1` | 玩家体验审计 PX-001…PX-011 的修复契约 |
| `VerifyProtagonistAvatarRevamp.ps1` | 主角表现层边界、视角空间主光、贴片轮廓裁剪 |
| `VerifyVisualJudgmentRevamp.ps1` | 2026-07-20 建模/特效/判定批次，含 LOD 阈值防回归 |
| `VerifyCustomMapAssetsBatch1.ps1` | 自定义地图资产批次一 |
| `VerifyCubePetAndNatureProps.ps1` | Cube Pet 主角路径 + 六主题路边开源 FBX（含中景带与灯/旗/冰晶/海草换模）与 credits |
| `VerifyQualityTierCalibration.ps1` | 画质分档由设备能力驱动、真的写入、且编辑器可达 |
| `VerifyFileSizeRatchet.ps1` | 六个超大文件只准缩小，不准增长 |
| `VerifyRunFeelBatch.ps1` | 2026-09-17 手感与局内 UI 批次：保留式全屏表现层取代 IMGUI、反馈配方单表、命中顿帧双端同步、擦身技巧分、难度倍率真实生效、振动单一权威、HUD 霜面单色板、无豆腐块字形 |
| `VerifyRunContentFeel.ps1` | 2026-09-21 游玩内容与操作体验：换道跟手、镜头与构图对齐、新手期第一次冲刺、片段横幅、鱼干称谓 |

**约定：** 这些脚本一律**纯 ASCII 注释**。无 BOM 时 PowerShell 5.1 按 ANSI 读取，中文注释会变成误导性的解析错误。

---

## Unity 校验器与截图

需要启动 Unity 批处理，单次约 1–2 分钟。

```powershell
$unity = 'C:\Program Files\Unity\Hub\Editor\6000.4.5f1\Editor\Unity.exe'
& $unity -batchmode -quit -projectPath . -executeMethod <入口> -logFile _tmp/<名字>.log
```

### 校验器（会失败）

| 入口 | 守的是什么 |
| --- | --- |
| `PenguinRun.Editor.ProjectRunnerAvatarValidation.ValidateFromCommandLine` | 主角 LOD 三角面预算（50000/22000/8000）、材质数 ≤3 |
| `PenguinRun.Editor.ProjectLobbyArtValidation.ValidateFromCommandLine` | 大厅美术不得静默降级成占位圆；伙伴不得复用吉祥物立绘 |
| `PenguinRun.Editor.ProjectQualityTierValidation.ValidateFromCommandLine` | 画质三档互不相同、按档单调，且 `Apply()` 真的写进 QualitySettings |
| `PenguinRun.Editor.ProjectThemeMechanicValidation.ValidateFromCommandLine` | 主题机关的确定性与生命周期 |
| `PenguinRun.Editor.ProjectOnboardingValidation.ValidateFromCommandLine` | 新手期抑制、片段轮转、毕业与迁移 |
| `PenguinRun.Editor.ProjectPlayerExperienceValidation.ValidateFromCommandLine` | 玩家体验审计的运行时契约 |
| `PenguinRun.Editor.ProjectVisualAssetValidation.BuildAndValidateFromCommandLine` | 视觉预制体双 LOD 与阈值 |
| `ProjectUiLayoutRegressionTest.RunFromCommandLine` | UI 布局回归（注意：**全局命名空间**，无前缀） |

### 截图（产出证据，部分带断言）

| 入口 | 产出 | 断言 |
| --- | --- | --- |
| `PenguinRun.Editor.ProjectPenguinPreviewCapture.CaptureAllFromCommandLine` | `Artifacts/PenguinPreviews/` | 七个动作姿势与 Idle 的像素差 ≥ 2.2 |
| `PenguinRun.Editor.ProjectAvatarGameplayViewCapture.CaptureFromCommandLine` | `Artifacts/AvatarGameplayView.png` | 实距下 `meanLuma ≥ 65`、`detailStdDev ≥ 18` |
| `PenguinRun.Editor.ProjectResultScreenCapture.CaptureFromCommandLine` | `Artifacts/ResultScreens/` | 结算页下半区区块**两两不相交** |
| `PenguinRun.Editor.ProjectPickupGameplayViewCapture.CaptureFromCommandLine` | `Artifacts/PickupGameplayView.png` | 无（人工看读感） |
| `PenguinRun.Editor.ProjectBossPreviewCapture.CaptureAllFromCommandLine` | `Artifacts/BossPreviews/` | 无 |
| `PenguinRun.Editor.ProjectVisualPreviewCapture.CaptureAllFromCommandLine` | `Artifacts/VisualPreviews/` | 无 |
| `PenguinRun.Editor.ProjectThemeMechanicPreviewCapture.CaptureFromCommandLine` | `Artifacts/ThemeMechanicPreviews/` | 无 |
| `PenguinRun.Editor.ProjectOpenNaturePreviewCapture.CaptureAllFromCommandLine` | `Artifacts/OpenNaturePreviews/` | 六主题路边 CC0：对真实 FBX 做网格高度校验，并用等高胶囊做 RT 像素差（`occupied≥1200`、`meanLuma≥28`）。batchmode 下导入材质不稳定，故图为等高代理剪影；**不要加 `-nographics`** |

### 构建器（改了源数据后需要重跑）

| 入口 | 作用 |
| --- | --- |
| `PenguinRun.Editor.RunnerAvatarAssetBuilder.BuildFromCommandLine` | 从 FBX 重建主角预制体、控制器与定义，并跑一次校验 |
| `PenguinRun.Editor.PenguinVisualPrefabBuilder.RebuildAllLodPrefabsFromCommandLine` | 重建障碍/道具/环境的双 LOD 预制体 |
| `PenguinRun.Editor.ThemeMechanicPrefabBuilder.BuildBatchFromCommandLine` | 重建主题机关预制体 |
| `PenguinRun.Editor.PenguinRunPostFxRigBuilder.BuildFromCommandLine` | 重建后处理 rig 预制体 |

---

## 资产生成器

资产是**生成**出来的，不要手改产物；改生成器后重跑并提交产物。

```powershell
# 主角网格 / LOD / 动画（改完必须重跑 RunnerAvatarAssetBuilder）
& "$env:LOCALAPPDATA\Programs\Blender Foundation\blender-4.5.10-windows-x64\blender.exe" `
    --background --factory-startup --python tools/blender/build_gugugaga_avatar.py

python tools/generate_theme_audio.py        # 六主题环境音床 + 地面音色
python tools/generate_ui_icons.py           # UI 图标（旗帜 / 鱼干）
python tools/generate_premium_home_assets.py # 营地建筑图集与特效
```

生成器一律**定种子**，重建字节一致，diff 可审阅。

---

## Android 构建

```powershell
powershell -ExecutionPolicy Bypass -File tools/BuildLatestAndroidApk.ps1
```

内部走 `PenguinRunCiBuild.BuildAndroidApkForTesting`，产物在 `Builds/Android/`。

---

## 本机已知限制

这几条会让人误判"验证过了"，务必知道：

1. **`CompileFullRoslyn.ps1` 通过 ≠ Unity 编译通过。** 它复用一份缓存的引用集（`_tmp/storybook-compile.rsp`），包版本可能与 Unity 实际解析的不一致。本会话就遇到过：Roslyn 门禁通过，Unity 报 `HasCharacter` 重载不匹配。**动到包 API 时以 Unity 批处理为准。**

2. **`ProjectPlayerJourneySmokeTest` 在本机 `-batchmode` 下截不到帧**（`Stage timeout: Capture`）。`ScreenCapture.CaptureScreenshot` 拿不到画面，因此 `Artifacts/PlayerExperienceAudit` 的五张旅程证据无法刷新。需要 UI 视觉证据时改用 RenderTexture 路径（见 `ProjectResultScreenCapture` / `ProjectAvatarGameplayViewCapture`），那条路径可用。
   注意：它由 `EditorApplication.update` 驱动，**不能加 `-quit`**，否则方法一返回 Unity 就退出，测试还没开始跑。

3. **本机无连接的 Android 设备。** 实机 60fps、材质批次数、手机外放听感一律标记为"待验证"，不得声称通过。

4. **`QualitySettings.asset` 容易被跑校验器弄脏。** 内容差异应为空（只剩行尾差异）；若出现真实差异说明某个校验器没做快照还原。

---

## 输入与暂停回归

本批次新增三层验证：

- tools/TestRunnerInput.ps1：用 PowerShell 7 编译真实 RunnerInput 与 RunnerGestureClassifier，替换的仅是 Unity 输入采样/数学边界。覆盖取消触摸、手指身份、双指暂停锁存、鼠标兼容事件、输入重置，以及正常按键/滑动/双击；不等价于 Android 实机触摸测试。
- tools/VerifyInputPauseSafety.ps1：静态检查暂停/教学冻结判断先于冲刺计时，重开、状态切换及应用生命周期确实调用输入清理。
- PenguinRun.Editor.ProjectInputPauseValidation.ValidateFromCommandLine：真实控制器、FSM、Motor、World 的五组 Unity 行为测试，仅渲染表现使用空实现。验证暂停冻结、动作缓冲清理、教学等待、正常冲刺缓冲和主题切换。

独立输入测试：

    pwsh -NoProfile -File tools/TestRunnerInput.ps1

Unity 编辑器验证（激活许可证后）：

    & 'C:/Program Files/Unity/Hub/Editor/6000.4.5f1/Editor/Unity.exe' -batchmode -nographics -quit -projectPath . -executeMethod PenguinRun.Editor.ProjectInputPauseValidation.ValidateFromCommandLine -logFile _tmp/input-pause-validation.log

本批次记录（本机 UTC+8 时间 2026-09-25）：旧代码的独立输入测试复现 13 项失败，初版静态门禁复现 4 项失败。Unity 批处理在加载测试前因无有效编辑器许可证退出（198，见 _tmp/input-pause-red.log），因此新增控制器行为测试及已有玩家体验 Unity 校验仍待运行，不能视为通过。

修复后的已验证结果：18 组独立输入行为回归、20 项静态门禁全部通过；全量 Roslyn 编译通过（192 个源文件），git diff --check 无错误。编译仍有既有的 ProjectThemeMechanicValidation.cs:965 CS0162 不可达代码警告，不属于本次输入修复。

人工复测：双指暂停后保持一指、加入第三指、全部抬起再恢复；系统取消触摸与切后台；空中暂停/恢复不应跳位或自动兑现旧动作；暂停期间冲刺冷却不走时；普通鼠标、键盘、单指滑动和双击手感不变。

## 碰撞计分与片段轮转回归

本轮对齐既有规则：擦身仅属于未跳铲的贴边通过，同一障碍不能同时奖励擦身和完美闪避；有其他模式可选时不连续抽到相同片段模式。

- tools/TestGameplayFairness.ps1：直接编译真实 CollisionSystem、SegmentWaveDirector、RunnerSegmentCatalog 和碰撞规格。替换 Unity 对象、随机采样、表现及不在本轮范围内的防御解析边界；任何意外进入伤害解析的测试场景立即失败。不是 Unity 物理、伤害系统或设备集成测试。
- 21 组场景覆盖关闭/重新启用碰撞、站立/跳跃/滑铲/起身、双向奖励互斥、合法奖励保留与重开去重，以及六主题各 2000 波、固定随机值、单模式/空池回退、候选槽位完整性和新手固定轮转。
- tools/VerifyGameplayFairness.ps1：静态检查碰撞开关先于奖励、姿态过滤、跨奖励去重与按可选定义抽样。行为正确性以独立回归为准。

运行命令：

    pwsh -NoProfile -File tools/TestGameplayFairness.ps1
    powershell -NoProfile -ExecutionPolicy Bypass -File tools/VerifyGameplayFairness.ps1

测试先于修复：最初 15 组场景中复现 8 项失败；修复并扩充边界覆盖后 21 组全部通过。原有 18 组输入行为回归、全部 21 项静态门禁和全量 Roslyn 编译（192 个源文件）均通过。

未调整判定盒尺寸、净空阈值、伤害/防御规则、片段池内容或新手轮转顺序。抽样改为直接从非上一模式的定义中等概率选择；所有定义同模式时仍允许正常抽取。

Unity 与 Android 运行时仍待验证：本轮未重新尝试此前返回 198 的无有效许可证批处理，也未生成新的实机证据。激活后需复测定时门开合擦身、跳/铲贴边、同障碍跨帧计分及连续片段提示。

## 主题技巧远征：冰湖与雪松（2026-09-25，UTC+8）

本轮按已批准规格实现冰湖 ice-fracture 的三次跳跃路线、雪松 cedar-rescue 的三次滑铲路线，以及两者的稳健补给路线。沿用真实障碍注册和拾取系统，未增加奖励、改动存档或输入规则。

验证分层：

- tools/TestThemeRouteEncounters.ps1：直接编译生产目录、路线导演、距离预约及提示逻辑，仅替换 Unity 数学边界。88 项断言覆盖精确主题/事件匹配、机会与鱼干数量、净空与逃生道、速度/滑翔时序、无效输入、确定性、暂停、取消、重开、新手抑制及提示文案；不是 Unity 物理或画面测试。
- tools/VerifyThemeRouteEncounters.ps1：静态检查真实生成入口、拥有权清理、普通波次及机关互斥、Boss/主题/死亡/销毁生命周期、待拾取滑翔羽毛时序预算、HUD 层级、路边地标完整轮廓净空和无碰撞体约束；不代替运行时验收。
- 新增编辑器菜单 Tools > Penguin Run > Validation > Theme Route Encounters (Play Mode)：在隔离的附加场景中检查真实 spawner 的四种选择、障碍规格、暂停、取消、复用及旧事件回退，并恢复场景和随机状态。已编译，尚未执行。

最终实测结果：

| 检查 | 结果 |
| --- | --- |
| 主题路线独立行为断言 | 88 项通过 |
| 原碰撞计分 / 片段轮转回归 | 21 组通过 |
| 原输入行为回归 | 18 组通过 |
| 全量 Roslyn 编译 | 199 个源文件通过；不等价于 Unity 编译或运行通过 |
| 全部 Verify*.ps1 | 21/22 通过；唯一失败为下述既有文件行数限制 |

每个 Add-Type 测试必须使用新的 pwsh 进程，避免重复的生产类型定义。运行命令：

    pwsh -NoProfile -File tools/TestThemeRouteEncounters.ps1
    pwsh -NoProfile -File tools/TestGameplayFairness.ps1
    pwsh -NoProfile -File tools/TestRunnerInput.ps1
    pwsh -NoProfile -File tools/CompileFullRoslyn.ps1
    pwsh -NoProfile -File tools/VerifyThemeRouteEncounters.ps1
    git diff --check

唯一静态失败：BossEncounter.cs 在 HEAD 与工作区均为 1,947 行，超出原有 1,900 行上限 47 行；本轮未编辑该文件或提高上限。抽取 partial 后，SegmentSpawner.cs 为 2,259/2,260 行，RunnerGameController.cs 为 1,150/1,150 行，相应上限已下调。原有路线深度门禁现在读取新增 partial，保留原断言。

RED → GREEN 记录：缺失目录和初版 12 项集成连接先失败；随后补充的提示、待拾取滑翔羽毛、销毁清理、Boss 前奏互斥和地标完整轮廓断言也先失败再修复。独立审查代理因客户端上下文格式错误未能启动，改为本地规格/质量复核，不声称完成独立代理审查。

运行时限制：本轮重新启动 Unity 6000.4.5f1 批处理，原输入/暂停验证在加载前因无有效 Editor 许可证退出 198（_tmp/theme-route-unity.log）。未修改许可或存档。新增真实 spawner 验证、Boss 模式 HUD 画面、路线连续动作手感与 Android 性能仍待验证；没有新的截图或实机通过证据。

激活许可后的验收重点：四种路线选择、三次动作可恢复性（含滑翔与冲刺）、换道逃生、普通片段恢复间隔、暂停/死亡/重开清理、Boss 前奏与主题切换抢占、HUD 不遮挡、地标完整轮廓不侵入赛道。已有测试入口不能替代这些人工画面与设备检查。

## 相关文档

- [CONTEXT.md](../CONTEXT.md) — 术语、领域规则与工程约束（**改动前先读**）
- [CURRENT_DESIGN.md](CURRENT_DESIGN.md) — 当前设计基准与逐批次记录
- [DEVELOPMENT_GUIDE.md](DEVELOPMENT_GUIDE.md) — 上手指南

## 营地升级时长预览对齐（2026-10-28）

新增 `tools/TestCampUpgradePreview.ps1`：独立编译真实存档、营地升级、跑酷调参映射和默认配置，仅用轻量边界替代 Unity 数学/属性、内存 PlayerPrefs 及无关战绩历史。74 项覆盖逐级时长、道具率百分点、三项旧时长读取器、奖励倍率未变、文案、等级边界、读操作无存档写入以及购买成本/满级约束。修复前编译成功但 54 项行为断言因数值漂移/多效果文案缺失而失败；修复后全数通过。替代默认值在测试进程内存中改写，不触碰项目配置文件。

新增 `tools/VerifyCampUpgradePreview.ps1`：静态检查预览默认配置源、`RunnerTuning` 字段映射、多行预览布局，并扫描全部 shipped `.unity` / `.prefab` 的 `PenguinRunnerGame` 组件和调参属性覆盖。

执行结果：

| 检查 | 结果 |
|---|---|
| 默认配置行为回归 | 74 项通过 |
| 替代默认调参行为回归 | 74 项通过 |
| 默认源 / 场景覆盖静态门禁 | 通过；扫描到 1 个跑酷场景组件，未发现上述字段覆盖 |

运行命令：

```powershell
pwsh -NoProfile -File tools/TestCampUpgradePreview.ps1
pwsh -NoProfile -File tools/TestCampUpgradePreview.ps1 -AlternateDefaults
pwsh -NoProfile -File tools/VerifyCampUpgradePreview.ps1
```

最终验证：默认值与替代默认值行为回归各 74 项通过；碰撞/片段 21 组、输入 18 组、主题路线 88 组、主题连续性 7 组、主题精通结算 7 组均通过；全量 Roslyn 205 个源文件编译通过。25 个 `Verify*.ps1` 中 24 个通过；唯一失败是既有 `VerifyFileSizeRatchet.ps1`：`BossEncounter.cs` 为 1,947/1,900 行，超限 47 行，本批次未修改该文件或提高上限。`git diff --check` 无错误（仅有仓库既存的 LF/CRLF 提示）。

本批次未运行 Unity Editor 批处理或 Android 实机。Roslyn、静态门禁和独立回归不等价于 Unity 实际编译、营地卡片画面或设备验收；本机此前 Unity 批处理受无有效 Editor 许可证限制，须在许可证恢复后验证多行布局与真实屏幕适配。

## M1 G5 礼盒购买行为复核（2026-10-28）

新增 `tools/TestShopBundlePurchase.ps1`，在独立 PowerShell 7 进程中编译真实 `ShopCatalog`、`ShopStore`、`PlayerSave` 和难度定义。仅替换 Unity 数学/颜色、内存 PlayerPrefs 与无关战绩边界，不接触玩家真实存档。内存存储区分工作副本与 Save 快照，以覆盖调用 Flush 后的重载；不代表操作系统、崩溃恢复或 Unity 持久化已验收。

33 项场景覆盖三个礼盒的目录/价格、首次购买、精确余额、库存叠加、pending 不被购买消耗、一次性领取、内存重载、余额不足、零/负价格、跨礼包状态隔离、已拥有围巾与默认装扮保持，以及礼包说明对应奖励。修复前实测：32 项通过，1 项失败——极地礼包“各类券 ×2”未明确列出两类实际奖励，且暗示了未发放的幸运起手令。极光礼包三类券各 3 张的实际发放回归通过。

用户随后确认“只明确列出两类券，维持奖励和价格不变”。本次仅修改 `ShopCatalog` 的极地礼盒说明；不补发幸运起手令，不改 `ShopStore`、`PlayerSave` 或其他礼包。保留原失败断言，不通过放宽测试来获得通过结果。

运行命令：

```powershell
pwsh -NoProfile -File tools/TestShopBundlePurchase.ps1
```

RED → GREEN：修复前退出码为 1，输出 `FAILED: 1/33 bundle purchase scenarios.`；修改目录说明后退出码为 0，输出 `PASS: 33 bundle purchase scenarios (in-memory storage only).`。这是说明修正，不是经济调整。测试使用真实生产交易及存档逻辑，但不等同于商店 UI、Unity 平台持久化或 Android 设备通过。

本次复验结果：

| 检查 | 结果 |
|---|---|
| 礼盒购买独立回归 | 33 项通过 |
| 营地预览默认 / 替代调参回归 | 各 74 项通过 |
| 全量 Roslyn | 205 个源文件通过 |
| 全部 `Verify*.ps1` | 24/25 通过；M1 说明/效果门禁通过 |

唯一静态失败仍为既有 `BossEncounter.cs` 1,947 行超出 1,900 行上限 47 行，本次未编辑或放宽门槛。Unity 批处理和 Android 实机未运行，说明新增字数的实际换行仍需页面验收；不能据此将 M1 整体标为完成。

收尾检查：`git diff --check` 通过；本批次修改前后 `ShopStore.cs` 和 `PlayerSave.cs` 的 SHA256 一致，发奖与存档代码未变。当前分支和全部未提交修改均保留，无提交、合并或推送。

## 大厅奖励说明真实化（2026-10-30）

新增 `tools/TestLobbyRewardSummary.ps1` 与 `tools/tests/LobbyRewardSummaryRegression.cs`。独立进程只编译真实 `MissionModels`、`LobbyRewardSummary` 和回归代码，不替换生产聚合逻辑、不连接 PlayerPrefs，也不触碰真实玩家记录。输入为内存中的明确合成任务状态；涉及真实任务时直接引用目录但不修改目录。

27 项场景覆盖空输入、默认摘要、未完成/已领取任务、目录中每个任务的真实金额、每日与成就混合、领取后重新计算、摘要快照不可变、重复读取不改输入、合成金额变化、负数/零奖励、大额合计和单次枚举，以及两条中性提示。金额变化用新建合成定义完成，不改真实任务配置。

RED → GREEN：先添加测试与门禁，行为入口因尚无生产摘要明确退出 1；静态门禁输出 21 项缺失/旧文案契约失败，其中包括原固定 80、首胜双倍、每日双倍与未使用 +50 赋值。添加生产摘要并完成接线后，27 项行为回归与静态门禁退出 0。初始缺少组件是入口失败，不等同于在旧代码上执行过全部 27 项行为用例。

新门禁 `tools/VerifyLobbyRewardTruth.ps1` 使用 ASCII 源码，匹配前剥离注释。它锁住双任务轨道快照、同一摘要的数量/金额绑定、旅程与每日提示解耦、摘要不调用存档/交易系统、无依据奖励承诺清理，以及真实签到和双倍券库存预览仍存在。错误文案检查限定于大厅，不禁止合法双倍道具。

```powershell
pwsh -NoProfile -File tools/TestLobbyRewardSummary.ps1
pwsh -NoProfile -File tools/VerifyLobbyRewardTruth.ps1
```

本次实际结果：

| 检查 | 结果 |
| --- | --- |
| 大厅奖励摘要 | 27 项通过 |
| 礼盒购买 | 33 项通过 |
| 营地预览默认 / 替代调参 | 各 74 项通过 |
| 碰撞/片段、输入、路线、主题连续性、精通结算 | 分别 21、18、88、7、7 项通过 |
| 全量 Roslyn | 206 个源文件通过 |
| 全部静态门禁 | 25/26 通过；新大厅奖励门禁通过 |
| 保护文件 SHA256 | 12 个文件与本批次修改前一致 |

保护文件包括任务定义/领取、PlayerSave、商店目录/购买、营地强化、结算/成长事务、两个大厅页面布局、BossEncounter 和原行数棘轮。它们在工作区中可能已经包含用户先前的未提交改动；本轮保留其字节不变。唯一静态失败仍是 BossEncounter 的 1,947/1,900 行限制，本次不改该文件、不提高阈值。MainMenuBootstrap 从 1,384 行减少为 1,376 行，低于 1,390 行上限。

本批次未运行 Unity Editor 或 Android。Roslyn、纯逻辑与静态检查不能证明实际页面换行、任务领取后返回大厅刷新或平台编译已经通过。人工验收须检查：零/单个/多个可领任务摘要、领取后摘要更新、普通旅程与每日挑战提示、签到及真实消耗券说明保持，以及横屏 16:9 和设备显示。历史许可限制并不作为本轮新执行过 Unity 的证据。

最终本地自审逐项核对规格、生产差异、新测试/门禁与记录，未委派独立代理。收尾再次执行摘要回归、全量 Roslyn 与全部静态门禁，结果仍为 27 项通过、206 个源文件通过、25/26 门禁通过；12 个保护文件 SHA256 仍一致。`git diff --check` 通过（仅既有 LF/CRLF 提示），另检查 6 个新增文件无行尾空白/多余末尾空行、两个新 PowerShell 脚本纯 ASCII、Unity GUID 唯一。与原 96 条状态基线相比仅增加本批次 7 条状态项，未移除原状态项；新回归文件包含在原已未跟踪的 `tools/tests/` 目录内。实现改动保留在当前分支，未暂存、提交、合并或推送。
