using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>
    /// Kâğıt etiket (PROMPT §A.6, §A.8; tokens_sade.desk.chip): 62 px; solda 36 px simge dairesi, sağında metin bloğu.
    /// Satırlar: etiket (CP 700 11, .08em) + güven işareti · değer (CP 700 24; raporda karbon “≈”) + küçük ek (“±6”) + değişim
    /// (ok 12 + CP 700 12) · isteğe bağlı eşik çubuğu. Şüphelide değerin çevresinde turuncu kalem halkası.
    /// Genişlik içerikten: TR metinle tasarımın ölçüsü, EN'de kutu çapa tarafından genişler.
    /// </summary>
    public sealed class HudChipView : MonoBehaviour
    {
        [SerializeField] SadePaper paper;
        [SerializeField] IconCircleView icon;
        [SerializeField] RectTransform body;
        [SerializeField] TMP_Text label;
        [SerializeField] ChipTrustMarkView trust;
        [SerializeField] CarbonText value;
        [SerializeField] TMP_Text small;
        [SerializeField] Image handRing;
        [SerializeField] Image deltaIcon;
        [SerializeField] TMP_Text deltaText;
        [Tooltip("Sağ köşedeki etiketler sağ-üst pivotludur: genişlik sola doğru büyür (prefab yerleşimi).")]
        [SerializeField] ThresholdBarView bar;

        [Header("Önizleme (edit mod; oyunda VM)")]
        [SerializeField] SadeChipVM sample = new SadeChipVM { Label = "FON", Icon = "fund", Value = 47.3f, Decimals = 1, Trust = "verified", Delta = -1.2f };

        const float PadLeft = 10, PadRight = 14, Border = 1, Circle = 36, Gap = 10, LabelRow = 14, ValueRow = 24, RowGap = 5;

        public SadeChipVM Data { get; private set; }
        /// <summary>Etiketin üstüne gelindi / çıkıldı (döküm fişi için).</summary>
        public event Action<HudChipView, bool> Hovered;
        public float Width => ((RectTransform)transform).sizeDelta.x;

        void OnEnable() { if (Data == null) Render(sample); }
        void OnValidate() => DeskUi.Deferred(this, () => { if (Data == null) Render(sample); });

        public void Bind(SadeChipVM vm)
        {
            Data = vm;
            if (vm != null) Render(vm);
        }

        public void NotifyHover(bool on) => Hovered?.Invoke(this, on);

        /// <summary>Önizleme verisini çizer (kurucu ve galeri; edit modda OnEnable çalışmaz).</summary>
        public void ShowSample() => Render(sample);

        void Render(SadeChipVM vm)
        {
            if (vm == null || body == null) return;
            var T = DeskLoc.Instance;
            var level = DeskUi.ParseTrust(vm.Trust);
            bool hasBar = vm.Bar != null && vm.Bar.Width > 0 && bar != null;   // serileştirilmiş boş çubuk genişliği 0

            if (icon != null) icon.Set(vm.Icon, vm.ThreatIcon);

            // Etiket satırı: etiket + 6 + güven işareti
            SadeUi.Type(label, "chipLabel");
            DeskUi.Tint(label, "sade.ink2");
            string lab = T.T(vm.Label);
            DeskUi.SetText(label, lab);
            float labelW = SadeUi.Width(label, lab);
            float labelRowW = labelW;
            if (trust != null)
            {
                trust.Set(level);
                bool mark = trust.Width > 0;
                DeskUi.SetActive(trust, mark);
                if (mark) { SadeUi.Place(trust, labelW + 6, 0, trust.Width, LabelRow); labelRowW += 6 + trust.Width; }
            }

            // Değer satırı: değer (+ küçük ek) + 7 + değişim
            bool carbon = level == TrustLevel.Report;
            string v = SadeUi.Value(vm.Value, vm.Decimals, vm.Percent, vm.Approx);
            value.Set(v, carbon, carbon ? "ink.carbon" : "ink.ink");
            var valueText = value.Main;
            SadeUi.Type(valueText, "chipValue");
            foreach (var t in value.GetComponentsInChildren<TMP_Text>(true)) SadeUi.Type(t, "chipValue");
            value.Apply();
            float valueW = SadeUi.Width(valueText, v);
            float x = valueW;
            bool hasSmall = vm.Uncertainty > 0 && small != null;
            DeskUi.SetActive(small, hasSmall);
            if (hasSmall)
            {
                SadeUi.Type(small, "chipValueSmall");
                DeskUi.Tint(small, carbon ? "ink.carbon" : "sade.ink");
                string s = "±" + DeskFormat.Number(vm.Uncertainty, 0);
                DeskUi.SetText(small, s);
                float sw = SadeUi.Width(small, s);
                // CSS small: margin-left −3, taban çizgisi değerle aynı (değer satırının 7 px altı)
                SadeUi.Place(small, x - 3, LabelRow + RowGap + 7, sw, 14);
                x += sw - 3;
            }
            const float vy = LabelRow + RowGap;
            SadeUi.Place(value, 0, vy, x, ValueRow);

            bool hasDelta = deltaText != null;
            float deltaW = 0;
            if (hasDelta)
            {
                string key = Mathf.Approximately(vm.Delta, 0) ? "sade.ink2" : vm.DeltaGood ? "sade.status.good" : "sade.status.threat";
                // Tasarımdaki kalın ok (2 px çizgi) paketteki ic_up_s'den kalın; C'nin 12 px kalın okları birebir tutuyor
                string arrow = Mathf.Approximately(vm.Delta, 0) ? "ic_flat_12b" : vm.Delta > 0 ? "ic_arrow_up_12b" : "ic_arrow_down_12b";
                SadeUi.Sprite(deltaIcon, arrow, key);
                SadeUi.Type(deltaText, "delta");
                DeskUi.Tint(deltaText, key);
                string d = DeskFormat.Number(Mathf.Abs(vm.Delta), 1);
                DeskUi.SetText(deltaText, d);
                float dw = SadeUi.Width(deltaText, d);
                SadeUi.Place(deltaIcon, x + 7, LabelRow + RowGap + 5, 12, 12);
                SadeUi.Place(deltaText, x + 7 + 12 + 1, LabelRow + RowGap + 5, dw, 12);
                deltaW = 7 + 12 + 1 + dw;
            }
            float valueRowW = x + deltaW;

            // Şüpheli: değerin çevresinde kalem halkası (değerin sol-üstünden −9, −7)
            bool ring = level == TrustLevel.Suspicious && handRing != null;
            DeskUi.SetActive(handRing, ring);
            if (ring)
            {
                SadeUi.Sprite(handRing, "hand_ring_value_76x37");
                DeskUi.SetColor(handRing, Color.white);
            }

            float bodyW = Mathf.Max(labelRowW, valueRowW);
            DeskUi.SetActive(bar, hasBar);
            if (hasBar)
            {
                bar.Set(vm.Bar);
                bodyW = Mathf.Max(bodyW, bar.Width);
            }

            // Dikey: blok 60 px iç yüksekliğe ortalanır (alt boşluk 2)
            float blockH = LabelRow + RowGap + ValueRow + (hasBar ? RowGap + 1 + 7 : 0);
            float top = Border + (60 - 2 - blockH) / 2;
            float bodyX = Border + PadLeft + Circle + Gap;
            SadeUi.Place(body, bodyX, top, bodyW, blockH);
            SadeUi.Place(label, 0, 1.5f, labelW, 11);
            if (trust != null) SadeUi.Place(trust, labelW + 6, 0, trust.Width, LabelRow);
            if (ring) SadeUi.Place(handRing, -9, LabelRow + RowGap - 7, valueW + 18, 37);
            if (hasBar) SadeUi.Place(bar, 0, LabelRow + RowGap + ValueRow + RowGap + 1, bar.Width, 7);

            float w = bodyX + bodyW + PadRight + Border;
            var rt = (RectTransform)transform;
            if (!Mathf.Approximately(rt.sizeDelta.x, w) || !Mathf.Approximately(rt.sizeDelta.y, 62)) rt.sizeDelta = new Vector2(w, 62);
            if (paper != null) paper.Apply();
            if (icon != null) SadeUi.Place(icon, Border + PadLeft, 13, Circle, Circle);
        }
    }
}
