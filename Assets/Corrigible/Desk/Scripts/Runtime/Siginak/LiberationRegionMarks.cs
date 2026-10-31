using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>
    /// Kurtuluş haritasında bir bölgenin çizimi (K2 notu; Revizyon 2 · S2, map2.js): tarama (bölge çokgeni, sınır
    /// çizgilerinin altında), seçim dolgusu, iç kontur, etiket (CP 700, bölgeye göre 34–48 harita px, hale; dar yerde iki
    /// satır, denizdeki adda ince çizgi) ve altı: işgalde yüzde, adayda kesik ADAY damgası, cephede sektör şeridi + yüzde
    /// + saha işaretleri (bölge özelliği mürekkep, imza makinesi kırmızı) + raptiye, özgürde ✓✓✓ ve sabit yerde mavi ÖZGÜR
    /// damgası. Durum yalnız <see cref="Bind"/> ile gelir; damga, silinme, ADAY damgası ve raptiye kısa canlandırmalardır
    /// (K3 notu). Konumlar harita pikselidir (sol üst, y aşağı); kök etiket noktasındadır.
    /// </summary>
    public sealed class LiberationRegionMarks : MonoBehaviour
    {
        /// <summary>Cephe şeridinin yeri: adın altında (sol kenarından), sağında ya da solunda (map2.js → strip, stripL).</summary>
        public enum StripSide { Below, Right, Left }

        [SerializeField] string regionId;
        [Tooltip("Etiketin merkezi (harita pikseli; map2.js → LAB.at).")]
        [SerializeField] Vector2 labelPoint;
        [Tooltip("Ad yazı boyu (harita pikseli; map2.js → LAB.s).")]
        [SerializeField, Min(10)] float labelSize = 40;
        [Tooltip("Ad tek satırda bu genişliği aşarsa boşluktan ya da tireden iki satıra bölünür.")]
        [SerializeField, Min(50)] float maxLineWidth = 400;
        [SerializeField] StripSide stripSide;
        [Tooltip("Sağda: adın sağ kenarından; solda: sol kenarından (x) ve etiket noktasından (y) şeridin sol üstü. Altta kullanılmaz.")]
        [SerializeField] Vector2 stripOffset;
        [Tooltip("ÖZGÜR damgasının merkezi (harita pikseli; map2.js → STAMP).")]
        [SerializeField] Vector2 stampPoint;
        [Tooltip("Ad bölgenin dışındaysa ince çizginin ucu (harita pikseli; map2.js → LAB.lead); yoksa (0, 0).")]
        [SerializeField] Vector2 leadPoint;
        [Tooltip("Sade haritanın bölge dolgusu: tıklama ve üzerine gelme alanı.")]
        [SerializeField] RegionMeshGraphic hit;
        [SerializeField] RegionMeshGraphic hatchA, hatchB, tint;
        [SerializeField] UILineGraphic contour;
        [SerializeField] TMP_Text label, sub;
        [SerializeField] Image chip, pin;
        [Tooltip("Sektör işaretleri (cephede 38 px şerit, özgürde 36 px ✓✓✓).")]
        [SerializeField] Image[] marks = Array.Empty<Image>();
        [Tooltip("Cephe bloğunun saha işaretleri: kâğıt zemin, bölge özelliği (mürekkep) ve imza makinesi (kırmızı).")]
        [SerializeField] RectTransform fieldBox;
        [SerializeField] Image traitGlyph, signatureGlyph;
        [SerializeField] RectTransform candidateStamp;
        [SerializeField] Image candidateFrame;
        [SerializeField] TMP_Text candidateWord;
        [SerializeField] RectTransform reoccStamp;
        [SerializeField] Image reoccFrame;
        [SerializeField] TMP_Text reoccWord;
        [SerializeField] UILineGraphic lead;
        [SerializeField] Image leadDot;
        [SerializeField] RectTransform stamp;
        [SerializeField] Image stampImage;
        [SerializeField] TMP_Text stampWord, stampDay;
        [SerializeField] Sprite markDone, markReady, markLocked, markReoccupied;
        [SerializeField] Texture hatch45, hatchMinus45, hatch45Wide;
        [SerializeField] Material haloMaterial, plainMaterial;

        MapRegionState state;
        bool hovered;
        // Canlandırma başlangıçları (gerçek zaman); −1 = yok
        float stampAt = -1, eraseAt = -1, arriveAt = -1, arriveSeconds = 0.3f, reoccAt = -1, blinkAt = -1, retakenAt = -1;
        int retaken = -1;
        RectTransform arriving;
        float hatchAlphaA, hatchAlphaB, eraseFrom;
        float labelWidth, labelHalfHeight;
        readonly System.Collections.Generic.List<Vector2> leadPoints = new System.Collections.Generic.List<Vector2>(2);

        public string RegionId => regionId;
        public RegionMeshGraphic Hit => hit;
        public Vector2 LabelPoint => labelPoint;
        public MapRegionState State => state;
        /// <summary>Cephe raptiyesinin harita noktası (görev fişinin ipi buraya bağlanır).</summary>
        public Vector2 PinPoint => Design(pin.rectTransform);
        /// <summary>Adayın ADAY damgasının sağ ucu (onay fişinin ipi).</summary>
        public Vector2 RingPoint => Design(candidateStamp) + new Vector2(candidateStamp.rect.width * 0.5f, 0);

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

            // İç kontur: cephe 5 px mürekkep (S2: koyu kontur, yağlı kalem halkası yok), aday 2,5 px kesik grafit (12/8)
            contour.gameObject.SetActive(s.Kind == MapRegionKind.Front || s.Kind == MapRegionKind.Candidate);
            if (s.Kind == MapRegionKind.Front)
            {
                contour.color = t.ColorOf("mapLiberation.frontContour");
                contour.Thickness = t.Number("mapLiberation.frontContour.width", 5);
                contour.SetDash(null);
            }
            else if (s.Kind == MapRegionKind.Candidate)
            {
                contour.color = s.Selected ? t.ColorOf("ink") : t.ColorOf("mapLiberation.candidateContour");
                contour.Thickness = t.Number("mapLiberation.candidateContour.width", 2.5f);
                contour.SetDash(new[] { t.Number("mapLiberation.candidateContour.dash[0]", 12), t.Number("mapLiberation.candidateContour.dash[1]", 8) });
            }

            Label(s, t);
            Below(s, t);
            if (stampAt < 0 && eraseAt < 0) ApplyStamp(1);
            if (arriveAt < 0) ApplyArrival(candidateStamp, 1);
        }

        /// <summary>Ad: CP 700, hale; seçili / üzerindeyken sarı çip (halesiz). Dar yerde iki satır, denizdeki adda ince çizgi.</summary>
        void Label(MapRegionState s, SiginakTheme t)
        {
            label.fontSize = labelSize;
            label.characterSpacing = 4;
            string name = DeskText.Upper(s.Name);
            if (label.GetPreferredValues(name).x > maxLineWidth) name = TwoLines(name);
            label.text = name;
            var size = label.GetPreferredValues(name);
            labelWidth = size.x;
            labelHalfHeight = size.y * 0.5f;
            label.rectTransform.sizeDelta = size + new Vector2(4, 0);

            bool chipOn = s.Selected || hovered && s.Kind != MapRegionKind.Free;
            var material = chipOn ? plainMaterial : haloMaterial;
            if (material != null && label.fontSharedMaterial != material) label.fontSharedMaterial = material;
            label.color = s.Kind == MapRegionKind.Candidate && s.Selected ? t.ColorOf("ink") : t.ColorOf("map.labelInk");
            chip.gameObject.SetActive(chipOn);
            chip.rectTransform.anchoredPosition = Vector2.zero;
            chip.rectTransform.sizeDelta = size + new Vector2(14, 2);

            bool hasLead = lead != null && leadPoint != Vector2.zero;
            if (lead != null) lead.gameObject.SetActive(hasLead);
            if (leadDot != null) leadDot.gameObject.SetActive(hasLead);
            if (!hasLead) return;
            // map2.js: adın 70 px solundan noktaya 2,4 px grafit çizgi, uçta 5 px nokta
            leadPoints.Clear();
            leadPoints.Add(new Vector2(-70, 0));
            leadPoints.Add(leadPoint - labelPoint);
            lead.SetPointsTopLeft(leadPoints, Vector2.zero);
            leadDot.rectTransform.anchoredPosition = Local(leadPoint - labelPoint);
        }

        /// <summary>“GÜNEY AMERİKA” → iki satır; tireli adda tireden sonra (“JAPONYA-” / “KORE”).</summary>
        static string TwoLines(string name)
        {
            int mid = name.Length / 2, best = -1;
            for (int i = 0; i < name.Length; i++)
                if ((name[i] == ' ' || name[i] == '-') && (best < 0 || Mathf.Abs(i - mid) < Mathf.Abs(best - mid))) best = i;
            if (best <= 0) return name;
            return name[best] == '-' ? name.Substring(0, best + 1) + "\n" + name.Substring(best + 1).TrimStart()
                : name.Substring(0, best) + "\n" + name.Substring(best + 1);
        }

        /// <summary>Adın altı/yanı: yüzde, ADAY damgası, cephe şeridi + saha işaretleri + YENİDEN İŞGAL, özgür ✓✓✓ ve damga.</summary>
        void Below(MapRegionState s, SiginakTheme t)
        {
            bool front = s.Kind == MapRegionKind.Front, free = s.Kind == MapRegionKind.Free;
            float below = labelHalfHeight;

            // Sektör işaretleri: cephede 38 px (44 aralık), özgürde 36 px (42 aralık) ✓✓✓
            Vector2 strip = front ? StripOrigin(below) : new Vector2(-60, below + 8);
            float markSize = front ? 38 : 36, step = front ? 44 : 42;
            for (int i = 0; i < marks.Length; i++)
            {
                var m = marks[i];
                m.gameObject.SetActive(front || free);
                if (!front && !free) continue;
                m.sprite = free || i < s.Captured ? markDone : i == s.Reoccupied ? markReoccupied : i == s.Captured ? markReady : markLocked;
                var rt = m.rectTransform;
                rt.sizeDelta = Vector2.one * markSize;
                rt.anchoredPosition = Local(strip + new Vector2(i * step + markSize / 2, markSize / 2));
            }

            // Yüzde: işgal/aday adın altında (CP 700 28, ink3); cephede şeridin sağında (CP 700 32, mürekkep, hale)
            sub.gameObject.SetActive(!free);
            if (!free)
            {
                sub.text = front ? DeskFormat.Percent(s.Occupation, s.Captured > 0 && s.Captured < 3 ? 1 : 0) : DeskFormat.Percent(s.Occupation);
                sub.fontSize = front ? 32 : 28;
                sub.color = front ? t.ColorOf("ink") : t.ColorOf("ink3");
                sub.alignment = front ? TextAlignmentOptions.MidlineLeft : TextAlignmentOptions.Top;
                var size = sub.GetPreferredValues(sub.text);
                sub.rectTransform.sizeDelta = size;
                sub.rectTransform.pivot = front ? new Vector2(0, 0.5f) : new Vector2(0.5f, 1);
                sub.rectTransform.anchoredPosition = front ? Local(strip + new Vector2(3 * step + 10, markSize / 2 + 1)) : Local(new Vector2(0, below + 6));
            }

            // ADAY: kesik grafit damga (seçiliyse mürekkep), yüzdenin altında
            bool candidate = s.Kind == MapRegionKind.Candidate;
            candidateStamp.gameObject.SetActive(candidate);
            if (candidate)
            {
                var c = s.Selected ? t.ColorOf("ink") : t.ColorOf("ink2");
                candidateWord.text = Corrigible.Core.Loc.T("ADAY");
                candidateWord.color = candidateFrame.color = c;
                candidateStamp.sizeDelta = candidateWord.GetPreferredValues(candidateWord.text) + new Vector2(22, 12);
                candidateStamp.anchoredPosition = Local(new Vector2(0, below + 62));
            }

            // Saha işaretleri: şeridin altında, kâğıt zeminde bölge özelliği (mürekkep) + imza makinesi (kırmızı)
            string trait = front ? SiginakGlyphs.Trait(s.Trait, framed: true) : null;
            string signature = front ? SiginakGlyphs.Machine(s.Signature, framed: true) : null;
            bool fields = trait != null || signature != null;
            fieldBox.gameObject.SetActive(fields);
            traitGlyph.gameObject.SetActive(false);
            signatureGlyph.gameObject.SetActive(false);
            if (fields)
            {
                var origin = strip + new Vector2(19, 70);
                fieldBox.sizeDelta = new Vector2(trait != null && signature != null ? 96 : 50, 46);
                fieldBox.anchoredPosition = Local(origin + new Vector2(-25 + fieldBox.sizeDelta.x / 2, 0));
                SetGlyph(traitGlyph, trait, t.ColorOf("ink"), origin);
                SetGlyph(signatureGlyph, signature, t.ColorOf("status.threat"), origin + new Vector2(trait != null ? 46 : 0, 0));
            }

            // YENİDEN İŞGAL: karşı saldırının aldığı sektör geri alınana kadar, saha işaretlerinin sağ altında
            bool reocc = front && s.Reoccupied >= 0;
            reoccStamp.gameObject.SetActive(reocc);
            if (reocc)
            {
                reoccWord.text = Corrigible.Core.Loc.T("YENİDEN İŞGAL");
                reoccStamp.sizeDelta = reoccWord.GetPreferredValues(reoccWord.text) + new Vector2(24, 14);
                reoccStamp.anchoredPosition = Local(strip + new Vector2(150, 128));
            }

            // Raptiye: adın solunda (şerit soldaysa sağında), ilk satır hizasında
            pin.gameObject.SetActive(front);
            float side = stripSide == StripSide.Left ? 1 : -1;
            pin.rectTransform.anchoredPosition = Local(new Vector2(side * (labelWidth * 0.5f + 18), -12));

            // Özgür damgası: “ÖZGÜR · V.011”, sabit yerde (adların dışında); taze mürekkep α .97 ve 1,1 kat, sonra .86
            stamp.gameObject.SetActive(free);
            if (free)
            {
                stamp.anchoredPosition = Local(stampPoint - labelPoint);
                stamp.localRotation = Quaternion.Euler(0, 0, -((regionId.Length > 0 ? regionId[0] : 'a') % 7 - 3) * 0.04f * Mathf.Rad2Deg);
                stampWord.text = Corrigible.Core.Loc.T("ÖZGÜR");
                stampDay.text = Corrigible.Core.Loc.F("V.{0:000}", s.FreeShift);
                var blue = t.ColorOf("mapLiberation.freeStamp");
                blue.a = s.FreshStamp ? 0.97f : 0.86f;
                stampImage.color = blue;
                // Bant yazısı beyaz kalır (sprites/README); vardiya kodu mavi
                stampDay.color = blue;
            }
        }

        /// <summary>Cephe şeridinin sol üstü (harita pikseli, etiket noktasına göre).</summary>
        Vector2 StripOrigin(float below) => stripSide switch
        {
            StripSide.Right => new Vector2(labelWidth * 0.5f + stripOffset.x, stripOffset.y),
            StripSide.Left => new Vector2(-labelWidth * 0.5f + stripOffset.x, stripOffset.y),
            _ => new Vector2(-labelWidth * 0.5f, below + 10),
        };

        static void SetGlyph(Image img, string sprite, Color color, Vector2 center)
        {
            img.gameObject.SetActive(sprite != null);
            if (sprite == null) return;
            DeskSprites.Apply(img, sprite);
            img.color = color;
            img.rectTransform.sizeDelta = Vector2.one * 34;
            img.rectTransform.anchoredPosition = Local(center);
        }

        /// <summary>Etikete göre harita pikseli (y aşağı) → kökün yerel noktası (y yukarı).</summary>
        static Vector2 Local(Vector2 map) => new Vector2(map.x, -map.y);

        // ---------- Canlandırmalar (K3 notu): damga iner, tarama silinir, ADAY damgası ve raptiye düşer ----------

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

        /// <summary>Aday: ADAY damgası iner; cephe: raptiye düşer (<paramref name="seconds"/>).</summary>
        public void AnimateArrival(float delay, float seconds)
        {
            arriving = state != null && state.Kind == MapRegionKind.Front ? pin.rectTransform : candidateStamp;
            arriveAt = Time.unscaledTime + delay;
            arriveSeconds = Mathf.Max(0.01f, seconds);
            ApplyArrival(arriving, 0);
        }

        /// <summary>Karşı saldırı (S5b): <paramref name="delay"/> sonra sektöre kırmızı ✕ 0,2 s'de çizilir, YENİDEN İŞGAL damgası 0,12 s'de iner.</summary>
        public void AnimateCounterattack(float delay)
        {
            if (state == null || state.Reoccupied < 0 || state.Reoccupied >= marks.Length) return;
            reoccAt = Time.unscaledTime + delay;
            ApplyCounterattack(0, 0);
        }

        /// <summary>Operasyon raporu (S7): <paramref name="delay"/> sonra geri alınan sektörün işareti 0,2 s'de mavi onaya döner.</summary>
        public void AnimateRetaken(int sector, float delay)
        {
            if (state == null || sector < 0 || sector >= marks.Length || !marks[sector].gameObject.activeSelf) return;
            retaken = sector;
            retakenAt = Time.unscaledTime + delay;
            marks[sector].rectTransform.localScale = Vector3.zero;
        }

        /// <summary>Raporun karşı saldırı kutusunun üzerine gelince ✕ iki kez yanıp söner.</summary>
        public void BlinkReoccupied()
        {
            if (state == null || state.Reoccupied < 0 || state.Reoccupied >= marks.Length) return;
            blinkAt = Time.unscaledTime;
        }

        void ApplyCounterattack(float mark, float word)
        {
            marks[state.Reoccupied].rectTransform.localScale = Vector3.one * (1 - (1 - mark) * (1 - mark));
            reoccStamp.localScale = Vector3.one * (word <= 0 ? 0 : Mathf.Lerp(1.4f, 1, word));
        }

        void Update()
        {
            float now = Time.unscaledTime;
            var t = SiginakTheme.Active;
            if (reoccAt >= 0 && state != null && state.Reoccupied >= 0 && state.Reoccupied < marks.Length)
            {
                float mark = Mathf.Clamp01((now - reoccAt) / 0.2f), word = Mathf.Clamp01((now - reoccAt - 0.2f) / 0.12f);
                ApplyCounterattack(now < reoccAt ? 0 : mark, now < reoccAt + 0.2f ? 0 : word);
                if (word >= 1) reoccAt = -1;
            }
            if (retakenAt >= 0 && retaken >= 0 && retaken < marks.Length)
            {
                // Damga gibi: 0 → 1,3 → 1
                float p = Mathf.Clamp01((now - retakenAt) / 0.2f);
                marks[retaken].rectTransform.localScale = Vector3.one * (now < retakenAt ? 0 : p < 0.6f ? Mathf.Lerp(0, 1.3f, p / 0.6f) : Mathf.Lerp(1.3f, 1, (p - 0.6f) / 0.4f));
                if (p >= 1) { retakenAt = -1; retaken = -1; }
            }
            if (blinkAt >= 0 && state != null && state.Reoccupied >= 0 && state.Reoccupied < marks.Length)
            {
                // İki yanıp sönme: 0,15 s sönük, 0,15 s yanık
                float e = now - blinkAt;
                var c = marks[state.Reoccupied].color;
                c.a = e >= 0.6f || (int)(e / 0.15f) % 2 == 1 ? 1 : 0.15f;
                marks[state.Reoccupied].color = c;
                if (e >= 0.6f) blinkAt = -1;
            }
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
            if (arriveAt >= 0 && arriving != null)
            {
                float p = Mathf.Clamp01((now - arriveAt) / arriveSeconds);
                ApplyArrival(arriving, now < arriveAt ? 0 : p);
                if (p >= 1) arriveAt = -1;
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
            float fresh = state != null && state.FreshStamp ? 1.1f : 1;
            stamp.localScale = Vector3.one * Mathf.Lerp(1.35f, 1, p) * fresh;
            var g = stamp.GetComponent<CanvasGroup>();
            if (g != null) g.alpha = p;
        }

        /// <summary>Düşüş: 1,35 kattan yerine oturur ve belirir (damga dili).</summary>
        static void ApplyArrival(RectTransform rt, float p)
        {
            if (rt == null) return;
            rt.localScale = Vector3.one * Mathf.Lerp(1.35f, 1, p);
            var g = rt.GetComponent<CanvasGroup>();
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
