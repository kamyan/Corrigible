using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>
    /// Baskın raporu (olay modalının belge kipi). Açıkken oda etkileşimsizdir; kâğıdın düğmesi masaya döndürür. Katmanlar:
    /// karartma + spot (bölgede) → rapor kâğıdı → iplik, kalem halkası, raptiyeler. Tasarım kökü 1920×1080 ortalı, karartma
    /// tüm ekranı kaplar. Açılış: karartma 150 ms, kâğıt 8 px düşerek 180 ms. Boşluk basılıyken her şey alfa 0: harita görünür.
    /// Spot, halka ve ipliğin bölge ucu masadaki kâğıt haritada bölgenin o anki ekran yerinden okunur (kamera hareket edebilir).
    /// </summary>
    [DefaultExecutionOrder(110)]
    public sealed class EventModalView : MonoBehaviour
    {
        [SerializeField] GameObject root;
        [Tooltip("Boşlukla gizlenen her şey.")]
        [SerializeField] CanvasGroup peek;
        [SerializeField] CanvasGroup scrimGroup;
        [SerializeField] SpotlightGraphic scrim;
        [Tooltip("Kâğıt ve iplik: 8 px düşerek belirir.")]
        [SerializeField] CanvasGroup papers;
        [SerializeField] RectTransform design;
        [SerializeField] EventMemoView memo;

        [Header("İplik, halka, raptiyeler")]
        [SerializeField] UILineGraphic thread, threadShadow;
        [SerializeField] Image pinFrom, pinTo, markerRing;
        [SerializeField] Vector2 threadShadowOffset = new Vector2(1.5f, 2);
        [Tooltip("Spot, halka ve ipliğin bölge ucu bu haritadaki bölgenin gerçek yerinden.")]
        [SerializeField] SadeMapView sadeMap;
        [Tooltip("İpliğin kâğıttaki raptiyesi: kâğıdın sol-üst köşesinden uzaklık (px, y aşağı).")]
        [SerializeField] Vector2 memoPinOffset = new Vector2(690, 22);

        EventVM vm;
        DeskTween scrimIn, paperIn;
        float papersY;
        static readonly List<Vector2> pts = new List<Vector2>(2);

        public static EventModalView Instance { get; private set; }
        public bool IsOpen => root != null && root.activeSelf;
        /// <summary>Belge düğmesi: VM komut dizisi.</summary>
        public event Action<string> DocInvoked;

        /// <summary>Enter (birincil) / Esc (ikincil).</summary>
        public void PressDoc(bool primary) { if (vm?.Doc != null) memo.Doc?.Press(primary); }

        void Awake()
        {
            Instance = this;
            papersY = ((RectTransform)papers.transform).anchoredPosition.y;
            if (memo.Doc != null) memo.Doc.Invoked += c => DocInvoked?.Invoke(c);
            root.SetActive(false);
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        public void Bind(EventVM e)
        {
            bool wasOpen = IsOpen;
            vm = e;
            root.SetActive(e != null);
            if (e == null) return;
            if (!wasOpen)
            {
                SetPeek(false);
                var theme = DeskTheme.Active;
                scrimIn.Snap(0); scrimIn.To(1, theme != null ? theme.modalScrim : 0.15f);
                paperIn.Snap(0); paperIn.To(1, theme != null ? theme.modalDrop : 0.18f);
                Animate();
            }
            BindSpot(e);
            memo.Bind(e.Memo, e.Doc);
            BindThread(e);
            HideUnderPaper(e);
        }

        /// <summary>Boşluk basılı: haritaya bak (arka plan yine etkileşimsiz).</summary>
        public void SetPeek(bool on)
        {
            float a = on ? 0 : 1;
            if (!Mathf.Approximately(peek.alpha, a)) peek.alpha = a;
        }

        // ------------------------------------------------------------------ parçalar
        void BindSpot(EventVM e)
        {
            var s = e.Spotlight;
            var c = DeskTheme.Active.Get("modal.scrim");
            if (s?.Center == null)
            {
                scrim.color = new Color(c.r, c.g, c.b, DeskMath.LinearOverlayAlpha(c, s?.ScrimAlpha ?? 0.78f));
                scrim.SetSpot(Vector2.zero, 0, 0, Color.clear, false);
                DeskUi.SetActive(markerRing, false);
                return;
            }
            Vector2 local = scrim.rectTransform.InverseTransformPoint(SpotWorld(s.Region, s.Center));
            // CSS: delik çevresinde .38 halka .78 karartmanın üstünde (≈ .86); Linear'da eşdeğer alfalar
            float outer = DeskMath.LinearOverlayAlpha(c, s.ScrimAlpha);
            float ring = DeskMath.LinearOverlayAlpha(c, 1 - (1 - 0.38f) * (1 - s.ScrimAlpha));
            scrim.color = new Color(c.r, c.g, c.b, outer);
            // Harita odada ölçeklenmiş: delik bölgeyle birlikte küçülür/büyür
            float k = DeskCanvasProjection.IsWorldSpace(sadeMap)
                ? DeskCanvasProjection.ScaleAt(sadeMap, RawSpotWorld(s.Region, s.Center), sadeMap.WorldScale, scrim.rectTransform)
                : sadeMap != null ? sadeMap.WorldScale / Mathf.Max(1e-5f, scrim.rectTransform.lossyScale.x) : 1;
            scrim.SetSpot(local, s.HoleDiameter / 2 * k, s.Ring * k, new Color(c.r, c.g, c.b, ring));

            var m = e.MarkerRing;
            DeskUi.SetActive(markerRing, m?.Center != null);
            if (m?.Center != null)
            {
                ColorUtility.TryParseHtmlString(m.Color ?? "#B8241A", out var rc);
                rc.a = m.Alpha;
                markerRing.color = rc;
                PlaceRing(m);
            }
        }

        /// <summary>Halka kâğıt katmanında ama yeri haritadaki bölge: kâğıt düşerken de bölgede kalır.</summary>
        void PlaceRing(EventRingVM m)
        {
            if (m?.Center == null) return;
            var rr = markerRing.rectTransform;
            rr.anchorMin = rr.anchorMax = new Vector2(0, 1); rr.pivot = new Vector2(0.5f, 0.5f);
            var tl = TopLeft((RectTransform)rr.parent, SpotWorld(vm?.Spotlight?.Region, m.Center));
            rr.anchoredPosition = new Vector2(tl.x, -tl.y);
        }

        void BindThread(EventVM e)
        {
            var s = e.String;
            bool on = s?.From != null && s.To != null;
            DeskUi.SetActive(thread, on); DeskUi.SetActive(threadShadow, on);
            DeskUi.SetActive(pinFrom, on); DeskUi.SetActive(pinTo, on);
            if (!on) return;
            ColorUtility.TryParseHtmlString(s.Color ?? "#B8241A", out var col);
            thread.color = col; thread.Thickness = s.Width;
            threadShadow.Thickness = s.Width;
            threadShadow.color = new Color(0, 0, 0, s.ShadowAlpha);
            PlaceThread(s);
        }

        /// <summary>Kâğıt ucu raptiye (kâğıt sahnede nerede duruyorsa), bölge ucu haritadaki bölge.</summary>
        void PlaceThread(EventStringVM s)
        {
            if (s?.From == null || s.To == null) return;
            Vector3 from = MemoPinWorld(s.From), to = SpotWorld(vm?.Spotlight?.Region, s.To);
            Line(thread, from, to, Vector2.zero);
            Line(threadShadow, from, to, threadShadowOffset);
            Pin(pinFrom, from); Pin(pinTo, to);
        }

        /// <summary>Bölge kâğıdın altında kalıyorsa halka ve iplik kâğıdın üstüne binmesin; spot yine çizilir.</summary>
        void HideUnderPaper(EventVM e)
        {
            var c = e.Spotlight?.Center;
            if (c == null || !Covers(memo != null ? memo.Paper : null, SpotWorld(e.Spotlight.Region, c))) return;
            DeskUi.SetActive(markerRing, false);
            DeskUi.SetActive(thread, false); DeskUi.SetActive(threadShadow, false);
            DeskUi.SetActive(pinFrom, false); DeskUi.SetActive(pinTo, false);
        }

        static bool Covers(RectTransform r, Vector3 world) =>
            r != null && r.gameObject.activeInHierarchy && r.rect.Contains(r.InverseTransformPoint(world));

        Vector2 Design(float x, float y) => new Vector2(x - design.rect.width / 2, design.rect.height / 2 - y);
        Vector3 RawSpotWorld(string regionId, float[] fallback) =>
            sadeMap != null && !string.IsNullOrEmpty(regionId) ? sadeMap.RegionWorld(regionId)
            : sadeMap != null ? sadeMap.MapToWorld(fallback[0], fallback[1]) : design.TransformPoint(Design(fallback[0], fallback[1]));
        /// <summary>Odadaki harita noktası kâğıt düzlemine ekran üzerinden çevrilir.</summary>
        Vector3 SpotWorld(string regionId, float[] fallback) => DeskCanvasProjection.WorldPoint(sadeMap, RawSpotWorld(regionId, fallback), design);

        Vector3 MemoPinWorld(float[] fallback)
        {
            var paper = memo != null ? memo.Paper : null;
            if (paper == null) return design.TransformPoint(Design(fallback[0], fallback[1]));
            var r = paper.rect;
            return paper.TransformPoint(new Vector3(r.xMin + memoPinOffset.x, r.yMax - memoPinOffset.y, 0));
        }

        /// <summary>Dünya noktasının <paramref name="space"/> içindeki sol-üst kökenli konumu (y aşağı).</summary>
        static Vector2 TopLeft(RectTransform space, Vector3 world)
        {
            Vector2 l = space.InverseTransformPoint(world);
            var r = space.rect;
            return new Vector2(l.x - r.xMin, r.yMax - l.y);
        }

        static void Line(UILineGraphic g, Vector3 a, Vector3 b, Vector2 offset)
        {
            pts.Clear();
            pts.Add(TopLeft(g.rectTransform, a) + offset); pts.Add(TopLeft(g.rectTransform, b) + offset);
            g.Curve = LineCurve.Polyline;
            g.SetPointsTopLeft(pts, Vector2.zero);
        }

        static void Pin(Image img, Vector3 world)
        {
            var rt = img.rectTransform;
            var sp = img.sprite;
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            if (sp != null) rt.pivot = new Vector2(sp.pivot.x / sp.rect.width, sp.pivot.y / sp.rect.height);
            var p = TopLeft((RectTransform)rt.parent, world);
            rt.anchoredPosition = new Vector2(p.x, -p.y);
        }

        // ------------------------------------------------------------------ açılış animasyonu
        /// <summary>Açılış geçişini hemen bitirir (yakalama ve testler için).</summary>
        public void CompleteAnimation()
        {
            scrimIn.Snap(1); paperIn.Snap(1);
            if (IsOpen) Animate();
        }

        void Animate()
        {
            scrimGroup.alpha = scrimIn.Value;
            papers.alpha = paperIn.Value;
            var pr = (RectTransform)papers.transform;
            pr.anchoredPosition = new Vector2(pr.anchoredPosition.x, papersY + 8 * (1 - paperIn.Value));
            // İplik ve halka kâğıt katmanında; haritadaki uçları kâğıtla birlikte düşmesin
            if (vm != null) { PlaceRing(vm.MarkerRing); PlaceThread(vm.String); }
        }

        void Update()
        {
            if (!IsOpen) return;
            bool a = scrimIn.Tick(Time.unscaledDeltaTime);
            if (paperIn.Tick(Time.unscaledDeltaTime) || a) Animate();
        }

        void LateUpdate()
        {
            if (!IsOpen || vm == null || !DeskCanvasProjection.IsWorldSpace(sadeMap)) return;
            // Kamera hareketi simülasyon güncellemesini beklemeden spotu ve ipliğin harita ucunu taşır
            BindSpot(vm);
            BindThread(vm);
            HideUnderPaper(vm);
        }
    }
}
