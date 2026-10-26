using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk.EditorTools
{
    /// <summary>
    /// Aşama 8 (K6): vardiya dizisi (amber ışık, <see cref="ShiftSequence"/>), rapor okuma görünümü (HUD canvas'ında,
    /// 600 px manila teleks kâğıdı, satır yuvaları) ve EVRAK tepsisindeki A5 rapor kâğıdı. Ölçüler K6b panosundan
    /// (reference/boards_html/K6b.dc.html): kâğıt sol 40, üst 290, 600 px, CSS −1,2° (Unity +1,2°), iç boşluk 12/46/16/46.
    /// </summary>
    public static partial class SiginakRoomBuilder
    {
        sealed class ShiftParts
        {
            public ShiftSequence Sequence;
            public ShiftReportView Report;
            public EvrakTray Tray;
        }

        const int FundsSlots = 8, ProductionSlots = 6, TeamSlots = 2, FailureSlots = 4;

        static ShiftParts BuildShift(Refs refs, Hud hud, Controls controls)
        {
            var parts = new ShiftParts();
            // Şaltere anlık amber ışık (lights.amberSwitch, yalnız K6a); dizi dışında kapalı
            var sw = Child(refs.Root, "Desk/Shift switch");
            var amber = AddLight(sw, "Amber light", "amberSwitch", new Vector3(0, 0.20f, -0.06f), LightType.Point, 0);
            amber.enabled = false;
            var seqGo = new GameObject("Shift sequence");
            seqGo.transform.SetParent(refs.Root, false);
            parts.Sequence = seqGo.AddComponent<ShiftSequence>();
            parts.Sequence.Configure(controls.Flicker, refs.DeskLamp, amber);

            parts.Tray = TraySheet(Child(refs.Root, "Desk/Paper tray"));
            parts.Report = BuildReport((RectTransform)hud.Canvas.transform);
            // Plakanın altında, teleks şeridi ve üzerine gelme katmanlarının altında
            parts.Report.transform.SetSiblingIndex(hud.Plaque != null ? 2 : 0);
            return parts;
        }

        /// <summary>Tepsideki son rapor: A5 teleks kâğıdı (manila), başlık CP 700.</summary>
        static EvrakTray TraySheet(Transform tray)
        {
            var sheet = Node(tray, "Report sheet", new Vector3(-0.012f, 0.0322f, 0.008f), R(0, 5 * D2R, 0));
            var paper = Plane(sheet, "Paper", 0.148f, 0.21f, M("paper"), 0, 0, 0, rx: -PI / 2);
            var textNode = Node(sheet, "Text", new Vector3(0, 0.0006f, 0), paper.transform.localRotation);
            var title = Label(textNode, "Title", "", DeskFont.CpBold, 0.0075f, T.ColorOf("ink"), new Vector3(0, 0.082f, 0), Quaternion.identity,
                new Vector2(0.13f, 0.016f), localize: false);
            Label(textNode, "Head", SiginakRoomWords.TelexHeader, DeskFont.CpBold, 0.005f, T.ColorOf("ink3"), new Vector3(0, 0.095f, 0),
                Quaternion.identity, new Vector2(0.13f, 0.01f));
            foreach (var y in new[] { 0.055f, 0.040f, 0.025f, 0.010f, -0.005f, -0.020f })
                Plane(textNode, "Line", 0.11f, 0.0016f, M("inkBlack"), 0, y, -0.0002f);
            var component = tray.gameObject.AddComponent<EvrakTray>();
            component.Configure(sheet.gameObject, title);
            sheet.gameObject.SetActive(false);
            return component;
        }

        // ---------- Rapor okuma görünümü ----------

        static ShiftReportView BuildReport(RectTransform canvas)
        {
            float w = T.Number("space.reportW", 600);
            var pad = new RectOffset(46, 46, 12, 16);
            var root = Stretch(canvas, "Shift report");
            var paper = UiRect(root, "Paper", new Vector2(0, 1), new Vector2(0, 1), new Vector2(0.5f, 1), new Vector2(40 + w / 2, -290), new Vector2(w, 700));
            paper.localRotation = Quaternion.Euler(0, 0, 1.2f);
            UiPaper(paper, T.ColorOf("ui.slipTag[0]"), T.ColorOf("ui.slipTag[1]"), "report");
            var group = paper.gameObject.AddComponent<CanvasGroup>();
            // İki yanda delik sırası, üstte kopma çizgisi
            float hole = T.Number("space.telexStrip.holeColumn", 10);
            UiImage(UiRect(paper, "Holes left", new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, 0.5f), new Vector2(14, 0), new Vector2(hole, -6)),
                S("ui_telex_holes"), Color.white, Image.Type.Tiled);
            UiImage(UiRect(paper, "Holes right", new Vector2(1, 0), new Vector2(1, 1), new Vector2(1, 0.5f), new Vector2(-14, 0), new Vector2(hole, -6)),
                S("ui_telex_holes"), Color.white, Image.Type.Tiled);
            UiImage(UiRect(paper, "Perforation", new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -5), new Vector2(0, 2)),
                S("ui_dash_h"), T.ColorOf("ui.reportPerforation"), Image.Type.Tiled);
            Column(paper.gameObject, 10, pad);
            Fit(paper.gameObject, false, true);

            // Başlık: TELEKS · KYGA-07 → KOMUTA / kod, VARDİYA RAPORU · 014, 014 → 015 · bir gün ilerledi; altında kırmızı çizgi
            var header = Block(paper, "Header", 2);
            header.GetComponent<VerticalLayoutGroup>().padding = new RectOffset(0, 0, 6, 8);
            UiImage(UiRect(header, "Rule", new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), Vector2.zero, new Vector2(0, 2)),
                S("ui_fill_r0"), T.ColorOf("ruleRed"));
            var headRow = UiRect(header, "Head row", Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(w, 18));
            Row(headRow.gameObject, 8);
            var head = SlipText(headRow, "Head", "reportHead", "ink3");
            var code = UiText(headRow, "Code", "", "reportHead", T.ColorOf("ink3"), TextAlignmentOptions.MidlineRight, false);
            var title = SlipText(header, "Title", "reportTitle", "ink");
            var sub = SlipText(header, "Sub", "reportSub", "ink2");

            // KAPANDI damgası: sağ 44, üst 76, CSS −7° (Unity +7°), 3 px kırmızı kenar
            var stampRt = UiRect(paper, "Stamp", Vector2.one, Vector2.one, Vector2.one, new Vector2(-44, -76), new Vector2(130, 32));
            stampRt.localRotation = Quaternion.Euler(0, 0, 7);
            stampRt.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            Row(stampRt.gameObject, 0, TextAnchor.MiddleCenter).padding = new RectOffset(9, 9, 1, 1);
            Fit(stampRt.gameObject, true, true);
            UiImage(Stretch(stampRt, "Frame"), S("ui_frame_3px_r0"), T.ColorOf("stamp.closed"));
            var stamp = UiText(stampRt, "Text", "", "reportStamp", T.ColorOf("stamp.closed"), TextAlignmentOptions.Center, false);
            stampRt.gameObject.AddComponent<CanvasGroup>().alpha = 0.9f;

            float rowWidth = w - pad.left - pad.right, spacing = 12;
            var funds = ReportSection(paper, "Funds", out var fundsTitle);
            var fundRows = Enumerable.Range(0, FundsSlots).Select(i => ReportRowSlot(funds, "Row " + i, rowWidth, spacing)).ToArray();
            var production = ReportSection(paper, "Production", out var productionTitle);
            var productionRows = Enumerable.Range(0, ProductionSlots).Select(i => ReportRowSlot(production, "Row " + i, rowWidth, spacing)).ToArray();
            var analyst = UiText(production, "Analyst", "", "analystNote", T.ColorOf("pen.graphite"), TextAlignmentOptions.TopRight, false);
            analyst.rectTransform.localRotation = Quaternion.Euler(0, 0, 3);

            var team = ReportSection(paper, "Team", out var teamTitle);
            var teamRows = Enumerable.Range(0, TeamSlots).Select(i => ReportTeamSlot(team, "Team " + i)).ToArray();
            var failed = ReportSection(paper, "Failed", out var failedTitle);
            var failures = Enumerable.Range(0, FailureSlots).Select(i => SlipText(failed, "Line " + i, "reportFailure", "ink", wrap: true)).ToArray();
            var more = SlipText(failed, "More", "reportRowNote", "ink3");

            var button = InkButton(paper, "Action", T.Number("space.inkButtonH.report", 54));
            button.cost.gameObject.SetActive(false);
            // Alt satır: tepsiden okurken ← → düğmeleri, ortada ipucu
            var hintRow = UiRect(paper, "Hint row", Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(w, 22));
            Row(hintRow.gameObject, 8, TextAnchor.MiddleCenter);
            var prev = ArrowButton(hintRow, "Previous", "←");
            var hint = UiText(hintRow, "Hint", "", "reportRowNote", T.ColorOf("ink3"), TextAlignmentOptions.Center, false);
            hint.font = DeskTheme.Active.Font(DeskFont.CpRegular);
            hint.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            var next = ArrowButton(hintRow, "Next", "→");

            var view = root.gameObject.AddComponent<ShiftReportView>();
            view.Configure(paper, group, head, code, title, sub, stamp, new TMP_Text[] { fundsTitle, productionTitle, teamTitle, failedTitle },
                fundRows, productionRows, analyst, team.gameObject, teamRows, failed.gameObject, failures, more, button.button, button.label, hint,
                prev, next, rowWidth, spacing);
            root.gameObject.SetActive(false);
            return view;
        }

        /// <summary>Bölüm: CP 700 14 başlık + noktalı ayraç (ui_dash_h, reportRule), altında satırlar.</summary>
        static RectTransform ReportSection(Transform paper, string name, out TextMeshProUGUI title)
        {
            var block = Block(paper, name, 3);
            var head = UiRect(block, "Head", Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(400, 18));
            Row(head.gameObject, 8);
            title = UiText(head, "Title", "", "reportSection", T.ColorOf("ink3"), TextAlignmentOptions.MidlineLeft, false);
            var dots = UiImage(UiRect(head, "Dots", Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(10, 2)),
                S("ui_dash_h"), T.ColorOf("ui.reportRule"), Image.Type.Tiled);
            var le = dots.GetComponent<LayoutElement>();
            le.ignoreLayout = false;
            le.flexibleWidth = 1;
            le.minHeight = le.preferredHeight = 2;
            return block;
        }

        /// <summary>Satır: ad (+ açıklama) · damga · boşluk · değer; toplamda üstte 1 px çizgi.</summary>
        static ShiftReportView.RowSlot ReportRowSlot(Transform parent, string name, float width, float spacing)
        {
            var r = UiRect(parent, name, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(width, 26));
            var h = Row(r.gameObject, spacing, TextAnchor.UpperLeft);
            h.padding = new RectOffset(0, 0, 3, 0);
            var label = UiText(r, "Label", "", "reportRow", T.ColorOf("ink"), TextAlignmentOptions.TopLeft, false, true);
            var labelLayout = label.gameObject.AddComponent<LayoutElement>();
            var badge = UiRect(r, "Badge", Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(100, 22));
            Row(badge.gameObject, 0, TextAnchor.MiddleCenter).padding = new RectOffset(5, 5, 1, 1);
            UiImage(Stretch(badge, "Frame"), S("ui_frame_2px_r3"), T.ColorOf("stamp.noPower"));
            var badgeText = UiText(badge, "Text", "", "reportSection", T.ColorOf("stamp.noPower"), TextAlignmentOptions.Center, false);
            var spacer = UiRect(r, "Spacer", Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(0, 1));
            spacer.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            var value = UiText(r, "Value", "", "reportValue", T.ColorOf("ink"), TextAlignmentOptions.TopRight, false);
            var rule = UiImage(UiRect(r, "Rule", new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), Vector2.zero, new Vector2(0, 1)),
                S("ui_fill_r0"), T.ColorOf("ink2"));
            return new ShiftReportView.RowSlot
            {
                root = r.gameObject, label = label, value = value, labelLayout = labelLayout, badge = badge.gameObject, badgeText = badgeText,
                rule = rule.gameObject,
            };
        }

        /// <summary>Ekip satırı: üstten figür simgesi (34 × 26), “Alfa · yaralı”, “2 → 1 gün”; altında revir cümlesi.</summary>
        static ShiftReportView.TeamSlot ReportTeamSlot(Transform parent, string name)
        {
            var block = Block(parent, name, 2);
            var row = UiRect(block, "Row", Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(400, 28));
            Row(row.gameObject, 10);
            LayoutImage(row, "Icon", S("icon_team_figure"), Color.white, T.Vector2Of("space.teamFigure.formIconPx"));
            var label = SlipText(row, "Label", "reportRow", "ink");
            var value = UiText(row, "Value", "", "reportValue", T.ColorOf("ink"), TextAlignmentOptions.MidlineRight, false);
            var detail = SlipText(block, "Detail", "reportRowNote", "ink2", wrap: true);
            return new ShiftReportView.TeamSlot { root = block.gameObject, label = label, value = value, detail = detail };
        }

        /// <summary>Tepside önceki / sonraki rapor: küçük çerçeveli düğme.</summary>
        static Button ArrowButton(Transform parent, string name, string glyph)
        {
            var r = UiRect(parent, name, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(30, 24));
            var le = r.gameObject.AddComponent<LayoutElement>();
            le.minWidth = le.preferredWidth = 30;
            le.minHeight = le.preferredHeight = 24;
            var fill = UiImage(Stretch(r, "Fill"), S("ui_fill_r2"), T.ColorOf("ui.frameButton"), Image.Type.Sliced, true);
            UiImage(Stretch(r, "Edge"), S("ui_frame_1px_r2"), T.ColorOf("ink2"));
            var t = UiText(r, "Glyph", glyph, "reportValue", T.ColorOf("ink"), TextAlignmentOptions.Center, false);
            t.rectTransform.anchorMin = Vector2.zero; t.rectTransform.anchorMax = Vector2.one; t.rectTransform.sizeDelta = Vector2.zero;
            var b = r.gameObject.AddComponent<Button>();
            b.targetGraphic = fill;
            b.transition = Selectable.Transition.None;
            return b;
        }
    }
}
