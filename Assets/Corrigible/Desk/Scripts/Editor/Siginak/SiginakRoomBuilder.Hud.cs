using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk.EditorTools
{
    /// <summary>
    /// Aşama 4 ekran katmanı (Overlay, sığınak kökünün altında; eski görünümde ve baskında kendiliğinden gizli):
    /// kenar satırı, AYARLAR plakası, teleks şeridi, sarı çerçeve, üzerine gelme etiketi. Kâğıt = dolgu + lif dokusu +
    /// kenar + sert gölge (PROMPT §4 “UI kâğıtları nasıl kurulur”).
    /// </summary>
    public static partial class SiginakRoomBuilder
    {
        sealed class Hud
        {
            public Canvas Canvas;
            public EdgeLine Edge;
            public SettingsPlaque Plaque;
            public TelexStrip Strip;
            public HoverTag Tag;
            public HoverHighlight Highlight;
        }

        static Sprite S(string name) => AssetDatabase.LoadAssetAtPath<Sprite>(SiginakSpriteManifest.SpritesDir + "/" + name + ".png");

        static RectTransform UiRect(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = 5;
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = anchorMin; rt.anchorMax = anchorMax; rt.pivot = pivot;
            rt.anchoredPosition = position; rt.sizeDelta = size;
            return rt;
        }

        static RectTransform Stretch(Transform parent, string name, Vector2 offset = default, float grow = 0)
        {
            var rt = UiRect(parent, name, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), offset, Vector2.one * grow * 2);
            return rt;
        }

        static Image UiImage(RectTransform rt, Sprite sprite, Color color, Image.Type type = Image.Type.Sliced, bool raycast = false)
        {
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite; img.color = color; img.type = type; img.raycastTarget = raycast;
            var le = rt.gameObject.AddComponent<LayoutElement>();
            le.ignoreLayout = true;
            return img;
        }

        static TextMeshProUGUI UiText(Transform parent, string name, string text, string role, Color color, TextAlignmentOptions align, bool localize, bool wrap = false)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = 5;
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<TextMeshProUGUI>();
            T.ApplyType(t, role);
            t.text = text; t.color = color; t.alignment = align; t.raycastTarget = false;
            t.textWrappingMode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
            if (localize) go.AddComponent<DeskLocText>();
            return t;
        }

        /// <summary>Kâğıt: gölge (token), dolgu, lif dokusu, kenar. Hepsi düzen dışı arka plan.</summary>
        static void UiPaper(RectTransform rt, Color fill, Color edge, string shadowKey, bool grain = true)
        {
            foreach (var layer in T.Shadow(shadowKey))
                UiImage(Stretch(rt, "Shadow", new Vector2(layer.offset.x, -layer.offset.y)), S("ui_fill_r2"), layer.color);
            UiImage(Stretch(rt, "Fill"), S("ui_fill_r2"), fill);
            if (grain)
            {
                var g = UiImage(Stretch(rt, "Grain"), S("tx_paper_grain"), Color.white, Image.Type.Tiled);
                g.pixelsPerUnitMultiplier = 2;
            }
            UiImage(Stretch(rt, "Edge"), S("ui_frame_1px_r2"), edge);
        }

        /// <summary>Tuş fişi: plaka kenarlı küçük kutu içinde tuş adı (CP 700 13).</summary>
        static RectTransform KeyChip(Transform parent, string key, Color border, Color text)
        {
            var rt = UiRect(parent, "Key", Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(22, 22));
            UiImage(Stretch(rt, "Box"), S("ui_frame_1px_r2"), border);
            var layout = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(6, 6, 2, 2);
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;
            var le = rt.gameObject.AddComponent<LayoutElement>();
            le.minWidth = le.minHeight = 22;
            UiText(rt, "Text", key, "keyChip", text, TextAlignmentOptions.Center, false);
            return rt;
        }

        static Hud BuildHud(Transform root, Camera view)
        {
            var hud = new Hud();
            var go = new GameObject("SiginakHud", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.layer = 5;
            go.transform.SetParent(root, false);
            hud.Canvas = go.GetComponent<Canvas>();
            hud.Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            hud.Canvas.sortingOrder = 45;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = MapProjection.DesignSize;
            scaler.matchWidthOrHeight = 1;
            var canvasRt = (RectTransform)go.transform;

            hud.Edge = BuildEdgeLine(canvasRt);
            hud.Plaque = BuildPlaque(canvasRt);
            hud.Strip = BuildStrip(canvasRt);
            hud.Highlight = BuildHighlight(canvasRt, hud.Canvas, view);
            hud.Tag = BuildTag(canvasRt, hud.Canvas);
            return hud;
        }

        // ---------- Kenar satırı (K4 notu) ----------

        static EdgeLine BuildEdgeLine(RectTransform canvas)
        {
            float h = T.Number("space.edgeLine.h", 40), pad = T.Number("space.edgeLine.padX", 16);
            var bar = UiRect(canvas, "Edge line", new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), Vector2.zero, new Vector2(0, h));
            UiImage(Stretch(bar, "Fill"), S("ui_fill_r0"), T.ColorOf("ui.edgeLine"));
            var border = UiRect(bar, "Border", new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), Vector2.zero, new Vector2(0, 1));
            UiImage(border, S("ui_fill_r0"), T.ColorOf("ui.edgeLineBorder"));
            var values = UiText(bar, "Values", "", "edgeLine", T.ColorOf("ui.edgeLineValue"), TextAlignmentOptions.MidlineLeft, false);
            var vrt = values.rectTransform;
            vrt.anchorMin = new Vector2(0, 0); vrt.anchorMax = new Vector2(0.55f, 1); vrt.offsetMin = new Vector2(pad, 0); vrt.offsetMax = Vector2.zero;

            var chipsRt = UiRect(bar, "Chips", new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-pad, 0), new Vector2(900, h - 8));
            var layout = chipsRt.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = T.Number("space.edgeLine.gap", 18) * 0.5f;
            layout.childAlignment = TextAnchor.MiddleRight;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;
            var fitter = chipsRt.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

            string[] keys = { "1", "2", "3", "H", "Home", "Esc" };
            string[] labels = { SiginakRoomWords.KeyCommand, SiginakRoomWords.KeyShelter, SiginakRoomWords.KeyMissions, SiginakRoomWords.KeyGuide, SiginakRoomWords.KeyDesk, SiginakRoomWords.Settings };
            var buttons = new Button[keys.Length];
            var backs = new Image[keys.Length];
            var chipLabels = new TMP_Text[keys.Length];
            for (int i = 0; i < keys.Length; i++)
            {
                var chip = UiRect(chipsRt, "Chip " + keys[i], Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(100, h - 10));
                backs[i] = UiImage(Stretch(chip, "Back"), S("ui_fill_r3"), Color.clear, Image.Type.Sliced, true);
                var cl = chip.gameObject.AddComponent<HorizontalLayoutGroup>();
                cl.padding = new RectOffset(6, 8, 3, 3);
                cl.spacing = 6;
                cl.childAlignment = TextAnchor.MiddleCenter;
                cl.childControlWidth = cl.childControlHeight = true;
                cl.childForceExpandWidth = cl.childForceExpandHeight = false;
                KeyChip(chip, keys[i], T.ColorOf("ui.edgeLineBorder"), T.ColorOf("ui.edgeLineValue"));
                chipLabels[i] = UiText(chip, "Label", labels[i], "edgeLine", T.ColorOf("ui.edgeLineActive"), TextAlignmentOptions.MidlineLeft, true);
                buttons[i] = chip.gameObject.AddComponent<Button>();
                buttons[i].targetGraphic = backs[i];
                buttons[i].transition = Selectable.Transition.None;
            }
            // Bileşen çubuğun değil canvas'ın üstünde: çubuk kapanınca da çalışır
            var edge = canvas.gameObject.AddComponent<EdgeLine>();
            edge.Configure(bar.gameObject, values, buttons, backs, chipLabels);
            bar.gameObject.SetActive(false);
            return edge;
        }

        // ---------- AYARLAR / ATLA plakası (K1 notu) ----------

        static SettingsPlaque BuildPlaque(RectTransform canvas)
        {
            float h = T.Number("space.plaque.h", 36);
            var rt = UiRect(canvas, "Settings plaque", Vector2.one, Vector2.one, Vector2.one, new Vector2(-16, -14), new Vector2(150, h));
            var bg = UiImage(Stretch(rt, "Fill"), S("ui_fill_r3"), T.ColorOf("plaque.bg"), Image.Type.Sliced, true);
            UiImage(Stretch(rt, "Edge"), S("ui_frame_1px_r2"), T.ColorOf("plaque.border"));
            foreach (var layer in T.Shadow("plaque"))
            {
                var sh = UiImage(Stretch(rt, "Shadow", new Vector2(layer.offset.x, -layer.offset.y)), S("ui_fill_r3"), layer.color);
                sh.transform.SetAsFirstSibling();
            }
            var layout = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(8, 12, 0, 0);
            layout.spacing = T.Number("space.plaque.gap", 8);
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;
            var fitter = rt.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            var key = KeyChip(rt, "Esc", T.ColorOf("plaque.border"), T.ColorOf("plaque.clock")).GetComponentInChildren<TMP_Text>();
            var label = UiText(rt, "Label", SiginakRoomWords.Settings, "plaqueButton", T.ColorOf("plaque.brass"), TextAlignmentOptions.MidlineLeft, false);
            var button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = bg;
            button.transition = Selectable.Transition.None;
            var plaque = canvas.gameObject.AddComponent<SettingsPlaque>();
            plaque.Configure(rt.gameObject, button, key, label, SiginakRoomWords.Settings, SiginakRoomWords.SpaceKey, SiginakRoomWords.Skip);
            return plaque;
        }

        // ---------- Teleks şeridi (K4 notu) ----------

        static TelexStrip BuildStrip(RectTransform canvas)
        {
            float w = T.Number("space.telexStrip.w", 820), h = T.Number("space.telexStrip.h", 52), hole = T.Number("space.telexStrip.holeColumn", 10);
            var rt = UiRect(canvas, "Telex strip", new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 18), new Vector2(w, h));
            UiPaper(rt, T.ColorOf("ui.telexStrip[0]"), T.ColorOf("ui.telexStrip[1]"), "eventSlip");
            foreach (var (name, x) in new[] { ("Holes left", 12f), ("Holes right", w - 12 - hole) })
            {
                var col = UiRect(rt, name, new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, 0.5f), new Vector2(x, 0), new Vector2(hole, -8));
                UiImage(col, S("ui_telex_holes"), Color.white, Image.Type.Tiled);
            }
            var text = UiText(rt, "Text", "", "telexMessage", T.ColorOf("ink"), TextAlignmentOptions.MidlineLeft, false);
            text.overflowMode = TextOverflowModes.Ellipsis;
            var trt = text.rectTransform;
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one; trt.offsetMin = new Vector2(34, 0); trt.offsetMax = new Vector2(-34, 0);
            var strip = canvas.gameObject.AddComponent<TelexStrip>();
            strip.Configure(rt, text);
            rt.gameObject.SetActive(false);
            return strip;
        }

        // ---------- Sarı çerçeve ve üzerine gelme etiketi (K1 notu) ----------

        static HoverHighlight BuildHighlight(RectTransform canvas, Canvas c, Camera view)
        {
            var rt = UiRect(canvas, "Highlight", Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(100, 100));
            UiImage(Stretch(rt, "Ink", default, 1.5f), S("ui_frame_1_5px_r2"), T.ColorOf("ink"));
            var yellow = UiImage(Stretch(rt, "Yellow"), S("ui_frame_8px_r0"), T.ColorOf("ui.focusRing"));
            yellow.pixelsPerUnitMultiplier = 8 / 5f;
            var highlight = canvas.gameObject.AddComponent<HoverHighlight>();
            highlight.Configure(rt, c, view);
            rt.gameObject.SetActive(false);
            return highlight;
        }

        static HoverTag BuildTag(RectTransform canvas, Canvas c)
        {
            var w = T.Vector2Of("space.hoverTagW");
            var rt = UiRect(canvas, "Hover tag", Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(w.y > 0 ? w.y : 300, 60));
            UiPaper(rt, T.ColorOf("panels.hoverPaper"), T.ColorOf("ink"), "hoverCard");
            // Mürekkep kenarı 1,5 px (1 px yerine)
            var edge = rt.Find("Edge").GetComponent<Image>();
            edge.sprite = S("ui_frame_1_5px_r2");
            var layout = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 9, 10);
            layout.spacing = 3;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true; layout.childForceExpandHeight = false;
            var fitter = rt.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            // Başlık satırı: koyu tuş fişi + başlık. Fiş ayrı öğe; TMP <mark> vurguyu yazının üstüne çizer, opak fiş tuşu örter.
            var head = UiRect(rt, "Head", Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(0, 24));
            var hl = head.gameObject.AddComponent<HorizontalLayoutGroup>();
            hl.spacing = 8;
            hl.childAlignment = TextAnchor.MiddleLeft;
            hl.childControlWidth = hl.childControlHeight = true;
            hl.childForceExpandWidth = hl.childForceExpandHeight = false;
            var chip = KeyChip(head, "", T.ColorOf("ink"), T.ColorOf("paper"));
            UiImage(Stretch(chip, "Fill"), S("ui_fill_r2"), T.ColorOf("ink")).transform.SetAsFirstSibling();
            var title = UiText(head, "Title", "", "hoverTagTitle", T.ColorOf("ink"), TextAlignmentOptions.MidlineLeft, false, true);
            title.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            var line = UiText(rt, "Line", "", "hoverTagLine", T.ColorOf("ink2"), TextAlignmentOptions.TopLeft, false, true);
            var tag = canvas.gameObject.AddComponent<HoverTag>();
            tag.Configure(rt, title, line, chip.gameObject, chip.GetComponentInChildren<TMP_Text>(), c);
            rt.gameObject.SetActive(false);
            return tag;
        }
    }
}
