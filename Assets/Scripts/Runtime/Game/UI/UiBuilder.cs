using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PenguinRun.Game.UI
{
    /// <summary>
    /// 程序化 UGUI 构建工具：避免手写 .unity YAML，按代码搭面板。
    /// 重构后的统一设计系统：现代化配色、间距规范、组件库。
    /// </summary>
    public static class UiBuilder
    {
        // ── 主色（Primary Palette）─────────────────────────────────────────────
        /// <summary>
        /// 选中/激活态强调色（Tab 选中填充、强调文字、次级 CTA）。
        ///
        /// 童话纸张改版是靠本别名层整体重映射完成的（SurfaceBase / Surface1 / Surface2 /
        /// WarmGold 都在那一批改掉了），唯独这一项被漏下，仍指向旧霓虹色板的
        /// <c>UiTheme.AccentCyan</c>。结果是营地商店、图鉴、排行榜、结算页与死亡页里
        /// 所有「选中」「主操作」元素都在米黄纸面上渲染成霓虹青。
        /// 旧名 <c>AccentCyan</c> 一并改名：一个叫 Cyan 的绿色正是这类 bug 复发的温床。
        ///
        /// 取 PineGreen 而非 CoralAction：CoralAction 已经是 <see cref="WarmGold"/>，
        /// 两者若同色，`isBest ? WarmGold : AccentActive` 这类分支会渲染成同一个颜色。
        /// 白字在 PineGreen 上对比度 5.1:1，满足 AA。
        /// </summary>
        public static readonly Color AccentActive = UiTheme.PineGreen;
        /// <summary>次要强调色：柔和金色，用于奖励、货币。</summary>
        public static readonly Color WarmGold = UiTheme.CoralAction;
        /// <summary>成功/已完成色。</summary>
        public static readonly Color Success = UiTheme.Success;
        /// <summary>警告/危险色。</summary>
        public static readonly Color Warning = UiTheme.Warning;
        /// <summary>错误/提示红色。</summary>
        public static readonly Color Danger = UiTheme.Danger;

        // ── 背景色（Surface Palette）──────────────────────────────────────────
        /// <summary>最深的应用底色（场景背景）。</summary>
        public static readonly Color SurfaceBase = UiTheme.PaperBase;
        /// <summary>第一层卡片/面板背景。</summary>
        public static readonly Color Surface1 = UiTheme.PaperWarm;
        /// <summary>第二层（悬浮卡片、Tab容器）。</summary>
        public static readonly Color Surface2 = Color.Lerp(UiTheme.PaperWarm, UiTheme.MistBlue, 0.28f);
        /// <summary>高亮 surface（hover/选中态）。</summary>
        public static readonly Color SurfaceElevated = Color.white;

        // ── 兼容旧字段 ────────────────────────────────────────────────────────
        public static readonly Color PanelBg = new(0.024f, 0.078f, 0.137f, 0.85f);
        /// <summary>全屏功能页根节点底色（不透明，遮挡下层主菜单）。</summary>
        public static readonly Color MenuPanelSolidBg = new(0.04f, 0.09f, 0.16f, 1f);
        /// <summary>功能页内层背景（略浅，与顶栏区分）。</summary>
        public static readonly Color MenuPanelInnerBg = new(0.07f, 0.14f, 0.24f, 1f);
        public static readonly Color PanelStroke = new(0.0f, 0.85f, 0.95f, 0.3f);

        // ── 文字色（Text Palette）─────────────────────────────────────────────
        public static readonly Color TextPrimary = UiTheme.InkPrimary;
        public static readonly Color TextSecondary = UiTheme.InkSecondary;
        public static readonly Color TextTertiary = UiTheme.TextTertiary;
        /// <summary>反向文字色（用于亮色按钮上的文字）。</summary>
        public static readonly Color TextOnAccent = Color.white;

        // ── 边框色 ─────────────────────────────────────────────────────────────
        public static readonly Color BorderSubtle = new(0.25f, 0.5f, 0.7f, 0.2f);
        public static readonly Color BorderDefault = new(0.3f, 0.6f, 0.8f, 0.35f);
        public static readonly Color BorderAccent = new(0.0f, 0.85f, 0.95f, 0.55f);

        // ── 间距规范（8px grid）───────────────────────────────────────────────
        public const float Spacing1 = UiTheme.Spacing1;
        public const float Spacing2 = UiTheme.Spacing2;
        public const float Spacing3 = UiTheme.Spacing3;
        public const float Spacing4 = UiTheme.Spacing4;
        public const float Spacing5 = UiTheme.Spacing5;
        public const float Spacing6 = UiTheme.Spacing6;

        /// <summary>与 <see cref="PanelHeader"/> 高度一致；列表区域用 <see cref="PanelScrollListTopInset"/>。</summary>
        public const float PanelHeaderHeightPixels = 188f;

        /// <summary>全屏列表距顶：顶栏 + 小间距。</summary>
        public static float PanelScrollListTopInset => PanelHeaderHeightPixels + 12f;

        /// <summary>
        /// 为已有 RectTransform 设置滚动行高度（子节点用 <see cref="CreateScrollListRow"/> 时可不调用）。
        /// 优先级高于 <see cref="Graphic"/> 的布局参与，否则同物体上的 Image 会把行高算成 0。
        /// </summary>
        public const int ScrollItemLayoutPriority = 1000;

        public static void SetScrollItemHeight(RectTransform rt, float height)
        {
            if (rt == null) return;
            var le = rt.gameObject.GetComponent<LayoutElement>();
            if (le == null) le = rt.gameObject.AddComponent<LayoutElement>();
            le.minHeight = height;
            le.preferredHeight = height;
            le.layoutPriority = ScrollItemLayoutPriority;
        }

        /// <summary>
        /// 在子节点全部挂好之后调用：先按 LayoutElement 汇总一次高度（兜底），再触发 LayoutRebuilder
        /// 让 <see cref="ContentSizeFitter"/> + <see cref="VerticalLayoutGroup"/> 完成最终排版。
        /// 必须在 ScrollView 的所有祖先（panelRoot/canvas）已经存在并完成一次布局之后调用，否则
        /// Viewport 尺寸可能仍为 0，导致 Mask 把所有子节点剪裁掉看不见。
        /// </summary>
        public static void RebuildScrollContent(RectTransform content)
        {
            if (content == null) return;
            var vlg = content.GetComponent<VerticalLayoutGroup>();
            if (vlg == null) return;

            var n = content.childCount;
            float height = vlg.padding.top + vlg.padding.bottom;
            for (var i = 0; i < n; i++)
            {
                var rect = content.GetChild(i) as RectTransform;
                if (rect == null) continue;
                var le = rect.GetComponent<LayoutElement>();
                var h = 0f;
                if (le != null) h = Mathf.Max(le.minHeight, le.preferredHeight);
                if (h <= 0f) h = LayoutUtility.GetPreferredHeight(rect);
                if (h <= 0f) h = rect.rect.height;
                height += Mathf.Max(h, 2f);
                if (i < n - 1)
                    height += vlg.spacing;
            }

            // 即便挂了 ContentSizeFitter，先显式写一次 sizeDelta 也能避免「父级在第一帧未完成布局时
            // ContentSizeFitter 拿不到 viewport 宽度→子节点期望宽度=0→preferredHeight=0」的连锁失效。
            content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Max(height, 4f));

            // 先在外层强制刷新：ScrollView 的 listRoot 容器靠 offsetMin/Max 撑开尺寸，
            // 必须先让它解析自己的 rect，Content 内部 VLG 才能知道目标宽度。
            // 之前只对 content 调用 ForceRebuildLayoutImmediate，首帧打开面板时父链尺寸仍是 0，
            // 导致 VLG 算出的子节点宽度也是 0 → 行内文字/控件全部看不见。
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
            Canvas.ForceUpdateCanvases();
        }

        /// <summary>
        /// Scroll 列表行：根物体只有 <see cref="LayoutElement"/>（无 Image），彩色底图在子节点上且 <c>ignoreLayout</c>，
        /// 避免同一物体上 Image 的 ILayoutElement 把首选高度压成 0。
        /// 使用 pivot (0.5, 1) 让 VerticalLayoutGroup 能从顶部正确堆叠（默认 0.5, 0.5 会导致行体一半在锚框外）。
        /// </summary>
        /// <param name="rowBackground">非空时添加全铺底图；空状态等可传 <c>null</c>。</param>
        public static RectTransform CreateScrollListRow(string name, Transform parent, float height, Color? rowBackground)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(0f, height);
            rt.anchoredPosition = Vector2.zero;

            var le = go.AddComponent<LayoutElement>();
            le.minHeight = height;
            le.preferredHeight = height;
            le.flexibleHeight = -1f;
            le.layoutPriority = ScrollItemLayoutPriority;

            if (rowBackground.HasValue)
            {
                var bgGo = new GameObject("RowBg", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                bgGo.transform.SetParent(go.transform, false);
                var brt = (RectTransform)bgGo.transform;
                brt.anchorMin = Vector2.zero;
                brt.anchorMax = Vector2.one;
                brt.sizeDelta = Vector2.zero;
                brt.offsetMin = Vector2.zero;
                brt.offsetMax = Vector2.zero;
                var ign = bgGo.AddComponent<LayoutElement>();
                ign.ignoreLayout = true;

                var rowImg = bgGo.GetComponent<Image>();
                rowImg.sprite = RoundedSprite;
                rowImg.type = Image.Type.Sliced;
                rowImg.color = rowBackground.Value;
            }

            // 前若干行按兄弟序号错峰淡入（一次性，只动 scale/alpha，不与 LayoutGroup 抢位置）。
            if (Application.isPlaying)
                rt.PlayListItemEnter(rt.GetSiblingIndex());

            return rt;
        }

        private static Sprite _roundedSprite;
        private static Sprite _circleSprite;
        private static Sprite _triangleSprite;
        private static Sprite _glowSprite;
        private static Sprite _solidUiSprite;

        public static Sprite RoundedSprite
        {
            get
            {
                if (_roundedSprite == null)
                    _roundedSprite = CreateUISlicedFallback();
                return _roundedSprite;
            }
        }

        public static Sprite CircleSprite
        {
            get
            {
                if (_circleSprite == null)
                    _circleSprite = CreateCircleFallback();
                return _circleSprite;
            }
        }

        public static Sprite TriangleSprite
        {
            get
            {
                if (_triangleSprite == null)
                    _triangleSprite = CreateTriangleFallback();
                return _triangleSprite;
            }
        }

        public static Sprite GlowSprite
        {
            get
            {
                if (_glowSprite == null)
                    _glowSprite = CreateGlowFallback();
                return _glowSprite;
            }
        }

        /// <summary>无九宫格、用于纯色块 / Mask 底图；Unity 6 中空 Sprite 的 Image 可能整片不画。</summary>
        public static Sprite SolidUiSprite
        {
            get
            {
                if (_solidUiSprite == null)
                {
                    var t = new Texture2D(2, 2, TextureFormat.RGBA32, false)
                    {
                        wrapMode = TextureWrapMode.Clamp,
                        filterMode = FilterMode.Bilinear,
                    };
                    for (var y = 0; y < 2; y++)
                    for (var x = 0; x < 2; x++)
                        t.SetPixel(x, y, Color.white);
                    t.Apply();
                    _solidUiSprite = Sprite.Create(t, new Rect(0f, 0f, 2f, 2f), new Vector2(0.5f, 0.5f), 100f);
                }
                return _solidUiSprite;
            }
        }

        /// <summary>
        /// Unity 6 等版本可能无法通过 GetBuiltinResource 加载旧版 UI 图集；用程序纹理保证圆角/圆形可用。
        /// </summary>
        private static Sprite CreateUISlicedFallback()
        {
            const int s = 32;
            const int b = 10;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };
            var px = Color.white;
            for (var y = 0; y < s; y++)
            for (var x = 0; x < s; x++)
                tex.SetPixel(x, y, px);
            tex.Apply();
            var border = new Vector4(b, b, b, b);
            return Sprite.Create(tex, new Rect(0f, 0f, s, s), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
        }

        private static Sprite CreateCircleFallback()
        {
            const int d = 64;
            var tex = new Texture2D(d, d, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };
            var clear = new Color(1f, 1f, 1f, 0f);
            var r = d * 0.5f - 0.5f;
            var cx = d * 0.5f - 0.5f;
            var cy = d * 0.5f - 0.5f;
            for (var y = 0; y < d; y++)
            for (var x = 0; x < d; x++)
            {
                var dx = x - cx;
                var dy = y - cy;
                tex.SetPixel(x, y, dx * dx + dy * dy <= r * r ? Color.white : clear);
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0f, 0f, d, d), new Vector2(0.5f, 0.5f), 100f);
        }

        private static Sprite CreateTriangleFallback()
        {
            const int s = 64;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };
            var clear = new Color(1f, 1f, 1f, 0f);
            var mid = (s - 1) * 0.5f;
            for (var y = 0; y < s; y++)
            for (var x = 0; x < s; x++)
            {
                var t = y / (float)(s - 1);
                var halfWidth = Mathf.Lerp(mid, 0f, t);
                tex.SetPixel(x, y, Mathf.Abs(x - mid) <= halfWidth ? Color.white : clear);
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0f, 0f, s, s), new Vector2(0.5f, 0.5f), 100f);
        }

        private static Sprite CreateGlowFallback()
        {
            const int d = 128;
            var tex = new Texture2D(d, d, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };
            var r = d * 0.5f - 0.5f;
            var cx = d * 0.5f - 0.5f;
            var cy = d * 0.5f - 0.5f;
            for (var y = 0; y < d; y++)
            for (var x = 0; x < d; x++)
            {
                var dx = x - cx;
                var dy = y - cy;
                var dist = Mathf.Sqrt(dx * dx + dy * dy);
                var a = Mathf.Clamp01(1f - (dist / r));
                a = a * a * (3f - 2f * a);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0f, 0f, d, d), new Vector2(0.5f, 0.5f), 100f);
        }

        public static GameObject CreateCanvas(string name, Transform parent = null, int sortingOrder = 0)
        {
            var canvasGo = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            if (parent != null) canvasGo.transform.SetParent(parent, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            return canvasGo;
        }

        public static GameObject CreatePanel(string name, Transform parent, Color? bg = null)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
            var img = go.GetComponent<Image>();
            img.sprite = SolidUiSprite;
            img.type = Image.Type.Simple;
            img.color = bg ?? new Color(0f, 0f, 0f, 0f);
            return go;
        }

        public static RectTransform CreateRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 sizeDelta, Vector2 anchoredPosition, Color? bg = null, bool rounded = false, bool circle = false, bool glow = false)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.sizeDelta = sizeDelta;
            rt.anchoredPosition = anchoredPosition;
            if (bg.HasValue)
            {
                var img = go.AddComponent<Image>();
                img.color = bg.Value;
                if (glow)
                {
                    img.sprite = GlowSprite;
                }
                else if (circle)
                {
                    img.sprite = CircleSprite;
                }
                else if (rounded)
                {
                    img.sprite = RoundedSprite;
                    img.type = Image.Type.Sliced;
                }
                else
                {
                    img.sprite = SolidUiSprite;
                    img.type = Image.Type.Simple;
                }
            }
            return rt;
        }

        public static RectTransform CreateTriangle(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 sizeDelta, Vector2 anchoredPosition, Color bg)
        {
            var rt = CreateRect(name, parent, anchorMin, anchorMax, sizeDelta, anchoredPosition, bg);
            var img = rt.GetComponent<Image>();
            img.sprite = TriangleSprite;
            img.type = Image.Type.Simple;
            return rt;
        }

        /// <summary>创建 TMP 文本（历史签名保留：面板层已全面切换 TextMeshPro，不再产出 legacy Text）。</summary>
        public static TextMeshProUGUI CreateText(string name, Transform parent, string content, int fontSize = UiTheme.Body, FontStyle style = FontStyle.Normal, TextAnchor anchor = TextAnchor.MiddleLeft, Color? color = null)
            => CreateTmpText(name, parent, content, fontSize, style, anchor, color);

        /// <summary>Creates a dynamic CJK-capable TMP label for newly migrated UI surfaces.</summary>
        public static TextMeshProUGUI CreateTmpText(string name, Transform parent, string content, int fontSize = UiTheme.Body, FontStyle style = FontStyle.Normal, TextAnchor anchor = TextAnchor.MiddleLeft, Color? color = null)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<TextMeshProUGUI>();
            text.text = content;
            text.font = UiTheme.TmpFont;
            text.fontSize = fontSize;
            text.fontStyle = style == FontStyle.Bold ? FontStyles.Bold : FontStyles.Normal;
            text.alignment = ToTmpAlignment(anchor);
            text.color = color ?? TextPrimary;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Overflow;
            text.raycastTarget = false;
            return text;
        }

        private static TextAlignmentOptions ToTmpAlignment(TextAnchor anchor)
        {
            return anchor switch
            {
                TextAnchor.UpperLeft => TextAlignmentOptions.TopLeft,
                TextAnchor.UpperCenter => TextAlignmentOptions.Top,
                TextAnchor.UpperRight => TextAlignmentOptions.TopRight,
                TextAnchor.MiddleLeft => TextAlignmentOptions.Left,
                TextAnchor.MiddleCenter => TextAlignmentOptions.Center,
                TextAnchor.MiddleRight => TextAlignmentOptions.Right,
                TextAnchor.LowerLeft => TextAlignmentOptions.BottomLeft,
                TextAnchor.LowerCenter => TextAlignmentOptions.Bottom,
                TextAnchor.LowerRight => TextAlignmentOptions.BottomRight,
                _ => TextAlignmentOptions.Left,
            };
        }

        /// <summary>支持富文本的标题等（如 &lt;color&gt;、&lt;size&gt;）。</summary>
        public static TextMeshProUGUI CreateRichText(string name, Transform parent, string content, int fontSize = UiTheme.Body, FontStyle style = FontStyle.Normal, TextAnchor anchor = TextAnchor.MiddleLeft, Color? color = null)
        {
            var text = CreateText(name, parent, content, fontSize, style, anchor, color);
            text.richText = true;
            return text;
        }

        public static void AddShadow(GameObject go, Color color, Vector2 distance)
        {
            var shadow = go.AddComponent<Shadow>();
            shadow.effectColor = color;
            shadow.effectDistance = distance;
        }

        public static void AddOutline(GameObject go, Color color, Vector2 distance)
        {
            var outline = go.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = distance;
        }

        public static Button CreateButton(string name, Transform parent, string label, Color bg, Color fg, int fontSize = 30, bool rounded = true)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.color = bg;
            if (rounded)
            {
                img.sprite = RoundedSprite;
                img.type = Image.Type.Sliced;
            }
            else
            {
                img.sprite = SolidUiSprite;
                img.type = Image.Type.Simple;
            }

            var btn = go.GetComponent<Button>();
            var colors = btn.colors;
            colors.highlightedColor = Color.Lerp(bg, Color.white, 0.18f);
            colors.pressedColor = Color.Lerp(bg, Color.black, 0.18f);
            colors.disabledColor = new Color(bg.r, bg.g, bg.b, bg.a * 0.45f);
            btn.colors = colors;

            var t = CreateTmpText("Label", go.transform, label, fontSize, FontStyle.Bold, TextAnchor.MiddleCenter, fg);
            var rt = (RectTransform)t.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
            WireClickFeedback(btn);
            return btn;
        }

        /// <summary>
        /// 为按钮绑定按压缩放反馈：PointerDown 压缩、PointerUp 回弹。
        /// 比 onClick 后置弹跳更跟手；二者会互抢 localScale，故只保留按压反馈。
        /// </summary>
        public static void WireClickFeedback(Button btn)
        {
            if (btn == null) return;
            if (!btn.TryGetComponent<PressScaleFeedback>(out _))
                btn.gameObject.AddComponent<PressScaleFeedback>();
        }

        public static Image CreateProgressFill(string name, Transform parent, Color trackColor, Color fillColor, out Image fill)
        {
            var trackGo = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            trackGo.transform.SetParent(parent, false);
            var trackImg = trackGo.GetComponent<Image>();
            trackImg.sprite = SolidUiSprite;
            trackImg.type = Image.Type.Simple;
            trackImg.color = trackColor;
            trackImg.raycastTarget = false;

            var fillGo = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            fillGo.transform.SetParent(trackGo.transform, false);
            var fillRt = (RectTransform)fillGo.transform;
            fillRt.anchorMin = new Vector2(0f, 0f);
            fillRt.anchorMax = new Vector2(0f, 1f);
            fillRt.pivot = new Vector2(0f, 0.5f);
            fillRt.sizeDelta = Vector2.zero;
            fillRt.anchoredPosition = Vector2.zero;
            fill = fillGo.GetComponent<Image>();
            fill.sprite = SolidUiSprite;
            fill.type = Image.Type.Simple;
            fill.color = fillColor;
            fill.raycastTarget = false;
            return trackImg;
        }

        public static void SetProgress01(Image fill, float v)
        {
            v = Mathf.Clamp01(v);
            var rt = (RectTransform)fill.transform;
            var parentRt = (RectTransform)rt.parent;
            var w = parentRt != null ? parentRt.rect.width : 0f;
            rt.sizeDelta = new Vector2(w * v, 0f);
        }

        public static GameObject CreateScrollView(string name, Transform parent, out RectTransform content)
        {
            var scrollGo = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(ScrollRect));
            scrollGo.transform.SetParent(parent, false);
            var scrollRt = (RectTransform)scrollGo.transform;
            scrollRt.anchorMin = Vector2.zero;
            scrollRt.anchorMax = Vector2.one;
            scrollRt.pivot = new Vector2(0.5f, 0.5f);
            scrollRt.sizeDelta = Vector2.zero;
            scrollRt.anchoredPosition = Vector2.zero;
            var scrollBg = scrollGo.GetComponent<Image>();
            scrollBg.sprite = SolidUiSprite;
            scrollBg.type = Image.Type.Simple;
            scrollBg.color = new Color(0, 0, 0, 0);
            scrollBg.raycastTarget = true;

            var maskGo = new GameObject("Viewport", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Mask));
            maskGo.transform.SetParent(scrollGo.transform, false);
            var maskRt = (RectTransform)maskGo.transform;
            maskRt.anchorMin = Vector2.zero;
            maskRt.anchorMax = Vector2.one;
            maskRt.pivot = new Vector2(0.5f, 0.5f);
            maskRt.sizeDelta = Vector2.zero;
            maskRt.anchoredPosition = Vector2.zero;
            var mvImg = maskGo.GetComponent<Image>();
            mvImg.sprite = SolidUiSprite;
            mvImg.type = Image.Type.Simple;
            // 之前用 0.001 正好等于 UI/Mask shader 里 `clip(color.a - 0.001)` 的阈值，
            // 在不少 GPU/驱动上会被判为丢弃 → mask 不写入 stencil → 所有子节点都被剪裁掉，
            // 表现就是面板内容（行/控件）全部不可见。这里改用 4% 作为透明度，对外观没有影响。
            mvImg.color = new Color(0f, 0f, 0f, 0.04f);
            mvImg.raycastTarget = true;
            var mask = maskGo.GetComponent<Mask>();
            mask.showMaskGraphic = false;

            var contentGo = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            contentGo.transform.SetParent(maskGo.transform, false);
            content = (RectTransform)contentGo.transform;
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.sizeDelta = new Vector2(0f, 0f);
            content.anchoredPosition = Vector2.zero;

            var vlg = contentGo.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 16f;
            vlg.padding = new RectOffset(24, 24, 18, 24);
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childScaleWidth = false;
            vlg.childScaleHeight = false;

            var fitter = contentGo.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = scrollGo.GetComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.content = content;
            scroll.viewport = maskRt;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.scrollSensitivity = 36f;

            return scrollGo;
        }

        // ── 现代化组件辅助方法 ──────────────────────────────────────────────

        /// <summary>
        /// 创建一张现代化卡片：圆角背景、淡边框、阴影，作为内容容器。
        /// </summary>
        public static RectTransform CreateCard(
            string name, Transform parent,
            Vector2 anchorMin, Vector2 anchorMax,
            Vector2 sizeDelta, Vector2 anchoredPosition,
            Color? bg = null, bool addBorder = true, bool addShadow = false)
        {
            var card = CreateRect(name, parent, anchorMin, anchorMax, sizeDelta, anchoredPosition,
                bg ?? Surface1, rounded: true);
            if (addBorder)
                AddOutline(card.gameObject, BorderSubtle, new Vector2(1f, -1f));
            if (addShadow)
                AddShadow(card.gameObject, new Color(0f, 0f, 0f, 0.3f), new Vector2(0f, -4f));
            return card;
        }

        /// <summary>
        /// 创建一个水平间隔条（细线分割），用于分隔区域。
        /// </summary>
        public static RectTransform CreateDivider(
            string name, Transform parent,
            float yOffset = 0f, Color? color = null, float horizontalMargin = 24f)
        {
            var divider = CreateRect(name, parent,
                new Vector2(0f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(0f, 1.5f), new Vector2(0f, yOffset),
                color ?? BorderSubtle, rounded: false);
            divider.offsetMin = new Vector2(horizontalMargin, divider.offsetMin.y);
            divider.offsetMax = new Vector2(-horizontalMargin, divider.offsetMax.y);
            return divider;
        }

        /// <summary>
        /// 现代化主操作按钮（CTA）：明亮高对比 + 边框/阴影。
        /// </summary>
        public static Button CreatePrimaryButton(
            string name, Transform parent, string label, int fontSize = 30,
            System.Action onClick = null)
        {
            // 与大厅底坞的「出发」和主 CTA「翻开旅程」同色（CoralAction + 白字）：
            // 玩家在大厅学到的「主操作长这样」在结算页与死亡页同样成立。
            // 此前是青色描边 + 青色投影的旧霓虹处理，落在米黄纸卡上像另一个产品的控件。
            var btn = CreateButton(name, parent, label, UiTheme.CoralAction, Color.white, fontSize, rounded: true);
            AddOutline(btn.gameObject, UiTheme.StitchBorder, new Vector2(2f, -2f));
            AddShadow(btn.gameObject, UiTheme.ShadowWarm, new Vector2(0f, -4f));
            if (onClick != null) btn.onClick.AddListener(() => onClick.Invoke());
            return btn;
        }

        /// <summary>
        /// 次要按钮：低对比，通常表示返回/取消。
        /// </summary>
        public static Button CreateSecondaryButton(
            string name, Transform parent, string label, int fontSize = 28,
            System.Action onClick = null)
        {
            // 纸面上的次级按钮：雾蓝实底 + 墨色字（对比度 6.97:1），明显从属于珊瑚色主 CTA。
            // 唯一调用点是死亡页的「回到营地」，那张卡是 PaperWarm；旧版给的是深海军蓝
            // 实底（Surface2 的旧值），在米黄卡上像一块挖空。
            //
            // 不用全透明「幽灵」底：AddOutline 走 uGUI 的 Shadow，会把描边 alpha 乘以源顶点
            // alpha，底色全透明时描边会一起消失，按钮就只剩一行字。
            var btn = CreateButton(name, parent, label, UiTheme.MistBlue, UiTheme.InkPrimary, fontSize, rounded: true);
            AddOutline(btn.gameObject, UiTheme.StitchBorder, new Vector2(2f, -2f));
            if (onClick != null) btn.onClick.AddListener(() => onClick.Invoke());
            return btn;
        }

        /// <summary>
        /// 危险按钮：用于退出/删除等不可逆操作。
        /// </summary>
        public static Button CreateDangerButton(
            string name, Transform parent, string label, int fontSize = 28,
            System.Action onClick = null)
        {
            var btn = CreateButton(name, parent, label, Danger, Color.white, fontSize, rounded: true);
            AddOutline(btn.gameObject, new Color(1f, 0.45f, 0.55f, 0.55f), new Vector2(1.5f, -1.5f));
            if (onClick != null) btn.onClick.AddListener(() => onClick.Invoke());
            return btn;
        }

        /// <summary>
        /// 现代化标签芯片（Chip/Badge）：用于分类、状态显示。
        /// </summary>
        public static RectTransform CreateChip(
            string name, Transform parent, string label,
            Color bgColor, Color textColor, int fontSize = 18)
        {
            var chip = CreateRect(name, parent,
                new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(80f, 28f), Vector2.zero,
                bgColor, rounded: true);
            var text = CreateText("Label", chip.transform, label, fontSize, FontStyle.Bold,
                TextAnchor.MiddleCenter, textColor);
            var trt = (RectTransform)text.transform;
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.sizeDelta = Vector2.zero;
            trt.offsetMin = new Vector2(10f, 0f);
            trt.offsetMax = new Vector2(-10f, 0f);
            return chip;
        }

        /// <summary>
        /// 货币胶囊：图标 + 数值（用于顶栏金币、奖励显示）。
        /// </summary>
        public static (RectTransform pill, TextMeshProUGUI valueText) CreateCurrencyPill(
            string name, Transform parent,
            Vector2 anchorMin, Vector2 anchorMax,
            Vector2 sizeDelta, Vector2 anchoredPosition,
            string iconChar, Color iconColor, string initialValue)
        {
            var pill = CreateRect(name, parent, anchorMin, anchorMax, sizeDelta, anchoredPosition,
                Surface2, rounded: true);
            AddOutline(pill.gameObject, new Color(iconColor.r, iconColor.g, iconColor.b, 0.45f),
                new Vector2(1.5f, -1.5f));
            AddShadow(pill.gameObject, new Color(0f, 0f, 0f, 0.3f), new Vector2(0f, -3f));

            var icon = CreateRect("Icon", pill.transform,
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(40f, 40f), new Vector2(28f, 0f),
                iconColor, circle: true);
            CreateText("IconText", icon.transform, iconChar, 16, FontStyle.Bold,
                TextAnchor.MiddleCenter, new Color(0.15f, 0.08f, 0.02f, 1f));

            var valueText = CreateText("Value", pill.transform, initialValue, 26, FontStyle.Bold,
                TextAnchor.MiddleCenter, iconColor);
            var vrt = (RectTransform)valueText.transform;
            vrt.anchorMin = new Vector2(0f, 0f);
            vrt.anchorMax = new Vector2(1f, 1f);
            vrt.offsetMin = new Vector2(56f, 0f);
            vrt.offsetMax = new Vector2(-14f, 0f);

            return (pill, valueText);
        }

        /// <summary>
        /// 一站式构建面板内容滚动区：在 panelRoot 下创建一个紧贴底部、避开顶栏与 dock 的列表容器，
        /// 内含 ScrollView/Viewport/Content（Content 自带 <see cref="ContentSizeFitter"/> + <see cref="VerticalLayoutGroup"/>）。
        /// 之前各面板手写 listRoot + offsetMin/offsetMax 的组合在某些 Canvas 缩放下会让 Content 高度计算异常，统一封装解决。
        /// </summary>
        public static GameObject CreatePanelScrollList(
            RectTransform panelRoot,
            out RectTransform content,
            float topInset = 0f,
            float bottomInset = 270f,
            string name = "ScrollList")
        {
            if (topInset <= 0f) topInset = PanelScrollListTopInset;

            var listGo = new GameObject(name, typeof(RectTransform));
            listGo.transform.SetParent(panelRoot, false);
            var listRt = (RectTransform)listGo.transform;
            listRt.anchorMin = new Vector2(0f, 0f);
            listRt.anchorMax = new Vector2(1f, 1f);
            listRt.pivot = new Vector2(0.5f, 0.5f);
            listRt.sizeDelta = Vector2.zero;
            listRt.anchoredPosition = Vector2.zero;
            // SetInsetAndSizeFromParentEdge 会改写 anchor，这里使用 offsetMin/Max 保持「上下贴边」
            listRt.offsetMin = new Vector2(0f, bottomInset);
            listRt.offsetMax = new Vector2(0f, -topInset);

            var scrollGo = CreateScrollView("Scroll", listRt, out content);
            return scrollGo;
        }
    }
}
