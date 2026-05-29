using System;
using PenguinRun.Game.Save;
using UnityEngine;
using UnityEngine.UI;

namespace PenguinRun.Game.UI
{
    /// <summary>
    /// 主菜单「我的战绩」模态 Sheet：完整统计 + 最近对局 + 快捷跳转。
    /// </summary>
    public static class ProfileStatsPanel
    {
        public sealed class HostCallbacks
        {
            public Action OnClose;
            public Action OnOpenLeaderboard;
            public Action OnOpenCodex;
            public Action<RectTransform> DrawMiniAvatar;
        }

        public static GameObject Show(Transform parent, HostCallbacks host)
        {
            if (parent == null || host == null) return null;

            PlayerSave.EnsureTotalRunsSynced();

            var overlay = UiBuilder.CreatePanel("ProfileStatsOverlay", parent,
                new Color(0f, 0f, 0f, 0.72f));
            overlay.transform.SetAsLastSibling();

            var card = UiBuilder.CreateRect("Card", overlay.transform,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(720f, 640f), Vector2.zero,
                UiBuilder.Surface1, rounded: true);
            UiBuilder.AddOutline(card.gameObject, UiBuilder.BorderDefault, new Vector2(1.5f, -1.5f));
            UiBuilder.AddShadow(card.gameObject, new Color(0f, 0f, 0f, 0.55f), new Vector2(0f, -12f));

            var closeBtn = UiBuilder.CreateButton("Close", card, "\u2715",
                UiBuilder.Surface2, UiBuilder.TextSecondary, 28, rounded: true);
            var cbrt = (RectTransform)closeBtn.transform;
            cbrt.anchorMin = new Vector2(1f, 1f);
            cbrt.anchorMax = new Vector2(1f, 1f);
            cbrt.pivot = new Vector2(1f, 1f);
            cbrt.sizeDelta = new Vector2(56f, 56f);
            cbrt.anchoredPosition = new Vector2(-16f, -16f);
            closeBtn.onClick.AddListener(() =>
            {
                UnityEngine.Object.Destroy(overlay);
                host.OnClose?.Invoke();
            });

            var titleText = UiBuilder.CreateText("Title", card, "我的战绩", 36, FontStyle.Bold,
                TextAnchor.UpperLeft, Color.white);
            var trt = (RectTransform)titleText.transform;
            trt.anchorMin = new Vector2(0f, 1f);
            trt.anchorMax = new Vector2(1f, 1f);
            trt.pivot = new Vector2(0f, 1f);
            trt.sizeDelta = new Vector2(-100f, 52f);
            trt.anchoredPosition = new Vector2(28f, -20f);

            var avatarSlot = UiBuilder.CreateRect("Avatar", card,
                new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(64f, 64f), new Vector2(28f, -88f),
                new Color(0.12f, 0.45f, 0.75f, 0.95f), circle: true);
            var avatarInner = UiBuilder.CreateRect("AvatarInner", avatarSlot,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(54f, 54f), Vector2.zero,
                new Color(0.06f, 0.2f, 0.38f, 0.98f), circle: true);
            host.DrawMiniAvatar?.Invoke(avatarInner);

            var nickname = PlayerSave.PlayerNickname;
            var nicknameText = UiBuilder.CreateText("Nickname", card, $"嗨，{nickname}", 24, FontStyle.Bold,
                TextAnchor.MiddleLeft, UiBuilder.TextPrimary);
            var nrt = (RectTransform)nicknameText.transform;
            nrt.anchorMin = new Vector2(0f, 1f);
            nrt.anchorMax = new Vector2(1f, 1f);
            nrt.pivot = new Vector2(0f, 1f);
            nrt.sizeDelta = new Vector2(-120f, 36f);
            nrt.anchoredPosition = new Vector2(104f, -92f);

            var level = PlayerSave.PlayerLevel;
            var xpProgress = PlayerSave.PlayerLevelProgress;
            var levelText = UiBuilder.CreateText("Level", card, $"Lv.{level}", 20, FontStyle.Bold,
                TextAnchor.MiddleLeft, UiBuilder.AccentCyan);
            var lrt = (RectTransform)levelText.transform;
            lrt.anchorMin = new Vector2(0f, 1f);
            lrt.anchorMax = new Vector2(1f, 1f);
            lrt.pivot = new Vector2(0f, 1f);
            lrt.sizeDelta = new Vector2(-120f, 28f);
            lrt.anchoredPosition = new Vector2(104f, -128f);

            var xpTrack = UiBuilder.CreateRect("XpTrack", card,
                new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(-56f, 14f), new Vector2(0f, -158f),
                new Color(0.04f, 0.09f, 0.16f, 1f), rounded: true);
            xpTrack.pivot = new Vector2(0.5f, 1f);
            var xpFill = UiBuilder.CreateRect("XpFill", xpTrack,
                new Vector2(0f, 0f), new Vector2(0f, 1f),
                Vector2.zero, Vector2.zero,
                UiBuilder.AccentCyan, rounded: true).GetComponent<Image>();
            xpFill.raycastTarget = false;
            var fillRt = (RectTransform)xpFill.transform;
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = new Vector2(Mathf.Clamp01(xpProgress), 1f);
            fillRt.offsetMin = new Vector2(3f, 3f);
            fillRt.offsetMax = new Vector2(-3f, -3f);

            var statsRow = UiBuilder.CreateRect("StatsRow", card,
                new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(-56f, 88f), new Vector2(0f, -188f));
            statsRow.pivot = new Vector2(0.5f, 1f);
            var recordStats = RunRecords.GetStats();
            BuildStatCell(statsRow, 0f, "历史最高", $"{PlayerSave.BestScore:N0}", UiBuilder.AccentCyan);
            BuildStatCell(statsRow, 0.34f, "总局数", $"{PlayerSave.TotalRuns:N0}", UiBuilder.WarmGold);
            BuildStatCell(statsRow, 0.67f, "累计鱼干", $"{PlayerSave.TotalFishSnacks:N0}", UiBuilder.Success);
            if (recordStats.TotalRuns > 0)
            {
                var avgRow = UiBuilder.CreateRect("AvgRow", card,
                    new Vector2(0f, 1f), new Vector2(1f, 1f),
                    new Vector2(-56f, 28f), new Vector2(0f, -286f));
                avgRow.pivot = new Vector2(0.5f, 1f);
                UiBuilder.CreateText("AvgLabel", avgRow,
                    $"场均得分 {recordStats.AverageScore:N0} · 最佳单局鱼干 {recordStats.BestFishInOneRun:N0}",
                    16, FontStyle.Normal, TextAnchor.MiddleLeft, UiBuilder.TextSecondary);
                var art = (RectTransform)avgRow.Find("AvgLabel");
                if (art != null)
                {
                    art.anchorMin = Vector2.zero;
                    art.anchorMax = Vector2.one;
                    art.offsetMin = new Vector2(28f, 0f);
                    art.offsetMax = new Vector2(-28f, 0f);
                }
            }

            UiBuilder.CreateText("RecentTitle", card, "最近对局", 20, FontStyle.Bold,
                TextAnchor.UpperLeft, UiBuilder.TextSecondary);
            var recentTitleRt = (RectTransform)card.Find("RecentTitle");
            if (recentTitleRt != null)
            {
                recentTitleRt.anchorMin = new Vector2(0f, 1f);
                recentTitleRt.anchorMax = new Vector2(1f, 1f);
                recentTitleRt.pivot = new Vector2(0f, 1f);
                recentTitleRt.sizeDelta = new Vector2(-56f, 28f);
                recentTitleRt.anchoredPosition = new Vector2(28f, -318f);
            }

            var listArea = UiBuilder.CreateRect("RecentList", card,
                new Vector2(0f, 0f), new Vector2(1f, 1f),
                new Vector2(-56f, 120f), new Vector2(28f, -352f));
            var recent = RunRecords.GetRecent(5);
            if (recent.Count == 0)
            {
                UiBuilder.CreateText("Empty", listArea, "还没有对局记录，快去跑一局吧！",
                    18, FontStyle.Normal, TextAnchor.MiddleCenter, UiBuilder.TextTertiary);
            }
            else
            {
                var y = 0f;
                const float rowH = 52f;
                const float gap = 8f;
                for (var i = 0; i < recent.Count; i++)
                {
                    var rec = recent[i];
                    var row = UiBuilder.CreateRect("Run" + i, listArea,
                        new Vector2(0f, 1f), new Vector2(1f, 1f),
                        new Vector2(0f, rowH), new Vector2(0f, -y),
                        new Color(0.08f, 0.16f, 0.28f, 0.85f), rounded: true);
                    row.pivot = new Vector2(0.5f, 1f);

                    var modeLabel = rec.mode == "daily" ? "今日挑战" : "极夜快跑";
                    var dateStr = rec.Timestamp.ToString("MM/dd HH:mm");
                    var line = $"{modeLabel} · {rec.score:N0} 分 · +{rec.fishSnacks} 鱼干";
                    if (rec.wasNewBest) line += " · 新纪录";
                    UiBuilder.CreateText("Line", row, line, 17, FontStyle.Normal,
                        TextAnchor.MiddleLeft, UiBuilder.TextPrimary);
                    var lineRt = (RectTransform)row.Find("Line");
                    if (lineRt != null)
                    {
                        lineRt.anchorMin = Vector2.zero;
                        lineRt.anchorMax = Vector2.one;
                        lineRt.offsetMin = new Vector2(14f, 0f);
                        lineRt.offsetMax = new Vector2(-100f, 0f);
                    }
                    UiBuilder.CreateText("Date", row, dateStr, 15, FontStyle.Normal,
                        TextAnchor.MiddleRight, UiBuilder.TextTertiary);
                    var dateRt = (RectTransform)row.Find("Date");
                    if (dateRt != null)
                    {
                        dateRt.anchorMin = Vector2.zero;
                        dateRt.anchorMax = Vector2.one;
                        dateRt.offsetMin = new Vector2(14f, 0f);
                        dateRt.offsetMax = new Vector2(-14f, 0f);
                    }
                    y += rowH + gap;
                }
            }

            var btnRow = UiBuilder.CreateRect("BtnRow", card,
                new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(-56f, 72f), new Vector2(28f, 24f));
            btnRow.pivot = new Vector2(0.5f, 0f);

            var lbBtn = UiBuilder.CreateButton("Leaderboard", btnRow, "排行榜",
                UiBuilder.Surface2, UiBuilder.AccentCyan, 22, rounded: true);
            var lbrt = (RectTransform)lbBtn.transform;
            lbrt.anchorMin = new Vector2(0f, 0f);
            lbrt.anchorMax = new Vector2(0.5f, 1f);
            lbrt.offsetMin = Vector2.zero;
            lbrt.offsetMax = new Vector2(-8f, 0f);
            lbBtn.onClick.AddListener(() =>
            {
                UnityEngine.Object.Destroy(overlay);
                host.OnClose?.Invoke();
                host.OnOpenLeaderboard?.Invoke();
            });

            var codexBtn = UiBuilder.CreateButton("Codex", btnRow, "图鉴",
                UiBuilder.Surface2, new Color(0.7f, 0.55f, 0.95f, 1f), 22, rounded: true);
            var cbrt2 = (RectTransform)codexBtn.transform;
            cbrt2.anchorMin = new Vector2(0.5f, 0f);
            cbrt2.anchorMax = new Vector2(1f, 1f);
            cbrt2.offsetMin = new Vector2(8f, 0f);
            cbrt2.offsetMax = Vector2.zero;
            codexBtn.onClick.AddListener(() =>
            {
                UnityEngine.Object.Destroy(overlay);
                host.OnClose?.Invoke();
                host.OnOpenCodex?.Invoke();
            });

            return overlay;
        }

        private static void BuildStatCell(Transform parent, float anchorX, string label, string value, Color accent)
        {
            var cell = UiBuilder.CreateRect("Stat_" + label, parent,
                new Vector2(anchorX, 0f), new Vector2(anchorX + 0.32f, 1f),
                Vector2.zero, Vector2.zero,
                new Color(0.08f, 0.16f, 0.28f, 0.9f), rounded: true);
            UiBuilder.CreateText("Label", cell, label, 14, FontStyle.Normal,
                TextAnchor.UpperCenter, UiBuilder.TextSecondary);
            var lbl = (RectTransform)cell.Find("Label");
            if (lbl != null)
            {
                lbl.anchorMin = new Vector2(0f, 0.5f);
                lbl.anchorMax = new Vector2(1f, 1f);
                lbl.offsetMin = new Vector2(4f, 0f);
                lbl.offsetMax = new Vector2(-4f, -4f);
            }
            UiBuilder.CreateText("Value", cell, value, 22, FontStyle.Bold,
                TextAnchor.LowerCenter, accent);
            var val = (RectTransform)cell.Find("Value");
            if (val != null)
            {
                val.anchorMin = new Vector2(0f, 0f);
                val.anchorMax = new Vector2(1f, 0.5f);
                val.offsetMin = new Vector2(4f, 4f);
                val.offsetMax = new Vector2(-4f, 0f);
            }
        }
    }
}
