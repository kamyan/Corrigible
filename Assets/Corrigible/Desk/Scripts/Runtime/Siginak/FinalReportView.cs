using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>
    /// Harekât sonu raporu (Revizyon 2 · S8a/S8b; Overlay, 1620 × 952, sol 150 · üst 64): son haritanın üstünde, harita %62
    /// karartılır. Başlık satırı ve 3 px çift çizgi; sonuç damgası (CP 700 88, 8 px çift kenar, −4°; KAZANILDI yeşil,
    /// KAYBEDİLDİ kırmızı) ve kayıpta neden damgası (CP 700 28, 3 px, +3°); sonucu söyleyen cümle ve altında ikinci satır;
    /// dört gösterge; işgal şeridi (<see cref="OccupationChart"/>) ve üstünde numaralı kırılma anları; altta aynı numarayla
    /// satırlar; YENİDEN BAŞLA (Enter). Açılış: harita 0,6 s'den sonra 0,5 s'de kararır, kâğıt aşağıdan 0,4 s'de gelir,
    /// damga 0,15 s (neden damgası 0,3 s sonra), göstergeler 0,12 s arayla (sayılar daktiloyla), çizgi 1,2 s'de soldan sağa
    /// çizilir, işaretler çizgi geldikçe 0,1 s'de belirir, satırlar 0,08 s arayla. İşaretin ya da satırın üzerine gelince
    /// ikisi birlikte seçilir (satır sarı, işaret 1,2 kat). Kamerayı ve sesi bağlayıcı yürütür (olaylar).
    /// </summary>
    public sealed class FinalReportView : MonoBehaviour
    {
        [Serializable]
        public sealed class IndicatorSlot
        {
            public CanvasGroup group;
            public TMP_Text label, value, sub;
        }

        [Serializable]
        public sealed class MarkerSlot
        {
            public RectTransform root;
            public Image ring, fill;
            public TMP_Text number;
            public UiHoverTarget hover;
        }

        [Serializable]
        public sealed class RowSlot
        {
            public CanvasGroup group;
            public Image highlight, ring, fill;
            public TMP_Text number, code, text;
            public UiHoverTarget hover;
        }

        [SerializeField] GameObject root;
        [SerializeField] Image dim;
        [Tooltip("Aşağıdan gelen kâğıt.")]
        [SerializeField] RectTransform sheet;
        [SerializeField] CanvasGroup sheetGroup;
        [SerializeField] TMP_Text head, number;
        [Header("Damgalar")]
        [SerializeField] RectTransform stamp;
        [SerializeField] TMP_Text stampText;
        [SerializeField] Image stampOuter, stampInner;
        [SerializeField] RectTransform reason;
        [SerializeField] TMP_Text reasonText;
        [SerializeField] Image reasonFrame;
        [Tooltip("Neden damgasının yeri sonuç damgasının (dönük) sağ ucuna göre: damga dile göre daralınca (EN “LOST”) birlikte kayar.")]
        [SerializeField] Vector2 reasonOffset = new Vector2(65, -113);
        [SerializeField] TMP_Text headline, sub;
        [Header("Göstergeler")]
        [SerializeField] IndicatorSlot[] indicators = Array.Empty<IndicatorSlot>();
        [Header("İşgal şeridi")]
        [SerializeField] TMP_Text chartTitle;
        [SerializeField] TMP_Text chartRange, occupiedLabel, retakenLabel;
        [SerializeField] OccupationChart chart;
        [Tooltip("x ekseni etiketleri (1, 5, 10, …, N); fazlası gizlenir.")]
        [SerializeField] TMP_Text[] xLabels = Array.Empty<TMP_Text>();
        [Tooltip("y ekseni etiketleri, yukarıdan aşağıya (100, 50, 0): biçim dile göre, DeskFormat.Percent (TR %100, EN 100%).")]
        [SerializeField] TMP_Text[] yLabels = Array.Empty<TMP_Text>();
        [SerializeField] MarkerSlot[] markers = Array.Empty<MarkerSlot>();
        [Header("Kırılma anları")]
        [SerializeField] TMP_Text momentsTitle;
        [SerializeField] RowSlot[] rows = Array.Empty<RowSlot>();
        [Header("Düğme")]
        [SerializeField] Button action;
        [SerializeField] UiHoverTarget actionHover;
        [SerializeField] RectTransform actionFace;
        [SerializeField] Image actionShadow;
        [SerializeField] TMP_Text actionLabel;
        [Header("Süreler (S8 › hareket)")]
        [SerializeField, Min(0)] float dimDelay = 0.6f;
        [SerializeField, Min(0.01f)] float dimSeconds = 0.5f, paperSeconds = 0.4f, stampSeconds = 0.15f;
        [SerializeField, Min(0)] float reasonDelay = 0.3f, indicatorStagger = 0.12f, chartDelay = 0.4f;
        [SerializeField, Min(0.01f)] float chartSeconds = 1.2f, markerSeconds = 0.1f;
        [SerializeField, Min(0)] float rowStagger = 0.08f, typeCharSeconds = 0.04f;

        FinalReportData data;
        float openedAt;
        bool stamped;
        Vector2 sheetRest;
        bool rested;

        static SiginakTheme T => SiginakTheme.Active;
        public static FinalReportView Instance { get; private set; }
        public bool IsOpen => root != null && root.activeSelf;

        /// <summary>Rapor açıldı (bağlayıcı kamerayı harita pozuna alır) ya da kapandı.</summary>
        public event Action Opened, Closed;
        /// <summary>Sonuç damgası basıldı (kazanıldı mı): bağlayıcı sesi çalar.</summary>
        public event Action<bool> Stamped;
        public event Action RestartClicked;

        float StampAt => dimDelay + dimSeconds + paperSeconds;
        float ChartAt => StampAt + chartDelay;

        void Awake()
        {
            Instance = this;
            Rest();
            action.onClick.AddListener(PressRestart);
            root.SetActive(false);
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        void Rest()
        {
            if (rested) return;
            rested = true;
            sheetRest = sheet.anchoredPosition;
        }

        /// <summary>YENİDEN BAŞLA (Enter ya da düğme).</summary>
        public void PressRestart() { if (IsOpen) RestartClicked?.Invoke(); }

        /// <summary>Raporu açar (null kapatır); açıkken yeniden bağlamak (dil) diziyi baştan oynatmaz.</summary>
        public void Bind(FinalReportData d)
        {
            Rest();
            data = d;
            if (d == null)
            {
                if (!IsOpen) return;
                root.SetActive(false);
                Closed?.Invoke();
                return;
            }
            bool fresh = !IsOpen;
            root.SetActive(true);
            Write(d);
            if (!fresh) return;
            openedAt = Application.isPlaying ? Time.unscaledTime : float.NegativeInfinity;
            stamped = false;
            Opened?.Invoke();
            Animate();
        }

        void Write(FinalReportData d)
        {
            head.text = d.Head;
            number.text = d.Number;
            var stampInk = T.ColorOf(d.Won ? "report.positive" : "report.counterAttackLine");
            stampText.text = d.Stamp;
            stampText.color = stampOuter.color = stampInner.color = stampInk;
            reason.gameObject.SetActive(!string.IsNullOrEmpty(d.Reason));
            reasonText.text = d.Reason ?? "";
            reasonText.color = reasonFrame.color = stampInk;
            LayoutRebuilder.ForceRebuildLayoutImmediate(stamp);
            float turn = stamp.localEulerAngles.z * Mathf.Deg2Rad, width = stamp.rect.width;
            reason.anchoredPosition = stamp.anchoredPosition + new Vector2(Mathf.Cos(turn), Mathf.Sin(turn)) * width + reasonOffset;
            headline.text = d.Headline;
            sub.text = d.Sub;
            // Kazanınca parola gibi (CP 700 16, aralıklı); kayıpta düz metin (LF 19)
            sub.font = DeskTheme.Active.Font(d.SubIsMotto ? DeskFont.CpBold : DeskFont.LfRegular);
            sub.fontSize = d.SubIsMotto ? 16 : 19;
            sub.characterSpacing = d.SubIsMotto ? 4 : 0;

            for (int i = 0; i < indicators.Length; i++)
            {
                bool on = i < d.Indicators.Count;
                indicators[i].group.gameObject.SetActive(on);
                if (!on) continue;
                var ind = d.Indicators[i];
                indicators[i].label.text = ind.Label;
                indicators[i].value.text = ind.Value;
                indicators[i].value.color = T.ColorOf(OperationReportView.ToneColor(ind.Tone));
                indicators[i].sub.text = ind.Sub;
            }

            chartTitle.text = d.ChartTitle;
            chartRange.text = d.ChartRange;
            occupiedLabel.text = d.OccupiedLabel;
            retakenLabel.text = d.RetakenLabel;
            chart.SetColors(T.ColorOf("report.chartLine"), T.ColorOf("report.chartOccupied"), T.ColorOf("report.chartRetaken"), T.ColorOf("report.summaryLine"));
            chart.SetSeries(d.Points, d.Shifts);
            Axis(d.Shifts);
            for (int i = 0; i < yLabels.Length; i++) yLabels[i].text = DeskFormat.Percent(100 - 50 * i);

            momentsTitle.text = d.MomentsTitle;
            var ink = T.ColorOf("ink");
            var red = T.ColorOf("report.counterAttackLine");
            var paper = T.ColorOf("report.summaryFill");
            for (int i = 0; i < markers.Length; i++)
            {
                bool on = i < d.Moments.Count;
                markers[i].root.gameObject.SetActive(on);
                rows[i].group.gameObject.SetActive(on);
                if (!on) continue;
                var m = d.Moments[i];
                // İşaretler grafiğin çocuğu (çapa ortada): grafiğin yerel noktası doğrudan konum; aynı vardiyadaki anlar
                // çizgide sola dizilir (son an noktanın üstünde)
                int later = 0;
                for (int k = i + 1; k < d.Moments.Count; k++) if (d.Moments[k].Shift == m.Shift) later++;
                markers[i].root.anchoredPosition = chart.Local(m.Shift, chart.ValueAt(m.Shift)) + new Vector2(-26 * later, 0);
                markers[i].number.text = m.Number.ToString();
                rows[i].number.text = m.Number.ToString();
                rows[i].code.text = m.Code;
                rows[i].text.text = m.Text;
                // Kaybın son anı kırmızı dolu yuvarlak
                markers[i].ring.color = rows[i].ring.color = m.Alert ? red : ink;
                markers[i].fill.color = rows[i].fill.color = m.Alert ? red : paper;
                markers[i].number.color = rows[i].number.color = m.Alert ? paper : ink;
            }
            actionLabel.text = d.Action;
        }

        /// <summary>x ekseni: 1, 5'in katları ve N (N'ye 2'den yakın kat atlanır); uzun harekâtta 10'un katları.</summary>
        void Axis(int shifts)
        {
            int step = shifts > 80 ? 10 : 5;
            int n = 0;
            void Put(int shift)
            {
                if (n >= xLabels.Length) return;
                var label = xLabels[n++];
                label.gameObject.SetActive(true);
                label.text = shift.ToString();
                // Etiketler grafiğin çocuğu (çapa ortada), y sahnede
                label.rectTransform.anchoredPosition = new Vector2(chart.Local(shift, 0).x, label.rectTransform.anchoredPosition.y);
            }
            Put(1);
            for (int s = step; s < shifts; s += step) if (shifts - s >= 2) Put(s);
            if (shifts > 1) Put(shifts);
            for (int i = n; i < xLabels.Length; i++) xLabels[i].gameObject.SetActive(false);
        }

        void Update()
        {
            if (!IsOpen) return;
            Animate();
            ActionButton();
        }

        /// <summary>Açılış dizisi; editörde ya da açıldıktan çok sonra son hâl.</summary>
        void Animate()
        {
            if (data == null) return;
            float t = Application.isPlaying ? Time.unscaledTime - openedAt : float.PositiveInfinity;
            var dc = T.ColorOf("report.dim");
            float dimA = Mathf.Clamp01((t - dimDelay) / dimSeconds);
            dim.color = new Color(dc.r, dc.g, dc.b, dc.a * dimA);

            float p = Mathf.Clamp01((t - dimDelay - dimSeconds) / paperSeconds), e = 1 - (1 - p) * (1 - p);
            sheet.anchoredPosition = sheetRest + new Vector2(0, -1100) * (1 - e);
            sheetGroup.alpha = p > 0 ? 1 : 0;

            float s = Mathf.Clamp01((t - StampAt) / stampSeconds);
            stamp.localScale = Vector3.one * (s <= 0 ? 0 : Mathf.Lerp(1.6f, 1, s));
            if (s > 0 && !stamped && Application.isPlaying) { stamped = true; Stamped?.Invoke(data.Won); }
            float r = Mathf.Clamp01((t - StampAt - reasonDelay) / stampSeconds);
            reason.localScale = Vector3.one * (r <= 0 ? 0 : Mathf.Lerp(1.6f, 1, r));

            for (int i = 0; i < indicators.Length; i++)
            {
                float since = t - StampAt - i * indicatorStagger;
                indicators[i].group.alpha = Mathf.Clamp01(since / indicatorStagger);
                indicators[i].value.maxVisibleCharacters = since <= 0 ? 0 : float.IsInfinity(since) ? 99999 : Mathf.FloorToInt(since / typeCharSeconds) + 1;
            }

            float drawn = Mathf.Clamp01((t - ChartAt) / chartSeconds);
            chart.Drawn = drawn;
            int hovered = Hovered();
            var highlight = T.ColorOf("slip.rowSelected");
            for (int i = 0; i < markers.Length && i < data.Moments.Count; i++)
            {
                float reach = ChartAt + chartSeconds * (data.Moments[i].Shift - 1) / Mathf.Max(1, data.Shifts - 1);
                float pop = Mathf.Clamp01((t - reach) / markerSeconds);
                markers[i].root.localScale = Vector3.one * pop * (hovered == i ? 1.2f : 1);
                rows[i].group.alpha = Mathf.Clamp01((t - ChartAt - i * rowStagger) / rowStagger);
                rows[i].highlight.color = hovered == i ? highlight : Color.clear;
            }
        }

        /// <summary>Üzerine gelinen kırılma anı (işaret ya da satır); yoksa −1.</summary>
        int Hovered()
        {
            for (int i = 0; i < markers.Length; i++)
                if (markers[i].hover != null && markers[i].hover.Over || rows[i].hover != null && rows[i].hover.Over) return i;
            return -1;
        }

        /// <summary>YENİDEN BAŞLA: üzerinde 2 px kalkar (gölge 5), basılı 2 px iner (gölge 1).</summary>
        void ActionButton()
        {
            bool pressed = actionHover.Pressed, over = actionHover.Over && !pressed;
            float lift = pressed ? -2 : over ? 2 : 0, drop = pressed ? 1 : over ? 5 : 3;
            actionFace.anchoredPosition = new Vector2(0, lift);
            actionShadow.rectTransform.anchoredPosition = new Vector2(0, lift - drop);
        }
    }
}
