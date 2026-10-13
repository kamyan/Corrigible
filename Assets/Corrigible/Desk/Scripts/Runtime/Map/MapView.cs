using System.Collections.Generic;
using Corrigible.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>
    /// Harita (C1 §7.1, <c>map.json</c>): pafta üstüne veriden kurulan katmanlar — başlık, ızgara ve kenar etiketleri, basılı
    /// yollar (kesik 6/4, karantinada kesik işareti), kenar okları, ekip jetonları, düğümler, etiketler ve analist katmanı
    /// (iplikler, raptiyeler, kalem okları, notlar, sarı not, “?”). Kök tam ekrandır; koordinatlar ekran pikseli (sol-üst).
    /// Düğüm konumları VM'den gelir (oyunda projeksiyon, F8).
    /// İllüzyonda (PROMPT §7.2) analist katmanı gizlenir; yerinde izler kalır: raptiye deliği, silik kalem notları, silgi lekesi.
    /// </summary>
    public sealed class MapView : MonoBehaviour, IIllusionAware
    {
        [Header("Başlık")]
        [SerializeField] DeskRow headerRow;
        [SerializeField] TMP_Text headerTitle, headerSummary, headerRight;

        [Header("Şablonlar (kendi katmanlarında, kapalı)")]
        [SerializeField] TMP_Text graticuleTemplate;
        [SerializeField] TMP_Text edgeLabelTemplate;
        [SerializeField] UILineGraphic routeTemplate;
        [SerializeField] Image edgeArrowTemplate;
        [SerializeField] Image cutMarkTemplate;
        [SerializeField] TeamTokenView teamTemplate;
        [SerializeField] RegionNodeView nodeTemplate;
        [SerializeField] RegionLabelView labelTemplate;

        [Header("Analist katmanı")]
        [SerializeField] RectTransform analystRoot;
        [SerializeField] UILineGraphic stringShadowTemplate;
        [SerializeField] UILineGraphic stringTemplate;
        [SerializeField] Image flagTemplate;
        [SerializeField] UILineGraphic pencilTemplate;
        [SerializeField] Image chevronTemplate;
        [SerializeField] Image pinTemplate;
        [SerializeField] TMP_Text noteTemplate;
        [SerializeField] TMP_Text questionTemplate;
        [SerializeField] RectTransform sticky;
        [SerializeField] TMP_Text stickyText;
        [SerializeField] SelectionThreadView selection;

        [Header("İllüzyon izleri")]
        [SerializeField] RectTransform tracesRoot;
        [SerializeField] Image pinholeTemplate;
        [SerializeField] TMP_Text ghostTemplate;
        [SerializeField] Image eraser;
        [Tooltip("Silik izlerin CSS opaklığı sRGB'de karışır; Linear'da eşdeğer alfa bu arka plan parlaklığına göre hesaplanır (harita kâğıdı).")]
        [SerializeField] float traceBackground = 0.85f;

        DeskPool<TMP_Text> graticules, edgeLabels, notes, questions, ghosts;
        DeskPool<Image> pinholes;
        MapVM last;
        CanvasGroup analystGroup;
        DeskTween analystFade;
        DeskPool<UILineGraphic> routes, stringShadows, strings, pencils;
        DeskPool<Image> arrows, cuts, flags, chevrons, pins;
        DeskPool<TeamTokenView> teams;
        DeskPool<RegionNodeView> nodes;
        DeskPool<RegionLabelView> labels;
        readonly List<RegionNodeView> nodeList = new List<RegionNodeView>();
        static readonly List<Vector2> pts = new List<Vector2>(8);

        public IReadOnlyList<RegionNodeView> Nodes => nodeList;
        public SelectionThreadView Selection => selection;
        public event System.Action<string> NodeClicked;

        public void Bind(MapVM m)
        {
            if (m == null) return;
            last = m;
            DeskUi.SetText(headerTitle, DeskLoc.Instance.T(m.Header?.Title));
            DeskUi.SetText(headerSummary, DeskLoc.Instance.T(m.Header?.Summary));
            DeskUi.SetText(headerRight, m.Header?.Right);
            headerRow.Layout();

            if (!Application.isPlaying && !DeskUi.EditPreview) return;
            Init();

            for (int i = 0; i < m.GraticuleLabels.Count; i++) PlaceText(graticules.Get(i), m.GraticuleLabels[i].Text, m.GraticuleLabels[i].X, m.GraticuleLabels[i].Y);
            graticules.Trim(m.GraticuleLabels.Count);
            for (int i = 0; i < m.EdgeLabels.Count; i++) PlaceText(edgeLabels.Get(i), m.EdgeLabels[i].Text, m.EdgeLabels[i].X, m.EdgeLabels[i].Y);
            edgeLabels.Trim(m.EdgeLabels.Count);

            // Basılı yollar (+ kenar yolları aynı stilde) ve kesik işaretleri
            int ri = 0, ci = 0;
            foreach (var r in m.Routes)
            {
                Curve(routes.Get(ri++), r.Q);
                if (r.State == "cut" && r.CutCenter != null) Sprite(cuts.Get(ci++), r.CutCenter, r.CutTangentDeg);
            }
            for (int i = 0; i < m.EdgeRoutes.Count; i++)
            {
                Curve(routes.Get(ri++), m.EdgeRoutes[i].Q);
                Sprite(arrows.Get(i), m.EdgeRoutes[i].ArrowPos, m.EdgeRoutes[i].ArrowRotDeg);
            }
            routes.Trim(ri); cuts.Trim(ci); arrows.Trim(m.EdgeRoutes.Count);

            for (int i = 0; i < m.Teams.Count; i++) teams.Get(i).Bind(m.Teams[i]);
            teams.Trim(m.Teams.Count);

            nodeList.Clear();
            for (int i = 0; i < m.Regions.Count; i++)
            {
                bool fresh = i >= nodes.Count;
                var n = nodes.Get(i); n.Bind(m.Regions[i]); nodeList.Add(n);
                var label = labels.Get(i);
                label.Bind(m.Regions[i]);
                if (fresh) Hook(n, label);
            }
            nodes.Trim(m.Regions.Count); labels.Trim(m.Regions.Count);

            bool fading = analystFade.Running;
            if (!fading) BindAnalyst(m.AnalystLayer && !DeskTheme.Illusion ? m.Analyst : null);
            BindTraces(DeskTheme.Illusion ? m : null);
            if (selection != null)
            {
                RectTransform sel = null;
                foreach (var n in nodeList) if (n.RegionId == m.SelectedRegion) sel = (RectTransform)n.transform;
                selection.Bind(sel, m.AnalystLayer && !DeskTheme.Illusion);
            }
        }

        /// <summary>
        /// İllüzyon anahtarı: aynı VM yeniden çizilir (analist katmanı, izler, düğüm rozetleri, karantina halkası).
        /// <see cref="IllusionPresenter.AnalystFadeSeconds"/> &gt; 0 ise analist katmanı tek seferde değil, solarak kaybolur (açık karar 5).
        /// </summary>
        public void SetIllusion(bool on)
        {
            float fade = IllusionPresenter.Instance != null ? IllusionPresenter.Instance.AnalystFadeSeconds : 0;
            if (on && fade > 0 && analystRoot != null && analystRoot.gameObject.activeSelf && Application.isPlaying)
            {
                if (analystGroup == null && !analystRoot.TryGetComponent(out analystGroup)) analystGroup = analystRoot.gameObject.AddComponent<CanvasGroup>();
                analystFade.Snap(1);
                analystFade.To(0, fade);
            }
            else if (!on && analystGroup != null) { analystFade.Snap(1); analystGroup.alpha = 1; }
            if (last != null) Bind(last);
        }

        void Update()
        {
            if (!analystFade.Tick(Time.unscaledDeltaTime)) return;
            analystGroup.alpha = analystFade.Value;
            if (!analystFade.Running) { analystGroup.alpha = 1; if (last != null) BindAnalyst(null); }
        }

        /// <summary>İllüzyon izleri: analist raptiyelerinin yerinde delik, silinmiş notların izi, silgi lekesi (PROMPT §7.2).</summary>
        void BindTraces(MapVM m)
        {
            DeskUi.SetActive(tracesRoot, m != null);
            if (m == null || tracesRoot == null) return;
            var pins = m.Analyst?.Pins;
            int np = pins?.Count ?? 0;
            for (int i = 0; i < np; i++) Sprite(pinholes.Get(i), pins[i], 0);
            pinholes.Trim(np);

            var tr = m.IllusionTraces;
            int ng = tr?.GhostNotes.Count ?? 0;
            for (int i = 0; i < ng; i++)
            {
                var g = tr.GhostNotes[i];
                var t = ghosts.Get(i);
                t.font = g.Weight >= 700 ? DeskTheme.Active.kalamBold : DeskTheme.Active.kalamRegular;
                var c = DeskTheme.Active.Get("ink.graphite");
                c.a = DeskMath.LinearOverlayAlpha(c, g.Alpha, traceBackground);
                t.color = c;
                Rotated(t, DeskLoc.Instance.T(g.Text), g.X, g.Y, g.RotDeg);
            }
            ghosts.Trim(ng);

            var e = tr?.EraserSmudge;
            DeskUi.SetActive(eraser, e?.Center != null && e.Radius != null);
            if (e?.Center != null && e.Radius != null)
            {
                ColorUtility.TryParseHtmlString(e.Color ?? "#000000", out var c);
                c.a = DeskMath.LinearOverlayAlpha(c, e.Alpha, traceBackground);
                eraser.color = c;
                var rt = eraser.rectTransform;
                rt.anchorMin = rt.anchorMax = new Vector2(0, 1); rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(e.Center[0], -e.Center[1]);
                rt.sizeDelta = new Vector2(e.Radius[0] * 2, e.Radius[1] * 2);
            }
        }

        /// <summary>Düğüm üstüne gelme (açık karar 4): etiket çipi mürekkep kenar alır, düğüm 1 px kalkar, ipucunda ad + değer + güven kelimesi.</summary>
        void Hook(RegionNodeView n, RegionLabelView label)
        {
            n.Clicked += v => NodeClicked?.Invoke(v.RegionId);
            n.Hovered += (v, on) =>
            {
                label.SetHover(on);
                if (!on) { DeskTooltip.Hide(v); return; }
                var d = v.Data;
                var T = DeskLoc.Instance;
                string value = d.Value.HasValue && d.Trust != "unknown" ? DeskFormat.Percent(d.Value.Value) : "?";
                string word = T.T(d.Trust switch { "verified" => "onaylandı", "report" => "~ RAPOR", "suspicious" => "şüpheli", _ => "BİLİNMİYOR" });
                DeskTooltip.ShowText(v, d.Name + " · " + value + " · " + word);
            };
        }

        void Init()
        {
            if (nodes != null) return;
            graticules = new DeskPool<TMP_Text>(graticuleTemplate);
            edgeLabels = new DeskPool<TMP_Text>(edgeLabelTemplate);
            routes = new DeskPool<UILineGraphic>(routeTemplate);
            arrows = new DeskPool<Image>(edgeArrowTemplate);
            cuts = new DeskPool<Image>(cutMarkTemplate);
            teams = new DeskPool<TeamTokenView>(teamTemplate);
            nodes = new DeskPool<RegionNodeView>(nodeTemplate);
            labels = new DeskPool<RegionLabelView>(labelTemplate);
            stringShadows = new DeskPool<UILineGraphic>(stringShadowTemplate);
            strings = new DeskPool<UILineGraphic>(stringTemplate);
            flags = new DeskPool<Image>(flagTemplate);
            pencils = new DeskPool<UILineGraphic>(pencilTemplate);
            chevrons = new DeskPool<Image>(chevronTemplate);
            pins = new DeskPool<Image>(pinTemplate);
            notes = new DeskPool<TMP_Text>(noteTemplate);
            questions = new DeskPool<TMP_Text>(questionTemplate);
            if (tracesRoot != null)
            {
                pinholes = new DeskPool<Image>(pinholeTemplate);
                ghosts = new DeskPool<TMP_Text>(ghostTemplate);
            }
        }

        void BindAnalyst(AnalystLayerVM a)
        {
            DeskUi.SetActive(analystRoot, a != null);
            if (a == null) return;
            for (int i = 0; i < a.Strings.Count; i++)
            {
                var s = a.Strings[i];
                Line(stringShadows.Get(i), s.A, s.B, new Vector2(1.5f, 2));
                Line(strings.Get(i), s.A, s.B, Vector2.zero);
                Sprite(flags.Get(i), s.FlagPos, s.FlagRotDeg);
            }
            stringShadows.Trim(a.Strings.Count); strings.Trim(a.Strings.Count); flags.Trim(a.Strings.Count);

            for (int i = 0; i < a.PencilArrows.Count; i++)
            {
                var p = a.PencilArrows[i];
                var g = pencils.Get(i);
                ColorUtility.TryParseHtmlString(p.Color, out var col);
                g.color = col; g.Thickness = p.Width; g.SetDash(p.Dash);
                Curve(g, p.Q);
                var head = chevrons.Get(i);
                head.color = col;
                Sprite(head, p.HeadPos, p.HeadRotDeg);
            }
            pencils.Trim(a.PencilArrows.Count); chevrons.Trim(a.PencilArrows.Count);

            for (int i = 0; i < a.Pins.Count; i++) Sprite(pins.Get(i), a.Pins[i], 0);
            pins.Trim(a.Pins.Count);

            for (int i = 0; i < a.Notes.Count; i++)
            {
                var n = a.Notes[i];
                var t = notes.Get(i);
                t.font = n.Weight >= 700 ? DeskTheme.Active.kalamBold : DeskTheme.Active.kalamRegular;
                ColorUtility.TryParseHtmlString(n.Color, out var col);
                t.color = col;
                Rotated(t, DeskLoc.Instance.T(n.Text), n.X, n.Y, n.RotDeg);
            }
            notes.Trim(a.Notes.Count);

            for (int i = 0; i < a.QuestionMarks.Count; i++) PlaceText(questions.Get(i), "?", a.QuestionMarks[i][0], a.QuestionMarks[i][1]);
            questions.Trim(a.QuestionMarks.Count);

            DeskUi.SetActive(sticky, a.Sticky != null);
            if (a.Sticky != null)
            {
                // Not kâğıdının yeri, genişliği ve eğimi sahnede; veri yalnız metni (ve ona göre yüksekliği) verir
                DeskUi.SetText(stickyText, DeskLoc.Instance.T(a.Sticky.Text));
                float h = Mathf.Ceil(stickyText.GetPreferredValues(stickyText.text, sticky.rect.width - 24, 1000).y) + 24;
                DeskUi.SetHeightKeepTop(sticky, h);
            }
        }

        /// <summary>
        /// Harita koordinatının (1920×1080 tasarım çerçevesi, sol-üst köken, y aşağı) ekrandaki gerçek yeri: harita sahnede ya da
        /// sürüklenerek başka yere konmuşsa düğümlerle birlikte kayar (olay kartının spotu ve ipliği buna bağlanır).
        /// </summary>
        public Vector3 MapToWorld(float x, float y)
        {
            var frame = Frame;
            var r = frame.rect;
            return frame.TransformPoint(new Vector3(r.xMin + x, r.yMax - y, 0));
        }

        /// <summary>Harita çerçevesinin dünya ölçeği (sahnede küçültülmüş haritada olay spotu da küçülür).</summary>
        public float WorldScale => Frame.lossyScale.x;

        RectTransform Frame => nodeTemplate != null ? nodeTemplate.transform.parent as RectTransform : (RectTransform)transform;

        // ------------------------------------------------------------------ yardımcılar
        static Vector2 P(float[] p) => new Vector2(p[0], p[1]);

        static void PlaceText(TMP_Text t, string text, float x, float y)
        {
            DeskUi.SetText(t, text);
            var rt = t.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1); rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(Mathf.Round(DeskUi.TextWidth(t, text)) + 2, rt.sizeDelta.y);
        }

        /// <summary>Döndürülmüş not: (x, y) dönüşümsüz kutunun sol-üstü, dönüş merkez etrafında (CSS).</summary>
        static void Rotated(TMP_Text t, string text, float x, float y, float rotDeg)
        {
            DeskUi.SetText(t, text);
            var size = t.GetPreferredValues(text, 10000, 1000);
            var rt = t.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1); rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(Mathf.Ceil(size.x), Mathf.Ceil(size.y));
            rt.anchoredPosition = new Vector2(x + rt.sizeDelta.x / 2, -(y + rt.sizeDelta.y / 2));
            rt.localEulerAngles = new Vector3(0, 0, -rotDeg);
        }

        static void Sprite(Image img, float[] pos, float rotDeg)
        {
            var rt = img.rectTransform;
            var sp = img.sprite;
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            if (sp != null) rt.pivot = new Vector2(sp.pivot.x / sp.rect.width, sp.pivot.y / sp.rect.height);
            rt.anchoredPosition = new Vector2(pos[0], -pos[1]);
            rt.localEulerAngles = new Vector3(0, 0, -rotDeg);
        }

        static void Curve(UILineGraphic g, List<float[]> q)
        {
            pts.Clear();
            foreach (var p in q) pts.Add(P(p));
            g.Curve = pts.Count == 3 ? LineCurve.Quadratic : LineCurve.Polyline;
            g.SetPointsTopLeft(pts, Vector2.zero);
        }

        static void Line(UILineGraphic g, float[] a, float[] b, Vector2 offset)
        {
            pts.Clear();
            pts.Add(P(a) + offset); pts.Add(P(b) + offset);
            g.Curve = LineCurve.Polyline;
            g.SetPointsTopLeft(pts, Vector2.zero);
        }

        /// <summary>Kaynak dizileri çeviri tablosuna toplatmak için (Loc.N işaretleri).</summary>
        internal static readonly string[] Words = { Loc.N("Ç") };
    }
}
