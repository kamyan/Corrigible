using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>
    /// Kurtuluş haritasında bir bölgenin çizimi (K2 notu › katmanlar ve durumlar): tarama (bölge çokgeni, sınır
    /// çizgilerinin altında), seçim dolgusu, iç kontur, yağlı kalem / kesik aday halkası, raptiye, etiket (CP 700 28,
    /// hale) ve alt satır, sektör işaretleri, mavi ÖZGÜR damgası. Durum yalnız <see cref="Bind"/> ile gelir; damga,
    /// silinme ve halka çizimi kısa canlandırmalardır (K3 notu). Konumlar tasarım pikselidir (sol üst, y aşağı).
    /// </summary>
    public sealed class LiberationRegionMarks : MonoBehaviour
    {
        [SerializeField] string regionId;
        [Tooltip("Etiketin merkezi (harita pikseli).")]
        [SerializeField] Vector2 labelPoint;
        [Tooltip("Sade haritanın bölge dolgusu: tıklama ve üzerine gelme alanı.")]
        [SerializeField] RegionMeshGraphic hit;
        [SerializeField] RegionMeshGraphic hatchA, hatchB, tint;
        [SerializeField] UILineGraphic contour;
        [SerializeField] TMP_Text label, sub;
        [SerializeField] Image chip, greaseRing, candidateRing, pin;
        [SerializeField] RectTransform marksRow;
        [SerializeField] Image[] marks = Array.Empty<Image>();
        [SerializeField] RectTransform stamp;
        [SerializeField] Image stampImage;
        [SerializeField] TMP_Text stampWord, stampDay;
        [SerializeField] Sprite markDone, markReady, markLocked;
        [SerializeField] Texture hatch45, hatchMinus45, hatch45Wide;
        [SerializeField] Material haloMaterial, plainMaterial;

        MapRegionState state;
        bool hovered;
        // Canlandırma başlangıçları (gerçek zaman); −1 = yok
        float stampAt = -1, eraseAt = -1, ringAt = -1, ringSeconds = 0.3f;
        Image animatedRing;
        float hatchAlphaA, hatchAlphaB, eraseFrom;

        public string RegionId => regionId;
        public RegionMeshGraphic Hit => hit;
        public Vector2 LabelPoint => labelPoint;
        public MapRegionState State => state;
        /// <summary>Cephe raptiyesinin harita noktası (görev fişinin ipi buraya bağlanır).</summary>
        public Vector2 PinPoint => Design(pin.rectTransform);
        /// <summary>Aday halkasının sağ ucu (onay fişinin ipi).</summary>
        public Vector2 RingPoint => labelPoint + new Vector2(candidateRing.rectTransform.rect.width * 0.5f * candidateRing.rectTransform.localScale.x, 14);

        public void Configure(string id, Vector2 point, RegionMeshGraphic hitArea, RegionMeshGraphic hatchFirst, RegionMeshGraphic hatchSecond,
            RegionMeshGraphic selectionTint, UILineGraphic outline, TMP_Text labelText, TMP_Text subText, Image labelChip, Image grease,
            Image candidate, Image frontPin, RectTransform row, Image[] sectorMarks, RectTransform freeStamp, Image freeStampImage,
            TMP_Text freeWord, TMP_Text freeDay, Sprite done, Sprite ready, Sprite locked, Texture hatch, Texture hatchCross,
            Texture hatchWide, Material halo, Material plain)
        {
            regionId = id; labelPoint = point; hit = hitArea; hatchA = hatchFirst; hatchB = hatchSecond; tint = selectionTint;
            contour = outline; label = labelText; sub = subText; chip = labelChip; greaseRing = grease; candidateRing = candidate;
            pin = frontPin; marksRow = row; marks = sectorMarks; stamp = freeStamp; stampImage = freeStampImage; stampWord = freeWord;
            stampDay = freeDay; markDone = done; markReady = ready; markLocked = locked; hatch45 = hatch; hatchMinus45 = hatchCross;
            hatch45Wide = hatchWide; haloMaterial = halo; plainMaterial = plain;
        }

        public void SetHovered(bool on)
        {
            if (hovered == on) return;
            hovered = on;
            if (state != null) Bind(state);
        }

        public void Bind(MapRegionState s)
        {
            state = s;
            bool visible = s != null;
            gameObject.SetActive(visible);
            foreach (var g in new Graphic[] { hatchA, tint, contour }) if (g != null) g.gameObject.SetActive(visible);
            if (!visible) { hatchB.gameObject.SetActive(false); return; }
            var t = SiginakTheme.Active;

            // Tarama: işgal çapraz (45° α .50 + −45° α .32, 11 px) · cephe tek yön (%66,7 → 11 px, %33,3 → 22 px) ·
            // aday taramayı korur · özgürde silinmiş hayalet iz (α .07)
            var occupied = t.ColorOf("mapLiberation.occupiedHatch[0]");
            var cross = t.ColorOf("mapLiberation.occupiedHatch[1]");
            // Silinme sürerken tarama canlandırmanın elindedir
            if (eraseAt < 0)
            switch (s.Kind)
            {
                case MapRegionKind.Front:
                {
                    var c = t.ColorOf("mapLiberation.frontHatch");
                    bool wide = s.Occupation < 50;
                    hatchA.SetAppearance(c, wide ? hatch45Wide : hatch45, wide ? 32 : 16);
                    hatchAlphaA = c.a; hatchAlphaB = 0;
                    break;
                }
                case MapRegionKind.Free:
                {
                    var c = t.ColorOf("mapLiberation.freeGhost");
                    hatchA.SetAppearance(c, hatch45, 16);
                    hatchAlphaA = c.a; hatchAlphaB = 0;
                    break;
                }
                default:
                    hatchA.SetAppearance(occupied, hatch45, 16);
                    hatchB.SetAppearance(cross, hatchMinus45, 16);
                    hatchAlphaA = occupied.a; hatchAlphaB = cross.a;
                    break;
            }
            if (eraseAt < 0) ApplyHatchAlpha();

            // Seçim / üzerinde dolgusu
            var fill = s.Selected ? t.ColorOf("mapLiberation.selectFill") : hovered ? t.ColorOf("mapLiberation.hoverFill") : Color.clear;
            tint.gameObject.SetActive(fill.a > 0);
            tint.SetAppearance(fill);

            // İç kontur: cephe 5 px mürekkep, aday 2,5 px kesik grafit (12/8)
            contour.gameObject.SetActive(s.Kind == MapRegionKind.Front || s.Kind == MapRegionKind.Candidate);
            if (s.Kind == MapRegionKind.Front)
            {
                contour.color = t.ColorOf("mapLiberation.frontContour");
                contour.Thickness = t.Number("mapLiberation.frontContour.width", 5);
                contour.SetDash(null);
            }
            else if (s.Kind == MapRegionKind.Candidate)
            {
                contour.color = t.ColorOf("mapLiberation.candidateContour");
                contour.Thickness = t.Number("mapLiberation.candidateContour.width", 2.5f);
                contour.SetDash(new[] { t.Number("mapLiberation.candidateContour.dash[0]", 12), t.Number("mapLiberation.candidateContour.dash[1]", 8) });
            }

            // Etiket: CP 700 28 harita px, hale; seçili / üzerindeyken sarı çip (halesiz)
            string name = DeskText.Upper(s.Name);
            float width = label.GetPreferredValues(name).x;
            if (width > 330)
            {
                // 330 px'i aşan ad “·”den (yoksa ortadaki boşluktan) iki satıra bölünür
                int cut = name.IndexOf('·');
                if (cut < 0) cut = name.LastIndexOf(' ', name.Length / 2 + 4);
                if (cut > 0) name = name.Substring(0, cut).TrimEnd() + "\n" + name.Substring(cut).TrimStart('·', ' ');
                width = label.GetPreferredValues(name).x;
            }
            label.text = name;
            bool chipOn = s.Selected || hovered && s.Kind != MapRegionKind.Free;
            var material = chipOn ? plainMaterial : haloMaterial;
            if (material != null && label.fontSharedMaterial != material) label.fontSharedMaterial = material;
            chip.gameObject.SetActive(chipOn);
            chip.rectTransform.sizeDelta = new Vector2(width + 14, label.GetPreferredValues(name).y + 2);

            // Alt satır: işgal yüzdesi, aday notu; cephede sektör işaretleri + yüzde; özgürde ✓✓✓
            sub.gameObject.SetActive(s.Kind != MapRegionKind.Free);
            sub.text = s.Kind switch
            {
                MapRegionKind.Candidate => Corrigible.Core.Loc.F("{0} · ADAY", DeskFormat.Percent(s.Occupation)),
                MapRegionKind.Front => DeskFormat.Percent(s.Occupation, s.Captured > 0 && s.Captured < 3 ? 1 : 0),
                _ => DeskFormat.Percent(s.Occupation),
            };
            bool showMarks = s.Kind == MapRegionKind.Front || s.Kind == MapRegionKind.Free;
            for (int i = 0; i < marks.Length; i++)
            {
                marks[i].gameObject.SetActive(showMarks);
                marks[i].sprite = s.Kind == MapRegionKind.Free || i < s.Captured ? markDone : i == s.Captured ? markReady : markLocked;
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate(marksRow);

            // Halkalar ve raptiye: halka etiketi ve alt satırı sarar (360 × 150 sprite, oran korunur)
            float ringW = Mathf.Max(width, marksRow.rect.width) + 76;
            foreach (var ring in new[] { greaseRing, candidateRing })
            {
                ring.rectTransform.sizeDelta = new Vector2(ringW, ringW / 2.4f);
                ring.rectTransform.anchoredPosition = new Vector2(0, -14);
            }
            greaseRing.gameObject.SetActive(s.Kind == MapRegionKind.Front);
            candidateRing.gameObject.SetActive(s.Kind == MapRegionKind.Candidate);
            // Seçili aday: 4 px karşılığı (README: ölçek 1,3)
            candidateRing.rectTransform.localScale = Vector3.one * (s.Selected ? 1.3f : 1);
            pin.gameObject.SetActive(s.Kind == MapRegionKind.Front);
            pin.rectTransform.anchoredPosition = new Vector2(ringW * 0.5f - 8, -6);

            // Özgür damgası: “ÖZGÜR · V.011”, taze mürekkep α .97, bir vardiya sonra .86. Etiketin sol üstünde (ekip noktası
            // etiketin altında ya da tam üstünde; S-07 fişi Batı Avrupa satırının solunda)
            stamp.gameObject.SetActive(s.Kind == MapRegionKind.Free);
            stamp.anchoredPosition = new Vector2(-width * 0.5f - 20, 70);
            if (s.Kind == MapRegionKind.Free)
            {
                stampWord.text = Corrigible.Core.Loc.T("ÖZGÜR");
                stampDay.text = Corrigible.Core.Loc.F("V.{0:000}", s.FreeShift);
                var blue = t.ColorOf("mapLiberation.freeStamp");
                blue.a = s.FreshStamp ? 0.97f : 0.86f;
                stampImage.color = blue;
                // Bant yazısı beyaz kalır (sprites/README); vardiya kodu mavi
                stampDay.color = blue;
            }
            if (stampAt < 0 && eraseAt < 0 && ringAt < 0) ApplyStamp(1);
        }

        // ---------- Canlandırmalar (K3 notu): damga iner, tarama silinir, halka çizilir ----------

        /// <summary>Özgürleşme: <paramref name="delay"/> sonra damga 0,1 s'de iner; tarama 0,5 s'de silinir.</summary>
        public void AnimateFreed(float delay)
        {
            // Özgürleşen bölge son sektöre kadar cepheydi: silinme cephe taramasından (tek yön, %33,3 → 22 px) başlar
            var front = SiginakTheme.Active.ColorOf("mapLiberation.frontHatch");
            eraseFrom = front.a;
            hatchAlphaA = SiginakTheme.Active.ColorOf("mapLiberation.freeGhost").a;
            hatchAlphaB = 0;
            hatchA.SetAppearance(front, hatch45Wide, 32);
            hatchB.gameObject.SetActive(false);
            float now = Time.unscaledTime;
            stampAt = now + delay;
            eraseAt = now + delay;
            Update();
        }

        /// <summary>Kalem halkası çizilir (aday 0,3 s, cephe yağlı kalemi 0,4 s).</summary>
        public void AnimateRing(float delay, float seconds)
        {
            animatedRing = state != null && state.Kind == MapRegionKind.Front ? greaseRing : candidateRing;
            ringAt = Time.unscaledTime + delay;
            ringSeconds = Mathf.Max(0.01f, seconds);
            // Halka (ve yağlı kalemin ikinci izi) radyal dolumla çizilir
            foreach (var img in animatedRing.GetComponentsInChildren<Image>())
            {
                img.type = Image.Type.Filled;
                img.fillMethod = Image.FillMethod.Radial360;
                img.fillAmount = 0;
            }
        }

        void Update()
        {
            float now = Time.unscaledTime;
            var t = SiginakTheme.Active;
            if (stampAt >= 0)
            {
                float p = Mathf.Clamp01((now - stampAt) / Mathf.Max(0.01f, t.NumberOf("motion.stampS", 0.1f)));
                ApplyStamp(now < stampAt ? 0 : p);
                if (p >= 1) stampAt = -1;
            }
            if (eraseAt >= 0)
            {
                // Silinme: taramanın ilk hâlinden hayalet ize (bu sırada ikinci katman da söner)
                float p = Mathf.Clamp01((now - eraseAt) / 0.5f);
                SetAlpha(hatchA, Mathf.Min(1, Mathf.Lerp(eraseFrom, hatchAlphaA, p) * Gain));
                if (p >= 1)
                {
                    eraseAt = -1;
                    hatchA.SetAppearance(t.ColorOf("mapLiberation.freeGhost"), hatch45, 16);
                    ApplyHatchAlpha();
                }
            }
            if (ringAt >= 0 && animatedRing != null)
            {
                float p = Mathf.Clamp01((now - ringAt) / ringSeconds);
                foreach (var img in animatedRing.GetComponentsInChildren<Image>()) img.fillAmount = p;
                if (p >= 1)
                {
                    ringAt = -1;
                    foreach (var img in animatedRing.GetComponentsInChildren<Image>()) img.type = Image.Type.Simple;
                }
            }
        }

        /// <summary>Tarama alfası × kazanç: K2'de doku pikseli ≈ 0,63 ekran pikseli, ince çizgi süzülürken soluyor (token).</summary>
        static float Gain => SiginakTheme.Active.NumberOf("mapLiberation.hatchGain", 1);

        void ApplyHatchAlpha()
        {
            hatchB.gameObject.SetActive(hatchAlphaB > 0);
            SetAlpha(hatchA, Mathf.Min(1, hatchAlphaA * Gain));
            SetAlpha(hatchB, Mathf.Min(1, hatchAlphaB * Gain));
        }

        void ApplyStamp(float p)
        {
            if (stamp == null) return;
            stamp.localScale = Vector3.one * Mathf.Lerp(1.35f, 1, p);
            var g = stamp.GetComponent<CanvasGroup>();
            if (g != null) g.alpha = p;
        }

        static void SetAlpha(Graphic g, float a)
        {
            var c = g.color;
            if (Mathf.Approximately(c.a, a)) return;
            c.a = a;
            g.color = c;
        }

        Vector2 Design(RectTransform rt)
        {
            // Bölge kökünün altındaki yerel nokta → harita pikseli (kök etiket noktasında)
            var local = (Vector2)transform.InverseTransformPoint(rt.position);
            return labelPoint + new Vector2(local.x, -local.y);
        }
    }
}
