# 地图精细模型第一批交付

状态：已完成并接入运行时（2026-07-15）。

## 交付范围

- 六套主题分别拥有 3 类核心障碍：高障碍、滑铲门、宽墙，共 18 个主题障碍预制体。
- 6 类可收集物与道具：鱼干、冲刺、磁铁、护盾、得分星、滑翔羽毛。
- 六套主题分别拥有地标、路牌、灯具、路缘装饰，共 24 个环境预制体。
- 合计 48 个独立预制体；每个预制体都包含可渲染的 LOD0 与 LOD1。
- 模型仅替换视觉层，`ObstacleColliderSpec` 与现有碰撞、躲避方式、对象池规则保持不变。

## 主题识别

| 主题 | 障碍与环境造型语言 |
|---|---|
| 冰湖回音 | 冰晶、冻裂墙、冰灯、方向冰标 |
| 雪松废墟 | 原木拒马、木梁、雪顶路牌、营灯 |
| 极光磁场 | 磁环、导电杆、发光核心、机械信标 |
| 北境雾堤 | 雾石、卵石墙、铜色束带、堤岸灯 |
| 海洋珊瑚礁 | 珊瑚枝、贝壳阵列、海蓝灯芯、粉色支架 |
| 天空飞翔 | 帆布、绳索、木箱墙、风向信标 |

## 自动验证

Unity 6.0.4 验证结果：

- 48/48 预制体可加载并实例化。
- LOD0：469 个独立渲染部件，83,904 三角面。
- LOD1：251 个独立渲染部件，44,748 三角面。
- 无视觉碰撞体、无缺失材质、无错误 Shader、无空边界。
- 缺失资源只警告一次并回退程序化外观。
- 对象池从冲刺道具切换到护盾后，不残留旧模型。
- 六张主题预览会检测非背景像素；使用 `-nographics` 误生成空白图时会直接失败。

验证入口：

```powershell
./tools/VerifyCustomMapAssetsBatch1.ps1

& 'C:\Program Files\Unity\Hub\Editor\6000.4.5f1\Editor\Unity.exe' `
  -batchmode -nographics -quit `
  -projectPath 'C:\Users\dxh53\Desktop\mygame' `
  -executeMethod PenguinRun.Editor.ProjectVisualAssetValidation.ValidateFromCommandLine
```

视觉预览位于 `Artifacts/VisualPreviews/`。捕获预览时必须启用图形设备，不要传入 `-nographics`。
