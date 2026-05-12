using System.Collections.Generic;
using System.Text;
using PenguinRun.Game;
using UnityEngine;

namespace PenguinRun
{
    /// <summary>
    /// 游戏内 HUD：基于 IMGUI 的实时数据面板。
    /// 重构：分层卡片式布局、更好的间距、清晰的视觉层次、增益条带视觉化。
    /// </summary>
    internal sealed class RunnerHud
    {
        private GUIStyle scoreValueStyle;
        private GUIStyle scoreLabelStyle;
        private GUIStyle bestLabelStyle;
        private GUIStyle statValueStyle;
        private GUIStyle statLabelStyle;
        private GUIStyle buffStyle;
        private GUIStyle feedbackStyle;
        private GUIStyle centerStyle;
        private GUIStyle centerSubStyle;
        private GUIStyle backButtonStyle;
        private GUIStyle modeChipStyle;
        private Font hudFont;

        // ── 设计系统颜色（与 UI Canvas 保持一致）─────────────────
        private static readonly Color ColorAccent = new(0.0f, 0.85f, 0.95f, 1f);
        private static readonly Color ColorGold = new(1f, 0.85f, 0.4f, 1f);
        private static readonly Color ColorSuccess = new(0.4f, 0.95f, 0.6f, 1f);
        private static readonly Color ColorDanger = new(1f, 0.35f, 0.45f, 1f);
        private static readonly Color ColorTextPrimary = new(0.98f, 0.99f, 1f, 1f);
        private static readonly Color ColorTextSecondary = new(0.78f, 0.86f, 0.94f, 0.9f);
        private static readonly Color ColorTextTertiary = new(0.6f, 0.72f, 0.84f, 0.7f);
        private static readonly Color ColorPanelBg = new(0.04f, 0.09f, 0.16f, 0.92f);
        private static readonly Color ColorPanelStroke = new(0.0f, 0.85f, 0.95f, 0.45f);
        private static readonly Color ColorSubPanelBg = new(0f, 0f, 0f, 0.28f);

        private static Texture2D pixel;

        private static Texture2D Pixel
        {
            get
            {
                if (pixel != null) return pixel;
                pixel = new Texture2D(1, 1, TextureFormat.RGBA32, false)
                {
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Point,
                };
                pixel.SetPixel(0, 0, Color.white);
                pixel.Apply();
                return pixel;
            }
        }

        private void EnsureGuiStyles()
        {
            if (scoreValueStyle != null) return;

            var font = ResolveHudFont();

            scoreValueStyle = new GUIStyle(GUI.skin.label)
            {
                font = font,
                fontSize = 44,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = ColorTextPrimary },
            };
            scoreLabelStyle = new GUIStyle(GUI.skin.label)
            {
                font = font,
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = ColorAccent },
            };
            bestLabelStyle = new GUIStyle(GUI.skin.label)
            {
                font = font,
                fontSize = 14,
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = ColorTextSecondary },
            };
            statValueStyle = new GUIStyle(GUI.skin.label)
            {
                font = font,
                fontSize = 22,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = ColorTextPrimary },
            };
            statLabelStyle = new GUIStyle(GUI.skin.label)
            {
                font = font,
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = ColorTextTertiary },
            };
            buffStyle = new GUIStyle(GUI.skin.label)
            {
                font = font,
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = new Color(0.82f, 0.94f, 1f, 0.98f) },
                wordWrap = true,
            };
            feedbackStyle = new GUIStyle(GUI.skin.label)
            {
                font = font,
                fontSize = 30,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white },
            };
            centerStyle = new GUIStyle(GUI.skin.label)
            {
                font = font,
                fontSize = 36,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white },
            };
            centerSubStyle = new GUIStyle(GUI.skin.label)
            {
                font = font,
                fontSize = 20,
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = ColorTextSecondary },
            };
            backButtonStyle = new GUIStyle(GUI.skin.button)
            {
                font = font,
                fontSize = 22,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = ColorTextPrimary, background = null },
                hover = { textColor = ColorAccent },
                active = { textColor = new Color(0.75f, 0.95f, 0.88f) },
                border = new RectOffset(0, 0, 0, 0),
                padding = new RectOffset(0, 0, 0, 0),
            };
            modeChipStyle = new GUIStyle(GUI.skin.label)
            {
                font = font,
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = ColorAccent },
            };
        }

        private Font ResolveHudFont()
        {
            if (hudFont != null) return hudFont;
            hudFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (hudFont == null)
            {
                hudFont = GUI.skin.font;
            }
            return hudFont;
        }

        public bool Draw(
            RunnerSessionConfig config,
            WorldDirector world,
            bool running,
            bool gameOver,
            int scoreOverride,
            string feedbackText,
            float feedbackTimer,
            RunnerMapTheme mapTheme,
            BossSystem bossSystem = null,
            bool paused = false)
        {
            EnsureGuiStyles();

            var buffLine = BuildBuffSummary(world);
            var hasBuff = !string.IsNullOrEmpty(buffLine);
            var hasComboLine = world.CoinCombo >= 2 || world.LastFishScoreMultiplier > 1.01f;

            // ── 主面板（左上）─────────────────────────────────
            var panelW = Mathf.Clamp(Screen.width * 0.4f, 320f, 460f);
            var panelH = 168f + (hasBuff ? 36f : 0f) + (hasComboLine ? 26f : 0f);
            var px = 16f;
            var py = 16f;

            DrawCard(new Rect(px, py, panelW, panelH), ColorPanelBg, ColorPanelStroke);

            // 顶部高光线
            DrawPanel(new Rect(px + 1f, py + 1f, panelW - 2f, 2f),
                new Color(ColorAccent.r, ColorAccent.g, ColorAccent.b, 0.5f));

            var ix = px + 18f;
            var iy = py + 14f;

            // 得分区域
            GUI.Label(new Rect(ix, iy, 100, 16), "得分", scoreLabelStyle);
            DrawTextShadow(new Rect(ix, iy + 16, 240, 50), scoreOverride.ToString("N0"), scoreValueStyle);

            // 生命值（右上）
            DrawHearts(new Rect(px + panelW - 130f, iy + 10f, 120f, 36f), world);

            iy += 70f;

            // 玩家信息条
            var nick = $"{Truncate(config.Nickname, 10)}  ·  最佳 {config.BestScore:N0}";
            GUI.Label(new Rect(ix, iy, panelW - 36f, 22f), nick, bestLabelStyle);
            iy += 26f;

            // 三栏统计
            var statW = (panelW - 48f) / 3f;
            DrawStatChip(new Rect(ix, iy, statW - 4f, 56f),
                "鱼干", world.Coins.ToString(), ColorGold);
            DrawStatChip(new Rect(ix + statW + 2f, iy, statW - 4f, 56f),
                "距离", $"{Mathf.RoundToInt(world.Distance)} m", ColorAccent);
            DrawStatChip(new Rect(ix + statW * 2f + 4f, iy, statW - 4f, 56f),
                "时长", $"{Mathf.RoundToInt(world.RunTime)} s", ColorSuccess);
            iy += 62f;

            // 模式 + 主题（chip 风格）
            var mode = config.Daily ? "每日挑战" : "无尽模式";
            var themeLabel = mapTheme switch
            {
                RunnerMapTheme.CedarRuins => "雪松废墟",
                RunnerMapTheme.AuroraField => "极光磁场",
                RunnerMapTheme.MistDike => "雾堤",
                RunnerMapTheme.OceanReef => "海洋珊瑚礁",
                RunnerMapTheme.SkyFlight => "天空飞翔",
                _ => "冰湖回音谷",
            };

            // 显示当前场景可能遭遇的 Boss（预告）
            var bossPreview = GetBossPreviewForTheme(mapTheme);
            var modeText = string.IsNullOrEmpty(bossPreview)
                ? $"{mode} · {themeLabel}"
                : $"{mode} · {themeLabel} · {bossPreview}";

            DrawChip(new Rect(ix, iy, panelW - 36f, 24f),
                modeText, new Color(ColorAccent.r, ColorAccent.g, ColorAccent.b, 0.18f), modeChipStyle);
            iy += 28f;

            // 增益条带（buff）
            if (hasBuff)
            {
                DrawSubPanel(new Rect(ix, iy, panelW - 36f, 30f),
                    new Color(0f, 0.4f, 0.5f, 0.25f));
                GUI.Label(new Rect(ix + 8f, iy, panelW - 50f, 28f), buffLine, buffStyle);
                iy += 34f;
            }

            // 连击行
            if (hasComboLine)
            {
                var combo = $"\u2605 连击 ×{world.CoinCombo}    倍率 ×{world.LastFishScoreMultiplier:0.00}";
                if (world.PeakCoinCombo >= 3)
                    combo += $"    峰值 ×{world.PeakCoinCombo}";
                var comboStyle = new GUIStyle(buffStyle) { normal = { textColor = ColorGold } };
                GUI.Label(new Rect(ix, iy, panelW - 36f, 24f), combo, comboStyle);
            }

            // 反馈文本（屏幕上方居中）
            if (feedbackTimer > 0f)
            {
                var alpha = Mathf.Clamp01(feedbackTimer / 0.45f);
                var isLongTip = feedbackText != null && feedbackText.Length > 20;
                var feedbackRenderStyle = new GUIStyle(feedbackStyle)
                {
                    fontSize = isLongTip ? 20 : 30,
                    wordWrap = true,
                    alignment = TextAnchor.MiddleCenter,
                };
                feedbackRenderStyle.normal.textColor = new Color(0.64f, 1f, 0.88f, alpha);

                // 反馈背景胶囊（长提示加宽，并下移到 Boss 面板下方，避免遮挡）
                var fbW = isLongTip
                    ? Mathf.Clamp(Screen.width * 0.6f, 460f, 920f)
                    : 360f;
                var fbX = (Screen.width - fbW) * 0.5f;
                var fbY = bossSystem != null && bossSystem.BossActive ? 154f : Screen.height * 0.18f;
                var textRect = new Rect(fbX + 12f, fbY + 8f, fbW - 24f, 120f);
                var textH = feedbackRenderStyle.CalcHeight(new GUIContent(feedbackText), textRect.width);
                var fbH = Mathf.Clamp(textH + 16f, 56f, 130f);

                DrawPanel(new Rect(fbX, fbY, fbW, fbH), new Color(0f, 0f, 0f, 0.5f * alpha));
                DrawPanel(new Rect(fbX + 1f, fbY + 1f, fbW - 2f, 2f),
                    new Color(ColorAccent.r, ColorAccent.g, ColorAccent.b, 0.6f * alpha));
                DrawTextShadow(new Rect(fbX + 8f, fbY + 4f, fbW - 16f, fbH - 8f), feedbackText, feedbackRenderStyle);
            }

            // 返回按钮（右上）
            var backR = new Rect(Screen.width - 140f, 18f, 120f, 56f);
            DrawCard(backR, new Color(0.06f, 0.13f, 0.22f, 0.9f), ColorPanelStroke);
            DrawPanel(new Rect(backR.x + 1f, backR.y + 1f, backR.width - 2f, 2f),
                new Color(ColorAccent.r, ColorAccent.g, ColorAccent.b, 0.5f));
            if (GUI.Button(backR, "\u2190 返回", backButtonStyle))
            {
                return true;
            }

            // ── BOSS 血条（屏幕顶部居中）─────────────────────────
            if (bossSystem != null && bossSystem.BossActive)
            {
                DrawBossPanel(bossSystem);
            }

            // ── 成就栏：boss 击败 / 完美闪避 / 鱼弹库存 ─────────────
            if (world.BossesDefeated > 0 || world.PerfectDodges > 0 || world.FishBombs > 0)
            {
                DrawAchievementBar(world);
            }

            // ── 暂停遮罩 ────────────────────────────────────────
            if (paused)
            {
                DrawPanel(new Rect(0, 0, Screen.width, Screen.height), new Color(0f, 0.04f, 0.08f, 0.6f));
                centerStyle.normal.textColor = ColorAccent;
                DrawTextShadow(new Rect(0, Screen.height * 0.4f, Screen.width, 60f), "\u23F8 暂停中", centerStyle);
                centerSubStyle.normal.textColor = ColorTextSecondary;
                GUI.Label(new Rect(0, Screen.height * 0.4f + 56f, Screen.width, 36f),
                    "按 P 继续 · 返回退出本局", centerSubStyle);
            }

            // 中央提示（未开始 / 游戏结束）
            if (!running)
            {
                var titleY = Screen.height * 0.42f;
                var subY = Screen.height * 0.42f + 56f;

                if (gameOver)
                {
                    centerStyle.normal.textColor = ColorDanger;
                    DrawTextShadow(new Rect(0, titleY, Screen.width, 60f), "游戏结束", centerStyle);
                    centerSubStyle.normal.textColor = ColorTextSecondary;
                    GUI.Label(new Rect(0, subY, Screen.width, 36f),
                        "点击屏幕重来  ·  按返回结算", centerSubStyle);
                }
                else
                {
                    centerStyle.normal.textColor = ColorAccent;
                    DrawTextShadow(new Rect(0, titleY, Screen.width, 60f), "点击屏幕开始", centerStyle);
                    centerSubStyle.normal.textColor = ColorTextSecondary;
                    GUI.Label(new Rect(0, subY, Screen.width, 36f),
                        "三条生命  ·  P 暂停  ·  屏幕边缘可结算", centerSubStyle);
                }
            }

            return false;
        }

        private void DrawBossPanel(BossSystem bossSystem)
        {
            var boss = bossSystem.CurrentBoss;
            if (boss == null) return;

            var w = Mathf.Clamp(Screen.width * 0.5f, 420f, 700f);
            var x = (Screen.width - w) * 0.5f;
            var y = 18f;
            var h = 132f;
            DrawCard(new Rect(x, y, w, h), new Color(0.12f, 0.05f, 0.08f, 0.92f), new Color(1f, 0.4f, 0.45f, 0.65f));
            DrawPanel(new Rect(x + 1f, y + 1f, w - 2f, 2.5f), new Color(1f, 0.5f, 0.4f, 0.85f));

            var bossNameStyle = new GUIStyle(scoreLabelStyle)
            {
                fontSize = 16,
                normal = { textColor = new Color(1f, 0.78f, 0.4f) },
                alignment = TextAnchor.MiddleCenter,
            };
            var rushPrefix = bossSystem.IsBossRushMode ? $"BOSS连战 第{bossSystem.CurrentBossRound}战 · " : "";
            GUI.Label(new Rect(x, y + 8f, w, 22f), $"\u26A0 {rushPrefix}{boss.Definition.DisplayName}", bossNameStyle);

            var barX = x + 28f;
            var barY = y + 36f;
            var barW = w - 56f;
            var barH = 18f;
            DrawPanel(new Rect(barX, barY, barW, barH), new Color(0.04f, 0.06f, 0.1f, 0.95f));
            DrawPanelBorder(new Rect(barX, barY, barW, barH), new Color(1f, 0.4f, 0.4f, 0.6f), 1f);

            var hpFrac = (float)boss.HitsRemaining / Mathf.Max(1, BossSystem.BossMaxHits);
            var fillW = barW * hpFrac;
            DrawPanel(new Rect(barX, barY, fillW, barH),
                new Color(1f, 0.42f - 0.15f * (1f - hpFrac), 0.32f, 0.95f));
            for (var i = 1; i < BossSystem.BossMaxHits; i++)
            {
                var sx = barX + barW * i / BossSystem.BossMaxHits;
                DrawPanel(new Rect(sx - 0.5f, barY, 1f, barH), new Color(0f, 0f, 0f, 0.7f));
            }

            var hpStyle = new GUIStyle(statValueStyle) { fontSize = 14, alignment = TextAnchor.MiddleCenter };
            DrawTextShadow(new Rect(barX, barY, barW, barH), $"血量 {boss.HitsRemaining}/{BossSystem.BossMaxHits}", hpStyle);

            // 阶段化节奏提示：显示当前循环和破绽预警
            string phaseLabel;
            Color phaseColor;
            switch (boss.Phase)
            {
                case BossPhase.Spawning:
                    phaseLabel = "BOSS 浮现中…";
                    phaseColor = ColorTextSecondary;
                    break;
                case BossPhase.Active:
                    var untilVuln = bossSystem.CurrentBossPatternsUntilVulnerable;
                    var cycle = bossSystem.CurrentBossPhaseCycle + 1;
                    if (bossSystem.CurrentBossInAttackInterval)
                    {
                        var nextAttackIn = bossSystem.CurrentBossAttackIntervalLeft;
                        // 改进：显示危险窗口倒计时
                        phaseLabel = $"第{cycle}轮 · 危险窗口 {nextAttackIn:0.0}s";
                    }
                    else
                    {
                        // 根据距离破绽的招式数显示不同颜色预警
                        phaseLabel = untilVuln <= 1
                            ? $"第{cycle}轮 · \u26A0 破绽即将出现！"
                            : $"第{cycle}轮 · 观察招式 ({untilVuln}招后破绽)";
                    }
                    phaseColor = untilVuln <= 1 ? new Color(1f, 0.7f, 0.4f) : ColorTextSecondary;
                    break;
                case BossPhase.Vulnerable:
                    var vulnLeft = bossSystem.CurrentBossPhaseElapsed;
                    var vulnDuration = 1.6f; // BossSystem.VulnerableSeconds
                    var vulnRemaining = Mathf.Max(0, vulnDuration - vulnLeft);
                    phaseLabel = $"\u2605 破绽窗口剩余 {vulnRemaining:0.0}s · 冲刺/护盾撞击";
                    phaseColor = new Color(0.4f, 1f, 0.6f);
                    break;
                case BossPhase.Retreating:
                    phaseLabel = "击败！BOSS 退场中";
                    phaseColor = new Color(1f, 0.85f, 0.4f);
                    break;
                default:
                    phaseLabel = "";
                    phaseColor = ColorTextSecondary;
                    break;
            }
            var phaseStyle = new GUIStyle(bestLabelStyle)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = phaseColor },
            };
            GUI.Label(new Rect(x, y + 58f, w, 20f), phaseLabel, phaseStyle);

            var rewardLeft = bossSystem.CurrentBossSpeedRewardTimeLeft;
            var rewardWindow = Mathf.Max(0.1f, bossSystem.CurrentBossSpeedRewardWindow);
            var rewardFrac = rewardLeft / rewardWindow;
            var rewardStyle = new GUIStyle(bestLabelStyle)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.Lerp(new Color(1f, 0.6f, 0.45f), new Color(0.55f, 1f, 0.76f), rewardFrac) },
            };
            GUI.Label(new Rect(x, y + 74f, w, 18f),
                $"速杀奖励窗口 {rewardLeft:0.0}s  ·  最多 +{bossSystem.CurrentBossMaxSpeedFishReward}鱼干 +{bossSystem.CurrentBossMaxSpeedScoreReward}分",
                rewardStyle);

            if (bossSystem.CurrentBossInAttackInterval)
            {
                var recBarX = x + 34f;
                var recBarY = y + 92f;
                var recBarW = w - 68f;
                var recBarH = 8f;
                var intervalDuration = Mathf.Max(0.01f, bossSystem.CurrentBossAttackIntervalDuration);
                var intervalLeft = Mathf.Clamp(bossSystem.CurrentBossAttackIntervalLeft, 0f, intervalDuration);
                var frac = 1f - intervalLeft / intervalDuration;
                DrawPanel(new Rect(recBarX, recBarY, recBarW, recBarH), new Color(0.03f, 0.06f, 0.1f, 0.9f));
                DrawPanelBorder(new Rect(recBarX, recBarY, recBarW, recBarH), new Color(0.25f, 0.7f, 0.95f, 0.5f), 1f);
                DrawPanel(new Rect(recBarX + 1f, recBarY + 1f, (recBarW - 2f) * frac, recBarH - 2f), new Color(0.3f, 0.95f, 1f, 0.92f));
            }

            var patternLabel = bossSystem.CurrentBossPatternLabel;
            var dodgeHint = bossSystem.CurrentBossDodgeHint;
            var counterHint = bossSystem.CurrentBossCounterHint;
            var showGuidance = bossSystem.GuidanceEnabledForCurrentBoss;
            var assistBaseY = bossSystem.CurrentBossInAttackInterval ? y + 102f : y + 92f;
            var assistStyle = new GUIStyle(bestLabelStyle)
            {
                fontSize = 13,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.86f, 0.93f, 1f, 0.92f) },
            };
            if (showGuidance && (!string.IsNullOrEmpty(patternLabel) || !string.IsNullOrEmpty(dodgeHint)))
            {
                var text = string.IsNullOrEmpty(patternLabel)
                    ? dodgeHint
                    : $"{patternLabel} · {dodgeHint}";
                GUI.Label(new Rect(x, assistBaseY, w, 18f), text, assistStyle);
            }

            if (showGuidance && !string.IsNullOrEmpty(counterHint))
            {
                var counterStyle = new GUIStyle(assistStyle)
                {
                    normal = { textColor = boss.Phase == BossPhase.Vulnerable ? new Color(0.6f, 1f, 0.72f) : new Color(0.8f, 0.9f, 1f, 0.8f) },
                };
                GUI.Label(new Rect(x, assistBaseY + 16f, w, 18f), counterHint, counterStyle);
            }
        }

        private void DrawAchievementBar(WorldDirector world)
        {
            var x = 16f;
            var y = (float)Screen.height - 70f;
            var w = 380f;
            var h = 54f;
            DrawCard(new Rect(x, y, w, h), ColorPanelBg, ColorPanelStroke);
            DrawPanel(new Rect(x + 1f, y + 1f, w - 2f, 2f),
                new Color(ColorAccent.r, ColorAccent.g, ColorAccent.b, 0.5f));

            var slot = (w - 24f) / 3f;
            var bossStyle = new GUIStyle(buffStyle) { alignment = TextAnchor.MiddleCenter, fontSize = 15 };
            bossStyle.normal.textColor = new Color(1f, 0.75f, 0.4f);
            GUI.Label(new Rect(x + 12f, y + 8f, slot, 38f),
                $"首领击败\n{world.BossesDefeated}", bossStyle);

            var dodgeStyle = new GUIStyle(buffStyle) { alignment = TextAnchor.MiddleCenter, fontSize = 15 };
            dodgeStyle.normal.textColor = new Color(0.5f, 1f, 0.7f);
            GUI.Label(new Rect(x + 12f + slot, y + 8f, slot, 38f),
                $"\u26A1 完美闪避\n{world.PerfectDodges}", dodgeStyle);

            var bombStyle = new GUIStyle(buffStyle) { alignment = TextAnchor.MiddleCenter, fontSize = 15 };
            bombStyle.normal.textColor = new Color(1f, 0.55f, 0.4f);
            GUI.Label(new Rect(x + 12f + slot * 2f, y + 8f, slot, 38f),
                $"\u2734 鱼弹\n{world.FishBombs}", bombStyle);
        }

        private static string BuildBuffSummary(WorldDirector w)
        {
            var parts = new List<string>();
            if (w.DashTimer > 0.06f) parts.Add($"\u279E {w.DashTimer:0.#}s");
            if (w.MagnetTimer > 0.06f) parts.Add($"\u269B {w.MagnetTimer:0.#}s");
            if (w.ShieldTimer > 0.06f) parts.Add($"\u2748 {w.ShieldTimer:0.#}s");
            if (w.ScoreBoostTimer > 0.06f) parts.Add($"\u2605 {w.ScoreBoostTimer:0.#}s");
            if (w.GlideTimer > 0.06f) parts.Add($"\u2197 {w.GlideTimer:0.#}s");
            if (w.DoubleFishTimer > 0.06f) parts.Add($"\u00D72 {w.DoubleFishTimer:0.#}s");
            if (w.SlowMoTimer > 0.06f) parts.Add($"\u23F1 {w.SlowMoTimer:0.#}s");

            if (parts.Count == 0) return null;

            var sb = new StringBuilder();
            sb.Append("增益  ");
            for (var i = 0; i < parts.Count; i++)
            {
                if (i > 0) sb.Append("   ");
                sb.Append(parts[i]);
            }

            return sb.ToString();
        }

        /// <summary>带阴影的卡片：圆角伪装通过双层填充 + 边框线模拟。</summary>
        private static void DrawCard(Rect r, Color fill, Color stroke)
        {
            // 阴影
            DrawPanel(new Rect(r.x + 2f, r.y + 4f, r.width, r.height), new Color(0f, 0f, 0f, 0.45f));
            DrawPanel(r, fill);
            DrawPanelBorder(r, stroke, 1.5f);
        }

        private void DrawStatChip(Rect area, string label, string value, Color accentColor)
        {
            DrawSubPanel(area, ColorSubPanelBg);
            // 顶部彩色标识条
            DrawPanel(new Rect(area.x, area.y, area.width, 2f),
                new Color(accentColor.r, accentColor.g, accentColor.b, 0.7f));

            var labelStyle = new GUIStyle(statLabelStyle) { normal = { textColor = accentColor } };
            GUI.Label(new Rect(area.x, area.y + 8f, area.width, 18f), label, labelStyle);
            DrawTextShadow(new Rect(area.x, area.y + 26f, area.width, 28f), value, statValueStyle);
        }

        private static void DrawChip(Rect area, string text, Color bg, GUIStyle style)
        {
            DrawSubPanel(area, bg);
            GUI.Label(area, text, style);
        }

        private void DrawHearts(Rect area, WorldDirector world)
        {
            var heartStyle = new GUIStyle(GUI.skin.label)
            {
                font = ResolveHudFont(),
                fontSize = 26,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
            };
            var x = area.x;
            for (var i = 0; i < world.MaxLives; i++)
            {
                heartStyle.normal.textColor =
                    i < world.Lives ? ColorDanger : new Color(0.25f, 0.32f, 0.42f, 0.6f);
                GUI.Label(new Rect(x, area.y, 30f, 34f), "\u2665", heartStyle);
                x += 30f;
            }
        }

        private static void DrawTextShadow(Rect r, string text, GUIStyle style)
        {
            var c = style.normal.textColor;
            style.normal.textColor = new Color(0f, 0f, 0f, c.a * 0.6f);
            GUI.Label(new Rect(r.x + 1.5f, r.y + 1.5f, r.width, r.height), text, style);
            style.normal.textColor = c;
            GUI.Label(r, text, style);
        }

        private static void DrawPanel(Rect r, Color fill)
        {
            var prev = GUI.color;
            GUI.color = fill;
            GUI.DrawTexture(r, Pixel);
            GUI.color = prev;
        }

        private static void DrawSubPanel(Rect r, Color fill)
        {
            DrawPanel(r, fill);
        }

        private static void DrawPanelBorder(Rect r, Color stroke, float t)
        {
            DrawPanel(new Rect(r.x, r.y, r.width, t), stroke);
            DrawPanel(new Rect(r.x, r.yMax - t, r.width, t), stroke);
            DrawPanel(new Rect(r.x, r.y, t, r.height), stroke);
            DrawPanel(new Rect(r.xMax - t, r.y, t, r.height), stroke);
        }

        private static string Truncate(string s, int maxChars)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            return s.Length <= maxChars ? s : s[..maxChars] + "…";
        }

        /// <summary>获取当前主题下可能遭遇的 Boss 预览文本</summary>
        private static string GetBossPreviewForTheme(RunnerMapTheme theme)
        {
            // 检查各 Boss 是否可能在当前主题出现
            var possibleBosses = new List<string>();

            if (BossDefinitions.WillSpawnInTheme("snow_king", theme))
                possibleBosses.Add("冰霜雪王");
            if (BossDefinitions.WillSpawnInTheme("cedar_sentinel", theme))
                possibleBosses.Add("雪松哨兵");
            if (BossDefinitions.WillSpawnInTheme("aurora_serpent", theme))
                possibleBosses.Add("极光长蛇");
            if (BossDefinitions.WillSpawnInTheme("mist_guardian", theme))
                possibleBosses.Add("雾堤守卫");
            if (BossDefinitions.WillSpawnInTheme("coral_kraken", theme))
                possibleBosses.Add("珊瑚海怪");
            if (BossDefinitions.WillSpawnInTheme("storm_eagle", theme))
                possibleBosses.Add("雷云苍鹰");

            if (possibleBosses.Count == 0) return string.Empty;
            if (possibleBosses.Count == 1) return $"\u2620 {possibleBosses[0]}";

            // 多只可能时，显示主要 Boss + "..."
            return $"\u2620 {possibleBosses[0]}等";
        }
    }
}
