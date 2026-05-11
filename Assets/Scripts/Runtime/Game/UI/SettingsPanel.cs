using PenguinRun.Game.Save;
using UnityEngine;
using UnityEngine.UI;

namespace PenguinRun.Game.UI
{
    /// <summary>
    /// 设置面板：BGM、大厅曲目、SFX、震动强度、昵称编辑；部分项立即写入存档。
    /// 重构：现代化滑块、开关、分段控件、统一的配置卡片样式。
    /// </summary>
    public sealed class SettingsPanel : MonoBehaviour
    {
        private System.Action<MainMenuBootstrap.PanelEvent, string> dispatch;
        private RectTransform contentRoot;
        private InputField nicknameField;
        private Toggle bgmToggle;
        private Toggle sfxToggle;
        private Slider hapticSlider;
        private Text hapticValueText;

        private readonly System.Collections.Generic.List<(Button btn, Image img, Text label, string trackId)> lobbyPickButtons = new();
        private readonly System.Collections.Generic.List<(Button btn, Image img, Text label, string trackId)> runPickButtons = new();
        private readonly System.Collections.Generic.List<(Button btn, Image img, Text label, string ambienceId)> ambiencePickButtons = new();
        private readonly System.Collections.Generic.List<(Button btn, Image img, Text label, string styleId)> sfxStylePickButtons = new();

        public static GameObject Build(Transform parent, System.Action<MainMenuBootstrap.PanelEvent, string> dispatch)
        {
            var rootRt = UiBuilder.CreateRect(
                "SettingsPanel", parent,
                Vector2.zero, Vector2.one,
                Vector2.zero, Vector2.zero,
                UiBuilder.MenuPanelSolidBg);
            var script = rootRt.gameObject.AddComponent<SettingsPanel>();
            script.dispatch = dispatch;
            script.BuildLayout(rootRt);
            return rootRt.gameObject;
        }

        private void BuildLayout(RectTransform parent)
        {
            PanelHeader.Create(parent, "设置", "调整昵称、音乐音效和震动", () => {
                Apply();
                dispatch(MainMenuBootstrap.PanelEvent.Close, null);
            });

            UiBuilder.CreatePanelScrollList(parent, out var content);
            contentRoot = content;

            BuildSectionHeader("个人资料");
            BuildNicknameRow();
            BuildSectionHeader("音频");
            BuildBgmRow();
            BuildLobbyTrackRow();
            BuildRunTrackRow();
            BuildAmbienceRow();
            BuildSfxStyleRow();
            BuildSfxRow();
            BuildSectionHeader("反馈");
            BuildHapticRow();
            UiBuilder.RebuildScrollContent(contentRoot);
        }

        /// <summary>设置分组小标题：用于把"个人资料/音频/反馈"分块。</summary>
        private void BuildSectionHeader(string title)
        {
            var header = UiBuilder.CreateScrollListRow("Section_" + title, contentRoot, 50f, null);
            // 左侧重音条
            var accent = UiBuilder.CreateRect("Accent", header,
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(4f, 22f), new Vector2(20f, 0f),
                UiBuilder.AccentCyan, rounded: true);
            accent.GetComponent<Image>().raycastTarget = false;

            var t = UiBuilder.CreateText("Title", header, title,
                22, FontStyle.Bold, TextAnchor.MiddleLeft, UiBuilder.AccentCyan);
            var trt = (RectTransform)t.transform;
            trt.anchorMin = new Vector2(0f, 0f);
            trt.anchorMax = new Vector2(1f, 1f);
            trt.offsetMin = new Vector2(36f, 0f);
            trt.offsetMax = new Vector2(-20f, 0f);
        }

        private void BuildNicknameRow()
        {
            var row = NewRow("昵称", "在排行榜上展示的名字。");
            var fieldGo = new GameObject("NicknameField",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(InputField));
            fieldGo.transform.SetParent(row.transform, false);
            var rt = (RectTransform)fieldGo.transform;
            rt.anchorMin = new Vector2(0.55f, 0.22f);
            rt.anchorMax = new Vector2(1f, 0.78f);
            rt.sizeDelta = Vector2.zero;
            rt.offsetMin = new Vector2(0f, 0f);
            rt.offsetMax = new Vector2(-24f, 0f);
            var fieldImg = fieldGo.GetComponent<Image>();
            fieldImg.color = new Color(0.04f, 0.09f, 0.16f, 1f);
            fieldImg.sprite = UiBuilder.RoundedSprite;
            fieldImg.type = Image.Type.Sliced;
            UiBuilder.AddOutline(fieldGo, UiBuilder.BorderDefault, new Vector2(1f, -1f));

            var textGo = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textGo.transform.SetParent(fieldGo.transform, false);
            var trt = (RectTransform)textGo.transform;
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.sizeDelta = Vector2.zero;
            trt.offsetMin = new Vector2(20f, 8f);
            trt.offsetMax = new Vector2(-20f, -8f);
            var t = textGo.GetComponent<Text>();
            t.alignment = TextAnchor.MiddleLeft;
            t.color = Color.white;
            t.fontSize = 26;
            t.fontStyle = FontStyle.Bold;
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.supportRichText = false;

            var input = fieldGo.GetComponent<InputField>();
            input.textComponent = t;
            input.text = PlayerSave.PlayerNickname;
            input.characterLimit = 14;
            nicknameField = input;
        }

        private void BuildBgmRow()
        {
            var row = NewRow("背景音乐", "总开关：关掉后主菜单与跑酷内 BGM/环境音均静音。");
            bgmToggle = AddModernToggle(row, PlayerSave.BgmEnabled);
            bgmToggle.onValueChanged.AddListener(on =>
            {
                PlayerSave.BgmEnabled = on;
                PlayerSave.Flush();
                GameAudioSettings.NotifyLobbyBgmChanged();
            });
        }

        private void BuildLobbyTrackRow()
        {
            var row = NewRow("大厅曲目", "主菜单循环；需开启上一项「背景音乐」。", 178f);
            var stripGo = new GameObject("LobbyTrackStrip", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            stripGo.transform.SetParent(row.transform, false);
            var srt = (RectTransform)stripGo.transform;
            srt.anchorMin = new Vector2(0.42f, 0.1f);
            srt.anchorMax = new Vector2(1f, 0.55f);
            srt.offsetMin = Vector2.zero;
            srt.offsetMax = new Vector2(-24f, 0f);
            var hlg = stripGo.GetComponent<HorizontalLayoutGroup>();
            hlg.spacing = 6f;
            hlg.padding = new RectOffset(0, 0, 0, 0);
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;

            // 容器背景，让分段控件更醒目
            var containerBg = stripGo.AddComponent<Image>();
            containerBg.sprite = UiBuilder.RoundedSprite;
            containerBg.type = Image.Type.Sliced;
            containerBg.color = new Color(0.04f, 0.09f, 0.16f, 1f);
            UiBuilder.AddOutline(stripGo, UiBuilder.BorderSubtle, new Vector2(1f, -1f));
            hlg.padding = new RectOffset(6, 6, 6, 6);

            AddLobbyPick(stripGo.transform, "温馨", PlayerSave.LobbyBgmCozy);
            AddLobbyPick(stripGo.transform, "剧情", PlayerSave.LobbyBgmStory);
            AddLobbyPick(stripGo.transform, "无尽", PlayerSave.LobbyBgmEndless);
            AddLobbyPick(stripGo.transform, "开源·晨", PlayerSave.LobbyBgmOpenWisdom);
            AddLobbyPick(stripGo.transform, "开源·跃", PlayerSave.LobbyBgmOpenSwing);
            AddLobbyPick(stripGo.transform, "静音", PlayerSave.LobbyBgmNone);
            RefreshLobbyPickStyle();
        }

        private void AddLobbyPick(Transform parent, string label, string trackId)
        {
            var btn = UiBuilder.CreateButton("Pick_" + trackId, parent, label,
                Color.clear, UiBuilder.TextSecondary, 20, rounded: true);
            var img = btn.GetComponent<Image>();
            var labelText = btn.GetComponentInChildren<Text>();
            lobbyPickButtons.Add((btn, img, labelText, trackId));
            btn.onClick.AddListener(() =>
            {
                PlayerSave.LobbyBgmTrack = trackId;
                PlayerSave.Flush();
                RefreshLobbyPickStyle();
                GameAudioSettings.NotifyLobbyBgmChanged();
            });
        }

        private void RefreshLobbyPickStyle()
        {
            var cur = PlayerSave.LobbyBgmTrack;
            foreach (var (btn, img, label, trackId) in lobbyPickButtons)
            {
                if (btn == null) continue;
                var sel = cur == trackId;
                img.color = sel ? UiBuilder.AccentCyan : new Color(0f, 0f, 0f, 0f);
                if (label != null)
                {
                    label.color = sel ? new Color(0.04f, 0.08f, 0.14f, 1f) : UiBuilder.TextSecondary;
                    label.fontStyle = sel ? FontStyle.Bold : FontStyle.Normal;
                }
            }
        }

        private void BuildRunTrackRow()
        {
            var row = NewRow("跑酷曲目", "跑酷内 BGM（切换后下一局生效）。", 178f);
            var stripGo = new GameObject("RunTrackStrip", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            stripGo.transform.SetParent(row.transform, false);
            var srt = (RectTransform)stripGo.transform;
            srt.anchorMin = new Vector2(0.42f, 0.1f);
            srt.anchorMax = new Vector2(1f, 0.55f);
            srt.offsetMin = Vector2.zero;
            srt.offsetMax = new Vector2(-24f, 0f);

            var hlg = stripGo.GetComponent<HorizontalLayoutGroup>();
            hlg.spacing = 6f;
            hlg.padding = new RectOffset(6, 6, 6, 6);
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;

            var containerBg = stripGo.AddComponent<Image>();
            containerBg.sprite = UiBuilder.RoundedSprite;
            containerBg.type = Image.Type.Sliced;
            containerBg.color = new Color(0.04f, 0.09f, 0.16f, 1f);
            UiBuilder.AddOutline(stripGo, UiBuilder.BorderSubtle, new Vector2(1f, -1f));

            AddRunPick(stripGo.transform, "无尽", PlayerSave.RunBgmEndless);
            AddRunPick(stripGo.transform, "剧情", PlayerSave.RunBgmStory);
            AddRunPick(stripGo.transform, "开源·晨", PlayerSave.RunBgmOpenWisdom);
            AddRunPick(stripGo.transform, "开源·冬", PlayerSave.RunBgmOpenWinter);
            AddRunPick(stripGo.transform, "开源·跃", PlayerSave.RunBgmOpenSwing);
            AddRunPick(stripGo.transform, "随机", PlayerSave.RunBgmRandom);
            RefreshRunPickStyle();
        }

        private void AddRunPick(Transform parent, string label, string trackId)
        {
            var btn = UiBuilder.CreateButton("RunPick_" + trackId, parent, label,
                Color.clear, UiBuilder.TextSecondary, 19, rounded: true);
            var img = btn.GetComponent<Image>();
            var labelText = btn.GetComponentInChildren<Text>();
            runPickButtons.Add((btn, img, labelText, trackId));
            btn.onClick.AddListener(() =>
            {
                PlayerSave.RunBgmTrack = trackId;
                PlayerSave.Flush();
                RefreshRunPickStyle();
            });
        }

        private void RefreshRunPickStyle()
        {
            var cur = PlayerSave.RunBgmTrack;
            foreach (var (btn, img, label, trackId) in runPickButtons)
            {
                if (btn == null) continue;
                var sel = cur == trackId;
                img.color = sel ? UiBuilder.AccentCyan : new Color(0f, 0f, 0f, 0f);
                if (label != null)
                {
                    label.color = sel ? new Color(0.04f, 0.08f, 0.14f, 1f) : UiBuilder.TextSecondary;
                    label.fontStyle = sel ? FontStyle.Bold : FontStyle.Normal;
                }
            }
        }

        private void BuildAmbienceRow()
        {
            var row = NewRow("环境氛围", "叠加背景环境音色（风声/洞穴/赛博）。", 142f);
            var stripGo = new GameObject("AmbienceStrip", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            stripGo.transform.SetParent(row.transform, false);
            var srt = (RectTransform)stripGo.transform;
            srt.anchorMin = new Vector2(0.5f, 0.2f);
            srt.anchorMax = new Vector2(1f, 0.7f);
            srt.offsetMin = Vector2.zero;
            srt.offsetMax = new Vector2(-24f, 0f);

            var hlg = stripGo.GetComponent<HorizontalLayoutGroup>();
            hlg.spacing = 8f;
            hlg.padding = new RectOffset(6, 6, 6, 6);
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;

            var containerBg = stripGo.AddComponent<Image>();
            containerBg.sprite = UiBuilder.RoundedSprite;
            containerBg.type = Image.Type.Sliced;
            containerBg.color = new Color(0.04f, 0.09f, 0.16f, 1f);
            UiBuilder.AddOutline(stripGo, UiBuilder.BorderSubtle, new Vector2(1f, -1f));

            AddAmbiencePick(stripGo.transform, "风声", PlayerSave.AmbienceClassicWind);
            AddAmbiencePick(stripGo.transform, "洞穴", PlayerSave.AmbienceOpenCavern);
            AddAmbiencePick(stripGo.transform, "赛博", PlayerSave.AmbienceOpenCyber);
            RefreshAmbiencePickStyle();
        }

        private void AddAmbiencePick(Transform parent, string label, string ambienceId)
        {
            var btn = UiBuilder.CreateButton("Ambience_" + ambienceId, parent, label,
                Color.clear, UiBuilder.TextSecondary, 20, rounded: true);
            var img = btn.GetComponent<Image>();
            var labelText = btn.GetComponentInChildren<Text>();
            ambiencePickButtons.Add((btn, img, labelText, ambienceId));
            btn.onClick.AddListener(() =>
            {
                PlayerSave.AmbienceTrack = ambienceId;
                PlayerSave.Flush();
                RefreshAmbiencePickStyle();
            });
        }

        private void RefreshAmbiencePickStyle()
        {
            var cur = PlayerSave.AmbienceTrack;
            foreach (var (btn, img, label, ambienceId) in ambiencePickButtons)
            {
                if (btn == null) continue;
                var sel = cur == ambienceId;
                img.color = sel ? UiBuilder.AccentCyan : new Color(0f, 0f, 0f, 0f);
                if (label != null)
                {
                    label.color = sel ? new Color(0.04f, 0.08f, 0.14f, 1f) : UiBuilder.TextSecondary;
                    label.fontStyle = sel ? FontStyle.Bold : FontStyle.Normal;
                }
            }
        }

        private void BuildSfxStyleRow()
        {
            var row = NewRow("音效风格", "经典为原版；开源包使用新增 UI/SFX 资源。", 142f);
            var stripGo = new GameObject("SfxStyleStrip", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            stripGo.transform.SetParent(row.transform, false);
            var srt = (RectTransform)stripGo.transform;
            srt.anchorMin = new Vector2(0.56f, 0.2f);
            srt.anchorMax = new Vector2(1f, 0.7f);
            srt.offsetMin = Vector2.zero;
            srt.offsetMax = new Vector2(-24f, 0f);

            var hlg = stripGo.GetComponent<HorizontalLayoutGroup>();
            hlg.spacing = 8f;
            hlg.padding = new RectOffset(6, 6, 6, 6);
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;

            var containerBg = stripGo.AddComponent<Image>();
            containerBg.sprite = UiBuilder.RoundedSprite;
            containerBg.type = Image.Type.Sliced;
            containerBg.color = new Color(0.04f, 0.09f, 0.16f, 1f);
            UiBuilder.AddOutline(stripGo, UiBuilder.BorderSubtle, new Vector2(1f, -1f));

            AddSfxStylePick(stripGo.transform, "经典", PlayerSave.SfxStyleClassic);
            AddSfxStylePick(stripGo.transform, "开源包", PlayerSave.SfxStyleOpenPack);
            RefreshSfxStylePickStyle();
        }

        private void AddSfxStylePick(Transform parent, string label, string styleId)
        {
            var btn = UiBuilder.CreateButton("SfxStyle_" + styleId, parent, label,
                Color.clear, UiBuilder.TextSecondary, 20, rounded: true);
            var img = btn.GetComponent<Image>();
            var labelText = btn.GetComponentInChildren<Text>();
            sfxStylePickButtons.Add((btn, img, labelText, styleId));
            btn.onClick.AddListener(() =>
            {
                PlayerSave.SfxStyle = styleId;
                PlayerSave.Flush();
                RefreshSfxStylePickStyle();
            });
        }

        private void RefreshSfxStylePickStyle()
        {
            var cur = PlayerSave.SfxStyle;
            foreach (var (btn, img, label, styleId) in sfxStylePickButtons)
            {
                if (btn == null) continue;
                var sel = cur == styleId;
                img.color = sel ? UiBuilder.AccentCyan : new Color(0f, 0f, 0f, 0f);
                if (label != null)
                {
                    label.color = sel ? new Color(0.04f, 0.08f, 0.14f, 1f) : UiBuilder.TextSecondary;
                    label.fontStyle = sel ? FontStyle.Bold : FontStyle.Normal;
                }
            }
        }

        private void BuildSfxRow()
        {
            var row = NewRow("音效", "跳跃、拾取等音效（本局已开始时下一局生效）。");
            sfxToggle = AddModernToggle(row, PlayerSave.SfxEnabled);
            sfxToggle.onValueChanged.AddListener(on =>
            {
                PlayerSave.SfxEnabled = on;
                PlayerSave.Flush();
            });
        }

        private void BuildHapticRow()
        {
            var row = NewRow("震动强度", "0 关闭；1 设备最大。", 180f);

            // 数值显示
            hapticValueText = UiBuilder.CreateText("HapticValue", row,
                $"{PlayerSave.HapticIntensity * 100f:F0}%",
                26, FontStyle.Bold, TextAnchor.MiddleRight, UiBuilder.AccentCyan);
            var vrt = (RectTransform)hapticValueText.transform;
            vrt.anchorMin = new Vector2(1f, 1f);
            vrt.anchorMax = new Vector2(1f, 1f);
            vrt.pivot = new Vector2(1f, 1f);
            vrt.sizeDelta = new Vector2(80f, 36f);
            vrt.anchoredPosition = new Vector2(-24f, -20f);

            var sliderGo = new GameObject("Slider", typeof(RectTransform), typeof(Slider));
            sliderGo.transform.SetParent(row.transform, false);
            var rt = (RectTransform)sliderGo.transform;
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(0f, 32f);
            rt.offsetMin = new Vector2(20f, 22f);
            rt.offsetMax = new Vector2(-20f, 22f);

            var bg = new GameObject("Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            bg.transform.SetParent(sliderGo.transform, false);
            var bgrt = (RectTransform)bg.transform;
            bgrt.anchorMin = new Vector2(0f, 0.5f);
            bgrt.anchorMax = new Vector2(1f, 0.5f);
            bgrt.sizeDelta = new Vector2(0f, 8f);
            var bgImg = bg.GetComponent<Image>();
            bgImg.color = new Color(0.04f, 0.09f, 0.16f, 1f);
            bgImg.sprite = UiBuilder.RoundedSprite;
            bgImg.type = Image.Type.Sliced;
            UiBuilder.AddOutline(bg, UiBuilder.BorderSubtle, new Vector2(1f, -1f));

            var fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(sliderGo.transform, false);
            var fart = (RectTransform)fillArea.transform;
            fart.anchorMin = new Vector2(0f, 0.5f);
            fart.anchorMax = new Vector2(1f, 0.5f);
            fart.sizeDelta = new Vector2(0f, 8f);
            fart.offsetMin = new Vector2(2f, fart.offsetMin.y);
            fart.offsetMax = new Vector2(-2f, fart.offsetMax.y);

            var fill = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            fill.transform.SetParent(fillArea.transform, false);
            var fillRt = (RectTransform)fill.transform;
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = new Vector2(0f, 1f);
            fillRt.sizeDelta = Vector2.zero;
            var fillImg = fill.GetComponent<Image>();
            fillImg.color = UiBuilder.AccentCyan;
            fillImg.sprite = UiBuilder.RoundedSprite;
            fillImg.type = Image.Type.Sliced;

            var handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
            handleArea.transform.SetParent(sliderGo.transform, false);
            var hart = (RectTransform)handleArea.transform;
            hart.anchorMin = Vector2.zero;
            hart.anchorMax = Vector2.one;
            hart.offsetMin = Vector2.zero;
            hart.offsetMax = Vector2.zero;

            var handle = new GameObject("Handle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            handle.transform.SetParent(handleArea.transform, false);
            var handleRt = (RectTransform)handle.transform;
            handleRt.sizeDelta = new Vector2(36f, 36f);
            var handleImg = handle.GetComponent<Image>();
            handleImg.color = Color.white;
            handleImg.sprite = UiBuilder.CircleSprite;
            UiBuilder.AddOutline(handle, UiBuilder.AccentCyan, new Vector2(2f, -2f));
            UiBuilder.AddShadow(handle, new Color(0f, 0f, 0f, 0.45f), new Vector2(0f, -3f));

            var slider = sliderGo.GetComponent<Slider>();
            slider.fillRect = fillRt;
            slider.handleRect = handleRt;
            slider.targetGraphic = handleImg;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = PlayerSave.HapticIntensity;
            hapticSlider = slider;
            slider.onValueChanged.AddListener(v =>
            {
                PlayerSave.HapticIntensity = v;
                PlayerSave.Flush();
                if (hapticValueText != null)
                    hapticValueText.text = $"{v * 100f:F0}%";
            });
        }

        private RectTransform NewRow(string title, string desc, float height = 156f)
        {
            var row = UiBuilder.CreateScrollListRow("Row_" + title, contentRoot, height,
                UiBuilder.Surface1);
            UiBuilder.AddOutline(row.gameObject, UiBuilder.BorderSubtle, new Vector2(1f, -1f));

            var t = UiBuilder.CreateText("Title", row, title,
                26, FontStyle.Bold, TextAnchor.UpperLeft, Color.white);
            var trt = (RectTransform)t.transform;
            trt.anchorMin = new Vector2(0f, 1f);
            trt.anchorMax = new Vector2(0.5f, 1f);
            trt.pivot = new Vector2(0f, 1f);
            trt.sizeDelta = new Vector2(0f, 40f);
            trt.anchoredPosition = new Vector2(24f, -18f);

            var d = UiBuilder.CreateText("Desc", row, desc,
                19, FontStyle.Normal, TextAnchor.UpperLeft, UiBuilder.TextSecondary);
            var drt = (RectTransform)d.transform;
            drt.anchorMin = new Vector2(0f, 1f);
            drt.anchorMax = new Vector2(0.5f, 1f);
            drt.pivot = new Vector2(0f, 1f);
            drt.sizeDelta = new Vector2(0f, 70f);
            drt.anchoredPosition = new Vector2(24f, -60f);
            return row;
        }

        /// <summary>
        /// 现代化开关：iOS 风格的 toggle pill，开/关有清晰的状态对比。
        /// </summary>
        private Toggle AddModernToggle(RectTransform parent, bool initial)
        {
            var toggleGo = new GameObject("Toggle", typeof(RectTransform), typeof(Toggle));
            toggleGo.transform.SetParent(parent, false);
            var rt = (RectTransform)toggleGo.transform;
            rt.anchorMin = new Vector2(1f, 0.5f);
            rt.anchorMax = new Vector2(1f, 0.5f);
            rt.pivot = new Vector2(1f, 0.5f);
            rt.sizeDelta = new Vector2(96f, 48f);
            rt.anchoredPosition = new Vector2(-24f, 0f);

            var bg = new GameObject("Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            bg.transform.SetParent(toggleGo.transform, false);
            var bgrt = (RectTransform)bg.transform;
            bgrt.anchorMin = Vector2.zero;
            bgrt.anchorMax = Vector2.one;
            bgrt.sizeDelta = Vector2.zero;
            var bgImg = bg.GetComponent<Image>();
            bgImg.color = initial ? UiBuilder.AccentCyan : new Color(0.18f, 0.25f, 0.34f, 1f);
            bgImg.sprite = UiBuilder.RoundedSprite;
            bgImg.type = Image.Type.Sliced;
            UiBuilder.AddOutline(bg,
                initial ? new Color(0.3f, 1f, 1f, 0.55f) : UiBuilder.BorderSubtle,
                new Vector2(1f, -1f));

            var knob = new GameObject("Knob", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            knob.transform.SetParent(toggleGo.transform, false);
            var krt = (RectTransform)knob.transform;
            krt.anchorMin = new Vector2(0f, 0.5f);
            krt.anchorMax = new Vector2(0f, 0.5f);
            krt.pivot = new Vector2(0.5f, 0.5f);
            krt.sizeDelta = new Vector2(38f, 38f);
            krt.anchoredPosition = new Vector2(initial ? 72f : 24f, 0f);
            var knobImg = knob.GetComponent<Image>();
            knobImg.color = Color.white;
            knobImg.sprite = UiBuilder.CircleSprite;
            UiBuilder.AddShadow(knob, new Color(0f, 0f, 0f, 0.45f), new Vector2(0f, -2f));

            var t = toggleGo.GetComponent<Toggle>();
            t.targetGraphic = bgImg;
            t.graphic = knobImg;
            t.isOn = initial;

            t.onValueChanged.AddListener(on =>
            {
                bgImg.color = on ? UiBuilder.AccentCyan : new Color(0.18f, 0.25f, 0.34f, 1f);
                krt.anchoredPosition = new Vector2(on ? 72f : 24f, 0f);
                var outline = bg.GetComponent<Outline>();
                if (outline != null)
                    outline.effectColor = on ? new Color(0.3f, 1f, 1f, 0.55f) : UiBuilder.BorderSubtle;
            });

            return t;
        }

        private void Apply()
        {
            if (nicknameField != null) PlayerSave.PlayerNickname = nicknameField.text;
            if (bgmToggle != null) PlayerSave.BgmEnabled = bgmToggle.isOn;
            if (sfxToggle != null) PlayerSave.SfxEnabled = sfxToggle.isOn;
            if (hapticSlider != null) PlayerSave.HapticIntensity = hapticSlider.value;
            RefreshLobbyPickStyle();
            PlayerSave.Flush();
            GameAudioSettings.NotifyLobbyBgmChanged();
            dispatch(MainMenuBootstrap.PanelEvent.RefreshHeader, null);
        }
    }
}
