using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk.EditorTools
{
    /// <summary>
    /// Aşama 9 (K7): açılış örtüsü (<see cref="OpeningSequence"/>; HUD canvas'ının en altında siyah) ve emir belgesi
    /// (<see cref="BriefingView"/>; HUD canvas'ında). Ölçüler K7 panosundan (reference/boards_html/K7.dc.html): belge sol 580,
    /// üst 254, 760 px, CSS −0,8° (Unity +0,8°), iç boşluk 26/48; dosya 640,268 · 752 × 676, +2°; sekme 1384,292 · 236 × 112.
    /// </summary>
    public static partial class SiginakRoomBuilder
    {
        sealed class BriefingParts
        {
            public OpeningSequence Opening;
            public BriefingView View;
        }

        static BriefingParts BuildBriefing(Refs refs, Hud hud, Controls controls)
        {
            var canvas = (RectTransform)hud.Canvas.transform;
            var black = UiImage(Stretch(canvas, "Blackout"), S("ui_fill_r0"), T.ColorOf("counter.window"), Image.Type.Simple, true);
            black.transform.SetAsFirstSibling();
            black.gameObject.SetActive(false);
            var go = new GameObject("Opening sequence");
            go.transform.SetParent(refs.Root, false);
            var opening = go.AddComponent<OpeningSequence>();
            opening.Configure(black, controls.Flicker);
            var view = BuildOrder(canvas);
            // Vardiya raporunun üstünde, teleks şeridinin altında (KAYIT VAR uyarısı belgenin üstünde görünür)
            var report = canvas.Find("Shift report");
            if (report != null) view.transform.SetSiblingIndex(report.GetSiblingIndex() + 1);
            return new BriefingParts { Opening = opening, View = view };
        }

        static TextMeshProUGUI OrderText(Transform parent, string name, string role, string colorKey, bool wrap = false, bool flexible = true)
        {
            var t = UiText(parent, name, "", role, T.ColorOf(colorKey), TextAlignmentOptions.TopLeft, false, wrap);
            if (flexible) t.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            return t;
        }

        /// <summary>Daktilo gövdesi: satır yüksekliği 1,5 (TMP satır aralığı em/100, fontun kendi satır yüksekliğine göre).</summary>
        static void LineHeight(TMP_Text t, float factor)
        {
            var face = t.font.faceInfo;
            float natural = face.lineHeight / Mathf.Max(1, face.pointSize);
            t.lineSpacing = (factor - natural) * 100;
        }

        static BriefingView BuildOrder(RectTransform canvas)
        {
            float w = T.Number("space.orderW", 760);
            var root = Stretch(canvas, "Briefing");

            // ---- Manila dosya + KAYITTAN DEVAM sekmesi ----
            var dossierRt = Stretch(root, "Dossier");
            var dossier = dossierRt.gameObject.AddComponent<CanvasGroup>();
            var folder = UiRect(dossierRt, "Folder", new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(640, -268), new Vector2(752, 676));
            folder.localRotation = Quaternion.Euler(0, 0, -2);
            UiPaper(folder, T.ColorOf("ui.folder[0]"), T.ColorOf("ui.folder[1]"), "folder");
            // Kahve halkası (anlatı): ekranda 1312,932 merkezli 80 px
            UiImage(UiRect(folder, "Coffee ring", new Vector2(0, 1), new Vector2(0, 1), new Vector2(0.5f, 0.5f), new Vector2(672, -664), new Vector2(80, 80)),
                S("decal_coffee_ring"), T.ColorOf("pen.coffee"), Image.Type.Simple);

            var tab = UiRect(dossierRt, "Tab", new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 0.5f), new Vector2(1384, -348), T.Vector2Of("space.folderTab"));
            tab.localRotation = Quaternion.Euler(0, 0, -2);
            UiPaper(tab, T.ColorOf("ui.folder[0]"), T.ColorOf("ui.folder[1]"), "folder");
            var tabFill = tab.Find("Fill").GetComponent<Image>();
            tabFill.raycastTarget = true;
            var tabFocus = UiImage(Stretch(tab, "Focus", default, 3), S("ui_frame_2px_r3"), T.ColorOf("ui.focusRing"));
            tabFocus.gameObject.SetActive(false);
            // Etiket genişler; KAYIT YOK damgası kendi boyunda ortada
            var tabColumn = Column(tab.gameObject, 4, new RectOffset(14, 12, 10, 10));
            tabColumn.childForceExpandWidth = false;
            tabColumn.childAlignment = TextAnchor.MiddleCenter;
            var label = Block(tab, "Label", 2);
            label.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            label.GetComponent<VerticalLayoutGroup>().padding = new RectOffset(8, 8, 5, 6);
            UiPaper(label, T.ColorOf("index"), T.ColorOf("edge"), "none", false);
            var tabTitle = OrderText(label, "Title", "folderTab", "ink");
            var tabSummary = OrderText(label, "Summary", "folderTabSub", "ink2");
            var hintRow = UiRect(tab, "Hint", Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(200, 22));
            Row(hintRow.gameObject, 6);
            hintRow.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            var chip = KeyChip(hintRow, "Tab", T.ColorOf("ink"), T.ColorOf("ui.edgeLineValue"));
            UiImage(Stretch(chip, "Fill"), S("ui_fill_r2"), T.ColorOf("ink")).transform.SetAsFirstSibling();
            var tabHint = UiText(hintRow, "Text", "", "reportHead", T.ColorOf("ink2"), TextAlignmentOptions.MidlineLeft, false);
            // Kayıt yok: soluk sekme, kesik kenarlı KAYIT YOK damgası
            var noSave = UiRect(tab, "No save", Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(140, 34));
            noSave.localRotation = Quaternion.Euler(0, 0, 4);
            Row(noSave.gameObject, 0, TextAnchor.MiddleCenter).padding = new RectOffset(10, 10, 4, 4);
            var dashed = UiImage(Stretch(noSave, "Frame"), S("ui_frame_dashed_2px"), T.ColorOf("stamp.noSave"), Image.Type.Tiled);
            dashed.fillCenter = false;
            var noSaveText = UiText(noSave, "Text", "", "stamp", T.ColorOf("stamp.noSave"), TextAlignmentOptions.Center, false);
            var tabButton = tab.gameObject.AddComponent<Button>();
            tabButton.targetGraphic = tabFill;
            tabButton.transition = Selectable.Transition.None;
            var hover = tab.gameObject.AddComponent<PointerHover>();

            // ---- Emir belgesi ----
            var doc = UiRect(root, "Document", new Vector2(0, 1), new Vector2(0, 1), new Vector2(0.5f, 1), new Vector2(580 + w / 2, -254), new Vector2(w, 660));
            doc.localRotation = Quaternion.Euler(0, 0, 0.8f);
            UiPaper(doc, T.ColorOf("index"), T.ColorOf("edge"), "order");
            var docGroup = doc.gameObject.AddComponent<CanvasGroup>();
            Column(doc.gameObject, 14, new RectOffset(48, 48, 26, 26));
            Fit(doc.gameObject, false, true);

            var header = Block(doc, "Header", 4);
            header.GetComponent<VerticalLayoutGroup>().padding = new RectOffset(0, 0, 0, 12);
            UiImage(UiRect(header, "Rule", new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), Vector2.zero, new Vector2(0, 2)),
                S("ui_fill_r0"), T.ColorOf("ruleRed"));
            var agencyRow = UiRect(header, "Agency row", Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(w, 18));
            Row(agencyRow.gameObject, 16);
            var agency = OrderText(agencyRow, "Agency", "orderAgency", "ink3");
            var number = UiText(agencyRow, "Number", "", "orderAgency", T.ColorOf("ink3"), TextAlignmentOptions.TopRight, false);
            var title = OrderText(header, "Title", "orderTitle", "ink");
            TextMeshProUGUI Meta(string name, out TextMeshProUGUI value)
            {
                var row = UiRect(header, name, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(w, 18));
                Row(row.gameObject, 0);
                var key = UiText(row, "Label", "", "orderMeta", T.ColorOf("ink3"), TextAlignmentOptions.TopLeft, false);
                var le = key.gameObject.AddComponent<LayoutElement>();
                le.minWidth = le.preferredWidth = 74;
                value = OrderText(row, "Value", "orderMeta", "ink2");
                return key;
            }
            var toLabel = Meta("To", out var to);
            var reLabel = Meta("Re", out var re);

            // GİZLİ: sağ 50, üst 66, CSS −8° (Unity +8°), 3 px kırmızı kenar, α .88
            var secret = UiRect(doc, "Secret", Vector2.one, Vector2.one, Vector2.one, new Vector2(-50, -66), new Vector2(150, 40));
            secret.localRotation = Quaternion.Euler(0, 0, 8);
            secret.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            Row(secret.gameObject, 0, TextAnchor.MiddleCenter).padding = new RectOffset(12, 12, 2, 2);
            Fit(secret.gameObject, true, true);
            UiImage(Stretch(secret, "Frame"), S("ui_frame_3px_r0"), T.ColorOf("stamp.secret"));
            var stamp = UiText(secret, "Text", "", "secretStamp", T.ColorOf("stamp.secret"), TextAlignmentOptions.Center, false);
            secret.gameObject.AddComponent<CanvasGroup>().alpha = 0.88f;

            var body = Block(doc, "Body", 12);
            var paragraphs = new TMP_Text[3];
            for (int i = 0; i < paragraphs.Length; i++)
            {
                var p = OrderText(body, "Paragraph " + i, "orderBody", "ink", true);
                LineHeight(p, 1.5f);
                paragraphs[i] = p;
            }
            var closing = OrderText(body, "Closing", "orderClosing", "ink", true);

            // İmza ve BAŞLA
            var footer = UiRect(doc, "Footer", Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(w, 80));
            Row(footer.gameObject, 24, TextAnchor.LowerLeft).padding = new RectOffset(0, 0, 6, 0);
            var sign = Block(footer, "Signature", 2);
            sign.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            // Öğeler kendi genişliğinde (imza 190 × 44 gerilmesin), sola yaslı
            sign.GetComponent<VerticalLayoutGroup>().childForceExpandWidth = false;
            LayoutImage(sign, "Ink", S("decal_signature"), Color.white, new Vector2(190, 44));
            var rule = UiImage(UiRect(sign, "Rule", Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(200, 1)),
                S("ui_fill_r0"), T.ColorOf("ink2"));
            var ruleLe = rule.GetComponent<LayoutElement>();
            ruleLe.ignoreLayout = false;
            ruleLe.minHeight = ruleLe.preferredHeight = 1;
            ruleLe.preferredWidth = 220;
            var office = OrderText(sign, "Office", "orderAgency", "ink2");
            var line = OrderText(sign, "Line", "folderTabSub", "ink3");
            var button = InkButton(footer, "Begin", T.Number("space.inkButtonH.order", 58));
            button.cost.gameObject.SetActive(false);
            var buttonLe = button.button.GetComponent<LayoutElement>();
            buttonLe.minWidth = buttonLe.preferredWidth = 300;
            buttonLe.flexibleWidth = 0;
            T.ApplyType(button.label, "orderButton");
            button.label.enableAutoSizing = false;
            var actionFocus = UiImage(Stretch(button.button.transform, "Focus", default, 4), S("ui_frame_2px_r3"), T.ColorOf("ui.focusRing"));
            actionFocus.gameObject.SetActive(false);

            // ALINDI: BAŞLA'nın üstüne iner, CSS −6°
            var received = UiRect(doc, "Received", new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-70, 104), new Vector2(150, 40));
            received.localRotation = Quaternion.Euler(0, 0, 6);
            received.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            Row(received.gameObject, 0, TextAnchor.MiddleCenter).padding = new RectOffset(12, 12, 2, 2);
            Fit(received.gameObject, true, true);
            UiImage(Stretch(received, "Frame"), S("ui_frame_3px_r0"), T.ColorOf("stamp.received"));
            var receivedText = UiText(received, "Text", "", "secretStamp", T.ColorOf("stamp.received"), TextAlignmentOptions.Center, false);
            received.gameObject.SetActive(false);

            // Ataş (sol üst; K7 panosundaki yol, 3 px #9AA0A0)
            var clip = UiRect(doc, "Paperclip", new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), Vector2.zero, new Vector2(60, 100));
            clip.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            var clipLine = clip.gameObject.AddComponent<UILineGraphic>();
            clipLine.color = T.ColorOf("ui.paperclip");
            clipLine.raycastTarget = false;
            clipLine.Thickness = 3;
            clipLine.SetRoundCap(true);
            clipLine.SetPoints(new[]
            {
                new Vector2(22, 16), new Vector2(22, -48), new Vector2(23.5f, -56), new Vector2(30, -59), new Vector2(36.5f, -56), new Vector2(38, -48),
                new Vector2(38, 4), new Vector2(37, 8), new Vector2(34, 10), new Vector2(31, 8), new Vector2(30, 4), new Vector2(30, -40),
            });

            var component = root.gameObject.AddComponent<BriefingView>();
            component.Configure(doc, docGroup, new TMP_Text[] { agency, number, title, toLabel, to, reLabel, re, stamp }, paragraphs, closing,
                office, line, received, receivedText, button.button, button.label, actionFocus.gameObject, dossier, tab, tabButton, hover, tabFill,
                label.gameObject, hintRow.gameObject, tabFocus.gameObject, noSave.gameObject, new TMP_Text[] { tabTitle, tabSummary, tabHint, noSaveText });
            root.gameObject.SetActive(false);
            return component;
        }
    }
}
