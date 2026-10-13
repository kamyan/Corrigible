using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>
    /// Sayaç rafı hücresi (C1 §7.1): analog sayaç (kadran, tehlike bölgesi, eşik çentiği, belirsizlik kaması, ibre, göbek, “?”)
    /// + etiket (ad, güven işareti, değer + değişim, not). Güven türü kâğıdı ve çizim tarzını belirler:
    /// ONAYLANDI damga, RAPOR karbon kâğıt + “≈” + kama, ŞÜPHELİ kalem halkası + kadranda “?”.
    /// </summary>
    public sealed class GaugeCellView : MonoBehaviour, IIllusionAware
    {
        [Header("Sayaç")]
        [SerializeField] Image dial;
        [SerializeField] Image zone;
        [SerializeField] RectTransform threshold;
        [SerializeField] Image wedge;
        [SerializeField] RectTransform needle;
        [SerializeField] TMP_Text dialQuestion;

        [Header("Etiket")]
        [SerializeField] PaperPanel tag;
        [SerializeField] TMP_Text title;
        [SerializeField] StampView stamp;
        [SerializeField] TrustMarkView mark;
        [SerializeField] CarbonText value;
        [SerializeField] HandRingView ring;
        [SerializeField] DeltaView delta;
        [SerializeField] TMP_Text note;
        [SerializeField] DeskRow valueRow;

        [Tooltip("Kalem halkasının değerin sol-üstüne göre konumu (TSV: −15, −9).")]
        [SerializeField] Vector2 ringOffset = new Vector2(-15, -9);

        [SerializeField] DeskHover hover;

        float needleTarget, needleAngle, needleVel;
        bool frozen, bound;
        BreakdownVM breakdown;
        GaugeVM last;

        /// <summary>İllüzyon: kadran <c>meter_dial_illusion</c>, kama yok, ibre tamamen sabit (PROMPT §7.2).</summary>
        public void SetIllusion(bool on) { if (last != null) Bind(last); }

        void Awake()
        {
            if (hover == null) return;
            hover.Entered += _ => DeskTooltip.ShowBreakdown(this, breakdown);
            hover.Exited += _ => DeskTooltip.Hide(this);
        }

        public void Bind(GaugeVM g)
        {
            if (g == null) return;
            last = g;
            var trust = DeskUi.ParseTrust(g.Trust);
            bool report = trust == TrustLevel.Report;

            // ---- sayaç
            DeskSprites.Apply(dial, DeskTheme.Illusion ? "meter_dial_illusion" : "meter_dial");
            needleTarget = -g.NeedleDeg;
            frozen = DeskTheme.Illusion;
            if (!Application.isPlaying || frozen || !bound) SetNeedle(needleTarget);
            bound = true;
            breakdown = g.Breakdown;

            bool hasZone = g.DangerZone != null && g.DangerZone.Length == 2;
            DeskUi.SetActive(zone, hasZone);
            if (hasZone)
            {
                var (fill, z) = DeskMath.Zone(g.DangerZone[0], g.DangerZone[1]);
                zone.fillAmount = fill;
                zone.rectTransform.localEulerAngles = new Vector3(0, 0, z);
            }
            DeskUi.SetActive(threshold, g.Threshold.HasValue && g.Threshold.Value < 100);
            if (g.Threshold.HasValue) threshold.localEulerAngles = new Vector3(0, 0, DeskMath.NeedleZ(g.Threshold.Value));

            bool hasWedge = report && g.Uncertainty.HasValue && g.Uncertainty.Value > 0 && !DeskTheme.Illusion;
            DeskUi.SetActive(wedge, hasWedge);
            if (hasWedge)
            {
                var (fill, z) = DeskMath.Wedge(g.Value, g.Uncertainty.Value);
                wedge.fillAmount = fill;
                wedge.rectTransform.localEulerAngles = new Vector3(0, 0, z);
            }
            DeskUi.SetActive(dialQuestion, g.DialQuestionMark && !DeskTheme.Illusion);

            // ---- etiket
            tag.SetKind(report ? PaperKind.Carbon : PaperKind.Page);
            DeskUi.SetText(title, DeskLoc.Instance.T(g.Label));
            DeskUi.SetActive(stamp, trust == TrustLevel.Verified);
            DeskUi.SetActive(mark, trust != TrustLevel.Verified);
            if (trust == TrustLevel.Verified) stamp.Set(DeskLoc.Instance.T("ONAYLANDI"), StampInk.Blue, StampSize.Small, g.StampRot, "gauge." + g.Id);
            else mark.Set(trust);
            ((RectTransform)(trust == TrustLevel.Verified ? stamp.transform : mark.transform)).GetComponentInParent<DeskRow>()?.Layout();

            string shown = g.Display;
            if (!string.IsNullOrEmpty(g.Suffix)) shown += "<space=3px><size=13>" + g.Suffix + "</size>";
            value.Set(shown, report);

            if (g.Delta != null) delta.Set(DeltaView.ParseDir(g.Delta.Dir), DeskLoc.Instance.T(g.Delta.Text), DeskUi.ParseStatus(g.Delta.Status));
            DeskUi.SetText(note, DeskLoc.Instance.T(g.Note));
            valueRow.Layout();
            value.Apply();

            // Kalem halkası analistin işaretidir; İllüzyonda (dosyadaki gibi) görünmez
            bool ringOn = trust == TrustLevel.Suspicious && !DeskTheme.Illusion;
            DeskUi.SetActive(ring, ringOn);
            if (ringOn) PlaceRing();
        }

        /// <summary>Halka, değerin sol-üst köşesine göre (TSV −15, −9) durur; değer satırı yerleştikten sonra çağrılır.</summary>
        void PlaceRing()
        {
            var r = (RectTransform)ring.transform;
            var corners = new Vector3[4];
            value.Main.rectTransform.GetWorldCorners(corners);   // 1 = sol-üst
            var parent = (RectTransform)r.parent;
            Vector2 local = parent.InverseTransformPoint(corners[1]);
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
            r.pivot = new Vector2(0, 1);
            r.anchoredPosition = local - parent.rect.center + new Vector2(ringOffset.x, -ringOffset.y);
        }

        void SetNeedle(float z)
        {
            needleAngle = z; needleVel = 0;
            needle.localEulerAngles = new Vector3(0, 0, z);
        }

        void Update()
        {
            if (frozen || Mathf.Approximately(needleAngle, needleTarget)) return;
            var theme = DeskTheme.Active;
            needleAngle = Mathf.SmoothDamp(needleAngle, needleTarget, ref needleVel, theme != null ? theme.needleSmooth * 0.35f : 0.18f, Mathf.Infinity, Time.unscaledDeltaTime);
            if (Mathf.Abs(needleAngle - needleTarget) < 0.01f) needleAngle = needleTarget;
            needle.localEulerAngles = new Vector3(0, 0, needleAngle);
        }
    }
}
