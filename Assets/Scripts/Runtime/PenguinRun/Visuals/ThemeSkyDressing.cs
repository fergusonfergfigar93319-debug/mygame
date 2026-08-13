using UnityEngine;

namespace PenguinRun
{
    /// <summary>
    /// 逐主题天象布置（月亮 / 太阳 / 极光带）。
    /// 此前六张图共用同一轮月亮和五条极光带，天空主题白昼挂着月亮、海洋礁湖上飘着极光，
    /// 主题身份在天上完全丢失。本类按主题给出各自的天象组合，纯表现层。
    ///
    /// 命名契约（PenguinRunnerGame 按名字驱动行为，不得改名）：
    /// - "Moon" / "Moon Glow"：跳过走廊淡出、不贴地（日盘复用该名字以继承同一行为）。
    /// - "Aurora Ribbon {i}"：AnimateScenery 中获得脉动缩放。
    /// </summary>
    internal static class ThemeSkyDressing
    {
        public static void Build(RunnerMapTheme theme, System.Action<GameObject> addProp)
        {
            switch (theme)
            {
                case RunnerMapTheme.AuroraField:
                    AddMoon(addProp, new Color(0.9f, 0.96f, 1f, 0.9f), new Color(0.6f, 0.95f, 0.9f, 0.26f), 5.6f);
                    // 极光是这张图唯一的主光源叙事：满编五条、绿粉交替。
                    AddAuroraRibbons(addProp, 5,
                        new Color(0.2f, 1f, 0.72f, 0.62f), new Color(1f, 0.42f, 0.72f, 0.52f));
                    break;

                case RunnerMapTheme.IceLakeEcho:
                    AddMoon(addProp, new Color(0.95f, 0.97f, 1f, 0.98f), new Color(0.74f, 0.92f, 1f, 0.3f), 6.4f);
                    // 冰湖只留两条微弱余晖：与极光图拉开天空密度差。
                    AddAuroraRibbons(addProp, 2,
                        new Color(0.3f, 0.9f, 0.85f, 0.3f), new Color(0.42f, 0.7f, 1f, 0.26f));
                    break;

                case RunnerMapTheme.CedarRuins:
                    // 暮色低月：偏琥珀、更低更大，呼应低角度暖主光。
                    AddMoon(addProp, new Color(1f, 0.88f, 0.72f, 0.95f), new Color(1f, 0.72f, 0.45f, 0.24f), 7.2f, y: 10.5f);
                    break;

                case RunnerMapTheme.MistDike:
                    // 雾堤看不见月盘，只有一团被雾漫开的冷光。
                    addProp(SceneryBillboards.CreateGlowQuad("Moon Glow", new Vector3(7.5f, 12f, 60f),
                        new Vector2(14f, 14f), new Color(0.85f, 0.9f, 0.98f, 0.2f), SceneryBillboards.HaloTexture()));
                    break;

                case RunnerMapTheme.OceanReef:
                    // 礁湖上的小而高的冷月：水面世界只需要一点月光提示。
                    AddMoon(addProp, new Color(0.85f, 0.95f, 1f, 0.85f), new Color(0.6f, 0.9f, 1f, 0.2f), 4.4f, y: 15f);
                    break;

                case RunnerMapTheme.SkyFlight:
                    // 白昼日盘 + 暖光晕（复用 Moon 命名继承"不贴地、不淡出"行为）。
                    addProp(SceneryBillboards.CreateGlowQuad("Moon", new Vector3(9.4f, 14.5f, 62f),
                        new Vector2(5.2f, 5.2f), new Color(1f, 0.95f, 0.78f, 1f), SceneryBillboards.MoonTexture()));
                    addProp(SceneryBillboards.CreateGlowQuad("Moon Glow", new Vector3(9.4f, 14.5f, 62.2f),
                        new Vector2(16f, 16f), new Color(1f, 0.86f, 0.55f, 0.3f), SceneryBillboards.HaloTexture()));
                    break;
            }
        }

        private static void AddMoon(System.Action<GameObject> addProp, Color body, Color halo, float size, float y = 13f)
        {
            addProp(SceneryBillboards.CreateGlowQuad("Moon", new Vector3(8.6f, y, 58f),
                new Vector2(size, size), body, SceneryBillboards.MoonTexture()));
            addProp(SceneryBillboards.CreateGlowQuad("Moon Glow", new Vector3(8.6f, y, 58.2f),
                new Vector2(size * 1.7f, size * 1.7f), halo, SceneryBillboards.HaloTexture()));
        }

        private static void AddAuroraRibbons(System.Action<GameObject> addProp, int count, Color even, Color odd)
        {
            for (var i = 0; i < count; i++)
            {
                var ribbon = SceneryBillboards.CreateGlowQuad(
                    $"Aurora Ribbon {i}",
                    new Vector3(-7f + i * 3.6f, 9.2f + i * 0.42f, 50f + i * 7f),
                    new Vector2(0.9f, 8.5f),
                    i % 2 == 0 ? even : odd,
                    SceneryBillboards.AuroraBandTexture(),
                    billboard: false);
                ribbon.transform.rotation = Quaternion.Euler(0f, 0f, -18f + i * 13f);
                addProp(ribbon);
            }
        }
    }
}
