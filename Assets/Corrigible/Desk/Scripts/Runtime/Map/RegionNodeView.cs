using System;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>
    /// Bölge düğümü (C1 §7.1 madde 7; 56×56): parça sırası alttan üste — seçiliyse fosfor, karantina halkası, disk (sızma
    /// rampası) + mürekkep halka, ŞÜPHELİ kalem halkası, güven rozeti (mühür / “~” fiş), çekirdek damgası “Ç”, değer.
    /// BİLİNMİYOR: kesik daire + Kalam “?”; sayı yazılmaz.
    /// </summary>
    public sealed class RegionNodeView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [SerializeField] Image phosphor;
        [SerializeField] Image quarantine;
        [SerializeField] Image unknownFill;
        [SerializeField] Image disc;
        [SerializeField] Image ring;
        [SerializeField] Image suspicious;
        [SerializeField] Image badgeVerified;
        [SerializeField] Image badgeReport;
        [SerializeField] RectTransform core;
        [SerializeField] TMP_Text coreLetter;
        [SerializeField] TMP_Text value;
        [SerializeField] TMP_Text unknown;
        [Tooltip("Üstüne gelince 1 px kalkan görsel kök (isabet alanı sabit).")]
        [SerializeField] RectTransform visual;

        public string RegionId { get; private set; }
        public RegionNodeVM Data { get; private set; }
        public event Action<RegionNodeView> Clicked;
        public event Action<RegionNodeView, bool> Hovered;

        public void OnPointerEnter(PointerEventData e) { DeskUi.SetY(visual, 1); Hovered?.Invoke(this, true); }
        public void OnPointerExit(PointerEventData e) { DeskUi.SetY(visual, 0); Hovered?.Invoke(this, false); }
        public void OnPointerClick(PointerEventData e) => Clicked?.Invoke(this);

        public void Bind(RegionNodeVM n)
        {
            RegionId = n.Id;
            Data = n;
            var rt = (RectTransform)transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1); rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(n.Center[0], -n.Center[1]);

            var trust = DeskUi.ParseTrust(n.Trust);
            bool unknownState = trust == TrustLevel.Unknown || !n.Value.HasValue;
            bool illusion = DeskTheme.Illusion;

            DeskUi.SetActive(phosphor, n.Selected);
            DeskUi.SetActive(quarantine, n.Quarantine);
            if (n.Quarantine) DeskSprites.Apply(quarantine, illusion ? "node_ring_quarantine_clean" : "node_ring_quarantine");

            DeskUi.SetActive(unknownFill, unknownState);
            DeskUi.SetActive(disc, !unknownState);
            DeskSprites.Apply(ring, unknownState ? "node_ring_unknown" : "node_ring");
            DeskUi.Tint(ring, unknownState ? "ink.graphite" : "ink.ink");
            DeskUi.SetActive(unknown, unknownState);
            DeskUi.SetActive(value, !unknownState);
            if (!unknownState)
            {
                var step = DeskTheme.Active.Ramp(n.Value.Value);
                DeskUi.SetColor(disc, step.fill);
                DeskUi.SetText(value, DeskFormat.Int(n.Value.Value));
                DeskUi.SetColor(value, step.text);
            }

            bool sus = trust == TrustLevel.Suspicious && !illusion;
            DeskUi.SetActive(suspicious, sus);
            if (sus) DeskSprites.Apply(suspicious, n.RingVariant == "a" ? "node_ring_suspicious_a" : "node_ring_suspicious_b");

            DeskUi.SetActive(badgeVerified, trust == TrustLevel.Verified || illusion && !unknownState);
            DeskUi.SetActive(badgeReport, trust == TrustLevel.Report && !illusion);

            DeskUi.SetActive(core, n.Core);
            if (n.Core) DeskUi.SetText(coreLetter, DeskLoc.Instance.T("Ç"));
        }
    }
}
