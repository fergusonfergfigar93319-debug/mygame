# 可选插件集成指南

项目本身不依赖任何第三方插件即可完整编译运行。以下两个插件可按需安装以增强动画和音频系统。

---

## 1. DOTween (Free) — 高性能补间动画库

### 安装方式（二选一）

**方式 A：Asset Store（推荐）**
1. Unity 菜单 → `Window` → `Asset Store`
2. 搜索 "DOTween" (Demigiant)
3. 点击 `Add to My Assets` → `Open in Unity` → `Download` → `Import`
4. 导入完成后，菜单 → `Tools` → `Demigiant` → `DOTween Utility Panel`
5. 点击 `Setup DOTween...` 等待编译

**方式 B：官网下载**
1. 访问 [DOTween 官网](http://dotween.demigiant.com/download.php)
2. 下载免费版 `.unitypackage`
3. 双击导入项目
4. 同上执行 DOTween Setup

### 安装后的效果
- 项目中的 `TweenExtensions.cs` 和 `TweenSetup.cs` 会自动激活（`#if DOTWEEN` 编译符号由 DOTween Setup 自动添加）
- 相机震动从手写逻辑切换到 `DOShakePosition()`
- 按钮弹跳从 `ClickScaleTween` 切换到 `DOPunchScale()`
- 可以逐步替换 `MainMenuBootstrap.cs` 中的 7 个手写 Tween 类

### 未安装时
- 代码中的 `#if DOTWEEN` 块被编译器跳过
- 所有动画回退到项目自带的轻量实现
- 编译和运行完全不受影响

---

## 2. JSAM (Free / Open Source) — 音频管理器

### 安装方式

**通过 Git URL（UPM）：**
1. Unity 菜单 → `Window` → `Package Manager`
2. 点击左上角 `+` → `Add package from git URL`
3. 输入: `https://github.com/jackyyang09/Simple-Unity-Audio-Manager.git`
4. 等待下载和编译

**注意**: 该包的 UPM 包名为 `com.brogrammist.jsam`（Git URL 会自动解析）。

### 安装后的效果
- 类型安全的枚举播放：`AudioManager.PlaySound(Sounds.Jump)` 替代字符串查找
- AudioSource 对象池自动复用
- BGM / SFX / Voice 独立音量分组
- 可以逐步替换 `RunnerAudio.cs` 的手动 AudioSource 管理

---

## 当前状态

`Packages/manifest.json` 已清理，不包含任何第三方依赖。项目使用 Unity 内置模块即可完整运行。

已创建的集成代码文件：
| 文件 | 状态 |
|------|------|
| `TweenExtensions.cs` | 就绪 — 安装 DOTween 后自动激活 |
| `TweenSetup.cs` | 就绪 — 安装 DOTween 后自动激活 |
| `PenguinRunnerGame.cs` (相机震动部分) | 就绪 — `#if DOTWEEN` 双路径 |
| `CutePenguinMesh.cs` | 就绪 — 纯 Unity API，无外部依赖 |
