using System.Collections.Generic;
using Corrigible.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Corrigible.Game.UI
{
    /// <summary>
    /// Oyun sonu: gerçek ve oyuncuya gösterilen ortalama sızma, gün gün. Sis, yalanlar ve illüzyon aynı resimde.
    /// Eksen yazıları AxisLabel prefab'ından üretilir; üzerine gelince çıkan bilgi "hover" yazısında gösterilir.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class EndChart : MaskableGraphic, IPointerMoveHandler, IPointerExitHandler
    {
        const float L = 44, R = 14, T = 30, B = 30;

        [SerializeField] UiTheme theme;
        [SerializeField] RectTransform labels;
        [SerializeField] TMP_Text axisLabelPrefab;
        [SerializeField] TMP_Text hover;

        List<HistPoint> hist = new List<HistPoint>();
        int awareDay, hoverDay = -1;
        readonly List<TMP_Text> pool = new List<TMP_Text>();
        int used;

        static UiTheme fallback;
        UiTheme Th => theme != null ? theme : fallback != null ? fallback : (fallback = ScriptableObject.CreateInstance<UiTheme>());

        public void SetData(List<HistPoint> points, int aware)
        {
            hist = points ?? new List<HistPoint>();
            awareDay = aware;
            hoverDay = -1;
            Canvas.ForceUpdateCanvases();
            Relabel();
            SetVerticesDirty();
        }

        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            if (Application.isPlaying && hist.Count > 1) Relabel();
        }

        Rect Area => rectTransform.rect;
        int LastDay => hist.Count > 0 ? hist[hist.Count - 1].Day : 1;
        // Koordinatlar sol üstten (prototip ile aynı); yerel uzaya P ile çevrilir
        float X(float d) => L + (d - 1) / Mathf.Max(1, LastDay - 1) * (Area.width - L - R);
        float Y(double v) => T + (1 - (float)v / 100f) * (Area.height - T - B);
        Vector2 P(float x, float y) => new Vector2(Area.xMin + x, Area.yMax - y);

        void Relabel()
        {
            used = 0;
            hover.text = "";
            if (hist.Count >= 2 && Area.width > 0)
            {
                float w = Area.width, h = Area.height;
                foreach (var v in new[] { 0, 50, 100 }) Label(Loc.F("%{0}", v), L - 34, Y(v) - 8);
                for (int d = 50; X(d) < w - R - 30; d += 50) Label(Loc.F("{0}. gün", d), X(d) - 18, h - B + 4);
                if (awareDay > 0) Label(Loc.T("fark edildi"), X(awareDay) + 4, h - B - 18);
                int start = -1;
                for (int i = 0; i < hist.Count; i++)
                {
                    if (hist[i].Illusion && start < 0) start = hist[i].Day;
                    if (start >= 0 && (!hist[i].Illusion || i == hist.Count - 1))
                    {
                        Label(Loc.T("illüzyon"), Mathf.Min(X(start) + 4, w - R - 60), T + 2);
                        start = -1;
                    }
                }
            }
            for (int i = used; i < pool.Count; i++) pool[i].gameObject.SetActive(false);
        }

        void Label(string text, float x, float y)
        {
            if (used == pool.Count)
            {
                var t0 = Instantiate(axisLabelPrefab, labels);
                t0.name = "axis-" + pool.Count;
                pool.Add(t0);
            }
            var t = pool[used++];
            t.gameObject.SetActive(true);
            t.text = text;
            t.rectTransform.anchoredPosition = new Vector2(x, -y);
        }

        public void OnPointerMove(PointerEventData e)
        {
            if (hist.Count < 2 || hover == null) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, e.position, e.pressEventCamera, out var local);
            float w = Area.width, x = local.x - Area.xMin;
            int d = Mathf.Clamp(Mathf.RoundToInt(1 + (x - L) / (w - L - R) * (LastDay - 1)), 1, LastDay);
            var h = hist.Find(p => p.Day == d);
            if (h == null || d == hoverDay) return;
            hoverDay = d;
            hover.text = Loc.F("Gün {0} · gerçek %{1} · gösterilen %{2}{3}", d, Js.Round(h.Real), Js.Round(h.Shown), (h.Illusion ? Loc.T(" · illüzyon") : ""));
            bool right = X(d) > w / 2;
            var rt = hover.rectTransform;
            rt.pivot = new Vector2(right ? 1 : 0, 1);
            rt.anchoredPosition = new Vector2(right ? X(d) - 6 : X(d) + 6, -(T - 2));
            hover.alignment = right ? TextAlignmentOptions.TopRight : TextAlignmentOptions.TopLeft;
            SetVerticesDirty();
        }

        public void OnPointerExit(PointerEventData e)
        {
            hoverDay = -1;
            if (hover != null) hover.text = "";
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            float w = Area.width, h = Area.height;
            var grid = Th.line;

            // Izgara her zaman çizilir (edit modda da görünür)
            foreach (var v in new[] { 0, 50, 100 })
                UiMesh.Line(vh, P(L, Y(v)), P(w - R, Y(v)), 1f, v == 0 ? Th.line2 : grid);
            if (hist.Count < 2) return;

            // İllüzyon bantları
            var band = Palette.Alpha(Th.threat, 0.1f);
            int start = -1;
            for (int i = 0; i < hist.Count; i++)
            {
                if (hist[i].Illusion && start < 0) start = hist[i].Day;
                if (start >= 0 && (!hist[i].Illusion || i == hist.Count - 1))
                {
                    int end = hist[i].Illusion ? hist[i].Day : hist[i].Day - 1;
                    float x0 = X(start), x1 = Mathf.Max(x0 + 2, X(end));
                    var a = P(x0, h - B); var b = P(x1, T);
                    UiMesh.Rect(vh, Rect.MinMaxRect(a.x, a.y, b.x, b.y), band);
                    start = -1;
                }
            }

            if (awareDay > 0) UiMesh.DashedLine(vh, P(X(awareDay), T), P(X(awareDay), h - B), 1f, Th.line2, 2, 4);

            // Gösterilen (kesik) ve gerçek (düz) çizgiler
            for (int i = 1; i < hist.Count; i += 2)
                UiMesh.Line(vh, P(X(hist[i - 1].Day), Y(hist[i - 1].Shown)), P(X(hist[i].Day), Y(hist[i].Shown)), 2f, Th.verify);
            var real = new Vector2[hist.Count];
            for (int i = 0; i < hist.Count; i++) real[i] = P(X(hist[i].Day), Y(hist[i].Real));
            UiMesh.Polyline(vh, real, 2f, Th.threat);

            if (hoverDay > 0) UiMesh.Line(vh, P(X(hoverDay), T), P(X(hoverDay), h - B), 1f, Th.muted);
        }
    }
}
