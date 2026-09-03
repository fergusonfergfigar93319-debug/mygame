# 验证体系

本项目靠一套门禁（gate）保持质量，而不是靠人工回归。每批改动落地时会**新增**锁住该批次契约的门禁，因此门禁数量只增不减。

改完代码至少跑「快速回合」；碰了美术、音频、UI 或性能相关的部分，再跑对应的 Unity 校验器。

---

## 快速回合

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

## 相关文档

- [CONTEXT.md](../CONTEXT.md) — 术语、领域规则与工程约束（**改动前先读**）
- [CURRENT_DESIGN.md](CURRENT_DESIGN.md) — 当前设计基准与逐批次记录
- [DEVELOPMENT_GUIDE.md](DEVELOPMENT_GUIDE.md) — 上手指南
