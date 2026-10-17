using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>
    /// Sade harita düğümü: 64 px görsel, 44 px dairesel isabet alanı. Yalnız Algı Katmanı'ndan gelen RegionNodeVM'i çizer;
    /// kaplama hesabını üst görünüm yapar, düğümün yazısı ve simgeleri 1× kalır.
    /// </summary>
    public sealed class SadeRegionNodeView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler, ICanvasRaycastFilter
    {
        [SerializeField] RectTransform visual;
        [SerializeField] Image halo, quarantine, discShadow, disc, ring, unknownDisc, suspicious;
        [SerializeField] Image badgeVerified, badgeReport, badgeCore;
        [SerializeField] TMP_Text value, unknown, suspiciousQuestion;

        public RegionNodeVM Data { get; private set; }
        public string RegionId => Data?.Id;
        public event Action<string> Clicked;
        public event Action<SadeRegionNodeView, bool> Hovered;

        public void Bind(RegionNodeVM vm)
        {
            Data = vm;
            if (vm == null) return;
            bool missing = !vm.Value.HasValue || DeskUi.ParseTrust(vm.Trust) == TrustLevel.Unknown;
            var trust = DeskUi.ParseTrust(vm.Trust);

            DeskUi.SetActive(halo, vm.Selected);
            if (vm.Selected) SadeUi.Sprite(halo, "node_halo_64", "sade.highlighter", 0.8f);
            DeskUi.SetActive(quarantine, vm.Quarantine);
            if (vm.Quarantine) SadeUi.Sprite(quarantine, "node_ring_quar_64");
            DeskUi.SetActive(discShadow, !missing);
            DeskUi.SetActive(disc, !missing);
            DeskUi.SetActive(ring, !missing);
            DeskUi.SetActive(value, !missing);
            DeskUi.SetActive(unknownDisc, missing);
            DeskUi.SetActive(unknown, missing);
            if (missing)
            {
                SadeUi.Sprite(unknownDisc, "node_unknown_44");
                DeskUi.SetColor(unknownDisc, Color.white);
                SadeUi.Type(unknown, "nodeUnknown");
                DeskUi.Tint(unknown, "ink.graphite");
                DeskUi.SetText(unknown, "?");
            }
            else
            {
                SadeUi.Sprite(discShadow, "node_disc_44");
                SadeUi.Sprite(disc, "node_disc_44");
                SadeUi.Sprite(ring, "node_ring_44", "sade.ink");
                var theme = SadeTheme.Active;
                if (theme != null)
                {
                    DeskUi.SetColor(disc, theme.RampFill(vm.Value.Value));
                    DeskUi.SetColor(value, theme.RampText(vm.Value.Value));
                }
                SadeUi.Type(value, "nodeValue");
                DeskUi.SetText(value, DeskFormat.Int(vm.Value.Value));
            }

            bool suspect = !missing && trust == TrustLevel.Suspicious;
            DeskUi.SetActive(suspicious, suspect);
            DeskUi.SetActive(suspiciousQuestion, suspect);
            if (suspect)
            {
                SadeUi.Sprite(suspicious, "node_ring_sus_64");
                SadeUi.Type(suspiciousQuestion, "suspiciousQ");
                DeskUi.Tint(suspiciousQuestion, "sade.status.warning");
                DeskUi.SetText(suspiciousQuestion, "?");
            }
            DeskUi.SetActive(badgeVerified, !missing && trust == TrustLevel.Verified);
            DeskUi.SetActive(badgeReport, !missing && trust == TrustLevel.Report);
            DeskUi.SetActive(badgeCore, vm.Core);
            SadeUi.Sprite(badgeVerified, "node_badge_seal_20");
            SadeUi.Sprite(badgeReport, "node_badge_report_22x16");
            SadeUi.Sprite(badgeCore, "node_badge_core_26");
        }

        public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
        {
            var rt = (RectTransform)transform;
            return RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, screenPoint, eventCamera, out var local) &&
                (local - rt.rect.center).sqrMagnitude <= 22 * 22;
        }

        public void OnPointerEnter(PointerEventData e)
        {
            if (visual != null) visual.anchoredPosition = new Vector2(0, 1);
            Hovered?.Invoke(this, true);
        }

        public void OnPointerExit(PointerEventData e)
        {
            if (visual != null) visual.anchoredPosition = Vector2.zero;
            Hovered?.Invoke(this, false);
        }

        public void OnPointerClick(PointerEventData e)
        {
            if (e.button == PointerEventData.InputButton.Left && RegionId != null) Clicked?.Invoke(RegionId);
        }

        void OnDisable() { if (visual != null) visual.anchoredPosition = Vector2.zero; }
    }
}
