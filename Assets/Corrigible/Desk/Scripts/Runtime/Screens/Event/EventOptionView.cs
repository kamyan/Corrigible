using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>
    /// Olay seçenek kartı (PROMPT §7.3, CSS .ocard 344×380, dizin kâğıdı + 39–41 px kırmızı çizgi): başlık (tuş fişi 22, CP 17, ikon),
    /// açıklama, BEDEL, KAZANÇ, risk kutusu, önizleme yuvası, imza düğmesi + tuş fişi. Gövde CSS'teki gibi 7 px aralıkla akar.
    /// <b>Seçili</b> (üstüne gelinen ya da 1/2/3) kart 7 px kalkar, 2 px mürekkep kenar, #FFFDF5 zemin, büyük gölge; yuva sarı
    /// ÖNİZLEME notuna, düğme mürekkep “Kararı imzala” + [Enter]'a döner. Diğerlerinde kesik yuva + kâğıt düğme + kendi tuşu.
    /// </summary>
    public sealed class EventOptionView : MonoBehaviour, IPointerEnterHandler
    {
        [SerializeField] RectTransform visual;
        [SerializeField] Image fill, frame, rule;
        [SerializeField] KeyChipView key;
        [SerializeField] TMP_Text title;
        [SerializeField] Image icon;
        [Tooltip("Gövde: sol-üst çapalı; kenar kalınlığına göre (1/2 px) kayar.")]
        [SerializeField] RectTransform content;
        [SerializeField] TMP_Text desc;
        [SerializeField] EventLineList costs, gains;
        [SerializeField] RectTransform risk;
        [SerializeField] TMP_Text riskText;
        [SerializeField] RectTransform previewBox;
        [SerializeField] TMP_Text previewCap, previewRight;
        [SerializeField] EventPreviewRow[] previewRows = Array.Empty<EventPreviewRow>();
        [SerializeField] RectTransform slot;
        [SerializeField] TMP_Text slotText;
        [SerializeField] RectTransform footer;
        [SerializeField] DeskButton sign;
        [SerializeField] KeyChipView signKey;
        [SerializeField] float width = 344, height = 380, lift = 7, gap = 7, descLineHeight = 17.6f, riskLineHeight = 15;

        EventOptionVM vm;
        bool selected;

        public int Index { get; set; }
        public event Action<int> Hovered, Signed;
        public bool Selected => selected;

        void Awake()
        {
            sign.onClick.AddListener(() => Signed?.Invoke(Index));
        }

        public void OnPointerEnter(PointerEventData e) => Hovered?.Invoke(Index);

        public void Bind(EventOptionVM o, bool isSelected)
        {
            vm = o;
            selected = isSelected;
            Render();
        }

        public void SetSelected(bool on)
        {
            if (selected == on || vm == null) return;
            selected = on;
            Render();
        }

        static string IconSprite(string id) => id switch
        {
            "power" => "ic_power_16", "lens" => "ic_lens_16", "hourglass" => "ic_hourglass_16", "antenna" => "ic_antenna_16", "target" => "ic_target_16",
            _ => "ic_circle_14",
        };

        void Render()
        {
            if (vm == null) return;
            var T = DeskLoc.Instance;
            float b = selected ? 2 : 1;
            float inner = width - 2 * b - 24;

            // Kâğıt: seçili kart kalkar, kenarı kalınlaşır, zemini açılır, gölgesi büyür
            DeskUi.SetY(visual, selected ? lift : 0);
            DeskUi.Tint(fill, selected ? "paper.hover" : "paper.index");
            DeskSprites.Apply(frame, selected ? "sh_frame_r2_2" : "sh_frame_r2_1");
            DeskUi.Tint(frame, selected ? "ink.ink" : "paper.actionCardEdge");
            if (fill.TryGetComponent<Shadow>(out var sh))
            {
                sh.effectDistance = selected ? new Vector2(5, -8) : new Vector2(3, -4);
                sh.effectColor = new Color(0, 0, 0, selected ? 0.55f : 0.5f);
            }
            Place(rule.rectTransform, b, b + 39, width - 2 * b, 2);

            // Başlık
            key.Set(vm.Key);
            Place((RectTransform)key.transform, b + 12, b + 9, 22, 22);
            DeskUi.SetText(title, T.T(vm.Title));
            Place(title.rectTransform, b + 43, b + 11.5f, inner - 43 - 16, 17);
            // Simgesiz kart (ör. kampanya yükseltmesi): simge gizli
            DeskUi.SetActive(icon, !string.IsNullOrEmpty(vm.Icon));
            if (!string.IsNullOrEmpty(vm.Icon))
            {
                DeskSprites.Apply(icon, IconSprite(vm.Icon));
                Place(icon.rectTransform, width - b - 12 - 16, b + 12, 16, 16);
            }

            // Gövde akışı
            content.anchoredPosition = new Vector2(b + 12, -(b + 49));
            float y = 0;
            DeskUi.SetText(desc, T.T(vm.Desc));
            desc.rectTransform.sizeDelta = new Vector2(inner, desc.rectTransform.sizeDelta.y);
            desc.ForceMeshUpdate();
            float dh = Mathf.Max(1, desc.textInfo.lineCount) * descLineHeight;
            Place(desc.rectTransform, 0, y, inner, dh);
            y += dh + gap;

            // Boş bölüm (oyunda bedelsiz ya da kazançsız seçenek) başlığıyla birlikte gizlenir
            DeskUi.SetActive(costs, vm.Costs.Count > 0);
            DeskUi.SetActive(gains, vm.Gains.Count > 0);
            if (vm.Costs.Count > 0) { Top(costs.transform, y); y += costs.Bind(Corrigible.Core.Loc.N("BEDEL"), vm.Costs, inner) + gap; }
            if (vm.Gains.Count > 0) { Top(gains.transform, y); y += gains.Bind(Corrigible.Core.Loc.N("KAZANÇ"), vm.Gains, inner) + gap; }

            bool hasRisk = !string.IsNullOrEmpty(vm.Risk);
            DeskUi.SetActive(risk, hasRisk);
            if (hasRisk)
            {
                DeskUi.SetText(riskText, T.T(vm.Risk));
                var rr = riskText.rectTransform;
                rr.sizeDelta = new Vector2(inner - 16 - 14 - 7, rr.sizeDelta.y);
                riskText.ForceMeshUpdate();
                float rh = Mathf.Max(28, riskText.textInfo.lineCount * riskLineHeight + 8 + 2);
                Place(risk, 0, y, inner, rh);
                y += rh + gap;
            }

            // Önizleme: etkisi hesaplanmış her kartta sarı not (üstüne gelmeden görünür); yoksa kesik yuva
            bool preview = vm.Preview != null;
            DeskUi.SetActive(previewBox, preview);
            DeskUi.SetActive(slot, !preview);
            if (preview)
            {
                DeskUi.SetText(previewCap, T.T(vm.Preview.Cap));
                DeskUi.SetText(previewRight, T.T(vm.Preview.Right));
                for (int i = 0; i < previewRows.Length; i++)
                {
                    bool on = i < vm.Preview.Rows.Count;
                    DeskUi.SetActive(previewRows[i], on);
                    if (on) previewRows[i].Bind(vm.Preview.Rows[i]);
                }
                Place(previewBox, 0, y, inner, 50);
            }
            else
            {
                DeskUi.SetText(slotText, T.T(string.IsNullOrEmpty(vm.PreviewSlot) ? Corrigible.Core.Loc.N("Üstüne gel: göstergelere etkisi") : vm.PreviewSlot));
                Place(slot, 0, y, inner, 50);
            }
            y += 50 + gap;

            // İmza: seçiliyken mürekkep + [Enter]; diğerlerinde kâğıt + kendi tuşu (CSS: alt kenara yaslı, taşarsa akışta)
            // Gövde kökü kartın (b + 49) px altında; iç alt kenar = yükseklik − b − 12 (iç boşluk)
            float fy = Mathf.Max(y, height - b - 12 - (b + 49) - 30);
            // Önce genişlik, sonra etiket: düğme etiketi kendi genişliğine göre ortalar (ilk bağlamada da)
            float kw = signKey.Measure(selected ? "Enter" : vm.Key);
            Place(footer, 0, fy, inner, 30);
            var srt = (RectTransform)sign.transform;
            srt.anchorMin = srt.anchorMax = new Vector2(0, 0.5f); srt.pivot = new Vector2(0, 0.5f);
            srt.anchoredPosition = new Vector2(0, 0);
            srt.sizeDelta = new Vector2(inner - 8 - kw, 30);
            sign.SetKind(selected ? DeskButtonVariant.Ink : DeskButtonVariant.Paper, false);
            sign.SetLabel(vm.Sign?.Label ?? Corrigible.Core.Loc.N("Kararı imzala"));
            var kr = (RectTransform)signKey.transform;
            kr.anchorMin = kr.anchorMax = new Vector2(0, 0.5f); kr.pivot = new Vector2(0, 0.5f);
            kr.anchoredPosition = new Vector2(inner - kw, 0);
        }

        /// <summary>Sol-üst çapalı yerleşim (y aşağı).</summary>
        static void Place(RectTransform r, float x, float y, float w, float h)
        {
            r.anchorMin = r.anchorMax = new Vector2(0, 1); r.pivot = new Vector2(0, 1);
            r.anchoredPosition = new Vector2(x, -y);
            r.sizeDelta = new Vector2(w, h);
        }

        static void Top(Transform t, float y)
        {
            var r = (RectTransform)t;
            r.anchorMin = r.anchorMax = new Vector2(0, 1); r.pivot = new Vector2(0, 1);
            r.anchoredPosition = new Vector2(0, -y);
        }

        /// <summary>Çeviri tablosu için.</summary>
        internal static readonly string[] Words = { Corrigible.Core.Loc.N("Olası:") };
    }
}
