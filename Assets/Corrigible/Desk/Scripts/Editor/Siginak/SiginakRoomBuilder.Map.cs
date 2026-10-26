using System.Collections.Generic;
using System.Linq;
using Corrigible.Desk.Bridge;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Corrigible.Desk.EditorTools
{
    /// <summary>
    /// Aşama 5: kurtuluş haritası (K2/K3) ve ekip figürü. Sade haritanın dünya canvas'ına iki katman eklenir — bölge
    /// taramaları sınır çizgilerinin altına (“Liberation fills”), işaretler, ip, lejant ve lamba dairesi en üste
    /// (“Liberation marks”); ikisi yalnız yeni görünümde açıktır (sunum anahtarı). Görev fişi ve onay fişi masadaki
    /// harita yuvasında World Space canvas'tır (haritanın 2 mm üstü); ekip figürü 3D'dir (room.js › buildTeamFigure).
    /// Tekrar kurulumda eski katmanlar silinir. Konumlar tasarım pikselidir (1920 × 1080, sol üst).
    /// </summary>
    public static partial class SiginakRoomBuilder
    {
        const string MapFillsName = "Liberation fills", MapMarksName = "Liberation marks";
        const string AnchorsPath = DeskSpriteManifest.Root + "/Data/Siginak/team_anchors.json";
        const string HaloMaterialPath = DeskSpriteManifest.Root + "/Resources/Fonts & Materials/Sade Map Label.mat";

        sealed class MapParts
        {
            public SiginakMap Controller;
            public GameObject Fills, Marks;
        }

        static Texture2D Tex(string name) => AssetDatabase.LoadAssetAtPath<Texture2D>(SiginakSpriteManifest.TexturesDir + "/" + name + ".png");

        /// <summary>Tam tasarım dikdörtgeni (pivot sol üst): harita pikseli = yerel nokta (x, −y).</summary>
        static RectTransform FullFrame(Transform parent, string name)
        {
            var rt = UiRect(parent, name, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), Vector2.zero, MapProjection.DesignSize);
            return rt;
        }

        /// <summary>Tasarım noktasına oturan küçük kök (pivot orta).</summary>
        static RectTransform At(Transform parent, string name, Vector2 point, Vector2 size) =>
            UiRect(parent, name, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0.5f, 0.5f), new Vector2(point.x, -point.y), size);

        /// <summary>Düzen grubuna katılan görsel (UiImage düzen dışı kurar).</summary>
        static Image LayoutImage(Transform parent, string name, Sprite sprite, Color color, Vector2 size)
        {
            var img = UiImage(UiRect(parent, name, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero, size), sprite, color, Image.Type.Simple);
            var le = img.GetComponent<LayoutElement>();
            le.ignoreLayout = false;
            le.preferredWidth = le.minWidth = size.x;
            le.preferredHeight = le.minHeight = size.y;
            return img;
        }

        static HorizontalLayoutGroup Row(GameObject go, float spacing, TextAnchor align = TextAnchor.MiddleLeft)
        {
            var h = go.AddComponent<HorizontalLayoutGroup>();
            h.spacing = spacing;
            h.childAlignment = align;
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = h.childForceExpandHeight = false;
            return h;
        }

        static VerticalLayoutGroup Column(GameObject go, float spacing, RectOffset padding = null)
        {
            var v = go.AddComponent<VerticalLayoutGroup>();
            v.spacing = spacing;
            v.padding = padding ?? new RectOffset();
            v.childAlignment = TextAnchor.UpperLeft;
            v.childControlWidth = v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;
            return v;
        }

        static void Fit(GameObject go, bool horizontal, bool vertical)
        {
            var f = go.AddComponent<ContentSizeFitter>();
            if (horizontal) f.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            if (vertical) f.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        static void Height(Component c, float h)
        {
            var le = c.GetComponent<LayoutElement>();
            if (le == null) le = c.gameObject.AddComponent<LayoutElement>();
            le.ignoreLayout = false;
            le.minHeight = le.preferredHeight = h;
            // Sabit yükseklik: içindeki düzen grubu esnek görünse de sütundaki boşluğu yutmasın
            le.flexibleHeight = 0;
        }

        static MapParts BuildMap(Scene scene, Refs refs, Hud hud, Camera camera, SiginakCameraRig rig, TelexFeed telex,
            Corrigible.Game.GameController game, LiberationDeskView surface)
        {
            var sade = Object.FindObjectsByType<SadeMapView>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(v => v.gameObject.scene == scene);
            if (sade == null) return null;
            var mapRoot = sade.transform.Find("MapRoot");
            foreach (var n in new[] { MapFillsName, MapMarksName })
            {
                var old = mapRoot.Find(n);
                if (old != null) Object.DestroyImmediate(old.gameObject);
            }
            var anchorsJson = JObject.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(AnchorsPath).text);
            Vector2 P(JToken a) => new Vector2((float)a[0], (float)a[1]);

            // ---------- Taramalar: bölge dolgularının hemen üstü, sınır çizgilerinin altı ----------
            var fills = FullFrame(mapRoot, MapFillsName);
            fills.SetSiblingIndex(mapRoot.Find("RegionFills").GetSiblingIndex() + 1);
            var marksRoot = FullFrame(mapRoot, MapMarksName);
            marksRoot.SetAsLastSibling();
            var contours = FullFrame(marksRoot, "Contours");
            var regionsRoot = FullFrame(marksRoot, "Regions");

            var halo = AssetDatabase.LoadAssetAtPath<Material>(HaloMaterialPath);
            var cpBold = DeskTheme.Active.Font(DeskFont.CpBold);
            var marks = new List<LiberationRegionMarks>();
            var anchors = new List<LiberationMapLayer.Anchor>();
            foreach (var slot in sade.Regions)
            {
                if (slot.Fill == null || slot.Label == null) continue;
                var labelTop = ((RectTransform)slot.Label.transform).anchoredPosition;
                var center = new Vector2(labelTop.x, -labelTop.y + 17);
                RegionMeshGraphic Copy(string name)
                {
                    var g = FullFrame(fills, slot.Id + " " + name).gameObject.AddComponent<RegionMeshGraphic>();
                    g.CopyGeometryFrom(slot.Fill);
                    g.raycastTarget = false;
                    return g;
                }
                var hatchA = Copy("hatch");
                var hatchB = Copy("hatch cross");
                var tint = Copy("tint");

                var contour = FullFrame(contours, slot.Id).gameObject.AddComponent<UILineGraphic>();
                var ring = slot.Fill.LargestOuterRing().ToList();
                if (ring.Count > 0) ring.Add(ring[0]);
                contour.SetPointsTopLeft(ring, Vector2.zero);
                contour.raycastTarget = false;

                var root = At(regionsRoot, slot.Id, center, Vector2.zero);
                var grease = UiImage(At(root, "Grease ring", Vector2.zero, new Vector2(280, 117)), S("map_grease_ring"), Color.white, Image.Type.Simple);
                var candidate = UiImage(At(root, "Candidate ring", Vector2.zero, new Vector2(280, 117)), S("map_candidate_ring"), Color.white, Image.Type.Simple);
                var chip = UiImage(At(root, "Chip", Vector2.zero, new Vector2(200, 34)), S("ui_fill_r0"), T.ColorOf("highlighter"));
                var label = UiText(root, "Label", "", "mapLabelTex", T.ColorOf("map.labelInk"), TextAlignmentOptions.Center, false);
                label.rectTransform.sizeDelta = new Vector2(420, 70);
                if (halo != null) label.fontSharedMaterial = halo;
                var row = At(root, "Sub row", new Vector2(0, 30), new Vector2(200, 28));
                Row(row.gameObject, 5, TextAnchor.MiddleCenter);
                Fit(row.gameObject, true, true);
                var markImages = Enumerable.Range(0, 3).Select(i => LayoutImage(row, "Mark " + (i + 1), S("mark_sector_locked"), Color.white, new Vector2(26, 26))).ToArray();
                var sub = UiText(row, "Sub", "", "mapSubTex", T.ColorOf("ink2"), TextAlignmentOptions.MidlineLeft, false);
                // Yağlı kalem: ince gri sprite iki kez (1 px kaydırılmış) basılır; K2'deki koyu, kalın iz
                var greaseInk = UiImage(At(grease.rectTransform, "Ink", new Vector2(1, 1), new Vector2(280, 117)), S("map_grease_ring"), Color.white, Image.Type.Simple);
                greaseInk.rectTransform.anchorMin = Vector2.zero; greaseInk.rectTransform.anchorMax = Vector2.one;
                greaseInk.rectTransform.sizeDelta = Vector2.zero; greaseInk.rectTransform.anchoredPosition = new Vector2(1.2f, -1);
                candidate.color = T.ColorOf("mapLiberation.candidateContour");
                var pin = UiImage(At(root, "Pin", new Vector2(130, 6), new Vector2(30, 30)), S("map_pin"), Color.white, Image.Type.Simple);
                var stamp = At(root, "Stamp", new Vector2(36, -78), new Vector2(112, 112));
                stamp.localRotation = Quaternion.Euler(0, 0, -6);
                stamp.gameObject.AddComponent<CanvasGroup>();
                var stampImage = UiImage(Stretch(stamp, "Ring"), S("map_stamp_free"), T.ColorOf("mapLiberation.freeStamp"), Image.Type.Simple);
                // Bant yazısı beyaz (sprites/README), vardiya kodu halkanın altında mavi
                var word = UiText(stamp, "Word", "", "stamp", T.ColorOf("index"), TextAlignmentOptions.Center, false);
                word.fontSize = 17;
                word.rectTransform.sizeDelta = new Vector2(100, 24);
                var day = UiText(stamp, "Day", "", "stamp", T.ColorOf("mapLiberation.freeStamp"), TextAlignmentOptions.Center, false);
                day.fontSize = 12;
                day.rectTransform.anchoredPosition = new Vector2(0, -30);
                day.rectTransform.sizeDelta = new Vector2(100, 18);
                var m = root.gameObject.AddComponent<LiberationRegionMarks>();
                m.Configure(slot.Id, center, slot.Fill, hatchA, hatchB, tint, contour, label, sub, chip, grease, candidate, pin, row, markImages, stamp,
                    stampImage, word, day, S("mark_sector_done"), S("mark_sector_ready"), S("mark_sector_locked"), Tex("map_hatch_45_p16"),
                    Tex("map_hatch_m45_p16"), Tex("map_hatch_45_p32"), halo, cpBold.material);
                marks.Add(m);
                var anchor = anchorsJson["regions"]?[slot.Id];
                anchors.Add(new LiberationMapLayer.Anchor { id = slot.Id, point = anchor != null ? P(anchor) : labelTop * new Vector2(1, -1) + new Vector2(0, 110) });
            }

            // S-07: sığınağın haritadaki fişi (Batı Avrupa'nın sol üstü)
            var shelter = P(anchorsJson["shelter"]);
            // Yazı 23 harita px → K2'de ≈ 14,5 ekran px (ikincil yazı alt sınırı)
            var tag = At(marksRoot, "S-07", shelter, new Vector2(82, 32));
            UiImage(Stretch(tag, "Fill"), S("ui_fill_r2"), T.ColorOf("ink"));
            var tagText = UiText(tag, "Text", SiginakRoomWords.ShelterTag, "stamp", T.ColorOf("paper"), TextAlignmentOptions.Center, true);
            tagText.fontSize = 23;
            tagText.rectTransform.sizeDelta = new Vector2(82, 32);

            // Kırmızı ip (#A3241A 2,6 px + gölge)
            var shadow = FullFrame(marksRoot, "String shadow").gameObject.AddComponent<UILineGraphic>();
            shadow.color = T.ColorOf("mapLiberation.stringShadow");
            shadow.Thickness = T.Number("space.stringPx", 2.6f);
            shadow.raycastTarget = false;
            var line = FullFrame(marksRoot, "String").gameObject.AddComponent<UILineGraphic>();
            line.color = T.ColorOf("mapLiberation.string");
            line.Thickness = T.Number("space.stringPx", 2.6f);
            line.raycastTarget = false;
            shadow.gameObject.SetActive(false);
            line.gameObject.SetActive(false);

            var legendBox = Legend(marksRoot);

            // Masa lambasının sıcak dairesi (Sade tx_lamp_desk); en üst katman
            var sadeLamp = sade.transform.Find("DeskLamp")?.GetComponent<RawImage>();
            if (sadeLamp != null)
            {
                var lamp = FullFrame(marksRoot, "Lamp").gameObject.AddComponent<RawImage>();
                lamp.texture = sadeLamp.texture;
                lamp.material = sadeLamp.material;
                lamp.color = sadeLamp.color;
                lamp.uvRect = sadeLamp.uvRect;
                lamp.raycastTarget = false;
            }

            var layer = marksRoot.gameObject.AddComponent<LiberationMapLayer>();
            layer.Configure(marksRoot, marks.ToArray(), anchors.ToArray(), shelter, P(anchorsJson["shelterTeam"]), line, shadow, legendBox);

            // ---------- Fişler ve ekip figürü: masadaki harita yuvasında ----------
            var mapCanvas = sade.GetComponent<Canvas>();
            var slip = OperationSlipCanvas(refs.MapMount, camera, mapCanvas);
            var front = FrontSlipCanvas(refs.MapMount, camera, mapCanvas);
            var figure = Figure(refs.MapMount, hud, out var drag, out var figureTarget);
            drag.Configure(figure, layer, camera, rig);

            var go = new GameObject("Map controller");
            go.transform.SetParent(refs.Root, false);
            var controller = go.AddComponent<SiginakMap>();
            controller.Configure(game, surface, rig, telex, sade, layer, slip, front, figure, drag, figureTarget);
            fills.gameObject.SetActive(false);
            marksRoot.gameObject.SetActive(false);
            return new MapParts { Controller = controller, Fills = fills.gameObject, Marks = marksRoot.gameObject };
        }

        // ---------- Lejant (K2: basılı, sol altta; EN'de sütunlar metne göre açılır) ----------

        static RectTransform Legend(RectTransform marksRoot)
        {
            var box = UiRect(marksRoot, "Legend", new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 0), new Vector2(22, -1058), new Vector2(560, 160));
            UiPaper(box, T.ColorOf("index"), T.ColorOf("ink"), "legendCard", false);
            Column(box.gameObject, 8, new RectOffset(16, 18, 12, 14));
            Fit(box.gameObject, true, true);
            UiText(box, "Title", SiginakRoomWords.LegendTitle, "mapSubTex", T.ColorOf("ink"), TextAlignmentOptions.MidlineLeft, true);
            RectTransform Line(string name)
            {
                var r = UiRect(box, name, Vector2.zero, Vector2.zero, new Vector2(0, 0.5f), Vector2.zero, new Vector2(500, 32));
                Row(r.gameObject, 26);
                return r;
            }
            void Item(RectTransform parent, string word, System.Action<RectTransform> icon)
            {
                var item = UiRect(parent, "Item " + word, Vector2.zero, Vector2.zero, new Vector2(0, 0.5f), Vector2.zero, new Vector2(120, 32));
                Row(item.gameObject, 10);
                icon(item);
                UiText(item, "Word", word, "mapSubTex", T.ColorOf("ink"), TextAlignmentOptions.MidlineLeft, true);
            }
            var first = Line("Row 1");
            Item(first, SiginakRoomWords.LegendOccupied, p =>
            {
                var sw = UiRect(p, "Swatch", Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(34, 24));
                var le = sw.gameObject.AddComponent<LayoutElement>();
                le.preferredWidth = le.minWidth = 34; le.preferredHeight = le.minHeight = 24;
                foreach (var (tex, key) in new[] { ("map_hatch_45_p16", "mapLiberation.occupiedHatch[0]"), ("map_hatch_m45_p16", "mapLiberation.occupiedHatch[1]") })
                {
                    var raw = Stretch(sw, tex).gameObject.AddComponent<RawImage>();
                    raw.texture = Tex(tex);
                    var c = T.ColorOf(key);
                    c.a = Mathf.Min(1, c.a * T.NumberOf("mapLiberation.hatchGain", 1));
                    raw.color = c;
                    raw.uvRect = new Rect(0, 0, 34 / 16f, 24 / 16f);
                    raw.raycastTarget = false;
                }
            });
            Item(first, SiginakRoomWords.LegendFront, p =>
            {
                var icon = LayoutImage(p, "Icon", S("map_grease_ring"), Color.white, new Vector2(48, 22));
                var ink = UiImage(Stretch(icon.rectTransform, "Ink", new Vector2(0.6f, -0.6f)), S("map_grease_ring"), Color.white, Image.Type.Simple);
                _ = ink;
            });
            Item(first, SiginakRoomWords.LegendCandidate, p => LayoutImage(p, "Icon", S("map_candidate_ring"), T.ColorOf("mapLiberation.candidateContour"), new Vector2(48, 22)));
            Item(first, SiginakRoomWords.LegendFree, p => LayoutImage(p, "Icon", S("map_stamp_free"), T.ColorOf("mapLiberation.freeStamp"), new Vector2(28, 28)));
            var second = Line("Row 2");
            Item(second, SiginakRoomWords.LegendRetaken, p => LayoutImage(p, "Icon", S("mark_sector_done"), Color.white, new Vector2(26, 26)));
            Item(second, SiginakRoomWords.LegendReady, p => LayoutImage(p, "Icon", S("mark_sector_ready"), Color.white, new Vector2(26, 26)));
            Item(second, SiginakRoomWords.LegendLocked, p => LayoutImage(p, "Icon", S("mark_sector_locked"), Color.white, new Vector2(26, 26)));
            return box;
        }

        // ---------- Fiş canvas'ları ----------

        /// <summary>Fiş canvas'ı: World Space, haritanın 2 mm üstü; 1 fiş pikseli = K2'de 1 ekran pikseli (harita 1210 px).</summary>
        static (Canvas canvas, RectTransform paper) SlipCanvas(Transform mapMount, string name, Camera camera, Canvas mapCanvas, float width, float rotationDeg)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
            go.layer = 5;
            var rt = (RectTransform)go.transform;
            rt.SetParent(mapMount, false);
            rt.localPosition = new Vector3(0, 0.0024f, 0);
            rt.localRotation = Quaternion.Euler(90, 0, 0);
            rt.sizeDelta = new Vector2(width, 700);
            rt.localScale = Vector3.one * (Settings.mapWidthMeters / T.Number("space.mapK2ScreenW", 1210));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = camera;
            canvas.sortingOrder = (mapCanvas != null ? mapCanvas.sortingOrder : 0) + 10;
            var paper = UiRect(rt, "Paper", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(width, 600));
            paper.localRotation = Quaternion.Euler(0, 0, rotationDeg);
            UiPaper(paper, T.ColorOf("index"), T.ColorOf("edge"), "card");
            var group = paper.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 1;
            return (canvas, paper);
        }

        static TextMeshProUGUI SlipText(Transform parent, string name, string role, string colorKey, bool wrap = false, bool localize = false, string text = "")
        {
            var t = UiText(parent, name, text, role, T.ColorOf(colorKey), TextAlignmentOptions.TopLeft, localize, wrap);
            var le = t.gameObject.AddComponent<LayoutElement>();
            le.flexibleWidth = 1;
            return t;
        }

        static RectTransform Block(Transform parent, string name, float spacing)
        {
            var r = UiRect(parent, name, Vector2.zero, Vector2.zero, new Vector2(0.5f, 1), Vector2.zero, new Vector2(400, 40));
            Column(r.gameObject, spacing);
            return r;
        }

        /// <summary>Damga: kenar (düz 2 px ya da kesik) + CP 700 13; renk ve kenar deseni koddan (durum).</summary>
        static (Image frame, TextMeshProUGUI text) StampBox(Transform parent, string name, Vector2 size, float rotation)
        {
            var r = UiRect(parent, name, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-14, 0), size);
            r.localRotation = Quaternion.Euler(0, 0, rotation);
            var frame = UiImage(Stretch(r, "Frame"), S("ui_frame_2px_r3"), T.ColorOf("stamp.locked"));
            var text = UiText(r, "Text", "", "stamp", T.ColorOf("stamp.locked"), TextAlignmentOptions.Center, false);
            text.rectTransform.anchorMin = Vector2.zero; text.rectTransform.anchorMax = Vector2.one; text.rectTransform.sizeDelta = Vector2.zero;
            return (frame, text);
        }

        /// <summary>Mürekkep düğme: tuş fişi + LF 700 20 etiket + sağda bedel; yükseklik token.</summary>
        static (Button button, Image fill, TextMeshProUGUI label, TextMeshProUGUI cost) InkButton(Transform parent, string name, float height)
        {
            var r = UiRect(parent, name, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(400, height));
            Height(r, height);
            foreach (var layer in T.Shadow("topButton"))
            {
                var sh = UiImage(Stretch(r, "Shadow", new Vector2(layer.offset.x, -layer.offset.y)), S("ui_fill_r2"), layer.color);
                sh.transform.SetAsFirstSibling();
            }
            var fill = UiImage(Stretch(r, "Fill"), S("ui_fill_r2"), T.ColorOf("ui.inkButton"), Image.Type.Sliced, true);
            var h = Row(r.gameObject, 12);
            h.padding = new RectOffset(12, 16, 0, 0);
            KeyChip(r, SiginakRoomWords.EnterKey, T.ColorOf("ui.inkButtonText"), T.ColorOf("ui.inkButtonText"));
            var label = UiText(r, "Label", "", "inkButton", T.ColorOf("ui.inkButtonText"), TextAlignmentOptions.MidlineLeft, false);
            label.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            // Uzun eylem adı (“SEÇİLİ BÖLGEDE CEPHE AÇ”, EN) düğmeye sığsın: en çok 2 punto küçülür
            label.enableAutoSizing = true;
            label.fontSizeMax = label.fontSize;
            label.fontSizeMin = label.fontSize - 2;
            label.characterSpacing = 2;
            var cost = UiText(r, "Cost", "", "slipProgress", T.ColorOf("ui.inkButtonText"), TextAlignmentOptions.MidlineRight, false);
            var button = r.gameObject.AddComponent<Button>();
            button.targetGraphic = fill;
            button.transition = Selectable.Transition.None;
            return (button, fill, label, cost);
        }

        static OperationSlip OperationSlipCanvas(Transform mapMount, Camera camera, Canvas mapCanvas)
        {
            var (canvas, paper) = SlipCanvas(mapMount, "Operation slip", camera, mapCanvas, T.Number("space.slipW.tr", 436), -1);
            Column(paper.gameObject, 0, new RectOffset(18, 14, 12, 14));
            Fit(paper.gameObject, false, true);

            // Başlık · 104: OP kodu CP 700 14, bölge CP 700 28, ilerleme CP 700 15, altında 2 px kırmızı çizgi
            var head = Block(paper, "Head", 2);
            var code = SlipText(head, "Code", "slipCode", "ink3");
            var region = SlipText(head, "Region", "slipRegion", "ink");
            var progress = SlipText(head, "Progress", "slipProgress", "ink2");
            var rule = UiImage(UiRect(head, "Rule", Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(400, 2)), S("ui_fill_r0"), T.ColorOf("ruleRed"));
            rule.GetComponent<LayoutElement>().ignoreLayout = false;
            Height(rule, 2);
            var close = UiRect(paper, "Close", new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-14, -12), new Vector2(64, 30));
            close.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            UiImage(Stretch(close, "Fill"), S("ui_fill_r3"), T.ColorOf("ui.frameButton"), Image.Type.Sliced, true);
            UiImage(Stretch(close, "Edge"), S("ui_frame_2px_r3"), T.ColorOf("ink"));
            var closeText = UiText(close, "Text", "× Esc", "keyChip", T.ColorOf("ink"), TextAlignmentOptions.Center, false);
            closeText.rectTransform.anchorMin = Vector2.zero; closeText.rectTransform.anchorMax = Vector2.one; closeText.rectTransform.sizeDelta = Vector2.zero;
            var closeButton = close.gameObject.AddComponent<Button>();
            closeButton.targetGraphic = close.GetComponentInChildren<Image>();

            // Sektörler · 3 × 62: işaret 26 + no + ad LF 600 18 + tür CP 700 14 + damga; hazır satır sarı α .32
            var rows = new OperationSlip.Row[3];
            for (int i = 0; i < 3; i++)
            {
                var r = UiRect(paper, "Sector " + (i + 1), Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(400, 62));
                Height(r, 62);
                var hl = UiImage(Stretch(r, "Highlight", default, 0), S("ui_fill_r0"), WithAlpha(T.ColorOf("highlighter"), 0.32f));
                var mark = UiImage(UiRect(r, "Mark", new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(4, 0), new Vector2(26, 26)),
                    S("mark_sector_locked"), Color.white, Image.Type.Simple);
                var no = UiText(r, "No", "", "sectorNo", T.ColorOf("ink3"), TextAlignmentOptions.MidlineLeft, false);
                no.rectTransform.anchorMin = no.rectTransform.anchorMax = new Vector2(0, 0.5f);
                no.rectTransform.pivot = new Vector2(0, 0.5f);
                no.rectTransform.anchoredPosition = new Vector2(42, 9); no.rectTransform.sizeDelta = new Vector2(30, 22);
                var nm = UiText(r, "Name", "", "sectorName", T.ColorOf("ink"), TextAlignmentOptions.MidlineLeft, false);
                nm.rectTransform.anchorMin = nm.rectTransform.anchorMax = new Vector2(0, 0.5f);
                nm.rectTransform.pivot = new Vector2(0, 0.5f);
                nm.rectTransform.anchoredPosition = new Vector2(72, 9); nm.rectTransform.sizeDelta = new Vector2(220, 24);
                nm.overflowMode = TextOverflowModes.Ellipsis;
                var kind = UiText(r, "Kind", "", "sectorType", T.ColorOf("ink3"), TextAlignmentOptions.MidlineLeft, false);
                kind.rectTransform.anchorMin = kind.rectTransform.anchorMax = new Vector2(0, 0.5f);
                kind.rectTransform.pivot = new Vector2(0, 0.5f);
                kind.rectTransform.anchoredPosition = new Vector2(72, -13); kind.rectTransform.sizeDelta = new Vector2(220, 20);
                var (frame, stamp) = StampBox(r, "Stamp", new Vector2(124, 26), -3);
                var divider = UiImage(UiRect(r, "Divider", new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), Vector2.zero, new Vector2(0, 1)),
                    S("ui_fill_r0"), T.ColorOf("edge"));
                rows[i] = new OperationSlip.Row { root = r, highlight = hl, mark = mark, stampFrame = frame, no = no, name = nm, kind = kind, stamp = stamp };
                _ = divider;
            }

            // Görev · 120
            var task = Block(paper, "Task", 4);
            task.GetComponent<VerticalLayoutGroup>().padding = new RectOffset(0, 0, 12, 10);
            var taskHead = SlipText(task, "Head", "taskHead", "ink3");
            var taskBody = SlipText(task, "Body", "taskBody", "ink", wrap: true);
            taskBody.lineSpacing = 8;

            // Bedel / ödül · 72
            var cost = Block(paper, "Cost", 4);
            cost.GetComponent<VerticalLayoutGroup>().padding = new RectOffset(0, 0, 8, 10);
            TextMeshProUGUI Pair(RectTransform parent, string labelSource, string role, string colorKey)
            {
                var r = UiRect(parent, labelSource, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(400, 26));
                Row(r.gameObject, 12, TextAnchor.UpperLeft);
                var l = UiText(r, "Label", labelSource, "taskHead", T.ColorOf("ink3"), TextAlignmentOptions.TopLeft, true);
                var le = l.gameObject.AddComponent<LayoutElement>();
                le.minWidth = le.preferredWidth = 72;
                var v = SlipText(r, "Value", role, colorKey, wrap: true);
                return v;
            }
            var costValue = Pair(cost, SiginakRoomWords.SlipCost, "costValue", "status.threat");
            var rewardValue = Pair(cost, SiginakRoomWords.SlipReward, "rewardLine", "ink2");

            // Ekip · 56
            var team = UiRect(paper, "Team", Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(400, 56));
            Height(team, 56);
            UiImage(UiRect(team, "Icon", new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0), new Vector2(34, 26)),
                S("icon_team_figure"), Color.white, Image.Type.Simple);
            var teamName = UiText(team, "Name", "", "slipProgress", T.ColorOf("ink"), TextAlignmentOptions.MidlineLeft, false);
            teamName.rectTransform.anchorMin = teamName.rectTransform.anchorMax = new Vector2(0, 0.5f);
            teamName.rectTransform.pivot = new Vector2(0, 0.5f);
            teamName.rectTransform.anchoredPosition = new Vector2(44, 10); teamName.rectTransform.sizeDelta = new Vector2(240, 20);
            var teamMembers = UiText(team, "Members", "", "rewardLine", T.ColorOf("ink2"), TextAlignmentOptions.MidlineLeft, false);
            teamMembers.rectTransform.anchorMin = teamMembers.rectTransform.anchorMax = new Vector2(0, 0.5f);
            teamMembers.rectTransform.pivot = new Vector2(0, 0.5f);
            teamMembers.rectTransform.anchoredPosition = new Vector2(44, -10); teamMembers.rectTransform.sizeDelta = new Vector2(260, 20);
            var (teamFrame, teamStamp) = StampBox(team, "Stamp", new Vector2(110, 26), -2);

            // Eylem · 72
            var actionRoot = Block(paper, "Action", 6);
            actionRoot.GetComponent<VerticalLayoutGroup>().padding = new RectOffset(0, 0, 10, 4);
            var (action, actionFill, actionLabel, actionCost) = InkButton(actionRoot, "Button", T.Number("space.inkButtonH.slip", 54));
            var reason = SlipText(actionRoot, "Reason", "rewardLine", "status.threat", wrap: true);

            // Bilgi fişi (cephe dışı bölge)
            var infoRoot = Block(paper, "Info", 0);
            infoRoot.GetComponent<VerticalLayoutGroup>().padding = new RectOffset(0, 0, 14, 8);
            var info = SlipText(infoRoot, "Text", "taskBody", "ink2", wrap: true);

            // EMİR VERİLDİ damgası (düzen dışı, eylem alanının üstünde, −6°)
            var order = UiRect(paper, "Order stamp", new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0.5f), new Vector2(40, 64), new Vector2(230, 52));
            order.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            order.localRotation = Quaternion.Euler(0, 0, -6);
            // K2 notu: “EMİR VERİLDİ” kırmızı, −6°
            UiImage(Stretch(order, "Frame"), S("ui_frame_3px_r0"), T.ColorOf("stamp.ready"));
            var orderText = UiText(order, "Text", SiginakRoomWords.OrderGiven, "secretStamp", T.ColorOf("stamp.ready"), TextAlignmentOptions.Center, true);
            orderText.fontSize = 22;
            orderText.rectTransform.anchorMin = Vector2.zero; orderText.rectTransform.anchorMax = Vector2.one; orderText.rectTransform.sizeDelta = Vector2.zero;
            order.gameObject.SetActive(false);

            // Raptiye: fişin sol üstü (ip buraya bağlanır)
            var pin = UiImage(UiRect(paper, "Pin", new Vector2(0, 1), new Vector2(0, 1), new Vector2(0.5f, 0.5f), new Vector2(4, -4), new Vector2(30, 30)),
                S("map_pin"), Color.white, Image.Type.Simple);

            var slip = canvas.gameObject.AddComponent<OperationSlip>();
            slip.Configure(paper, paper.GetComponent<CanvasGroup>(), code, region, progress, closeButton, rows, task.gameObject, cost.gameObject, team.gameObject,
                actionRoot.gameObject, infoRoot.gameObject, taskHead, taskBody, costValue, rewardValue, teamName, teamMembers, teamStamp, info, reason,
                teamFrame, action, actionFill, actionLabel, actionCost, order, pin.rectTransform, S("mark_sector_done"), S("mark_sector_ready"),
                S("mark_sector_locked"), S("ui_frame_2px_r3"), S("ui_frame_dashed_2px"));
            canvas.gameObject.SetActive(false);
            return slip;
        }

        static FrontSlip FrontSlipCanvas(Transform mapMount, Camera camera, Canvas mapCanvas)
        {
            var (canvas, paper) = SlipCanvas(mapMount, "Front slip", camera, mapCanvas, T.Number("space.frontSlipW", 404), 1.2f);
            Column(paper.gameObject, 4, new RectOffset(18, 16, 14, 14));
            Fit(paper.gameObject, false, true);
            var head = SlipText(paper, "Head", "slipCode", "ink3");
            var region = SlipText(paper, "Region", "slipRegion", "ink");
            var line = UiRect(paper, "Neighbor line", Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(360, 24));
            Row(line.gameObject, 8);
            var neighbor = UiText(line, "Neighbor", "", "rewardLine", T.ColorOf("ink2"), TextAlignmentOptions.MidlineLeft, false);
            neighbor.fontSize = 16;
            UiText(line, "Dot", "·", "rewardLine", T.ColorOf("ink3"), TextAlignmentOptions.MidlineLeft, false);
            var occupation = UiText(line, "Occupation", "", "slipProgress", T.ColorOf("status.threat"), TextAlignmentOptions.MidlineLeft, false);
            var rule = UiImage(UiRect(paper, "Rule", Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(360, 2)), S("ui_fill_r0"), T.ColorOf("ruleRed"));
            rule.GetComponent<LayoutElement>().ignoreLayout = false;
            Height(rule, 2);
            var sectors = Enumerable.Range(1, 3).Select(i => SlipText(paper, "Sector " + i, "slipProgress", "ink")).ToArray();
            var ruleText = SlipText(paper, "Rule text", "rewardLine", "ink2", wrap: true);
            var (action, fill, label, cost) = InkButton(paper, "Button", 52);
            // EN “OPEN FRONT IN SELECTED REGION” 404 px'lik fişe sığsın: en çok 5 punto küçülür (15 px, Aşama 10)
            label.fontSizeMin = label.fontSizeMax - 5;
            Object.DestroyImmediate(cost.gameObject);
            var reason = SlipText(paper, "Reason", "rewardLine", "status.threat", wrap: true);
            // Vazgeç küçük çerçeve düğme (satır başında, tam genişlik değil)
            var cancelRow = UiRect(paper, "Cancel row", Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(360, 30));
            Height(cancelRow, 30);
            Row(cancelRow.gameObject, 0);
            var cancel = UiRect(cancelRow, "Cancel", Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(140, 30));
            var cancelFill = UiImage(Stretch(cancel, "Fill"), S("ui_fill_r3"), T.ColorOf("ui.frameButton"), Image.Type.Sliced, true);
            UiImage(Stretch(cancel, "Edge"), S("ui_frame_2px_r3"), T.ColorOf("ink"));
            var ch = Row(cancel.gameObject, 8);
            ch.padding = new RectOffset(8, 12, 0, 0);
            var cancelLe = cancel.gameObject.AddComponent<LayoutElement>();
            cancelLe.preferredWidth = 150;
            KeyChip(cancel, SiginakRoomWords.EscKey, T.ColorOf("ink"), T.ColorOf("ink"));
            var cancelLabel = UiText(cancel, "Label", "", "frameButton", T.ColorOf("ink"), TextAlignmentOptions.MidlineLeft, false);
            cancelLabel.fontSize = 17;
            var cancelButton = cancel.gameObject.AddComponent<Button>();
            cancelButton.targetGraphic = cancelFill;
            cancelButton.transition = Selectable.Transition.None;
            var pin = UiImage(UiRect(paper, "Pin", new Vector2(1, 1), new Vector2(1, 1), new Vector2(0.5f, 0.5f), new Vector2(-6, -4), new Vector2(30, 30)),
                S("map_pin"), Color.white, Image.Type.Simple);
            var slip = canvas.gameObject.AddComponent<FrontSlip>();
            slip.Configure(paper, paper.GetComponent<CanvasGroup>(), head, region, neighbor, occupation, ruleText, reason, sectors, action, cancelButton, fill,
                label, cancelLabel, pin.rectTransform);
            canvas.gameObject.SetActive(false);
            return slip;
        }

        static Color WithAlpha(Color c, float a) { c.a = a; return c; }

        // ---------- Ekip figürü (room.js › buildTeamFigure; konum Unity, three +z → Unity −z) ----------

        /// <summary>
        /// ALFA etiketi (K6b): figürün altında dizin kâğıdı “ALFA [YARALI] 1 GÜN” (CP 700, kırmızı çerçeveli damga, CSS −2°).
        /// World Space canvas, 1 px = K2'de 1 ekran px; sıra 2: harita (0) üstünde, harita fişleri (10) altında.
        /// </summary>
        static (GameObject tag, TextMeshProUGUI name, TextMeshProUGUI days) InjuredTag(Transform figure)
        {
            var go = new GameObject("Injured tag", typeof(RectTransform), typeof(Canvas));
            go.layer = 5;
            var rt = (RectTransform)go.transform;
            rt.SetParent(figure, false);
            rt.localPosition = new Vector3(0, 0.0025f, -0.042f);
            rt.localRotation = Quaternion.Euler(90, 0, 0);
            rt.localScale = Vector3.one * (Settings.mapWidthMeters / T.Number("space.mapK2ScreenW", 1210));
            rt.sizeDelta = new Vector2(240, 40);
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 2;
            var paper = UiRect(rt, "Paper", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(200, 32));
            paper.localRotation = Quaternion.Euler(0, 0, 2);
            UiPaper(paper, T.ColorOf("index"), T.ColorOf("edge"), "card");
            Row(paper.gameObject, 10, TextAnchor.MiddleCenter).padding = new RectOffset(10, 10, 6, 7);
            Fit(paper.gameObject, true, true);
            var name = UiText(paper, "Name", "", "plaqueButton", T.ColorOf("ink"), TextAlignmentOptions.MidlineLeft, false);
            var stamp = UiRect(paper, "Stamp", Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(70, 20));
            stamp.localRotation = Quaternion.Euler(0, 0, 3);
            Row(stamp.gameObject, 0, TextAnchor.MiddleCenter).padding = new RectOffset(6, 6, 1, 1);
            UiImage(Stretch(stamp, "Frame"), S("ui_frame_2px_r3"), T.ColorOf("stamp.injured"));
            UiText(stamp, "Text", SiginakRoomWords.Injured, "reportSection", T.ColorOf("stamp.injured"), TextAlignmentOptions.Center, true);
            var days = UiText(paper, "Days", "", "plaqueButton", T.ColorOf("ink"), TextAlignmentOptions.MidlineLeft, false);
            go.SetActive(false);
            return (go, name, days);
        }

        static TeamFigure Figure(Transform mapMount, Hud hud, out TeamFigureDrag drag, out DeskTarget target)
        {
            var root = new GameObject("Team figure").transform;
            root.SetParent(mapMount, false);
            root.localPosition = new Vector3(0, 0.0004f, 0);
            var body = Node(root, "Body", Vector3.zero, Quaternion.identity);
            Cyl(body, "Base", 0.0345f, 0.006f, M("teamBase"), 0, 0.003f, 0).transform.localScale = new Vector3(0.069f, 0.003f, 0.069f * 0.68f);
            Cyl(body, "Rim", 0.0355f, 0.0018f, M("teamRim"), 0, 0.0009f, 0, cast: false).transform.localScale = new Vector3(0.071f, 0.0009f, 0.071f * 0.68f);
            // Harf plakası tabanın ön yarısında (koltuğa doğru)
            Cyl(body, "Plate", 0.0105f, 0.0022f, M("teamPlate"), 0, 0.0071f, -0.0122f);
            Mesh(body, "Plate ring", SiginakMeshLib.Ring(0.0088f, 0.0108f, 36), M("brass"), new Vector3(0, 0.0083f, -0.0122f), Quaternion.Euler(90, 0, 0), Vector3.one, false);
            Label(body, "Letter", "A", DeskFont.CpBold, 0.012f, T.ColorOf("team.letter"), new Vector3(0, 0.0084f, -0.0122f), Quaternion.Euler(90, 0, 0),
                new Vector2(0.02f, 0.016f), localize: false);
            string[] bands = { "teamTechnician", "teamEmp", "teamAnalyst" };
            float[] xs = { -0.019f, 0, 0.019f };
            var pawns = new Transform[3];
            for (int i = 0; i < 3; i++)
            {
                var p = Node(body, "Pawn " + (i + 1), new Vector3(xs[i], 0.006f, 0.007f), Quaternion.identity);
                Cyl(p, "Body", 0.0066f, 0.016f, M("pawn"), 0, 0.008f, 0, r2: 0.0058f, seg: 16);
                Cyl(p, "Band", 0.0066f, 0.0042f, M(bands[i]), 0, 0.0105f, 0, r2: 0.0062f, seg: 16, cast: false);
                Sphere(p, "Head", 0.0062f, M(bands[i]), 0, 0.0205f, 0);
                pawns[i] = p;
            }
            // Seçim halkası (sarı, gölgesiz) ve taşıma gölgesi (siyah α .28): ikisi de haritada kalır
            var ringMat = SiginakMaterials.UnlitTexture("Team select ring", null, 1, true);
            ringMat.SetColor("_BaseColor", WithAlpha(T.ColorOf("team.selectRing"), 0.95f));
            EditorUtility.SetDirty(ringMat);
            var ring = Mesh(root, "Select ring", SiginakMeshLib.Ring(0.037f, 0.0425f, 48), ringMat, new Vector3(0, 0.0005f, 0), Quaternion.Euler(90, 0, 0),
                new Vector3(1, 0.68f, 1), false);
            ring.GetComponent<MeshRenderer>().receiveShadows = false;
            ring.SetActive(false);
            var shadowMat = SiginakMaterials.UnlitTexture("Team carry shadow", null, 1, true);
            shadowMat.SetColor("_BaseColor", T.ColorOf("team.carryShadow"));
            EditorUtility.SetDirty(shadowMat);
            var shadow = Mesh(root, "Carry shadow", SiginakMeshLib.Ring(0, 0.036f, 40), shadowMat, new Vector3(0, 0.0004f, 0), Quaternion.Euler(90, 0, 0),
                new Vector3(1, 0.68f, 1), false);
            shadow.GetComponent<MeshRenderer>().receiveShadows = false;
            shadow.SetActive(false);
            // Saydam gölge haritadaki canvas'lardan (sıra 0, uzaklığa göre sıralanır) sonra çizilsin
            shadow.GetComponent<MeshRenderer>().sortingOrder = 1;
            var (tag, tagName, tagDays) = InjuredTag(root);

            var figure = root.gameObject.AddComponent<TeamFigure>();
            figure.Configure(body, pawns, ring, shadow.transform, tag, tagName, tagDays);
            // Tık hedefi en az 44 px (K1'de figür ~30 px): görünmez kutu
            var renderers = body.GetComponentsInChildren<MeshRenderer>().Where(r => r.GetComponent<TextMeshPro>() == null).ToArray();
            var box = root.gameObject.AddComponent<BoxCollider>();
            box.center = new Vector3(0, 0.016f, 0);
            box.size = new Vector3(0.11f, 0.034f, 0.08f);
            target = root.gameObject.AddComponent<DeskTarget>();
            target.Configure(SiginakRoomWords.TeamTitle, null, null, false, null, renderers, hud.Tag, hud.Highlight);
            drag = root.gameObject.AddComponent<TeamFigureDrag>();
            return figure;
        }
    }
}
