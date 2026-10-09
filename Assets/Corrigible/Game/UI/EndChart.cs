using System.Collections.Generic;
using Corrigible.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace Corrigible.Game.UI
{
    /// <summary>Oyun sonu: gerçek ve oyuncuya gösterilen ortalama sızma, gün gün. Sis, yalanlar ve illüzyon aynı resimde.</summary>
    public sealed class EndChart : VisualElement
    {
        const float L = 44, R = 14, T = 30, B = 30;
        static readonly Color Threat = MapView.Hex("#ff5b4d"), Verify = MapView.Hex("#5cc8d9"), Grid = MapView.Hex("#22343c"),
            Axis = MapView.Hex("#2f4650");

        readonly List<HistPoint> hist;
        readonly int awareDay;
        readonly Label hover;
        readonly List<Label> axisLabels = new List<Label>();
        int hoverDay = -1;

        public EndChart(List<HistPoint> hist, int awareDay)
        {
            this.hist = hist;
            this.awareDay = awareDay;
            AddToClassList("endchart");
            generateVisualContent += Draw;

            var legend = new VisualElement();
            legend.AddToClassList("row");
            legend.style.position = Position.Absolute; legend.style.left = L; legend.style.top = 2;
            legend.Add(Swatch(Threat, "Gerçek ortalama sızma"));
            legend.Add(Swatch(Verify, "Sana gösterilen (kesik)"));
            Add(legend);

            hover = new Label();
            hover.style.position = Position.Absolute; hover.style.fontSize = 11; hover.style.color = MapView.Hex("#d6e1e3");
            hover.pickingMode = PickingMode.Ignore;
            Add(hover);

            RegisterCallback<GeometryChangedEvent>(_ => Layout());
            RegisterCallback<PointerMoveEvent>(OnMove);
            RegisterCallback<PointerLeaveEvent>(_ => { hoverDay = -1; hover.text = ""; MarkDirtyRepaint(); });
        }

        static VisualElement Swatch(Color c, string text)
        {
            var row = new VisualElement(); row.AddToClassList("row"); row.style.marginRight = 18;
            var sw = new VisualElement(); sw.style.width = 18; sw.style.height = 2; sw.style.backgroundColor = c; sw.style.marginRight = 6;
            var l = new Label(text); l.style.fontSize = 11; l.style.color = MapView.Hex("#7f949b");
            row.Add(sw); row.Add(l);
            return row;
        }

        int LastDay => hist.Count > 0 ? hist[hist.Count - 1].Day : 1;
        float X(float d) => L + (d - 1) / Mathf.Max(1, LastDay - 1) * (resolvedStyle.width - L - R);
        float Y(double v) => T + (1 - (float)v / 100f) * (resolvedStyle.height - T - B);

        void Layout()
        {
            foreach (var l in axisLabels) l.RemoveFromHierarchy();
            axisLabels.Clear();
            if (hist.Count < 2 || resolvedStyle.width <= 0) return;
            foreach (var v in new[] { 0, 50, 100 }) AxisLabel($"%{v}", L - 34, Y(v) - 8);
            for (int d = 50; X(d) < resolvedStyle.width - R - 30; d += 50) AxisLabel($"{d}. gün", X(d) - 18, resolvedStyle.height - B + 4);
            if (awareDay > 0) AxisLabel("fark edildi", X(awareDay) + 4, resolvedStyle.height - B - 18);
            int start = -1;
            for (int i = 0; i < hist.Count; i++)
            {
                if (hist[i].Illusion && start < 0) start = hist[i].Day;
                if (start >= 0 && (!hist[i].Illusion || i == hist.Count - 1))
                {
                    AxisLabel("illüzyon", Mathf.Min(X(start) + 4, resolvedStyle.width - R - 60), T + 2);
                    start = -1;
                }
            }
            MarkDirtyRepaint();
        }

        void AxisLabel(string text, float x, float y)
        {
            var l = new Label(text);
            l.style.position = Position.Absolute; l.style.left = x; l.style.top = y; l.style.fontSize = 11;
            l.style.color = MapView.Hex("#7f949b");
            l.pickingMode = PickingMode.Ignore;
            Add(l); axisLabels.Add(l);
        }

        void OnMove(PointerMoveEvent e)
        {
            if (hist.Count < 2) return;
            float w = resolvedStyle.width;
            int d = Mathf.Clamp(Mathf.RoundToInt(1 + (e.localPosition.x - L) / (w - L - R) * (LastDay - 1)), 1, LastDay);
            var h = hist.Find(p => p.Day == d);
            if (h == null) return;
            hoverDay = d;
            hover.text = $"Gün {d} · gerçek %{Js.Round(h.Real)} · gösterilen %{Js.Round(h.Shown)}{(h.Illusion ? " · illüzyon" : "")}";
            bool right = X(d) > w / 2;
            hover.style.left = right ? new StyleLength(StyleKeyword.Auto) : new StyleLength(X(d) + 6);
            hover.style.right = right ? new StyleLength(w - X(d) + 6) : new StyleLength(StyleKeyword.Auto);
            hover.style.top = T - 2;
            MarkDirtyRepaint();
        }

        void Draw(MeshGenerationContext ctx)
        {
            if (hist.Count < 2) return;
            var p = ctx.painter2D;
            float w = resolvedStyle.width, h = resolvedStyle.height;

            // İllüzyon bantları
            p.fillColor = new Color(Threat.r, Threat.g, Threat.b, 0.1f);
            int start = -1;
            for (int i = 0; i < hist.Count; i++)
            {
                if (hist[i].Illusion && start < 0) start = hist[i].Day;
                if (start >= 0 && (!hist[i].Illusion || i == hist.Count - 1))
                {
                    int end = hist[i].Illusion ? hist[i].Day : hist[i].Day - 1;
                    float x0 = X(start), x1 = Mathf.Max(x0 + 2, X(end));
                    p.BeginPath(); p.MoveTo(new Vector2(x0, T)); p.LineTo(new Vector2(x1, T)); p.LineTo(new Vector2(x1, h - B)); p.LineTo(new Vector2(x0, h - B)); p.ClosePath(); p.Fill();
                    start = -1;
                }
            }

            // Izgara
            p.lineWidth = 1;
            foreach (var v in new[] { 0, 50, 100 })
            {
                p.strokeColor = v == 0 ? Axis : Grid;
                p.BeginPath(); p.MoveTo(new Vector2(L, Y(v))); p.LineTo(new Vector2(w - R, Y(v))); p.Stroke();
            }
            if (awareDay > 0)
            {
                p.strokeColor = Axis;
                for (float y = T; y < h - B; y += 6) { p.BeginPath(); p.MoveTo(new Vector2(X(awareDay), y)); p.LineTo(new Vector2(X(awareDay), Mathf.Min(h - B, y + 2))); p.Stroke(); }
            }

            // Gösterilen (kesik) ve gerçek (düz) çizgiler
            p.strokeColor = Verify; p.lineWidth = 2;
            for (int i = 1; i < hist.Count; i += 2)
            {
                p.BeginPath(); p.MoveTo(new Vector2(X(hist[i - 1].Day), Y(hist[i - 1].Shown))); p.LineTo(new Vector2(X(hist[i].Day), Y(hist[i].Shown))); p.Stroke();
            }
            p.strokeColor = Threat; p.lineWidth = 2;
            p.BeginPath();
            p.MoveTo(new Vector2(X(hist[0].Day), Y(hist[0].Real)));
            for (int i = 1; i < hist.Count; i++) p.LineTo(new Vector2(X(hist[i].Day), Y(hist[i].Real)));
            p.Stroke();

            if (hoverDay > 0)
            {
                p.strokeColor = MapView.Hex("#7f949b"); p.lineWidth = 1;
                p.BeginPath(); p.MoveTo(new Vector2(X(hoverDay), T)); p.LineTo(new Vector2(X(hoverDay), h - B)); p.Stroke();
            }
        }
    }
}
