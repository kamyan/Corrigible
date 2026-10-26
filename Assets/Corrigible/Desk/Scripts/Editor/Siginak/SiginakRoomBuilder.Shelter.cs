using System.Linq;
using Corrigible.Desk.Bridge;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk.EditorTools
{
    /// <summary>
    /// Aşama 6: sığınak (K4). Kesit maketinin odalarına <see cref="RoomView"/> (yuva, piyon, dizin kartı, seçim çerçevesi,
    /// hasar), BOŞTA tepsisine piyonlar ve plaka, kıskaçlı panoya sığınak özeti (World Space 600 × 800) ve denetleyici.
    /// Ölçüler room.js › buildDiorama (slot/peg) ve K4 notundan; kart 380 × 110 px = 19 × 5,5 cm.
    /// </summary>
    public static partial class SiginakRoomBuilder
    {
        static readonly string[] PawnBands = { "teamTechnician", "teamEmp", "book4", "teamAnalyst", "pawnViolet", "pawnOchre" };

        /// <summary>room.js › peg: ahşap gövde, renkli bant, baş (kök tabanında).</summary>
        static Transform Peg(Transform parent, string name, string band)
        {
            var p = Node(parent, name, Vector3.zero, Quaternion.identity);
            Cyl(p, "Body", 0.012f, 0.036f, M("pawn"), 0, 0.018f, 0, r2: 0.0105f, seg: 14);
            Cyl(p, "Band", 0.0118f, 0.009f, M(band), 0, 0.026f, 0, r2: 0.0112f, seg: 14, cast: false);
            Sphere(p, "Head", 0.0105f, M("pawn"), 0, 0.046f, 0);
            return p;
        }

        static Canvas WorldCanvas(Transform parent, string name, Vector3 localPos, Vector2 size, float metersPerPx, Camera camera)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
            go.layer = 5;
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.localPosition = localPos;
            rt.localRotation = Quaternion.identity;
            rt.sizeDelta = size;
            rt.localScale = Vector3.one * metersPerPx;
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = camera;
            return canvas;
        }

        static TextMeshProUGUI Place(TextMeshProUGUI t, Vector2 topLeft, Vector2 size)
        {
            var r = t.rectTransform;
            r.anchorMin = r.anchorMax = new Vector2(0, 1);
            r.pivot = new Vector2(0, 1);
            r.anchoredPosition = new Vector2(topLeft.x, -topLeft.y);
            r.sizeDelta = size;
            return t;
        }

        /// <summary>Oda hasarı (moloz, düşmüş kiriş, çatlak): revirde maketle birlikte kurulu; öteki odalar için aynı desen.</summary>
        static GameObject Damage(Transform room, float cx, float fy, float D)
        {
            var damage = Group(room, "Damage", 0, 0, 0);
            float dx = cx - 0.2075f, dy = fy - 0.085f;
            var rubble = new[] { (0.22f, 0.095f, -0.02f, 0.03f, 0.02f), (0.26f, 0.092f, 0.02f, 0.025f, 0.018f), (0.12f, 0.093f, 0.03f, 0.02f, 0.015f),
                (0.29f, 0.094f, -0.06f, 0.028f, 0.02f), (0.18f, 0.091f, -0.07f, 0.022f, 0.014f) };
            for (int i = 0; i < rubble.Length; i++)
            {
                var (x, y, z, s, h) = rubble[i];
                Box(damage, "Rubble", s, h, s * 0.9f, M("rubble"), x + dx, y + dy, z, ry: i * 0.7f, rz: i * 0.3f);
            }
            Box(damage, "Fallen beam", 0.24f, 0.016f, 0.016f, M("wood"), 0.20f + dx, 0.16f + dy, -0.01f, rz: -0.42f);
            Plane(damage, "Crack", 0.012f, 0.12f, M("crack"), 0.25f + dx, 0.17f + dy, D / 2 - 0.0135f, rz: 0.4f);
            return damage.gameObject;
        }

        static SiginakShelter BuildShelter(Refs refs, Hud hud, Camera camera, SiginakCameraRig rig, Corrigible.Game.GameController game,
            LiberationDeskView surface, DeskTarget shelfTarget)
        {
            const float D = 0.34f, roomH = 0.17f, roomW = 0.325f;
            var dio = refs.Diorama;
            var views = new RoomView[DioramaRooms.Length];
            var targets = new DeskTarget[DioramaRooms.Length];
            string[] titles = { SiginakRoomWords.RoomLiving, SiginakRoomWords.RoomGenerator, SiginakRoomWords.RoomWorkshop, SiginakRoomWords.RoomInfirmary };
            for (int n = 0; n < DioramaRooms.Length; n++)
            {
                var (id, name, cx, fy) = DioramaRooms[n];
                var room = Child(dio, "Room " + name);
                var damage = room.Find("Props/Damage")?.gameObject ?? Damage(room, cx, fy, D);
                // Yuvalar ve piyonlar (en çok 3 seviye × 2 = 6); yerleri kapasiteye göre çalışma anında
                var origin = new Vector3(cx, fy + 0.004f, -0.09f);
                var slots = new Transform[6];
                var pawns = new Transform[6];
                for (int i = 0; i < 6; i++)
                {
                    var slot = Cyl(room, "Slot " + (i + 1), 0.012f, 0.002f, M("brassDark"), origin.x, origin.y + 0.001f, origin.z, seg: 18, cast: false);
                    slots[i] = slot.transform;
                    pawns[i] = Peg(room, "Pawn " + (i + 1), PawnBands[i % PawnBands.Length]);
                    pawns[i].localPosition = origin + new Vector3(0, 0.002f, 0);
                }

                // Dizin kartı (380 × 110 px, 0,5 mm/px) pirinç çerçevenin içinde; seçimde oda ağzına sarı çerçeve aynı canvas'ta
                var canvas = WorldCanvas(room, "Card", new Vector3(cx, fy - 0.035f, -D / 2 + 0.005f), new Vector2(380, 110), 0.0005f, camera);
                var crt = (RectTransform)canvas.transform;
                var paper = UiImage(Stretch(crt, "Paper"), S("ui_fill_r0"), T.ColorOf("index"));
                UiImage(UiRect(crt, "Rule", new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), Vector2.zero, new Vector2(0, 3)), S("ui_fill_r0"), T.ColorOf("ruleRed"));
                var nm = Place(UiText(crt, "Name", "", "roomCardNameTex", T.ColorOf("ink"), TextAlignmentOptions.TopLeft, false), new Vector2(18, 16), new Vector2(240, 36));
                var line = Place(UiText(crt, "Line", "", "roomCardLineTex", T.ColorOf("ink2"), TextAlignmentOptions.TopLeft, false), new Vector2(18, 58), new Vector2(350, 28));
                var stamp = UiRect(crt, "Stamp", new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-12, -10), new Vector2(150, 30));
                stamp.localRotation = Quaternion.Euler(0, 0, 5);
                var stampFrame = UiImage(Stretch(stamp, "Frame"), S("ui_frame_3px_r0"), T.ColorOf("stamp.damaged"));
                var stampText = UiText(stamp, "Text", "", "roomCardLineTex", T.ColorOf("stamp.damaged"), TextAlignmentOptions.Center, false);
                stampText.rectTransform.anchorMin = Vector2.zero; stampText.rectTransform.anchorMax = Vector2.one; stampText.rectTransform.sizeDelta = Vector2.zero;
                stampText.enableAutoSizing = true; stampText.fontSizeMax = stampText.fontSize; stampText.fontSizeMin = stampText.fontSize - 5;
                var select = UiImage(Stretch(crt, "Select", default, -4), S("ui_frame_4px_r0"), T.ColorOf("ui.focusRing"));
                select.pixelsPerUnitMultiplier = 4 / 8f;
                // Oda ağzı: 8 K4 ekran px sarı (≈ 5,8 mm) + 2 px mürekkep; oda merkezi kartın 0,12 m üstünde
                var opening = UiRect(crt, "Opening", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(0, (0.035f + roomH / 2) / 0.0005f), new Vector2(roomW / 0.0005f + 12, roomH / 0.0005f + 12));
                var yellow = UiImage(Stretch(opening, "Yellow"), S("ui_frame_8px_r0"), T.ColorOf("ui.focusRing"));
                yellow.pixelsPerUnitMultiplier = 8 / 11.6f;
                var ink = UiImage(Stretch(opening, "Ink", default, 1.5f), S("ui_frame_1_5px_r2"), T.ColorOf("ink"));
                ink.pixelsPerUnitMultiplier = 1.5f / 2.9f;
                opening.gameObject.SetActive(false);
                foreach (var g in canvas.GetComponentsInChildren<Graphic>(true)) g.raycastTarget = false;

                var view = room.gameObject.AddComponent<RoomView>();
                view.Configure(id, Child(room, "Back wall").GetComponent<Renderer>(), Child(room, "Floor").GetComponent<Renderer>(),
                    Child(room, "Bulb").GetComponent<Renderer>(), M("roomWall"), M("roomWallOff"), M("roomFloor"), M("roomFloorOff"), M("bulbGlass"),
                    M("roomBulbOff"), Child(room, "Room light").GetComponent<Light>(), damage, origin, slots, pawns, paper, select, nm, line, stamp,
                    stampFrame, stampText, opening.gameObject);
                views[n] = view;

                // Tık hedefi: oda ağzı + kart (raf pozunda açılır; K1'de maket bütün olarak hedef)
                var hit = new GameObject("Target");
                hit.transform.SetParent(room, false);
                hit.transform.localPosition = new Vector3(cx, fy + 0.03f, -D / 2 + 0.01f);
                var box = hit.AddComponent<BoxCollider>();
                box.size = new Vector3(roomW, roomH + 0.07f, 0.02f);
                box.enabled = false;
                var target = hit.AddComponent<DeskTarget>();
                target.Configure(titles[n], null, SiginakRoomWords.TagRoom, false, null, new Renderer[0], hud.Tag, hud.Highlight);
                targets[n] = target;
            }

            // BOŞTA tepsisi: en çok 6 piyon, plakada sayı
            var idle = Child(refs.Shelf, "Idle tray");
            var idlePawns = Enumerable.Range(0, 6).Select(i => Peg(idle, "Idle pawn " + (i + 1), PawnBands[i % PawnBands.Length])).ToArray();
            // Plaka: krem kâğıt etiket, koyu CP 700 (K4 referansı)
            Box(idle, "Plate", 0.11f, 0.024f, 0.003f, M("paperWhite"), 0, 0.014f, -0.0395f, cast: false);
            var plate = Label(idle, "Plate text", "", DeskFont.CpBold, 0.013f, T.ColorOf("ink"), new Vector3(0, 0.014f, -0.0415f),
                Quaternion.identity, new Vector2(0.10f, 0.02f), localize: false, spacingEm: 0.08f);
            var tray = idle.gameObject.AddComponent<IdleTray>();
            tray.Configure(idlePawns, plate, 0.018f, 0.06f, 0.22f);

            var board = Clipboard(Child(refs.Shelf, "Clipboard"), camera);

            var go = new GameObject("Shelter controller");
            go.transform.SetParent(refs.Root, false);
            var controller = go.AddComponent<SiginakShelter>();
            controller.Configure(game, surface, rig, views, targets, tray, board, shelfTarget);
            return controller;
        }

        // ---------- Sığınak özeti panosu (K4: 600 × 800, kâğıt 0,35 × 0,50 m) ----------

        static ShelterClipboard.SlipButton BoardButton(Transform parent, string name, bool ink, float width)
        {
            var r = UiRect(parent, name, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(width, 72));
            var le = r.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = le.minWidth = width;
            le.preferredHeight = le.minHeight = 72;
            var shadowKey = ink ? "topButton" : "action";
            foreach (var layer in T.Shadow(shadowKey))
            {
                var sh = UiImage(Stretch(r, "Shadow", new Vector2(layer.offset.x, -layer.offset.y)), S("ui_fill_r2"), layer.color);
                sh.transform.SetAsFirstSibling();
            }
            var fill = UiImage(Stretch(r, "Fill"), S("ui_fill_r2"), T.ColorOf(ink ? "ui.inkButton" : "ui.frameButton"), Image.Type.Sliced, true);
            var edge = UiImage(Stretch(r, "Edge"), S("ui_frame_2px_r3"), T.ColorOf("ink"));
            var label = UiText(r, "Label", "", ink ? "inkButton" : "frameButton", T.ColorOf(ink ? "ui.inkButtonText" : "ink"), TextAlignmentOptions.Center, false);
            label.fontSize = ink ? 32 : 30;
            label.rectTransform.anchorMin = Vector2.zero; label.rectTransform.anchorMax = Vector2.one; label.rectTransform.sizeDelta = new Vector2(-12, 0);
            label.enableAutoSizing = true; label.fontSizeMax = label.fontSize; label.fontSizeMin = label.fontSize - 6;
            var stampRoot = UiRect(r, "Stamp", new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 0.5f), new Vector2(8, 0), new Vector2(112, 30));
            stampRoot.localRotation = Quaternion.Euler(0, 0, 6);
            var stampFrame = UiImage(Stretch(stampRoot, "Frame"), S("ui_frame_2px_r3"), T.ColorOf("status.threat"));
            UiImage(Stretch(stampRoot, "Fill", default, -2), S("ui_fill_r2"), T.ColorOf("index")).transform.SetAsFirstSibling();
            var stamp = UiText(stampRoot, "Text", "", "stamp", T.ColorOf("status.threat"), TextAlignmentOptions.Center, false);
            stamp.fontSize = 22;
            stamp.rectTransform.anchorMin = Vector2.zero; stamp.rectTransform.anchorMax = Vector2.one; stamp.rectTransform.sizeDelta = Vector2.zero;
            stampRoot.gameObject.SetActive(false);
            var button = r.gameObject.AddComponent<Button>();
            button.targetGraphic = fill;
            button.transition = Selectable.Transition.None;
            return new ShelterClipboard.SlipButton { button = button, root = r, fill = fill, edge = edge, stampFrame = stampFrame, label = label, stamp = stamp, ink = ink };
        }

        static ShelterClipboard Clipboard(Transform clipboard, Camera camera)
        {
            var canvas = WorldCanvas(clipboard, "Summary", new Vector3(0, 0.255f, -0.0068f), new Vector2(600, 800), 0.35f / 600f, camera);
            var root = (RectTransform)canvas.transform;
            // Kâğıt canvas'ın içinde ışıksız: kıskaçlı panonun ışıklı kutusu odada kahverengiye kayıyor, referansta pano düz krem
            UiPaper(Stretch(root, "Paper"), T.ColorOf("paper"), T.ColorOf("edge"), "drawerSheet");
            var content = Stretch(root, "Content");
            // Sağ kenar maket çerçevesinin arkasında kalır (K4 açısı): içerik sağdan 70 px içeride
            Column(content.gameObject, 10, new RectOffset(30, 70, 30, 30));

            // Özet: başlık + vardiya kodu, kırmızı çizgi, dört satır
            var head = UiRect(content, "Head", Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(500, 44));
            Height(head, 44);
            var title = Place(UiText(head, "Title", "", "panelTitleTex", T.ColorOf("ink"), TextAlignmentOptions.MidlineLeft, false), new Vector2(0, 0), new Vector2(400, 44));
            var shift = UiText(head, "Shift", "", "slipCode", T.ColorOf("ink3"), TextAlignmentOptions.MidlineRight, false);
            shift.fontSize = 22;
            shift.rectTransform.anchorMin = new Vector2(1, 0); shift.rectTransform.anchorMax = new Vector2(1, 1); shift.rectTransform.pivot = new Vector2(1, 0.5f);
            shift.rectTransform.anchoredPosition = Vector2.zero; shift.rectTransform.sizeDelta = new Vector2(140, 0);
            var rule = UiImage(UiRect(content, "Rule", Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(532, 2)), S("ui_fill_r0"), T.ColorOf("ruleRed"));
            rule.GetComponent<LayoutElement>().ignoreLayout = false;
            Height(rule, 2);
            var labels = new TMP_Text[4];
            var values = new TMP_Text[4];
            RectTransform starveStamp = null;
            TMP_Text starveText = null;
            for (int i = 0; i < 4; i++)
            {
                var row = UiRect(content, "Row " + (i + 1), Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(532, 38));
                Height(row, 38);
                // Etiket sütunu “SON VARDİYA” sığacak kadar (EN “LAST SHIFT”); değer sağında
                labels[i] = Place(UiText(row, "Label", "", "taskHead", T.ColorOf("ink2"), TextAlignmentOptions.MidlineLeft, false), new Vector2(0, 0), new Vector2(178, 38));
                // K4'te pano pikseli ≈ 0,65 ekran pikseli: 22 → ~14 px (punto taraması, Aşama 10)
                labels[i].fontSize = 22;
                labels[i].enableAutoSizing = false;
                values[i] = Place(UiText(row, "Value", "", "cardBodyTex", T.ColorOf("ink"), TextAlignmentOptions.MidlineLeft, false), new Vector2(184, 0), new Vector2(316, 38));
                // EN değerler uzar (“rations +0.4 · materials 0.0”): en çok 4 punto küçülür
                values[i].enableAutoSizing = true; values[i].fontSizeMax = values[i].fontSize; values[i].fontSizeMin = values[i].fontSize - 4;
                if (i == 2)
                {
                    // AÇLIK damgası (Soru 9): ERZAK satırının sağında
                    starveStamp = UiRect(row, "Starving", new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-6, 0), new Vector2(120, 32));
                    starveStamp.localRotation = Quaternion.Euler(0, 0, -5);
                    UiImage(Stretch(starveStamp, "Frame"), S("ui_frame_3px_r0"), T.ColorOf("status.threat"));
                    starveText = UiText(starveStamp, "Text", "", "roomCardLineTex", T.ColorOf("status.threat"), TextAlignmentOptions.Center, false);
                    starveText.fontSize = 22;
                    starveText.rectTransform.anchorMin = Vector2.zero; starveText.rectTransform.anchorMax = Vector2.one; starveText.rectTransform.sizeDelta = Vector2.zero;
                    starveStamp.gameObject.SetActive(false);
                }
            }

            // Seçili odanın dizin kartı (−1,2°): ad + SEÇİLİ, satır, açıklama, sonuç notu, maliyet, hazırlık, eylemler
            var card = UiRect(content, "Card", Vector2.zero, Vector2.zero, new Vector2(0.5f, 1), Vector2.zero, new Vector2(532, 420));
            card.localRotation = Quaternion.Euler(0, 0, -1.2f);
            UiPaper(card, T.ColorOf("index"), T.ColorOf("edge"), "card", false);
            var cardFade = card.gameObject.AddComponent<CanvasGroup>();
            Column(card.gameObject, 8, new RectOffset(18, 18, 18, 18));
            var top = UiImage(UiRect(card, "Top rule", new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), Vector2.zero, new Vector2(0, 3)), S("ui_fill_r0"), T.ColorOf("ruleRed"));
            _ = top;
            var nameRow = UiRect(card, "Name row", Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(496, 52));
            Height(nameRow, 52);
            var roomName = UiText(nameRow, "Name", "", "cardSelectedNameTex", T.ColorOf("ink"), TextAlignmentOptions.MidlineLeft, false);
            // Ad satırın çipten kalan genişliğinde (satır kartın iç boşluklarıyla daralır); EN “LIVING QUARTERS” sığmazsa en çok
            // 12 punto küçülür (30 → K4'te ~19 px)
            roomName.rectTransform.anchorMin = Vector2.zero; roomName.rectTransform.anchorMax = Vector2.one;
            roomName.rectTransform.offsetMin = Vector2.zero; roomName.rectTransform.offsetMax = new Vector2(-140, 0);
            roomName.enableAutoSizing = true; roomName.fontSizeMax = roomName.fontSize; roomName.fontSizeMin = roomName.fontSize - 12;
            var chipRoot = UiRect(nameRow, "Chip", new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f), Vector2.zero, new Vector2(132, 32));
            UiImage(Stretch(chipRoot, "Fill"), S("ui_fill_r0"), T.ColorOf("highlighter"));
            var chip = UiText(chipRoot, "Text", "", "stamp", T.ColorOf("ink"), TextAlignmentOptions.Center, false);
            chip.fontSize = 22;
            chip.rectTransform.anchorMin = Vector2.zero; chip.rectTransform.anchorMax = Vector2.one; chip.rectTransform.sizeDelta = Vector2.zero;
            var roomLine = SlipText(card, "Line", "roomCardLineTex", "ink2");
            roomLine.fontSize = 26;
            var description = SlipText(card, "Description", "cardBodyTex", "ink", wrap: true);
            var noteRoot = UiRect(card, "Note", Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(496, 80));
            noteRoot.localRotation = Quaternion.Euler(0, 0, 0.8f);
            UiImage(Stretch(noteRoot, "Fill"), S("ui_fill_r2"), T.ColorOf("sticky"));
            var noteColumn = noteRoot.gameObject.AddComponent<VerticalLayoutGroup>();
            noteColumn.padding = new RectOffset(14, 14, 10, 12);
            noteColumn.childControlWidth = noteColumn.childControlHeight = true;
            noteColumn.childForceExpandWidth = true; noteColumn.childForceExpandHeight = false;
            var note = UiText(noteRoot, "Text", "", "cardNoteTex", T.ColorOf("ink"), TextAlignmentOptions.TopLeft, false, true);
            var cost = SlipText(card, "Cost", "roomCardLineTex", "ink");
            cost.fontSize = 26;
            cost.enableAutoSizing = true; cost.fontSizeMax = 26; cost.fontSizeMin = 22;
            var readiness = SlipText(card, "Readiness", "cardBodyTex", "status.good", wrap: true);
            readiness.fontSize = 24;
            var buttons = UiRect(card, "Buttons", Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(496, 80));
            Height(buttons, 80);
            var hb = Row(buttons.gameObject, 12);
            hb.padding = new RectOffset(0, 0, 6, 0);
            // Kart iç genişliği ≈ 464 px: eylem 190 + iki çerçeve düğme 2 × 118 + aralık
            var action = BoardButton(buttons, "Action", true, 190);
            var minus = BoardButton(buttons, "Minus", false, 118);
            var plus = BoardButton(buttons, "Plus", false, 118);

            // EMİR UYGULANDI damgası (kartın üstünde, −6°)
            var done = UiRect(card, "Done stamp", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(40, 30), new Vector2(300, 56));
            done.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            done.localRotation = Quaternion.Euler(0, 0, -6);
            UiImage(Stretch(done, "Frame"), S("ui_frame_3px_r0"), T.ColorOf("stamp.orderDone"));
            var doneText = UiText(done, "Text", SiginakRoomWords.OrderDone, "secretStamp", T.ColorOf("stamp.orderDone"), TextAlignmentOptions.Center, true);
            doneText.fontSize = 24;
            doneText.rectTransform.anchorMin = Vector2.zero; doneText.rectTransform.anchorMax = Vector2.one; doneText.rectTransform.sizeDelta = Vector2.zero;
            done.gameObject.SetActive(false);
            Fit(card.gameObject, false, true);

            var board = canvas.gameObject.AddComponent<ShelterClipboard>();
            board.Configure(title, shift, labels, values, starveStamp, starveText, card, cardFade, roomName, chip, roomLine, description, noteRoot.gameObject,
                note, cost, readiness, action, minus, plus, done, S("ui_frame_2px_r3"), S("ui_frame_dashed_2px"));
            return board;
        }
    }
}
