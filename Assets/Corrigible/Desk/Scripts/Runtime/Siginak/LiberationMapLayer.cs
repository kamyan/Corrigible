using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Corrigible.Desk
{
    /// <summary>
    /// Kurtuluş haritasının çizim katmanı (K2/K3; Revizyon 2 · S2): bölge işaretleri, S-07 fişi, işaretler kutusu (sağ altta,
    /// başlıkta TEHDİT n/4), kırmızı ip ve masa lambası dairesi. Sade haritanın dünya canvas'ı içinde, tasarım dikdörtgeninde (1920 × 1080, sol üst) durur; tıklama ve
    /// üzerine gelme Sade bölge dolgularından gelir. Harita ↔ dünya dönüşümü ve bölge isabeti (figür bırakma) buradadır.
    /// Yalnız yeni görünümde etkindir.
    /// </summary>
    public sealed class LiberationMapLayer : MonoBehaviour
    {
        [Serializable] public struct Anchor { public string id; public Vector2 point; }

        [Tooltip("Tasarım dikdörtgeni (1920 × 1080, pivot sol üst); harita pikseli bunun yerel uzayıdır.")]
        [SerializeField] RectTransform frame;
        [SerializeField] LiberationRegionMarks[] regions = Array.Empty<LiberationRegionMarks>();
        [Tooltip("Ekip noktası (bölge başına; yoksa etiket + 110 px). Kaynağı Data/Siginak/team_anchors.json.")]
        [SerializeField] Anchor[] teamAnchors = Array.Empty<Anchor>();
        [Tooltip("Sığınak (S-07) fişinin yeri.")]
        [SerializeField] Vector2 shelterPoint;
        [SerializeField] UILineGraphic stringLine, stringShadow;
        [Tooltip("Basılı lejant (fiş yerleşimi onu da örtmemeye çalışır).")]
        [SerializeField] RectTransform legend;
        [Tooltip("İşaretler kutusunun sağ üstündeki “TEHDİT n/4”.")]
        [SerializeField] TMP_Text legendTier;

        readonly List<Vector2> curve = new List<Vector2>(3);
        string hovered;
        LiberationMapState state;

        public event Action<string> RegionClicked;
        public LiberationMapState State => state;
        public IReadOnlyList<LiberationRegionMarks> Regions => regions;
        public void Configure(RectTransform designFrame, LiberationRegionMarks[] marks, Anchor[] anchors, Vector2 shelter,
            UILineGraphic redString, UILineGraphic redStringShadow, RectTransform legendBox)
        {
            frame = designFrame; regions = marks; teamAnchors = anchors; shelterPoint = shelter;
            stringLine = redString; stringShadow = redStringShadow; legend = legendBox;
        }

        /// <summary>Lejantın harita dikdörtgeni (sol üst orijin).</summary>
        public Rect LegendRect
        {
            get
            {
                if (legend == null) return default;
                var corners = new Vector3[4];
                legend.GetWorldCorners(corners);
                Vector2 a = WorldToMap(corners[0]), b = WorldToMap(corners[2]);
                return Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
            }
        }

        void OnEnable()
        {
            foreach (var r in regions)
            {
                if (r == null || r.Hit == null) continue;
                r.Hit.Clicked += Clicked;
                r.Hit.Hovered += Hovered;
            }
        }

        void OnDisable()
        {
            foreach (var r in regions)
            {
                if (r == null || r.Hit == null) continue;
                r.Hit.Clicked -= Clicked;
                r.Hit.Hovered -= Hovered;
            }
            SetHovered(null);
        }

        /// <summary>Girdi kilidi (modal, baskın); bağlayıcı kurar.</summary>
        public Func<bool> Blocked;

        void Clicked(string id)
        {
            if (Blocked != null && Blocked()) return;
            RegionClicked?.Invoke(id);
        }

        void Hovered(string id, bool on)
        {
            if (on && Blocked != null && Blocked()) return;
            SetHovered(on ? id : hovered == id ? null : hovered);
        }

        public void SetHovered(string id)
        {
            if (hovered == id) return;
            hovered = id;
            foreach (var r in regions) if (r != null) r.SetHovered(r.RegionId == id);
        }

        public void Bind(LiberationMapState s)
        {
            state = s;
            foreach (var r in regions) if (r != null) r.Bind(s?.Find(r.RegionId));
            if (legendTier != null && s != null) legendTier.text = Corrigible.Core.Loc.F("TEHDİT {0}/4", s.Tier);
        }

        public LiberationRegionMarks Marks(string id)
        {
            foreach (var r in regions) if (r != null && r.RegionId == id) return r;
            return null;
        }

        // ---------- Harita ↔ dünya ----------

        public Vector3 MapToWorld(Vector2 p)
        {
            var r = frame.rect;
            return frame.TransformPoint(new Vector3(r.xMin + p.x, r.yMax - p.y, 0));
        }

        public Vector2 WorldToMap(Vector3 world)
        {
            var r = frame.rect;
            var local = frame.InverseTransformPoint(world);
            return new Vector2(local.x - r.xMin, r.yMax - local.y);
        }

        /// <summary>Haritanın dünyadaki dönüşü (fişler aynı düzlemde yatar).</summary>
        public Quaternion MapRotation => frame.rotation;

        /// <summary>Haritanın düzlemi (normal yukarı, kâğıdın görünen yüzü).</summary>
        public Plane MapPlane => new Plane(-frame.forward, frame.position);

        /// <summary>Haritadaki bir metre kaç harita pikseli (masada 1600 px/m).</summary>
        public float PixelsPerMeter => 1f / Mathf.Max(1e-6f, frame.lossyScale.x);

        /// <summary>Dünya noktasının altındaki bölge (deniz ve delikler hariç); yoksa null.</summary>
        public string RegionAt(Vector3 world)
        {
            var p = WorldToMap(world);
            foreach (var r in regions)
                if (r != null && r.Hit != null && r.gameObject.activeInHierarchy && r.Hit.ContainsMapPoint(p)) return r.RegionId;
            return null;
        }

        /// <summary>Ekip noktası: bölge verisi <c>teamAnchor</c>; yoksa etiket + (0, 110).</summary>
        public Vector2 TeamAnchor(string id)
        {
            foreach (var a in teamAnchors) if (a.id == id) return a.point;
            var m = Marks(id);
            return m != null ? m.LabelPoint + SiginakTheme.Active.Vector2Of("space.teamFigure.anchorOffsetMapPx") : shelterPoint;
        }

        /// <summary>
        /// Figürün durduğu nokta: bölgesinin ekip noktası. Yaralı ekibin bölgesi sığınak bölgesidir (Batı Avrupa, S-07);
        /// figür orada karada durur, ilk cephede cepheyle aynı yer.
        /// </summary>
        public Vector2 TeamPoint(TeamFigureState team) => TeamAnchor(team.Region);

        // ---------- Kırmızı ip (S2: fişin iğnesinden bölgenin iğnesine yay, 3 noktalı eğri) ----------

        /// <summary>
        /// İpi çizer. <paramref name="drawn"/> &lt; 1 ise yalnız bölge ucundan o kadarı (iplik bölgeden çekilir / sarılır).
        /// </summary>
        public void SetString(bool on, Vector2 from = default, Vector2 to = default, float drawn = 1)
        {
            on &= drawn > 0;
            stringLine.gameObject.SetActive(on);
            stringShadow.gameObject.SetActive(on);
            if (!on) return;
            var mid = (from + to) * 0.5f;
            // İp adların ve damgaların altından sarkar: kontrol noktası kiriş ortasının 30–40 px altında (haritada y artar)
            float sag = Mathf.Clamp(Vector2.Distance(from, to) * 0.05f, 30, 40);
            Vector2 p0 = to, p1 = mid + new Vector2(0, sag), p2 = from;
            if (drawn < 1)
            {
                // de Casteljau: bölge ucundan başlayan alt eğri
                var q1 = Vector2.Lerp(p0, p1, drawn);
                var q2 = Vector2.Lerp(q1, Vector2.Lerp(p1, p2, drawn), drawn);
                p1 = q1; p2 = q2;
            }
            curve.Clear();
            curve.Add(p2); curve.Add(p1); curve.Add(p0);
            stringLine.Curve = LineCurve.Quadratic;
            stringLine.SetPointsTopLeft(curve, Vector2.zero);
            var offset = SiginakTheme.Active.Vector2Of("space.stringShadowPx");
            if (offset == Vector2.zero) offset = new Vector2(2, 3);
            for (int i = 0; i < curve.Count; i++) curve[i] += offset;
            stringShadow.Curve = LineCurve.Quadratic;
            stringShadow.SetPointsTopLeft(curve, Vector2.zero);
        }

        // ---------- K3 dizisi ----------

        /// <summary>Özgürleşen bölge: damga ve silinme.</summary>
        public void PlayFreed(string id, float delay) => Marks(id)?.AnimateFreed(delay);

        /// <summary>Adaylar 0,12 s arayla ADAY damgasıyla işaretlenir (0,3 s).</summary>
        public float PlayCandidates(IList<string> ids, float delay)
        {
            var t = SiginakTheme.Active;
            float step = t.NumberOf("motion.candidateStepS", 0.12f), draw = t.NumberOf("motion.candidateDrawS", 0.3f);
            for (int i = 0; i < ids.Count; i++) Marks(ids[i])?.AnimateArrival(delay + i * step, draw);
            return delay + Mathf.Max(0, ids.Count - 1) * step + draw;
        }

        /// <summary>Yeni cephe ya da açılan fiş: bölgeye iğne düşer (motion.pinDropS, 0,12 s).</summary>
        /// <summary>Karşı saldırı: bölgenin yeniden işgal edilen sektörüne ✕ çizilir, YENİDEN İŞGAL damgası iner.</summary>
        public void PlayCounterattack(string id, float delay) => Marks(id)?.AnimateCounterattack(delay);

        /// <summary>Operasyon raporu (S7): geri alınan sektörün işareti maviye döner.</summary>
        public void PlayRetaken(string id, int sector, float delay) => Marks(id)?.AnimateRetaken(sector, delay);

        /// <summary>Raporun karşı saldırı kutusu: ✕ iki kez yanıp söner.</summary>
        public void BlinkCounterattack(string id) => Marks(id)?.BlinkReoccupied();

        public void PlayPinDrop(string id, float delay = 0) => Marks(id)?.AnimateArrival(delay, SiginakTheme.Active.NumberOf("motion.pinDropS", 0.12f));
    }
}
