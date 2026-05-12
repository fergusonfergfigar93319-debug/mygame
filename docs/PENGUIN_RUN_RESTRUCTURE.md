# 企鹅快跑重构方案

## 参考结论

结合 Subway Surfers、Temple Run 等移动跑酷游戏，可以抽象出几条稳定模式：

- 自动前进是核心，玩家主要处理跳跃、下滑、路线微调、道具时机，而不是持续按住移动。
- 地图以片段池拼接，障碍必须可读、可预判，难度通过速度、障碍密度和组合复杂度逐步提升。
- 道具以短反馈为主，磁吸、护盾、冲刺、倍率、救援都服务于“这一局跑得更远”。
- 复玩来自每日挑战、任务、排行榜、角色外观和能力成长，而不是单关卡反复试错。
- 失败后要能短链路重开，结算清楚展示距离、收集、动作、倍率和新纪录。

参考来源：

- [Subway Surfers 玩法概览](https://en.wikipedia.org/wiki/Subway_Surfers)
- [Temple Run 玩法概览](https://en.wikipedia.org/wiki/Temple_Run)
- [Game Developer 跑酷进程设计分析](https://www.gamedeveloper.com/design/studying-gameplay-progression-on-runners)

## 新定位

项目名：企鹅快跑

一句话：咕咕嘎嘎在极夜冰原上自动向前冲刺，玩家通过跳跃、下滑、伙伴支援和道具时机，躲避高松鹅布下的冰裂、雪兽、矮桥和风暴陷阱。

## 核心循环

1. 首页点击“开始快跑”。
2. 角色自动奔跑，速度随时间和片段难度提升。
3. 玩家用跳跃、下滑和支援处理障碍，沿途收集鱼干、信标和剧情残页。
4. 撞击或掉落后结算距离、收集、动作分和倍率。
5. 鱼干进入营地升级，排行榜记录本地和今日挑战成绩。

## 已落地进度

- 应用名已改为“企鹅快跑”。
- 首页主入口已默认进入无尽跑酷模式，底部主按钮和推荐卡文案已改为快跑语境。
- 无尽模式已改为自动向前跑，玩家不再需要同时按住左右移动和跳跃。
- 局内控制已收敛为“跳跃”和“下滑”，团团支援保留为辅助按钮。
- 下滑已接入低姿态碰撞盒，可以钻过新增的低矮冰拱障碍。
- 无尽片段池已开始投放低矮冰拱，为后续“跳跃/下滑组合段”打基础。

## 下一阶段

- 增加手势输入：上滑跳跃、下滑滑铲、左右滑微调路线，按钮保留为辅助模式。
- 将 `EndlessSegmentPool` 拆成教学、普通、困难、奖励、Boss 压迫五类片段。
- 为每个片段标注输入需求，避免生成“不可解”的障碍组合。
- 建立首局教学流程：先单跳，再下滑，再跳跃接下滑，最后进入自由随机段。
- 继续强化障碍可读性：提前预警、颜色区分、失败回放提示和更短重开链路。

## 已实施：Boss 系统重构

参考 Subway Surfers 的 Boss Chase、Temple Run 的追逐压力和 Crash Bandicoot: On the Run 的阶段高潮设计，已完成：

### 攻击判定时间优化

核心原则：**预警时间长而明确，伤害窗口短而精准**。

#### 判定时间结构
```
[攻击开始] -> [预警期] -> [危险窗口] -> [攻击结束]
   |            |           |            |
   |         0.4-1.0s     0.3-0.5s      总计1.5-2.5s
   |                        |
   |                    判定玩家受伤
```

#### 各难度参数
| 难度 | 预警比例 | 预警圈显示 | 伤害窗口 | 破绽概率 |
|------|----------|------------|----------|----------|
| Easy | 65% | 0.88 | 0.35s | 72% |
| Normal | 55% | 0.72 | 0.38s | 55% |
| Hard | 48% | 0.65 | 0.35s | 42% |
| Expert | 42% | 0.55 | 0.32s | 35% |

#### Boss 特色判定调整
| Boss | 预警乘数 | 伤害窗口 | 说明 |
|------|----------|----------|------|
| 冰霜雪王 | 1.15x | 0.40s | 新手友好，更长预警 |
| 雪松哨兵 | 1.0x | 0.38s | 标准 |
| 极光长蛇 | 0.88x | 0.35s | 配合颜色轨迹 |
| 雾堤守卫 | 1.0x | 0.38s | 依靠真假预警机制 |
| 珊瑚海怪 | 1.0x | 0.38s | 标准 |
| 雷云苍鹰 | 0.82x | 0.32s | 高速，更短预警 |

#### 招式差异化
| 招式 | 伤害窗口 | 说明 |
|------|----------|------|
| SweepLow 横扫 | 0.35s | 短窗口，快速反应 |
| DiveHigh 俯冲 | 0.42s | 稍长，给跳跃时机 |
| ChargeAcross 冲锋 | 0.38s | 中等 |
| RangedSalvo 齐射 | 0.32s | 分散投射物，短窗口 |
| CenterBeam 光束 | 0.45s | 持续型，稍长 |
| QuakePulse 震波 | 0.35s | 短窗口 |

#### 新增调试接口
- `GetAttackProgress01()` - 攻击进度 0-1
- `GetDangerWindow()` - 危险窗口起止时间
- `GetTelegraphTimeRemaining()` - 预警剩余时间


### Boss 身份系统

新增 `BossArchetype` 枚举定义 6 种定位：
- `Balanced` - 均衡型（冰霜雪王）
- `Grounded` - 地面型（雪松哨兵）
- `Evasive` - 机动型（极光长蛇）
- `Deceptive` - 诡诈型（雾堤守卫）
- `Defensive` - 防御型（珊瑚海怪）
- `Aerial` - 空战型（雷云苍鹰）

每个 Boss 配置：
- `SignatureMechanic` - 主机制描述
- `RecommendedCounter` - 推荐反制动作
- `SignatureReward` - 专属图鉴奖励
- `PhaseIntensity` - 阶段节奏强度
- `PreferredPowerUps` - 推荐道具权重

### Boss 刷新条件系统

新增 `BossSpawnContext` 和 `BossSpawnCondition`：
- 按地图主题筛选（冰霜雪王在冰湖/雪松哨兵在废墟）
- 按距离条件（雾堤守卫 800m 后才出现）
- 按难度条件（雷云苍鹰需要 Hard 以上）
- 按前置 Boss 击败条件（击败雪松哨兵后才出现雾堤守卫）
- 按总击败数条件（击败 3 只 Boss 后才出现雷云苍鹰）

### Boss 阶段节奏

`BossEncounter` 新增阶段控制：
- `patternsSinceLastVulnerable` - 距离上次破绽已发出的招式数
- `MaxPatternsBeforeVulnerable` - 最多连续 4 招后强制破绽
- `currentPhaseCycle` - 当前攻击-破绽循环计数

### 镜头与反馈

`PenguinRunnerGame` 新增：
- `cameraShakeTimer/Intensity` - 震动时长和强度
- `defeatFreezeTimer` - 击败定格时间
- Boss 危险攻击时轻微震动（0.1s, 0.05 强度）
- Boss 被击败时定格 0.25s + 震动 0.5s

### 死亡复盘

`BossSystem` 新增：
- `lastHitPattern` - 最后击中玩家的招式
- `hitCountInCurrentFight` - 本战受击次数
- `GetDeathAnalysisHint()` - 根据被击败的招式返回具体建议

### 前奏与奖励片段

`SegmentSpawner` 新增：
- `SpawnBossPrelude()` - Boss 前 100m 生成补给、教学金币线、特色道具
- `SpawnBossReward()` - Boss 后生成鱼干大串、安全冲刺、主题道具
- `SpawnTutorialCoinLine()` - 根据 Boss 类型展示不同动作路线（跳跃/滑铲）
