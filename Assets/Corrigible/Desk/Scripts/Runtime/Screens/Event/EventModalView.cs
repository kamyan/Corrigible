using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>
    /// Olay bildirimi (PROMPT §7.3, C3). Açıkken masa duraklatılmıştır, arka plan etkileşimsizdir, kapatma düğmesi yoktur:
    /// karar zorunludur. Katmanlar: karartma + spot (bölge düğümünde) → bildirim kâğıdı, seçenek kartları, YZ günlüğü,
    /// danışman notu → iplik, kalem halkası, raptiyeler → alt şerit. Tasarım kökü 1920×1080 ortalı (harita köküyle aynı uzay),
    /// karartma tüm ekranı kaplar. Açılış: karartma 150 ms, kâğıtlar 8 px düşerek 180 ms (PROMPT §8).
    /// Boşluk basılıyken alt şerit dışındaki her şey (karartma ve spot dahil) alfa 0: harita görünür; bırakınca geri.
    /// Kâğıtların yeri, genişliği ve eğimi sahnede (prefab); veri yalnız içeriği ve ona bağlı yüksekliği verir. Spot, halka ve
    /// ipliğin bölge ucu haritadaki düğümün o anki yerinden okunur (harita sürüklenmiş olabilir).
    /// </summary>
    public sealed class EventModalView : MonoBehaviour
    {
        [SerializeField] GameObject root;
        [Tooltip("Boşlukla gizlenen her şey (alt şerit hariç).")]
        [SerializeField] CanvasGroup peek;
        [SerializeField] CanvasGroup scrimGroup;
        [SerializeField] SpotlightGraphic scrim;
        [Tooltip("Kâğıtlar (bildirim, kartlar, günlük, danışman, iplik): 8 px düşerek belirir.")]
        [SerializeField] CanvasGroup papers;
        [SerializeField] RectTransform design;

        [Header("Bildirim ve seçenekler")]
        [SerializeField] EventMemoView memo;
        [SerializeField] EventOptionView[] options = Array.Empty<EventOptionView>();

        [Header("YZ günlüğü")]
        [SerializeField] RectTransform log;
        [SerializeField] TMP_Text logHeader, logRight, logFooter;
        [SerializeField] RectTransform logLines;
        [SerializeField] TMP_Text logLineTemplate;
        [SerializeField] RectTransform logFooterRoot;
        [SerializeField] float logHeaderHeight = 29, logPad = 10, logLineHeight = 19.5f, logGap = 7, logFooterHeight = 31.2f;

        [Header("Danışman notu")]
        [SerializeField] RectTransform advisor;
        [SerializeField] TMP_Text advisorTitle, advisorText;
        [SerializeField] DeskButton advisorLink;
        [SerializeField] float advisorTextTop = 40.2f, advisorLineHeight = 19.88f, advisorLinkGap = 10, advisorBottom = 14;

        [Header("İplik, halka, raptiyeler")]
        [SerializeField] UILineGraphic thread, threadShadow;
        [SerializeField] Image pinFrom, pinTo, markerRing;
        [SerializeField] Vector2 threadShadowOffset = new Vector2(1.5f, 2);
        [Tooltip("Spot, halka ve ipliğin bölge ucu bu haritadaki düğümün gerçek yerinden (boşsa tasarım koordinatı).")]
        [SerializeField] MapView map;
        [Tooltip("İpliğin bildirimdeki raptiyesi: bildirim kâğıdının sol-üst köşesinden uzaklık (px, y aşağı).")]
        [SerializeField] Vector2 memoPinOffset = new Vector2(690, 22);

        [Header("Alt şerit")]
        [Tooltip("Belge kipinde (alt şerit yoksa) gizlenir.")]
        [SerializeField] RectTransform footerRoot;
        [SerializeField] TMP_Text footerText;
        [SerializeField] KeyChipView[] footerKeys = Array.Empty<KeyChipView>();
        [SerializeField] TMP_Text[] footerHintTexts = Array.Empty<TMP_Text>();
        [SerializeField] DeskRow footerHints;

        EventVM vm;
        int selected = -1;
        DeskPool<TMP_Text> lines;
        DeskTween scrimIn, paperIn;
        float papersY;
        static readonly List<Vector2> pts = new List<Vector2>(2);

        public static EventModalView Instance { get; private set; }
        public bool IsOpen => root != null && root.activeSelf;
        public int SelectedIndex => selected;
        public int OptionCount => vm?.Options.Count ?? 0;
        /// <summary>Belge kipi (perde raporu, baskın emri/raporu): seçenek yok, düğmeler kâğıtta.</summary>
        public bool IsDoc => vm?.Doc != null;
        /// <summary>Belge düğmesi: VM komut dizisi.</summary>
        public event Action<string> DocInvoked;

        /// <summary>Belge kipinde Enter (birincil) / Esc (ikincil).</summary>
        public void PressDoc(bool primary) { if (IsDoc) memo.Doc?.Press(primary); }

        /// <summary>Seçenek imzalandı (dizin).</summary>
        public event Action<int> Signed;
        /// <summary>Danışman notundaki bağlantı (komut).</summary>
        public event Action<string> AdvisorLink;

        void Awake()
        {
            Instance = this;
            papersY = ((RectTransform)papers.transform).anchoredPosition.y;
            for (int i = 0; i < options.Length; i++)
            {
                options[i].Index = i;
                options[i].Hovered += Select;
                options[i].Signed += k => Signed?.Invoke(k);
            }
            if (memo.Doc != null) memo.Doc.Invoked += c => DocInvoked?.Invoke(c);
            advisorLink.onClick.AddListener(() =>
            {
                if (vm == null) return;
                AdvisorLink?.Invoke(!string.IsNullOrEmpty(vm.Advisor?.Command) ? vm.Advisor.Command : "FocusRegion:" + vm.Spotlight?.Region);
            });
            root.SetActive(false);
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        public void Bind(EventVM e)
        {
            bool wasOpen = IsOpen;
            vm = e;
            root.SetActive(e != null);
            if (e == null) { selected = -1; return; }
            if (!wasOpen)
            {
                selected = e.Options.FindIndex(o => o.State == "hover");
                SetPeek(false);
                var theme = DeskTheme.Active;
                scrimIn.Snap(0); scrimIn.To(1, theme != null ? theme.modalScrim : 0.15f);
                paperIn.Snap(0); paperIn.To(1, theme != null ? theme.modalDrop : 0.18f);
                Animate();
            }

            BindSpot(e);
            memo.Bind(e.Memo, e.Doc);
            for (int i = 0; i < options.Length; i++)
            {
                bool on = i < e.Options.Count;
                DeskUi.SetActive(options[i], on);
                if (on) options[i].Bind(e.Options[i], i == selected);
            }
            BindLog(e.AiLog);
            BindAdvisor(e.Advisor);
            BindThread(e);
            HideUnderPapers(e);
            BindFooter(e.Footer);
        }

        // ------------------------------------------------------------------ seçim ve imza (1/2/3, Enter, üstüne gelme)
        public void Select(int index)
        {
            if (vm == null || index < 0 || index >= vm.Options.Count) return;
            selected = index;
            for (int i = 0; i < options.Length && i < vm.Options.Count; i++) options[i].SetSelected(i == selected);
        }

        public void SignSelected()
        {
            if (vm != null && selected >= 0 && selected < vm.Options.Count) Signed?.Invoke(selected);
        }

        /// <summary>Boşluk basılı: haritaya bak (alt şerit kalır; arka plan yine etkileşimsiz).</summary>
        public void SetPeek(bool on)
        {
            float a = on ? 0 : 1;
            if (!Mathf.Approximately(peek.alpha, a)) peek.alpha = a;
        }

        // ------------------------------------------------------------------ parçalar
        Vector2 Design(float x, float y) => new Vector2(x - design.rect.width / 2, design.rect.height / 2 - y);

        void BindSpot(EventVM e)
        {
            var s = e.Spotlight;
            if (s?.Center == null)
            {
                // Bölgeye bağlı olmayan olay (oyunda küresel kartlar): delik yok, masa aynı karartmayla örtülür
                var c0 = DeskTheme.Active.Get("modal.scrim");
                scrim.color = new Color(c0.r, c0.g, c0.b, DeskMath.LinearOverlayAlpha(c0, s?.ScrimAlpha ?? 0.78f));
                scrim.SetSpot(Vector2.zero, 0, 0, Color.clear, false);
                DeskUi.SetActive(markerRing, false);
                return;
            }
            Vector2 local = scrim.rectTransform.InverseTransformPoint(MapWorld(s.Center));
            // CSS: delik çevresinde .38 halka .78 karartmanın üstünde (≈ .86); Linear'da eşdeğer alfalar
            var c = DeskTheme.Active.Get("modal.scrim");
            float outer = DeskMath.LinearOverlayAlpha(c, s.ScrimAlpha);
            float ring = DeskMath.LinearOverlayAlpha(c, 1 - (1 - 0.38f) * (1 - s.ScrimAlpha));
            scrim.color = new Color(c.r, c.g, c.b, outer);
            // Harita sahnede ölçeklenmişse delik düğümle birlikte küçülür/büyür
            float k = map != null ? map.WorldScale / Mathf.Max(1e-5f, scrim.rectTransform.lossyScale.x) : 1;
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

        /// <summary>Halka kâğıt katmanında ama yeri haritadaki düğüm: kâğıtlar düşerken de düğümde kalır.</summary>
        void PlaceRing(EventRingVM m)
        {
            if (m?.Center == null) return;
            var rr = markerRing.rectTransform;
            rr.anchorMin = rr.anchorMax = new Vector2(0, 1); rr.pivot = new Vector2(0.5f, 0.5f);
            var tl = TopLeft((RectTransform)rr.parent, MapWorld(m.Center));
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

        /// <summary>Bildirim ucu kâğıdın raptiyesi (kâğıt sahnede nerede duruyorsa), bölge ucu haritadaki düğüm.</summary>
        void PlaceThread(EventStringVM s)
        {
            if (s?.From == null || s.To == null) return;
            Vector3 from = MemoPinWorld(s.From), to = MapWorld(s.To);
            Line(thread, from, to, Vector2.zero);
            Line(threadShadow, from, to, threadShadowOffset);
            Pin(pinFrom, from); Pin(pinTo, to);
        }

        /// <summary>
        /// Bölge kâğıtların (bildirim, seçenekler, günlük, danışman notu) altında kalıyorsa halka ve iplik kâğıdın üstüne binmesin;
        /// spot yine çizilir. Kâğıtların ve haritanın sahnedeki gerçek yerine bakılır (ikisi de taşınmış olabilir).
        /// </summary>
        void HideUnderPapers(EventVM e)
        {
            var c = e.Spotlight?.Center;
            if (c == null) return;
            var world = MapWorld(c);
            bool under = Covers(memo != null ? memo.Paper : null, world) || Covers(log, world) || Covers(advisor, world);
            foreach (var o in options) under |= o != null && Covers((RectTransform)o.transform, world);
            if (!under) return;
            DeskUi.SetActive(markerRing, false);
            DeskUi.SetActive(thread, false); DeskUi.SetActive(threadShadow, false);
            DeskUi.SetActive(pinFrom, false); DeskUi.SetActive(pinTo, false);
        }

        static bool Covers(RectTransform r, Vector3 world) =>
            r != null && r.gameObject.activeInHierarchy && r.rect.Contains(r.InverseTransformPoint(world));

        /// <summary>Harita koordinatının ekrandaki yeri: harita bağlıysa düğümlerin gerçek yeri, değilse tasarım kökü.</summary>
        Vector3 MapWorld(float[] p) => map != null ? map.MapToWorld(p[0], p[1]) : design.TransformPoint(Design(p[0], p[1]));

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

        void BindLog(EventLogVM l)
        {
            DeskUi.SetActive(log, l != null);
            if (l == null) return;
            var T = DeskLoc.Instance;
            DeskUi.SetText(logHeader, T.T(l.Header));
            DeskUi.SetText(logRight, l.Right);
            DeskUi.SetText(logFooter, T.T(l.Footer));
            lines ??= new DeskPool<TMP_Text>(logLineTemplate, logLines);
            float y = logPad;
            float w = logLines.rect.width;
            for (int i = 0; i < l.Lines.Count; i++)
            {
                var t = lines.Get(i);
                DeskUi.SetText(t, T.T(l.Lines[i]));
                var r = t.rectTransform;
                r.anchorMin = r.anchorMax = new Vector2(0, 1); r.pivot = new Vector2(0, 1);
                r.sizeDelta = new Vector2(w, logLineHeight);
                t.ForceMeshUpdate();
                float h = Mathf.Max(1, t.textInfo.lineCount) * logLineHeight;
                r.sizeDelta = new Vector2(w, h);
                r.anchoredPosition = new Vector2(0, -y);
                y += h + (i < l.Lines.Count - 1 ? logGap : 0);
            }
            lines.Trim(l.Lines.Count);
            y += logPad;
            logLines.sizeDelta = new Vector2(logLines.sizeDelta.x, y);
            float total = logHeaderHeight + y + logFooterHeight + 1;
            logFooterRoot.anchoredPosition = new Vector2(logFooterRoot.anchoredPosition.x, -(logHeaderHeight + y));
            DeskUi.SetHeightKeepTop(log, total);
        }

        void BindAdvisor(EventAdvisorVM a)
        {
            DeskUi.SetActive(advisor, a != null);
            if (a == null) return;
            var T = DeskLoc.Instance;
            DeskUi.SetText(advisorTitle, T.T(a.Title));
            DeskUi.SetText(advisorText, T.T(a.Text));
            advisorText.ForceMeshUpdate();
            float th = Mathf.Max(1, advisorText.textInfo.lineCount) * advisorLineHeight;
            var tr = advisorText.rectTransform;
            tr.sizeDelta = new Vector2(tr.sizeDelta.x, th);
            DeskUi.SetActive(advisorLink, !string.IsNullOrEmpty(a.Link));
            float y = advisorTextTop + th;
            if (!string.IsNullOrEmpty(a.Link))
            {
                advisorLink.SetLabel(a.Link);
                var lr = (RectTransform)advisorLink.transform;
                lr.anchoredPosition = new Vector2(lr.anchoredPosition.x, -(y + advisorLinkGap));
                y += advisorLinkGap + lr.sizeDelta.y;
            }
            DeskUi.SetHeightKeepTop(advisor, y + advisorBottom);
        }

        void BindFooter(EventFooterVM f)
        {
            if (footerRoot != null) DeskUi.SetActive(footerRoot, f != null);
            if (f == null) return;
            var T = DeskLoc.Instance;
            DeskUi.SetText(footerText, T.T(f.Text));
            int k = 0;
            for (int h = 0; h < f.Hints.Count && h < footerHintTexts.Length; h++)
            {
                foreach (var key in f.Hints[h].Keys)
                    if (k < footerKeys.Length) { DeskUi.SetActive(footerKeys[k], true); footerKeys[k++].Set(key == "Boşluk" ? "Space" : key); }
                bool lastHint = h == f.Hints.Count - 1;
                DeskUi.SetText(footerHintTexts[h], T.T(f.Hints[h].Text) + (lastHint ? "" : " ·"));
            }
            // Seçenek sayısından az tuş: artan fişler gizlenir (oyunda iki seçenekli kartlar)
            for (; k < footerKeys.Length; k++) DeskUi.SetActive(footerKeys[k], false);
            footerHints.Layout();
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
    }
}
