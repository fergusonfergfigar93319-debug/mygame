using System;
using System.Collections.Generic;
using PenguinRun.Game.Leaderboard;
using UnityEngine;
using UnityEngine.UI;

namespace PenguinRun.Game.UI
{
    /// <summary>
    /// 本地排行榜面板：按「总分 / 距离 / 存活时间」三种排序切换；空状态展示鼓励文案。
    /// 重构：现代化排序栏、奖牌徽章、前三名特殊高亮。
    /// </summary>
    public sealed class LeaderboardPanel : MonoBehaviour
    {
        private System.Action<MainMenuBootstrap.PanelEvent, string> dispatch;
        private LeaderboardSort sort = LeaderboardSort.ByTotalScore;
        private RectTransform contentRoot;
        private readonly System.Collections.Generic.List<(Image img, Text label, LeaderboardSort s)> sortMarkers = new();

        // 前三名奖牌色（金/银/铜）
        private static readonly Color GoldMedal = new(1f, 0.84f, 0.32f);
        private static readonly Color SilverMedal = new(0.82f, 0.85f, 0.92f);
        private static readonly Color BronzeMedal = new(0.85f, 0.55f, 0.32f);

        public static GameObject Build(Transform parent, System.Action<MainMenuBootstrap.PanelEvent, string> dispatch)
        {
            var rootRt = UiBuilder.CreateRect(
                "LeaderboardPanel", parent,
                Vector2.zero, Vector2.one,
                Vector2.zero, Vector2.zero,
                UiBuilder.MenuPanelSolidBg);
            var script = rootRt.gameObject.AddComponent<LeaderboardPanel>();
            script.dispatch = dispatch;
            script.BuildLayout(rootRt);
            script.Refresh();
            return rootRt.gameObject;
        }

        private void BuildLayout(RectTransform parent)
        {
            PanelHeader.Create(parent, "排行榜", "本地保存最近最好成绩",
                () => dispatch(MainMenuBootstrap.PanelEvent.Close, null));

            const float sortRowHeight = 88f;
            const float sortRowGap = 14f;
            var sortRowTop = UiBuilder.PanelHeaderHeightPixels + sortRowGap;

            // 现代化分段控件容器（segment control 风格）
            var sortRow = UiBuilder.CreateRect("SortRow", parent,
                new Vector2(0.04f, 1f), new Vector2(0.96f, 1f),
                new Vector2(0f, sortRowHeight), new Vector2(0f, -sortRowTop),
                UiBuilder.Surface1, rounded: true);
            sortRow.pivot = new Vector2(0.5f, 1f);
            UiBuilder.AddOutline(sortRow.gameObject, UiBuilder.BorderSubtle, new Vector2(1f, -1f));

            var hlg = sortRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(8, 8, 8, 8);
            hlg.spacing = 6f;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;

            CreateSortBtn(sortRow, "\u2605 总分", LeaderboardSort.ByTotalScore);
            CreateSortBtn(sortRow, "\u279E 距离", LeaderboardSort.ByDistance);
            CreateSortBtn(sortRow, "\u231B 存活", LeaderboardSort.BySurvivalTime);
            ApplySortBarStyle();

            UiBuilder.CreatePanelScrollList(parent, out var content,
                topInset: sortRowTop + sortRowHeight + sortRowGap);
            contentRoot = content;
        }

        private void CreateSortBtn(RectTransform parent, string label, LeaderboardSort s)
        {
            var btn = UiBuilder.CreateButton("Sort_" + s, parent, label,
                Color.clear, Color.white, 24, rounded: true);
            var img = btn.GetComponent<Image>();
            var labelText = btn.GetComponentInChildren<Text>();
            sortMarkers.Add((img, labelText, s));
            btn.onClick.AddListener(() =>
            {
                sort = s;
                ApplySortBarStyle();
                Refresh();
            });
        }

        private void ApplySortBarStyle()
        {
            foreach (var (img, label, s) in sortMarkers)
            {
                if (img == null) continue;
                var sel = sort == s;
                img.color = sel ? UiBuilder.AccentCyan : new Color(0f, 0f, 0f, 0f);
                if (label != null)
                {
                    label.color = sel ? new Color(0.04f, 0.08f, 0.14f, 1f)
                        : UiBuilder.TextSecondary;
                    label.fontStyle = sel ? FontStyle.Bold : FontStyle.Normal;
                }
            }
        }

        private void Refresh()
        {
            for (var i = contentRoot.childCount - 1; i >= 0; i--)
            {
                Destroy(contentRoot.GetChild(i).gameObject);
            }

            var entries = LeaderboardStore.GetTopEntries(20, sort);
            if (entries.Count == 0)
            {
                BuildEmpty();
                UiBuilder.RebuildScrollContent(contentRoot);
                return;
            }

            for (var i = 0; i < entries.Count; i++)
            {
                BuildRow(i + 1, entries[i]);
            }

            UiBuilder.RebuildScrollContent(contentRoot);
        }

        private void BuildEmpty()
        {
            var empty = UiBuilder.CreateScrollListRow("Empty", contentRoot, 400f, null);

            // 大图标
            var iconCircle = UiBuilder.CreateRect("Icon", empty,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(120f, 120f), new Vector2(0f, 60f),
                new Color(UiBuilder.AccentCyan.r, UiBuilder.AccentCyan.g, UiBuilder.AccentCyan.b, 0.15f),
                circle: true);
            UiBuilder.AddOutline(iconCircle.gameObject,
                new Color(UiBuilder.AccentCyan.r, UiBuilder.AccentCyan.g, UiBuilder.AccentCyan.b, 0.45f),
                new Vector2(2f, -2f));
            UiBuilder.CreateText("IconText", iconCircle.transform, "\u2605", 56, FontStyle.Bold,
                TextAnchor.MiddleCenter, UiBuilder.AccentCyan);

            var t = UiBuilder.CreateText("Title", empty, "还没有本地纪录",
                30, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            var trt = (RectTransform)t.transform;
            trt.anchorMin = new Vector2(0f, 0.5f);
            trt.anchorMax = new Vector2(1f, 0.5f);
            trt.pivot = new Vector2(0.5f, 0.5f);
            trt.sizeDelta = new Vector2(0f, 60f);
            trt.anchoredPosition = new Vector2(0f, -30f);

            var b = UiBuilder.CreateText("Body", empty, "去跑一局，刷下这块榜单。",
                22, FontStyle.Normal, TextAnchor.MiddleCenter, UiBuilder.TextSecondary);
            var brt = (RectTransform)b.transform;
            brt.anchorMin = new Vector2(0f, 0.5f);
            brt.anchorMax = new Vector2(1f, 0.5f);
            brt.pivot = new Vector2(0.5f, 0.5f);
            brt.sizeDelta = new Vector2(0f, 40f);
            brt.anchoredPosition = new Vector2(0f, -80f);
        }

        private void BuildRow(int index, LeaderboardEntry e)
        {
            // 前三名突出显示：更亮的背景 + 奖牌效果
            var isMedal = index <= 3;
            Color rowBg;
            Color medalColor;
            string medalSymbol;

            if (index == 1)
            {
                rowBg = new Color(0.18f, 0.14f, 0.06f, 0.92f);
                medalColor = GoldMedal;
                medalSymbol = "1";
            }
            else if (index == 2)
            {
                rowBg = new Color(0.13f, 0.16f, 0.22f, 0.92f);
                medalColor = SilverMedal;
                medalSymbol = "2";
            }
            else if (index == 3)
            {
                rowBg = new Color(0.16f, 0.1f, 0.06f, 0.92f);
                medalColor = BronzeMedal;
                medalSymbol = "3";
            }
            else
            {
                rowBg = UiBuilder.Surface1;
                medalColor = UiBuilder.TextSecondary;
                medalSymbol = index.ToString();
            }

            var row = UiBuilder.CreateScrollListRow("Row_" + index, contentRoot, 120f, rowBg);
            UiBuilder.AddOutline(row.gameObject,
                isMedal ? new Color(medalColor.r, medalColor.g, medalColor.b, 0.4f) : UiBuilder.BorderSubtle,
                new Vector2(1f, -1f));

            // 排名徽章（前三名为奖牌，其余为简洁数字）
            var medalBadge = UiBuilder.CreateRect("Medal", row,
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(72f, 72f), new Vector2(50f, 0f),
                isMedal ? medalColor : new Color(0.12f, 0.22f, 0.34f, 0.9f),
                circle: true);
            if (isMedal)
            {
                UiBuilder.AddOutline(medalBadge.gameObject,
                    new Color(1f, 1f, 1f, 0.55f), new Vector2(2f, -2f));
                UiBuilder.AddShadow(medalBadge.gameObject,
                    new Color(medalColor.r * 0.5f, medalColor.g * 0.5f, medalColor.b * 0.5f, 0.5f),
                    new Vector2(0f, -2f));
            }
            else
            {
                UiBuilder.AddOutline(medalBadge.gameObject, UiBuilder.BorderSubtle, new Vector2(1f, -1f));
            }
            UiBuilder.CreateText("MedalNum", medalBadge.transform, medalSymbol,
                isMedal ? 32 : 26, FontStyle.Bold, TextAnchor.MiddleCenter,
                isMedal ? new Color(0.15f, 0.08f, 0.02f, 1f) : UiBuilder.AccentCyan);

            // 玩家昵称
            var name = UiBuilder.CreateText("Name", row, e.nickname ?? "玩家",
                28, FontStyle.Bold, TextAnchor.UpperLeft, Color.white);
            var nrt = (RectTransform)name.transform;
            nrt.anchorMin = new Vector2(0f, 0.5f);
            nrt.anchorMax = new Vector2(0.7f, 1f);
            nrt.pivot = new Vector2(0f, 0.5f);
            nrt.offsetMin = new Vector2(140f, 0f);
            nrt.offsetMax = new Vector2(-10f, -8f);

            // 元信息
            var meta = UiBuilder.CreateText("Meta", row,
                $"\u279E {Mathf.RoundToInt(e.distanceScoreUnits)}m   \u231B {Mathf.RoundToInt(e.survivalSeconds)}s   {FormatDate(e.timestampMillis)}",
                19, FontStyle.Normal, TextAnchor.LowerLeft, UiBuilder.TextSecondary);
            var mrt = (RectTransform)meta.transform;
            mrt.anchorMin = new Vector2(0f, 0f);
            mrt.anchorMax = new Vector2(0.7f, 0.5f);
            mrt.pivot = new Vector2(0f, 0.5f);
            mrt.offsetMin = new Vector2(140f, 8f);
            mrt.offsetMax = new Vector2(-10f, -2f);

            // 总分（右对齐，金色高亮）
            var score = UiBuilder.CreateText("Score", row, e.totalScore.ToString("N0"),
                isMedal ? 40 : 36, FontStyle.Bold, TextAnchor.MiddleRight,
                isMedal ? medalColor : UiBuilder.WarmGold);
            var srt = (RectTransform)score.transform;
            srt.anchorMin = new Vector2(0.7f, 0f);
            srt.anchorMax = new Vector2(1f, 1f);
            srt.sizeDelta = Vector2.zero;
            srt.offsetMin = new Vector2(10f, 0f);
            srt.offsetMax = new Vector2(-24f, 0f);

            // "分" 单位（小字提示）
            var scoreUnit = UiBuilder.CreateText("ScoreUnit", row, "分",
                14, FontStyle.Normal, TextAnchor.LowerRight, UiBuilder.TextTertiary);
            var surt = (RectTransform)scoreUnit.transform;
            surt.anchorMin = new Vector2(0.7f, 0f);
            surt.anchorMax = new Vector2(1f, 0f);
            surt.pivot = new Vector2(1f, 0f);
            surt.sizeDelta = new Vector2(60f, 22f);
            surt.anchoredPosition = new Vector2(-24f, 6f);
        }

        private static string FormatDate(long unixMs)
        {
            var dt = DateTimeOffset.FromUnixTimeMilliseconds(unixMs).LocalDateTime;
            return dt.ToString("MM-dd HH:mm");
        }
    }
}
