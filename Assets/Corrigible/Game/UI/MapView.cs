using System;
using System.Collections.Generic;
using Corrigible.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace Corrigible.Game.UI
{
    /// <summary>
    /// Nokta matrisi dünya haritası. Koordinatlar simülasyonla aynı 1000×480 eşdikdörtgen düzlemde; ekran ölçeğine çevrilir.
    /// Statik kara noktaları ayrı katmanda bir kez çizilir; bağlantı ve düğümler her gün yeniden çizilir.
    /// </summary>
    public sealed class MapView : VisualElement
    {
        public const float W = 1000f, H = 480f;

        static readonly Color Bg = Hex("#0a1114"), Line = Hex("#22343c"), Line2 = Hex("#2f4650"), Land = Hex("#27424d"),
            Fg = Hex("#d6e1e3"), Muted = Hex("#7f949b"), Threat = Hex("#ff5b4d"), Verify = Hex("#5cc8d9"), Warn = Hex("#f0b03f"),
            Unknown = Hex("#142229"), NodeBase = new Color(28 / 255f, 44 / 255f, 52 / 255f);

        readonly VisualElement land = new VisualElement();
        readonly VisualElement dynamicLayer = new VisualElement();
        readonly VisualElement labels = new VisualElement();
        readonly Dictionary<string, Label> names = new Dictionary<string, Label>(), subs = new Dictionary<string, Label>(),
            values = new Dictionary<string, Label>(), coreBadges = new Dictionary<string, Label>();
        readonly List<Label> latLabels = new List<Label>();

        Simulation sim;
        public string Selected { get; set; }
        public event Action<string> RegionClicked;

        float Scale => resolvedStyle.width > 0 ? resolvedStyle.width / W : 1f;

        public MapView()
        {
            AddToClassList("map");
            land.AddToClassList("map-layer");
            dynamicLayer.AddToClassList("map-layer");
            labels.AddToClassList("map-layer");
            labels.pickingMode = PickingMode.Ignore;
            land.pickingMode = PickingMode.Ignore;
            Add(land); Add(dynamicLayer); Add(labels);
            land.generateVisualContent += DrawLand;
            dynamicLayer.generateVisualContent += DrawDynamic;

            foreach (var (lat, text) in new[] { (60.0, "60°K"), (30.0, "30°K"), (0.0, "0°"), (-30.0, "30°G") })
            {
                var l = new Label(text) { userData = lat };
                l.AddToClassList("map-lat");
                labels.Add(l); latLabels.Add(l);
            }
            foreach (var r in GameData.Regions)
            {
                names[r.Id] = MakeLabel(r.Name, "map-name");
                subs[r.Id] = MakeLabel("", "map-sub");
                values[r.Id] = MakeLabel("?", "map-val");
                var cb = MakeLabel("Ç", "map-badge");
                cb.style.backgroundColor = Threat;
                cb.style.color = Hex("#1d0b08");
                coreBadges[r.Id] = cb;
            }
            RegisterCallback<GeometryChangedEvent>(_ => Relayout());
            dynamicLayer.RegisterCallback<PointerDownEvent>(OnPointerDown);
        }

        Label MakeLabel(string text, string cls)
        {
            var l = new Label(text);
            l.AddToClassList(cls);
            l.pickingMode = PickingMode.Ignore;
            labels.Add(l);
            return l;
        }

        public void Bind(Simulation s)
        {
            sim = s;
            Refresh();
        }

        static float Radius(RegionDef d) => 12 + d.Compute * 3;
        static float ProjY(double lat) => (float)((78 - lat) / 136 * 480);

        void Relayout()
        {
            float w = resolvedStyle.width;
            if (w <= 0) return;
            float h = w * H / W;
            if (Mathf.Abs(resolvedStyle.height - h) > 0.5f) style.height = h;
            float k = Scale;
            foreach (var l in latLabels)
            {
                l.style.left = 4;
                l.style.top = ProjY((double)l.userData) * k - 14;
            }
            foreach (var d in GameData.Regions)
            {
                float r = Radius(d), x = (float)d.X, y = (float)d.Y;
                Place(values[d.Id], x * k, y * k, -50, -50);
                Place(coreBadges[d.Id], (x + r * 0.72f) * k, (y + r * 0.72f) * k, -50, -50);
                switch (d.LabelPos)
                {
                    case 't':
                        Place(names[d.Id], x * k, (y - r - 10) * k, -50, -100);
                        Place(subs[d.Id], x * k, (y - r - 10) * k - 16, -50, -100);
                        break;
                    case 'l':
                        Place(names[d.Id], (x - r - 8) * k, y * k, -100, -50);
                        Place(subs[d.Id], (x - r - 8) * k, y * k + 14, -100, -50);
                        break;
                    case 'r':
                        Place(names[d.Id], (x + r + 8) * k, y * k, 0, -50);
                        Place(subs[d.Id], (x + r + 8) * k, y * k + 14, 0, -50);
                        break;
                    default:
                        Place(names[d.Id], x * k, (y + r + 6) * k, -50, 0);
                        Place(subs[d.Id], x * k, (y + r + 6) * k + 15, -50, 0);
                        break;
                }
            }
            land.MarkDirtyRepaint();
            dynamicLayer.MarkDirtyRepaint();
        }

        static void Place(VisualElement e, float x, float y, float tx, float ty)
        {
            e.style.left = x;
            e.style.top = y;
            e.style.translate = new Translate(Length.Percent(tx), Length.Percent(ty));
        }

        /// <summary>Her gün çağrılır: düğüm değerleri, alt etiketler, rozetler ve çizimler güncellenir.</summary>
        public void Refresh()
        {
            if (sim == null) return;
            var s = sim.S;
            bool ended = s.Ending != null;
            foreach (var r in s.Regions)
            {
                var sh = sim.Shown(r);
                bool offNow = r.Off > 0 && !ended;
                var v = values[r.Id];
                v.text = sh.Src == "unknown" ? "?" : offNow ? "—" : Js.Round(sh.V ?? 0).ToString();
                v.EnableInClassList("report", sh.Src == "report");
                v.style.color = sh.V.HasValue && sh.V > 55 && !offNow ? Hex("#1d0b08") : Fg;
                var sub = subs[r.Id];
                sub.text = ended ? "" : r.Left ? "İTTİFAK DIŞI" : offNow ? $"KAPALI · {r.Off} G" : r.Q ? "KARANTİNA" : "";
                sub.style.color = r.Off > 0 ? Threat : r.Left ? Muted : Warn;
                names[r.Id].style.opacity = r.Left && !ended ? 0.45f : 1f;
                coreBadges[r.Id].style.display = (ended ? s.Core == r.Id : s.CoreKnown == r.Id) ? DisplayStyle.Flex : DisplayStyle.None;
            }
            dynamicLayer.MarkDirtyRepaint();
        }

        void OnPointerDown(PointerDownEvent e)
        {
            float k = Scale;
            var p = e.localPosition / k;
            string best = null; float bestD = float.MaxValue;
            foreach (var d in GameData.Regions)
            {
                float dist = Vector2.Distance(p, new Vector2((float)d.X, (float)d.Y));
                if (dist < Radius(d) + 14 && dist < bestD) { best = d.Id; bestD = dist; }
            }
            if (best != null) RegionClicked?.Invoke(best);
        }

        // ---------- Çizim ----------
        void DrawLand(MeshGenerationContext ctx)
        {
            var p = ctx.painter2D;
            float k = Scale;
            p.strokeColor = Line;
            p.lineWidth = 1;
            foreach (var lat in new[] { 60.0, 30.0, 0.0, -30.0 })
            {
                float y = ProjY(lat) * k;
                p.BeginPath(); p.MoveTo(new Vector2(0, y)); p.LineTo(new Vector2(W * k, y)); p.Stroke();
            }
            p.fillColor = Land;
            float rad = 1.7f * k;
            foreach (var dot in LandMask.Dots)
            {
                p.BeginPath();
                p.Arc(dot * k, rad, 0, 360);
                p.Fill();
            }
        }

        void DrawDynamic(MeshGenerationContext ctx)
        {
            if (sim == null) return;
            var p = ctx.painter2D;
            var s = sim.S;
            float k = Scale;
            bool ended = s.Ending != null;
            Vector2 Pos(string id) { var d = GameData.Region(id); return new Vector2((float)d.X, (float)d.Y) * k; }
            double ShownV(RegionState r) => sim.Shown(r).V ?? 0;

            // Bağlantılar
            foreach (var l in GameData.Links)
            {
                var A = s.Region(l.A); var B = s.Region(l.B);
                bool q = (A.Q || B.Q) && !ended, off = (A.Off > 0 || B.Off > 0) && !ended;
                bool hot = ShownV(A) > 15 && ShownV(B) > 15;
                var c = hot ? Color.Lerp(Line2, Threat, 0.55f) : Line2;
                if (off) c.a = 0.15f; else if (q) c.a = 0.5f;
                DrawLink(p, Pos(l.A), Pos(l.B), l.Wrap, c, 1.6f * k, q ? 6 * k : 0);
            }
            if (s.Has("uydu"))
                foreach (var l in GameData.Satellites)
                    DrawDotted(p, Pos(l.A), Pos(l.B), l.Wrap, new Color(Verify.r, Verify.g, Verify.b, 0.6f), k);

            // Düğümler
            foreach (var r in s.Regions)
            {
                var d = GameData.Region(r.Id);
                var c = Pos(r.Id);
                float rad = Radius(d) * k;
                var sh = sim.Shown(r);
                bool offNow = r.Off > 0 && !ended;
                if (r.Q && !ended) DrawDashedCircle(p, c, rad + 6 * k, Warn, 1.6f * k);
                Color fill = sh.Src == "unknown" ? Unknown : offNow ? Bg : InfColor(sh.V ?? 0);
                if (r.Left && !ended) fill.a = 0.45f;
                p.fillColor = fill;
                p.BeginPath(); p.Arc(c, rad, 0, 360); p.Fill();
                p.strokeColor = Selected == r.Id ? Fg : Line2;
                p.lineWidth = (Selected == r.Id ? 2.5f : 1.5f) * k;
                p.BeginPath(); p.Arc(c, rad, 0, 360); p.Stroke();
                if (sh.Src == "unknown") DrawHatch(p, c, rad, k);
                if ((sh.Src == "verified" || sh.Src == "field") && !offNow) DrawCheck(p, c + new Vector2(rad * 0.72f, -rad * 0.72f), k);
                if (sh.Flag && sh.Src == "report" && !offNow) DrawFlag(p, c + new Vector2(-rad * 0.72f, -rad * 0.72f), k);
            }
        }

        static Color InfColor(double v)
        {
            float t = Mathf.Pow(Mathf.Clamp01((float)v / 100f), 0.8f);
            return Color.Lerp(NodeBase, Threat, t);
        }

        static void DrawLink(Painter2D p, Vector2 a, Vector2 b, bool wrap, Color c, float width, float dash)
        {
            p.strokeColor = c; p.lineWidth = width;
            if (wrap)
            {
                // Pasifik: iki uç haritanın kenarına uzanır
                float k = width / 1.6f;
                Seg(p, a, new Vector2(0, a.y - 6 * k), dash);
                Seg(p, b, new Vector2(W * k, b.y + 6 * k), dash);
            }
            else Seg(p, a, b, dash);
        }

        static void Seg(Painter2D p, Vector2 a, Vector2 b, float dash)
        {
            if (dash <= 0) { p.BeginPath(); p.MoveTo(a); p.LineTo(b); p.Stroke(); return; }
            float len = Vector2.Distance(a, b);
            var dir = (b - a) / len;
            for (float t = 0; t < len; t += dash * 1.6f)
            {
                p.BeginPath(); p.MoveTo(a + dir * t); p.LineTo(a + dir * Mathf.Min(len, t + dash * 0.6f)); p.Stroke();
            }
        }

        static void DrawDotted(Painter2D p, Vector2 a, Vector2 b, bool wrap, Color c, float k)
        {
            void Dots(Vector2 x, Vector2 y)
            {
                float len = Vector2.Distance(x, y);
                p.fillColor = c;
                for (float t = 0; t < len; t += 7 * k) { p.BeginPath(); p.Arc(Vector2.Lerp(x, y, t / len), 0.9f * k, 0, 360); p.Fill(); }
            }
            if (wrap) { Dots(a, new Vector2(0, a.y - 6 * k)); Dots(b, new Vector2(W * k, b.y + 6 * k)); }
            else Dots(a, b);
        }

        static void DrawDashedCircle(Painter2D p, Vector2 c, float r, Color col, float width)
        {
            p.strokeColor = col; p.lineWidth = width;
            for (int a = 0; a < 360; a += 24) { p.BeginPath(); p.Arc(c, r, a, a + 12); p.Stroke(); }
        }

        static void DrawHatch(Painter2D p, Vector2 c, float r, float k)
        {
            p.strokeColor = Line2; p.lineWidth = 1.5f * k;
            for (float o = -r; o <= r; o += 5 * k)
            {
                // Çemberin içinde kalan 45° çizgi parçası
                var dir = new Vector2(0.7071f, -0.7071f);
                var n = new Vector2(0.7071f, 0.7071f);
                var center = c + n * o;
                float half = Mathf.Sqrt(Mathf.Max(0, r * r - o * o)) - 1.5f * k;
                if (half <= 0) continue;
                p.BeginPath(); p.MoveTo(center - dir * half); p.LineTo(center + dir * half); p.Stroke();
            }
        }

        static void DrawCheck(Painter2D p, Vector2 c, float k)
        {
            p.fillColor = Verify; p.BeginPath(); p.Arc(c, 6.5f * k, 0, 360); p.Fill();
            p.strokeColor = Bg; p.lineWidth = 1.8f * k;
            p.BeginPath(); p.MoveTo(c + new Vector2(-3, 0) * k); p.LineTo(c + new Vector2(-0.8f, 2.4f) * k); p.LineTo(c + new Vector2(3.2f, -2.4f) * k); p.Stroke();
        }

        static void DrawFlag(Painter2D p, Vector2 c, float k)
        {
            p.fillColor = Warn; p.BeginPath(); p.Arc(c, 6.5f * k, 0, 360); p.Fill();
            p.strokeColor = Bg; p.lineWidth = 1.8f * k;
            p.BeginPath(); p.MoveTo(c + new Vector2(0, -3.2f) * k); p.LineTo(c + new Vector2(0, 0.8f) * k); p.Stroke();
            p.fillColor = Bg; p.BeginPath(); p.Arc(c + new Vector2(0, 2.8f) * k, 1f * k, 0, 360); p.Fill();
        }

        public static Color Hex(string hex) => ColorUtility.TryParseHtmlString(hex, out var c) ? c : Color.magenta;
    }
}
